import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormControl } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { catchError, EMPTY, filter, Subject, switchMap, tap } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { AppointmentsApiService } from '../../../../api-services/appointments/appointments-api.service';
import {
  DentistScheduleAppointmentDto,
  GetDentistScheduleQueryDto
} from '../../../../api-services/appointments/appointments-api.model';
import { BaseComponent } from '../../../../core/components/base-classes/base-component';
import { addDays, clinicToday, readDateOnly, toDateOnly } from '../../../../core/forms/date-only';
import { getErrorMessage } from '../../../../core/interceptors/error-logging-interceptor.service';
import { APP_LOCALE } from '../../../../core/locale/app-locale';
import { ToasterService } from '../../../../core/services/toaster.service';
import { DialogButton } from '../../../shared/models/dialog-config.model';
import { ClinicTimePipe } from '../../../shared/pipes/clinic-time.pipe';
import { DialogHelperService } from '../../../shared/services/dialog-helper.service';
import { isTooLateToCancel } from '../../../shared/utils/appointment-cancellation';

/**
 * "My schedule" (/client/schedule, Dentist only): one day of the logged-in dentist,
 * i.e. working hours and appointments (cancelled ones too, faded).
 *
 * The URL is the only source of truth for the day (?date=yyyy-MM-dd), like the public lists:
 *   URL → queryParamMap → selectedDate (invalid/missing → today in the clinic) → load
 *   button / datepicker → router.navigate (only the URL changes) → queryParamMap emits again → load
 * Today is removed from the URL (default value), so /client/schedule always shows today.
 */
