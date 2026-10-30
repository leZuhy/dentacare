import { Component, DestroyRef, inject, OnInit } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormControl, FormGroup, Validators } from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import { TranslateService } from "@ngx-translate/core";
import { HttpErrorResponse } from "@angular/common/http";
import { Observable } from "rxjs";
import { SpecializationsApiService } from "../../../../../api-services/specializations/specializations-api.service";
import {
  GetSpecializationByIdQueryDto,
  UpsertSpecializationCommand,
} from "../../../../../api-services/specializations/specializations-api.model";
import { BaseFormComponent } from "../../../../../core/components/base-classes/base-form-component";
import { applyServerErrors } from "../../../../../core/forms/server-errors";
import { getErrorMessage } from "../../../../../core/interceptors/error-logging-interceptor.service";
import { ToasterService } from "../../../../../core/services/toaster.service";

/** same limit as SpecializationEntity.Constraints.NameMaxLength on the backend */
const NAME_MAX_LENGTH = 100;

interface SpecializationForm {
  name: FormControl<string>;
}

/**
 * Add (/admin/catalog/specializations/new) and edit (/admin/catalog/specializations/:id/edit)
 * of a specialization. Edit mode is when the route has an id.
 */
@Component({
  selector: "app-specialization-upsert",
  standalone: false,
  templateUrl: "./specialization-upsert.component.html",
  styleUrl: "./specialization-upsert.component.scss",
})
export class SpecializationUpsertComponent
  extends BaseFormComponent<
    GetSpecializationByIdQueryDto,
    FormGroup<SpecializationForm>
  >
  implements OnInit
{
  private api = inject(SpecializationsApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private translate = inject(TranslateService);
  private destroyRef = inject(DestroyRef);

  readonly nameMaxLength = NAME_MAX_LENGTH;

  /** id from the route; null in add mode */
  private id: number | null = null;

  isSaving = false;

  ngOnInit(): void {
    this.form = new FormGroup<SpecializationForm>({
      name: new FormControl("", {
        nonNullable: true,
        validators: [
          Validators.required,
          Validators.maxLength(NAME_MAX_LENGTH),
        ],
      }),
    });

    const idParam = this.route.snapshot.paramMap.get("id");
    this.id = idParam ? Number(idParam) : null;

    this.initForm(this.id !== null);
  }

  protected loadData(): void {
    this.startLoading();

    this.api
      .getById(this.id!)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (specialization) => {
          this.model = specialization;
          this.form.patchValue({ name: specialization.name });
          this.stopLoading();
        },
        error: (err: HttpErrorResponse) => {
          if (err.status === 404) {
            // deleted in the meantime (e.g. browser Back after delete): no form for a missing record
            this.toaster.error(getErrorMessage(err));
            this.router.navigate(["/admin/catalog/specializations"], {
              replaceUrl: true,
            });
            return;
          }
          this.stopLoading(getErrorMessage(err));
        },
      });
  }

  protected save(): void {
    if (this.isSaving) {
      return;
    }

    const command: UpsertSpecializationCommand = {
      name: this.form.controls.name.value.trim(),
    };

    // create returns { id } and update returns nothing; we don't need either result here
    const request$: Observable<unknown> = this.isEditMode
      ? this.api.update(this.id!, command)
      : this.api.create(command);

    this.isSaving = true;
    this.errorMessage = null;

    request$.subscribe({
      next: () => {
        this.isSaving = false;
        const messageKey = this.isEditMode
          ? "SPECIALIZATIONS.UPDATED"
          : "SPECIALIZATIONS.SAVED";
        this.toaster.success(this.translate.instant(messageKey));
        this.goBack();
      },
      error: (err) => {
        this.isSaving = false;
        // e.g. duplicate name -> error under the "name" field
        this.errorMessage = applyServerErrors(this.form, err);
      },
    });
  }

  goBack(): void {
    this.router.navigate(["/admin/catalog/specializations"]);
  }
}
