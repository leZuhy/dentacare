import { Component, DestroyRef, inject, OnInit } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormControl, FormGroup, Validators } from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import { TranslateService } from "@ngx-translate/core";
import { HttpErrorResponse } from "@angular/common/http";
import { Observable } from "rxjs";
import { WorkingHoursApiService } from "../../../../api-services/working-hours/working-hours-api.service";
import {
  CreateWorkingHoursCommand,
  DayOfWeekNumber,
  GetWorkingHoursByIdQueryDto,
  toApiTime,
  toDisplayTime,
  UpdateWorkingHoursCommand,
  WORK_DAYS,
} from "../../../../api-services/working-hours/working-hours-api.model";
import { DentistsApiService } from "../../../../api-services/dentists/dentists-api.service";
import {
  ListDentistQueryDto,
  ListDentistRequest,
} from "../../../../api-services/dentists/dentists-api.model";
import { BaseFormComponent } from "../../../../core/components/base-classes/base-form-component";
import { applyServerErrors } from "../../../../core/forms/server-errors";
import {
  AppValidators,
  CrossFieldErrorStateMatcher,
} from "../../../../core/forms/validators";
import { allItemsPaging } from "../../../../core/models/paging/paging-utils";
import { getErrorMessage } from "../../../../core/interceptors/error-logging-interceptor.service";
import { ToasterService } from "../../../../core/services/toaster.service";

interface WorkingHoursForm {
  dentistId: FormControl<number | null>;
  dayOfWeek: FormControl<DayOfWeekNumber | null>;
  /** "HH:mm" from input type="time" */
  startTime: FormControl<string>;
  endTime: FormControl<string>;
}

/**
 * Add (/admin/working-hours/new) and edit (/admin/working-hours/:id/edit) of a working-hours interval.
 * Edit mode is when the route has an id. The dentist is chosen only when adding;
 * in edit mode it is shown but cannot be changed (the backend does not allow it).
 */
@Component({
  selector: "app-working-hours-upsert",
  standalone: false,
  templateUrl: "./working-hours-upsert.component.html",
  styleUrl: "./working-hours-upsert.component.scss",
})
export class WorkingHoursUpsertComponent
  extends BaseFormComponent<GetWorkingHoursByIdQueryDto, FormGroup<WorkingHoursForm>>
  implements OnInit
{
  private api = inject(WorkingHoursApiService);
  private dentistsApi = inject(DentistsApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private translate = inject(TranslateService);
  private destroyRef = inject(DestroyRef);

  /** days for the dropdown, Monday first */
  readonly days = WORK_DAYS;

  /** shows the group error "timeRange" under the end-time field */
  readonly timeRangeMatcher = new CrossFieldErrorStateMatcher("timeRange");

  /** options for the dentist dropdown */
  dentists: ListDentistQueryDto[] = [];

  /** id from the route; null in add mode */
  private id: number | null = null;

  isSaving = false;

  ngOnInit(): void {
    this.form = new FormGroup<WorkingHoursForm>(
      {
        dentistId: new FormControl<number | null>(null, {
          validators: [Validators.required],
        }),
        dayOfWeek: new FormControl<DayOfWeekNumber | null>(null, {
          validators: [Validators.required],
        }),
        startTime: new FormControl("", {
          nonNullable: true,
          validators: [Validators.required],
        }),
        endTime: new FormControl("", {
          nonNullable: true,
          validators: [Validators.required],
        }),
      },
      // compares two fields, so it goes on the group, not on one control
      { validators: AppValidators.timeRange("startTime", "endTime") }
    );

    const idParam = this.route.snapshot.paramMap.get("id");
    this.id = idParam ? Number(idParam) : null;

    this.loadDentists();
    this.initForm(this.id !== null);
  }

  protected loadData(): void {
    this.startLoading();

    this.api
      .getById(this.id!)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (workingHours) => {
          this.model = workingHours;
          this.form.patchValue({
            dentistId: workingHours.dentistId,
            dayOfWeek: workingHours.dayOfWeek,
            // the backend sends "08:00:00", input type="time" shows "08:00"
            startTime: toDisplayTime(workingHours.startTime),
            endTime: toDisplayTime(workingHours.endTime),
          });
          // first fill in the dentist, then lock it: the selected dentist stays visible,
          // and a disabled control is not validated and is left out of form.value
          this.form.controls.dentistId.disable();
          this.stopLoading();
        },
        error: (err: HttpErrorResponse) => {
          if (err.status === 404) {
            // deleted in the meantime (e.g. browser Back after delete): no form for a missing record
            this.toaster.error(getErrorMessage(err));
            this.router.navigate(["/admin/working-hours"], { replaceUrl: true });
            return;
          }
          this.stopLoading(getErrorMessage(err));
        },
      });
  }

  /** all dentists for the dropdown (one request, up to 1000 items) */
  private loadDentists(): void {
    const request = new ListDentistRequest();
    request.paging = allItemsPaging;

    this.dentistsApi
      .list(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => (this.dentists = response.items),
        error: (err) => (this.errorMessage = getErrorMessage(err)),
      });
  }

  protected save(): void {
    if (this.isSaving) {
      return;
    }

    const controls = this.form.controls;
    // fields that both commands have
    const common: UpdateWorkingHoursCommand = {
      // required validator passed, so the value is not null here
      dayOfWeek: controls.dayOfWeek.value!,
      startTime: toApiTime(controls.startTime.value),
      endTime: toApiTime(controls.endTime.value),
    };

    // create returns { id } and update returns nothing; we don't need either result here
    let request$: Observable<unknown>;
    if (this.isEditMode) {
      request$ = this.api.update(this.id!, common);
    } else {
      const command: CreateWorkingHoursCommand = {
        ...common,
        dentistId: controls.dentistId.value!,
      };
      request$ = this.api.create(command);
    }

    this.isSaving = true;
    this.errorMessage = null;

    request$.subscribe({
      next: () => {
        this.isSaving = false;
        const messageKey = this.isEditMode ? "WORKING_HOURS.UPDATED" : "WORKING_HOURS.SAVED";
        this.toaster.success(this.translate.instant(messageKey));
        this.goBack();
      },
      error: (err) => {
        this.isSaving = false;
        // overlap -> error under "startTime", end before start -> under "endTime"
        this.errorMessage = applyServerErrors(this.form, err);
      },
    });
  }

  goBack(): void {
    this.router.navigate(["/admin/working-hours"]);
  }
}
