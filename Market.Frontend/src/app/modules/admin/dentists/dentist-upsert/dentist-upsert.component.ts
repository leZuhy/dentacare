import { Component, DestroyRef, inject, OnInit } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormControl, FormGroup, Validators } from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import { TranslateService } from "@ngx-translate/core";
import { HttpErrorResponse } from "@angular/common/http";
import { Observable } from "rxjs";
import { DentistsApiService } from "../../../../api-services/dentists/dentists-api.service";
import {
  CreateDentistCommand,
  GetDentistByIdQueryDto,
  UpdateDentistCommand,
} from "../../../../api-services/dentists/dentists-api.model";
import { SpecializationsApiService } from "../../../../api-services/specializations/specializations-api.service";
import {
  ListSpecializationQueryDto,
  ListSpecializationRequest,
} from "../../../../api-services/specializations/specializations-api.model";
import { BaseFormComponent } from "../../../../core/components/base-classes/base-form-component";
import { applyServerErrors } from "../../../../core/forms/server-errors";
import {
  AppValidators,
  PASSWORD_MIN_LENGTH,
} from "../../../../core/forms/validators";
import { allItemsPaging } from "../../../../core/models/paging/paging-utils";
import { getErrorMessage } from "../../../../core/interceptors/error-logging-interceptor.service";
import { ToasterService } from "../../../../core/services/toaster.service";
import { USER_CONSTRAINTS } from "../../../account/user-constraints";

/** same limit as DentistEntity.Constraints.BioMaxLength on the backend */
const BIO_MAX_LENGTH = 1000;

interface DentistForm {
  firstname: FormControl<string>;
  lastname: FormControl<string>;
  email: FormControl<string>;
  password: FormControl<string>;
  specializationId: FormControl<number | null>;
  bio: FormControl<string>;
}

/**
 * Add (/admin/dentists/new) and edit (/admin/dentists/:id/edit) of a dentist.
 * Edit mode is when the route has an id. Email and password are set only when adding
 * (the backend creates the user account then); in edit mode email is read-only.
 */
@Component({
  selector: "app-dentist-upsert",
  standalone: false,
  templateUrl: "./dentist-upsert.component.html",
  styleUrl: "./dentist-upsert.component.scss",
})
export class DentistUpsertComponent
  extends BaseFormComponent<GetDentistByIdQueryDto, FormGroup<DentistForm>>
  implements OnInit
{
  private api = inject(DentistsApiService);
  private specializationsApi = inject(SpecializationsApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private translate = inject(TranslateService);
  private destroyRef = inject(DestroyRef);

  readonly limits = USER_CONSTRAINTS;
  readonly bioMaxLength = BIO_MAX_LENGTH;
  readonly passwordMinLength = PASSWORD_MIN_LENGTH;

  /** options for the specialization dropdown */
  specializations: ListSpecializationQueryDto[] = [];

  /** id from the route; null in add mode */
  private id: number | null = null;

  isSaving = false;

  ngOnInit(): void {
    this.form = new FormGroup<DentistForm>({
      firstname: new FormControl("", {
        nonNullable: true,
        validators: [
          Validators.required,
          Validators.maxLength(this.limits.firstnameMaxLength),
        ],
      }),
      lastname: new FormControl("", {
        nonNullable: true,
        validators: [
          Validators.required,
          Validators.maxLength(this.limits.lastnameMaxLength),
        ],
      }),
      email: new FormControl("", {
        nonNullable: true,
        validators: [
          Validators.required,
          Validators.email,
          Validators.maxLength(this.limits.emailMaxLength),
        ],
      }),
      password: new FormControl("", {
        nonNullable: true,
        validators: [Validators.required, AppValidators.password],
      }),
      specializationId: new FormControl<number | null>(null, {
        validators: [Validators.required],
      }),
      bio: new FormControl("", {
        nonNullable: true,
        validators: [Validators.maxLength(BIO_MAX_LENGTH)],
      }),
    });

    const idParam = this.route.snapshot.paramMap.get("id");
    this.id = idParam ? Number(idParam) : null;

    if (this.id !== null) {
      // a disabled control is not validated and is left out of form.value,
      // so email and password don't block saving in edit mode
      this.form.controls.email.disable();
      this.form.controls.password.disable();
    }

    this.loadSpecializations();
    this.initForm(this.id !== null);
  }

  protected loadData(): void {
    this.startLoading();

    this.api
      .getById(this.id!)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (dentist) => {
          this.model = dentist;
          this.form.patchValue({
            firstname: dentist.firstname,
            lastname: dentist.lastname,
            email: dentist.email,
            specializationId: dentist.specializationId,
            bio: dentist.bio ?? "",
          });
          this.stopLoading();
        },
        error: (err: HttpErrorResponse) => {
          if (err.status === 404) {
            // deleted in the meantime (e.g. browser Back after delete): no form for a missing record
            this.toaster.error(getErrorMessage(err));
            this.router.navigate(["/admin/dentists"], { replaceUrl: true });
            return;
          }
          this.stopLoading(getErrorMessage(err));
        },
      });
  }

  /** all specializations for the dropdown (one request, up to 1000 items) */
  private loadSpecializations(): void {
    const request = new ListSpecializationRequest();
    request.paging = allItemsPaging;

    this.specializationsApi
      .list(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => (this.specializations = response.items),
        error: (err) => (this.errorMessage = getErrorMessage(err)),
      });
  }

  protected save(): void {
    if (this.isSaving) {
      return;
    }

    const controls = this.form.controls;
    // fields that both commands have
    const common: UpdateDentistCommand = {
      firstname: controls.firstname.value.trim(),
      lastname: controls.lastname.value.trim(),
      // required validator passed, so the value is not null here
      specializationId: controls.specializationId.value!,
      bio: controls.bio.value.trim() || null,
    };

    // create returns { id } and update returns nothing; we don't need either result here
    let request$: Observable<unknown>;
    if (this.isEditMode) {
      request$ = this.api.update(this.id!, common);
    } else {
      const command: CreateDentistCommand = {
        ...common,
        email: controls.email.value.trim(),
        // no trim: spaces are part of the password
        password: controls.password.value,
      };
      request$ = this.api.create(command);
    }

    this.isSaving = true;
    this.errorMessage = null;

    request$.subscribe({
      next: () => {
        this.isSaving = false;
        const messageKey = this.isEditMode ? "DENTISTS.UPDATED" : "DENTISTS.SAVED";
        this.toaster.success(this.translate.instant(messageKey));
        this.goBack();
      },
      error: (err) => {
        this.isSaving = false;
        // e.g. email already exists -> error under the "email" field
        this.errorMessage = applyServerErrors(this.form, err);
      },
    });
  }

  goBack(): void {
    this.router.navigate(["/admin/dentists"]);
  }
}
