import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormArray, FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { catchError, EMPTY, Subject, switchMap, tap } from 'rxjs';
import { AppointmentsApiService } from '../../../../api-services/appointments/appointments-api.service';
import { CompleteAppointmentCommand } from '../../../../api-services/appointments/appointments-api.model';
import { PublicCatalogApiService } from '../../../../api-services/public-catalog/public-catalog-api.service';
import {
  ListPublicServicesQueryDto,
  ListPublicServicesRequest
} from '../../../../api-services/public-catalog/public-catalog-api.model';
import { BaseFormComponent } from '../../../../core/components/base-classes/base-form-component';
import { applyServerErrors } from '../../../../core/forms/server-errors';
import { AppValidators } from '../../../../core/forms/validators';
import { getErrorMessage } from '../../../../core/interceptors/error-logging-interceptor.service';
import { allItemsPaging } from '../../../../core/models/paging/paging-utils';
import { MEDICAL_RECORD_CONSTRAINTS } from '../medical-record-constraints';

/**
 * What the schedule page gives to the dialog (MatDialog "data").
 * Only what the dialog shows; the server takes everything else from the appointment itself.
 */
export interface CompleteAppointmentDialogData {
  appointmentId: number;
  /** UTC strings from GET api/appointments/schedule */
  startUtc: string;
  endUtc: string;
  patientFullName: string;
  /** the service(s) already on the appointment (the one the patient booked) */
  dentalServiceNames: string[];
  /** sum of the copied prices already on the appointment */
  totalPriceBam: number;
}

/**
 * Typed form. C#: like a class with one property per field.
 * The FormArray is named exactly like the backend property, so a server error
 * "additionalDentalServiceIds[1]" lands on row 1 (see applyServerErrors).
 */
interface CompleteAppointmentForm {
  findings: FormControl<string>;
  therapy: FormControl<string>;
  /** one row = one performed procedure; null = service not chosen yet */
  additionalDentalServiceIds: FormArray<FormControl<number | null>>;
}

/**
 * Dialog for completing a visit (#17, Dentist only): findings, therapy and extra performed services.
 *
 * The dialog sends POST .../complete ITSELF, so on an error it can stay open and show
 * the server's message (400 under the fields, 409 at the top). It is closed with true only
 * after a success; "Cancel", Esc or a click outside close it with undefined.
 *
 * BaseFormComponent gives onSubmit() (marks all fields as touched and stops if the form is invalid).
 * There is no edit mode here: loadData() loads the list of active services for the dropdowns.
 */
