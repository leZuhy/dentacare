import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormControl, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { catchError, EMPTY, map, Subject, switchMap } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { AppointmentsApiService } from '../../../../api-services/appointments/appointments-api.service';
import { ListRescheduleFreeSlotsQueryDto } from '../../../../api-services/appointments/appointments-api.model';
import { addDays, clinicToday, toDateOnly } from '../../../../core/forms/date-only';
import { getErrorMessage } from '../../../../core/interceptors/error-logging-interceptor.service';
import { BOOKING_CONSTRAINTS } from '../../../public/booking/booking-constraint';
import { groupSlotsByDay, SlotDay } from '../../../shared/utils/slot-days';

/**
 * What the details page gives to the dialog (MatDialog "data").
 * Only what the dialog shows or needs for the requests; the server takes the dentist
 * and the duration from the appointment itself.
 */
export interface RescheduleAppointmentDialogData {
  appointmentId: number;
  /** current (old) time, UTC strings from GET api/appointments/my/{id} */
  startUtc: string;
  endUtc: string;
  dentistFullName: string;
}

/** First and last day of the slots that are shown (local midnight, both included). */
interface ShownRange {
  from: Date;
  to: Date;
}

/**
 * Dialog for moving an appointment to another time (#58, Patient only).
 *
 * The dialog sends POST .../reschedule ITSELF, so on a 409 it can stay open, show the
 * server's message and load fresh slots. It is closed with true only after a success;
 * "Cancel", Esc or a click outside close it with undefined.
 */
@Component({
  selector: 'app-reschedule-appointment-dialog',
  standalone: false,
  templateUrl: './reschedule-appointment-dialog.component.html',
  styleUrl: './reschedule-appointment-dialog.component.scss'
})
export class RescheduleAppointmentDialogComponent implements OnInit {
  private api = inject(AppointmentsApiService);
  private destroyRef = inject(DestroyRef);
  /** handle of this open dialog: close(result), disableClose (C#: like a Form's DialogResult) */
  private dialogRef = inject<MatDialogRef<RescheduleAppointmentDialogComponent, boolean>>(MatDialogRef);
  /** the object passed as "data" to dialog.open(...) on the details page */
  readonly data = inject<RescheduleAppointmentDialogData>(MAT_DIALOG_DATA);

  // === date: the same limits as the booking stepper ===
  private readonly timeZoneId = environment.clinic.timeZoneId;
  readonly maxBookingDaysAhead = environment.clinic.maxBookingDaysAhead;
  readonly daysShown = Math.min(BOOKING_CONSTRAINTS.daysShown, BOOKING_CONSTRAINTS.maxRangeDays);
  /** today in the clinic; the datepicker does not offer earlier days */
  readonly minDate = clinicToday(this.timeZoneId);
  /** last day that can be booked (Clinic:MaxBookingDaysAhead on the backend) */
  readonly maxDate = addDays(this.minDate, this.maxBookingDaysAhead);

  /** first day shown; starts with today */
  readonly dateControl = new FormControl<Date | null>(this.minDate, Validators.required);

  // === slots ===
  days: SlotDay<ListRescheduleFreeSlotsQueryDto>[] = [];
  /** days of the last successful load (for the "no free slots from ... to ..." message) */
  shownRange: ShownRange | null = null;
  isLoadingSlots = false;
  /** error while loading slots (e.g. 409 "less than 24 h") */
  slotsError: string | null = null;
  /** every next() = "load the slots for the current date" */
  private readonly load$ = new Subject<void>();

  // === choice and confirmation ===
  /** the chosen slot, exactly the server's string (sent back unchanged) */
  selectedStartUtc: string | null = null;
  /** true while POST .../reschedule is running: the button cannot be clicked twice */
  isSubmitting = false;
  /** error of the last confirmation (e.g. 409 "slot taken"); stays until a new slot is chosen */
  submitError: string | null = null;

  ngOnInit(): void {
    // switchMap: a new date cancels the previous (still running) request
    this.load$
      .pipe(
        switchMap(() => {
          this.days = [];
          this.shownRange = null;
          this.slotsError = null;
          this.selectedStartUtc = null;

          const range = this.rangeForCurrentDate();
          if (range === null) {
            this.isLoadingSlots = false;
            return EMPTY;
          }

          this.isLoadingSlots = true;
          return this.api
            .getRescheduleFreeSlots(this.data.appointmentId, {
              fromDate: toDateOnly(range.from),
              toDate: toDateOnly(range.to),
            })
            .pipe(
              map((slots) => ({ range, days: groupSlotsByDay(range.from, range.to, slots, this.timeZoneId) })),
              // inside switchMap: an error ends only this request, the dialog keeps working
              catchError((err: HttpErrorResponse) => {
                this.isLoadingSlots = false;
                this.slotsError = getErrorMessage(err);
                return EMPTY;
              })
            );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((result) => {
        this.days = result.days;
        this.shownRange = result.range;
        this.isLoadingSlots = false;
      });

    // a new date -> new slots
    this.dateControl.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadSlots());

    this.loadSlots();
  }

  loadSlots(): void {
    this.load$.next();
  }

  /**
   * From the chosen date, daysShown days, but never after maxDate.
   * null when the date is empty or outside [minDate, maxDate] (the datepicker marks it invalid).
   */
  private rangeForCurrentDate(): ShownRange | null {
    const from = this.dateControl.value;
    if (from === null || this.dateControl.invalid) {
      return null;
    }

    let to = addDays(from, this.daysShown - 1);
    if (to > this.maxDate) {
      to = this.maxDate;
    }
    return { from, to };
  }

  /** true when at least one shown day has a free slot */
  get hasAnySlot(): boolean {
    return this.days.some((day) => day.slots.length > 0);
  }

  /** Click on a slot. A new choice also hides the error of the previous confirmation. */
  selectSlot(slot: ListRescheduleFreeSlotsQueryDto): void {
    if (this.isSubmitting) {
      return;
    }
    this.selectedStartUtc = slot.startUtc;
    this.submitError = null;
  }

  /**
   * "Confirm": POST .../reschedule.
   * 204 -> close(true), the details page shows the message and reloads.
   * 409 -> server message in the dialog, the dialog stays open, fresh slots
   *        (someone may have taken the slot in the meantime).
   * other errors -> server message in the dialog. 401/403 are handled by the interceptors.
   */
  confirm(): void {
    const startUtc = this.selectedStartUtc;
    if (startUtc === null || this.isSubmitting) {
      return;
    }

    this.isSubmitting = true;
    this.submitError = null;
    // Esc and a click outside must not close the dialog while the request is running
    this.dialogRef.disableClose = true;

    this.api
      .reschedule(this.data.appointmentId, { startUtc })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.dialogRef.close(true);
        },
        error: (err: HttpErrorResponse) => {
          this.isSubmitting = false;
          this.dialogRef.disableClose = false;
          if (err.status === 401 || err.status === 403) {
            return;
          }

          this.submitError = getErrorMessage(err);
          if (err.status === 409) {
            this.loadSlots();
          }
        },
      });
  }
}