@Component({
  selector: 'app-dentist-schedule',
  standalone: false,
  templateUrl: './dentist-schedule.component.html',
  styleUrl: './dentist-schedule.component.scss',
  // the pipe is also needed in TypeScript (text of the confirm dialog), so it is provided here
  providers: [ClinicTimePipe]
})
export class DentistScheduleComponent extends BaseComponent implements OnInit {
  private api = inject(AppointmentsApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  private dialogHelper = inject(DialogHelperService);
  private toaster = inject(ToasterService);
  private translate = inject(TranslateService);
  private clinicTime = inject(ClinicTimePipe);

  /** must match Clinic:TimeZoneId in appsettings.json */
  private readonly timeZoneId = environment.clinic.timeZoneId;
  /** must match Clinic:CancellationMinHoursBefore in appsettings.json */
  private readonly cancellationMinHours = environment.clinic.cancellationMinHoursBefore;

  /** "ponedjeljak, 23. 11. 2026." – formats a calendar day (local midnight), not a UTC time */
  private readonly dayFormatter = new Intl.DateTimeFormat(APP_LOCALE, {
    weekday: 'long',
    day: 'numeric',
    month: 'numeric',
    year: 'numeric'
  });

  /** the day that is shown (local midnight); always read from the URL */
  selectedDate: Date = clinicToday(this.timeZoneId);
  /** the datepicker; filled from the URL with emitEvent: false, so it does not navigate again */
  readonly dateControl = new FormControl<Date | null>(this.selectedDate);

  /** null = not loaded yet for this day (spinner) */
  schedule: GetDentistScheduleQueryDto | null = null;
  /** id of the appointment being cancelled; while not null, every "Cancel" button is disabled */
  cancellingId: number | null = null;

  /** every next() = "load the schedule for this.selectedDate" (also used by "Try again") */
  private readonly load$ = new Subject<void>();

  ngOnInit(): void {
    // 1) load: switchMap cancels the previous HTTP request when a new one starts,
    //    so a slow response for the previous day can never overwrite the newer day
    this.load$
      .pipe(
        tap(() => this.startLoading()),
        switchMap(() =>
          this.api.getDentistSchedule(toDateOnly(this.selectedDate)).pipe(
            // inside switchMap: an error ends only this request, not the whole chain
            catchError((err: HttpErrorResponse) => {
              this.schedule = null;
              this.stopLoading(getErrorMessage(err));
              return EMPTY;
            })
          )
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((schedule) => {
        this.schedule = schedule;
        this.stopLoading();
      });

    // 2) URL → day → datepicker → load (also runs once at the start, for the initial URL)
    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => {
        this.selectedDate = readDateOnly(params, 'date') ?? clinicToday(this.timeZoneId);
        this.dateControl.setValue(this.selectedDate, { emitEvent: false });
        // a different day: do not show the old day's appointments under the new title
        this.schedule = null;
        this.load();
      });

    // 3) datepicker → URL (null = the typed text is not a date, so nothing happens)
    this.dateControl.valueChanges
      .pipe(
        filter((date): date is Date => date !== null),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((date) => this.goToDate(date));
  }

  /** Load the schedule for the selected day (also used by "Try again" and after a cancel). */
  load(): void {
    this.load$.next();
  }

  /** Full date under the title, e.g. "ponedjeljak, 23. 11. 2026." */
  get dayTitle(): string {
    return this.dayFormatter.format(this.selectedDate);
  }

  /** true when the shown day is today in the clinic (the "Today" button is then disabled) */
  get isToday(): boolean {
    return toDateOnly(this.selectedDate) === toDateOnly(clinicToday(this.timeZoneId));
  }

  /** "08:00–12:00, 13:00–17:00" (seconds removed: "08:00:00" → "08:00") */
  get workingHoursText(): string {
    return (this.schedule?.workingHours ?? [])
      .map((interval) => `${interval.startTime.slice(0, 5)}–${interval.endTime.slice(0, 5)}`)
      .join(', ');
  }

  previousDay(): void {
    this.goToDate(addDays(this.selectedDate, -1));
  }

  nextDay(): void {
    this.goToDate(addDays(this.selectedDate, 1));
  }

  goToToday(): void {
    this.goToDate(clinicToday(this.timeZoneId));
  }

  /**
   * The "Cancel" button is shown only for a Scheduled appointment that starts in more than
   * cancellationMinHours. Only for display: the server checks the rule again (409).
   */
  canCancel(appointment: DentistScheduleAppointmentDto): boolean {
    return (
      appointment.status === 'Scheduled' &&
      !isTooLateToCancel(appointment.startUtc, this.cancellationMinHours)
    );
  }

  /** Confirm dialog -> POST .../cancel -> success message and reload of the same day, or the server's error. */
  onCancel(appointment: DentistScheduleAppointmentDto): void {
    if (this.cancellingId !== null) {
      return;
    }

    this.dialogHelper
      .confirm('CLIENT.SCHEDULE.CANCEL_TITLE', 'CLIENT.SCHEDULE.CANCEL_MESSAGE', {
        patient: appointment.patientFullName,
        dateTime: this.clinicTime.transform(appointment.startUtc, 'dateTime')
      })
      .pipe(
        filter((result) => result?.button === DialogButton.YES),
        tap(() => (this.cancellingId = appointment.id)),
        switchMap(() =>
          this.api.cancel(appointment.id).pipe(
            catchError((err: HttpErrorResponse) => {
              this.cancellingId = null;
              this.toaster.error(getErrorMessage(err));
              return EMPTY;
            })
          )
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        this.cancellingId = null;
        this.toaster.success(this.translate.instant('CLIENT.SCHEDULE.CANCELLED_SUCCESS'));
        // the server is the source of truth: reload, so the chip shows "Cancelled"
        this.load();
      });
  }

  /**
   * Only changes the URL; the day is loaded in step 2 of ngOnInit.
   * Today is the default, so it is removed from the URL (date: null).
   */
  private goToDate(date: Date): void {
    const value = toDateOnly(date);
    const today = toDateOnly(clinicToday(this.timeZoneId));

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { date: value === today ? null : value },
      queryParamsHandling: 'merge'
    });
  }
}