@Component({
  selector: 'app-complete-appointment-dialog',
  standalone: false,
  templateUrl: './complete-appointment-dialog.component.html',
  styleUrl: './complete-appointment-dialog.component.scss'
})
export class CompleteAppointmentDialogComponent
  extends BaseFormComponent<never, FormGroup<CompleteAppointmentForm>>
  implements OnInit
{
  private api = inject(AppointmentsApiService);
  private catalogApi = inject(PublicCatalogApiService);
  private destroyRef = inject(DestroyRef);
  /** handle of this open dialog: close(result), disableClose (C#: like a Form's DialogResult) */
  private dialogRef = inject<MatDialogRef<CompleteAppointmentDialogComponent, boolean>>(MatDialogRef);
  /** the object passed as "data" to dialog.open(...) on the schedule page */
  readonly data = inject<CompleteAppointmentDialogData>(MAT_DIALOG_DATA);

  readonly limits = MEDICAL_RECORD_CONSTRAINTS;

  // === active services for the dropdowns ===
  services: ListPublicServicesQueryDto[] = [];
  isLoadingServices = false;
  servicesError: string | null = null;

  // === sending ===
  /** true while POST .../complete is running: the button cannot be clicked twice */
  isSaving = false;
  /** every next(command) = "send this command"; see ngOnInit */
  private readonly submit$ = new Subject<CompleteAppointmentCommand>();

  ngOnInit(): void {
    this.form = new FormGroup<CompleteAppointmentForm>({
      findings: new FormControl('', {
        nonNullable: true,
        validators: [
          Validators.required,
          AppValidators.notBlank,
          Validators.maxLength(this.limits.findingsMaxLength)
        ]
      }),
      therapy: new FormControl('', {
        nonNullable: true,
        validators: [Validators.maxLength(this.limits.therapyMaxLength)]
      }),
      additionalDentalServiceIds: new FormArray<FormControl<number | null>>([])
    });

    // Sending: the same pattern as cancelling on the schedule page.
    // catchError is INSIDE switchMap, so an error ends only this request
    // and the dialog can send again (an error outside would end the whole chain).
    this.submit$
      .pipe(
        tap(() => {
          this.isSaving = true;
          this.errorMessage = null;
          // Esc and a click outside must not close the dialog while the request is running
          this.dialogRef.disableClose = true;
        }),
        switchMap((command) =>
          this.api.complete(this.data.appointmentId, command).pipe(
            catchError((err: HttpErrorResponse) => {
              this.isSaving = false;
              this.dialogRef.disableClose = false;
              this.handleError(err);
              return EMPTY;
            })
          )
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        // 204: the schedule page shows the message and reloads the day
        this.dialogRef.close(true);
      });

    this.loadData();
  }

  /** Rows of extra services (C#: the list behind the FormArray). */
  get serviceRows(): FormControl<number | null>[] {
    return this.form.controls.additionalDentalServiceIds.controls;
  }

  get canAddService(): boolean {
    return this.serviceRows.length < this.limits.additionalServicesMax;
  }

  /** New empty row; the dentist then chooses the service in it. */
  addService(): void {
    if (!this.canAddService) {
      return;
    }
    this.form.controls.additionalDentalServiceIds.push(
      new FormControl<number | null>(null, Validators.required)
    );
  }

  removeService(index: number): void {
    this.form.controls.additionalDentalServiceIds.removeAt(index);
  }

  /** Current price of the chosen service (null if nothing is chosen or it is no longer active). */
  priceOf(serviceId: number | null): number | null {
    return this.services.find((s) => s.id === serviceId)?.priceBam ?? null;
  }

  /**
   * Booked services + current prices of the chosen extra services.
   * Only for display: the server copies the prices and computes the invoice itself.
   */
  get totalBam(): number {
    return this.serviceRows.reduce(
      (sum, row) => sum + (this.priceOf(row.value) ?? 0),
      this.data.totalPriceBam
    );
  }

  /**
   * Here "data" = the list of ACTIVE services (GET api/public/services returns only active ones),
   * not a model to edit. Also called after a 409, because a service may have been deactivated.
   */
  protected loadData(): void {
    this.isLoadingServices = true;
    this.servicesError = null;

    const request = new ListPublicServicesRequest();
    request.paging = allItemsPaging;

    this.catalogApi
      .listServices(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.services = result.items;
          this.isLoadingServices = false;
          this.markUnavailableServices();
        },
        error: (err: HttpErrorResponse) => {
          this.isLoadingServices = false;
          this.servicesError = getErrorMessage(err);
        }
      });
  }

  /** Called by onSubmit() only when the form is valid. */
  protected save(): void {
    if (this.isSaving) {
      return;
    }

    const value = this.form.getRawValue();
    const therapy = value.therapy.trim();

    this.submit$.next({
      findings: value.findings.trim(),
      therapy: therapy === '' ? null : therapy,
      // the rows are required, so there is no null here; the filter only satisfies TypeScript
      additionalDentalServiceIds: value.additionalDentalServiceIds.filter(
        (id): id is number => id !== null
      )
    });
  }

  /**
   * 400 -> messages under the fields (applyServerErrors) and the general message at the top.
   * 409 -> the server's message at the top (e.g. "already completed", a service is no longer active);
   *        the services are loaded again, so a deactivated service is marked in its row.
   * 401/403 are handled by the interceptors.
   */
  private handleError(err: HttpErrorResponse): void {
    if (err.status === 401 || err.status === 403) {
      return;
    }

    this.errorMessage = applyServerErrors(this.form, err);

    if (err.status === 409) {
      this.loadData();
    }
  }

  /** A row whose service is not in the (fresh) list of active services gets an error. */
  private markUnavailableServices(): void {
    for (const row of this.serviceRows) {
      if (row.value !== null && this.priceOf(row.value) === null) {
        row.setErrors({ serviceUnavailable: true });
        row.markAsTouched();
      }
    }
  }
}
