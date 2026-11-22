import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Location } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { catchError, EMPTY, filter, Subject, switchMap, tap } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { AppointmentsApiService } from '../../../../api-services/appointments/appointments-api.service';
import { GetMyAppointmentByIdQueryDto } from '../../../../api-services/appointments/appointments-api.model';
import { BaseComponent } from '../../../../core/components/base-classes/base-component';
import { getErrorMessage } from '../../../../core/interceptors/error-logging-interceptor.service';
import { ToasterService } from '../../../../core/services/toaster.service';
import { DialogButton } from '../../../shared/models/dialog-config.model';
import { ClinicTimePipe } from '../../../shared/pipes/clinic-time.pipe';
import { DialogHelperService } from '../../../shared/services/dialog-helper.service';
import { isTooLateToCancel } from '../../../shared/utils/appointment-cancellation';
import {
  RescheduleAppointmentDialogComponent,
  RescheduleAppointmentDialogData
} from '../reschedule-appointment-dialog/reschedule-appointment-dialog.component';

/** Address of the list; the "Back" button never leaves it (see backToList). */
const LIST_URL = '/client/appointments';

/**
 * Details of one appointment of the logged-in patient (/client/appointments/:id, Patient only).
 *
 * The server returns 404 both for a missing appointment and for someone else's appointment,
 * so the page shows the same "not found" message in both cases.
 */
@Component({
  selector: 'app-my-appointment-details',
  standalone: false,
  templateUrl: './my-appointment-details.component.html',
  styleUrl: './my-appointment-details.component.scss',
  // the pipe is also needed in TypeScript (text of the confirm dialog), so it is provided here
  // and injected like a service; this instance lives only as long as this component
  providers: [ClinicTimePipe]
})
export class MyAppointmentDetailsComponent extends BaseComponent implements OnInit {
  private api = inject(AppointmentsApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private location = inject(Location);
  private destroyRef = inject(DestroyRef);
  private dialogHelper = inject(DialogHelperService);
  private toaster = inject(ToasterService);
  private translate = inject(TranslateService);
  private clinicTime = inject(ClinicTimePipe);
  private dialog = inject(MatDialog);

  appointment: GetMyAppointmentByIdQueryDto | null = null;
  /** true after a 404 (or an id in the URL that is not a number) */
  notFound = false;

  /** must match Clinic:CancellationMinHoursBefore in appsettings.json */
  readonly cancellationMinHours = environment.clinic.cancellationMinHoursBefore;
  /** true while POST .../cancel is running, so the button cannot be clicked twice */
  isCancelling = false;

  /** columns of the services table */
  readonly displayedColumns = ['name', 'price'];

  private id: number | null = null;
  /** every next() = "load the appointment this.id" (also used by "Try again") */
  private readonly load$ = new Subject<void>();

  ngOnInit(): void {
    // switchMap: when the id changes, the old request is cancelled
    this.load$
      .pipe(
        tap(() => {
          this.startLoading();
          this.notFound = false;
        }),
        switchMap(() =>
          this.api.getMyById(this.id!).pipe(
            catchError((err: HttpErrorResponse) => {
              this.appointment = null;
              if (err.status === 404) {
                this.notFound = true;
                this.stopLoading();
              } else {
                this.stopLoading(getErrorMessage(err));
              }
              return EMPTY;
            })
          )
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((appointment) => {
        this.appointment = appointment;
        this.stopLoading();
      });

    // paramMap (and not snapshot), so the page also reacts if only :id changes
    this.route.paramMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => {
        const raw = params.get('id') ?? '';
        // only digits, e.g. "abc" or "-1" is not sent to the server: it cannot exist anyway
        this.id = /^\d+$/.test(raw) && Number(raw) >= 1 ? Number(raw) : null;
        if (this.id === null) {
          this.appointment = null;
          this.notFound = true;
          return;
        }
        this.load();
      });
  }

  load(): void {
    this.load$.next();
  }

  /** The Cancel and Reschedule buttons are shown only for a Scheduled appointment. */
  get canCancel(): boolean {
    return this.appointment?.status === 'Scheduled';
  }

  /**
   * true when less than cancellationMinHours are left until the start: the button is disabled.
   * Only for display (the browser clock can be wrong): the server checks the rule again (409).
   */
  get isTooLateToCancel(): boolean {
    if (!this.appointment) {
      return false;
    }
    return isTooLateToCancel(this.appointment.startUtc, this.cancellationMinHours);
  }

  /** Confirm dialog -> POST .../cancel -> success message and reload, or the server's error. */
  onCancel(): void {
    const appointment = this.appointment;
    if (!appointment || this.isCancelling) {
      return;
    }

    this.dialogHelper
      .confirm('CLIENT.APPOINTMENTS.CANCEL_TITLE', 'CLIENT.APPOINTMENTS.CANCEL_MESSAGE', {
        dateTime: this.clinicTime.transform(appointment.startUtc, 'dateTime'),
        dentist: appointment.dentistFullName
      })
      .pipe(
        filter((result) => result?.button === DialogButton.YES),
        tap(() => (this.isCancelling = true)),
        switchMap(() =>
          this.api.cancel(appointment.id).pipe(
            // inside switchMap: an error ends only this request, not the whole chain
            catchError((err: HttpErrorResponse) => {
              this.isCancelling = false;
              this.toaster.error(getErrorMessage(err));
              return EMPTY;
            })
          )
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        this.isCancelling = false;
        this.toaster.success(this.translate.instant('CLIENT.APPOINTMENTS.CANCELLED_SUCCESS'));
        // the server is the source of truth: reload, so the chip shows "Cancelled"
        this.load();
      });
  }

  /**
   * Opens the dialog for choosing a new time. The dialog sends POST .../reschedule itself
   * (so it can stay open on a 409) and closes with true only after a success.
   */
  onReschedule(): void {
    const appointment = this.appointment;
    if (!appointment || this.isCancelling) {
      return;
    }

    // C#: like new RescheduleForm(data).ShowDialog() — data goes in, the result comes back on close
    this.dialog
      .open<RescheduleAppointmentDialogComponent, RescheduleAppointmentDialogData, boolean>(
        RescheduleAppointmentDialogComponent,
        {
          data: {
            appointmentId: appointment.id,
            startUtc: appointment.startUtc,
            endUtc: appointment.endUtc,
            dentistFullName: appointment.dentistFullName
          },
          width: '640px',
          maxWidth: '95vw',
          // focus the dialog itself, not the date input (no keyboard popping up on a phone)
          autoFocus: 'dialog'
        }
      )
      .afterClosed()
      .pipe(
        filter((moved) => moved === true),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        this.toaster.success(this.translate.instant('CLIENT.APPOINTMENTS.RESCHEDULE.SUCCESS'));
        // the server is the source of truth: reload, so the new time is shown
        this.load();
      });
  }

  /**
   * Back to the list WITH the filters it had: the list sends its URL in the router state
   * (state.listUrl). The browser keeps that state in history, so it also works after F5.
   * The value is used only if it is the list address; otherwise (e.g. the details page was
   * opened from a link or a bookmark) we go to the list without filters.
   */
  backToList(): void {
    const state = this.location.getState() as { listUrl?: unknown } | null;
    const listUrl = state?.listUrl;
    const isListUrl =
      typeof listUrl === 'string' && (listUrl === LIST_URL || listUrl.startsWith(`${LIST_URL}?`));

    this.router.navigateByUrl(isListUrl ? listUrl : LIST_URL);
  }
}
