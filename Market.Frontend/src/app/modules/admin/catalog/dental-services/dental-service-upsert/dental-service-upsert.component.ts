import { Component, DestroyRef, inject, OnInit } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormControl, FormGroup, Validators } from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import { TranslateService } from "@ngx-translate/core";
import { HttpErrorResponse } from "@angular/common/http";
import { Observable } from "rxjs";
import { DentalServicesApiService } from "../../../../../api-services/dental-services/dental-services-api.service";
import {
  CreateDentalServiceCommand,
  GetDentalServiceByIdQueryDto,
  UpdateDentalServiceCommand,
} from "../../../../../api-services/dental-services/dental-services-api.model";
import { ServiceCategoriesApiService } from "../../../../../api-services/service-categories/service-categories-api.service";
import {
  ListServiceCategoryQueryDto,
  ListServiceCategoryRequest,
} from "../../../../../api-services/service-categories/service-categories-api.model";
import { BaseFormComponent } from "../../../../../core/components/base-classes/base-form-component";
import { applyServerErrors } from "../../../../../core/forms/server-errors";
import { AppValidators } from "../../../../../core/forms/validators";
import { allItemsPaging } from "../../../../../core/models/paging/paging-utils";
import { getErrorMessage } from "../../../../../core/interceptors/error-logging-interceptor.service";
import { ToasterService } from "../../../../../core/services/toaster.service";

// same limits as DentalServiceEntity.Constraints on the backend
const NAME_MAX_LENGTH = 100;
const DESCRIPTION_MAX_LENGTH = 500;
const PRICE_MAX = 10000;
const DURATION_MIN = 15;
const DURATION_MAX = 480;
const DURATION_STEP = 15;

/** the backend rule is "greater than 0"; with 2 decimals the smallest such price is 0.01 */
const PRICE_MIN = 0.01;
const PRICE_MAX_DECIMALS = 2;

interface DentalServiceForm {
  categoryId: FormControl<number | null>;
  name: FormControl<string>;
  description: FormControl<string>;
  priceBam: FormControl<number | null>;
  durationMinutes: FormControl<number | null>;
  isActive: FormControl<boolean>;
}

/**
 * Add (/admin/catalog/dental-services/new) and edit (/admin/catalog/dental-services/:id/edit)
 * of a dental service. Edit mode is when the route has an id.
 * The "active" checkbox exists only in edit mode, because a new service is always active.
 */
@Component({
  selector: "app-dental-service-upsert",
  standalone: false,
  templateUrl: "./dental-service-upsert.component.html",
  styleUrl: "./dental-service-upsert.component.scss",
})
export class DentalServiceUpsertComponent
  extends BaseFormComponent<GetDentalServiceByIdQueryDto, FormGroup<DentalServiceForm>>
  implements OnInit
{
  private api = inject(DentalServicesApiService);
  private categoriesApi = inject(ServiceCategoriesApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private translate = inject(TranslateService);
  private destroyRef = inject(DestroyRef);

  readonly nameMaxLength = NAME_MAX_LENGTH;
  readonly descriptionMaxLength = DESCRIPTION_MAX_LENGTH;

  /** options for the category dropdown */
  categories: ListServiceCategoryQueryDto[] = [];

  /** id from the route; null in add mode */
  private id: number | null = null;

  isSaving = false;

  ngOnInit(): void {
    this.form = new FormGroup<DentalServiceForm>({
      categoryId: new FormControl<number | null>(null, {
        validators: [Validators.required],
      }),
      name: new FormControl("", {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(NAME_MAX_LENGTH)],
      }),
      description: new FormControl("", {
        nonNullable: true,
        validators: [Validators.maxLength(DESCRIPTION_MAX_LENGTH)],
      }),
      priceBam: new FormControl<number | null>(null, {
        validators: [
          Validators.required,
          Validators.min(PRICE_MIN),
          Validators.max(PRICE_MAX),
          AppValidators.maxDecimals(PRICE_MAX_DECIMALS),
        ],
      }),
      durationMinutes: new FormControl<number | null>(null, {
        validators: [
          Validators.required,
          Validators.min(DURATION_MIN),
          Validators.max(DURATION_MAX),
          AppValidators.multipleOf(DURATION_STEP),
        ],
      }),
      isActive: new FormControl(true, { nonNullable: true }),
    });

    const idParam = this.route.snapshot.paramMap.get("id");
    this.id = idParam ? Number(idParam) : null;

    this.loadCategories();
    this.initForm(this.id !== null);
  }

  protected loadData(): void {
    this.startLoading();

    this.api
      .getById(this.id!)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (service) => {
          this.model = service;
          this.form.patchValue({
            categoryId: service.categoryId,
            name: service.name,
            // the backend sends null for "no description", the text field works with ""
            description: service.description ?? "",
            priceBam: service.priceBam,
            durationMinutes: service.durationMinutes,
            isActive: service.isActive,
          });
          this.stopLoading();
        },
        error: (err: HttpErrorResponse) => {
          if (err.status === 404) {
            // no form for a record that does not exist
            this.toaster.error(getErrorMessage(err));
            this.router.navigate(["/admin/catalog/dental-services"], {
              replaceUrl: true,
            });
            return;
          }
          this.stopLoading(getErrorMessage(err));
        },
      });
  }

  /** all categories for the dropdown (one request, up to 1000 items) */
  private loadCategories(): void {
    const request = new ListServiceCategoryRequest();
    request.paging = allItemsPaging;

    this.categoriesApi
      .list(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => (this.categories = response.items),
        error: (err) => (this.errorMessage = getErrorMessage(err)),
      });
  }

  protected save(): void {
    if (this.isSaving) {
      return;
    }

    const controls = this.form.controls;
    // fields that both commands have
    const common: CreateDentalServiceCommand = {
      // required validators passed, so these values are not null here
      categoryId: controls.categoryId.value!,
      name: controls.name.value.trim(),
      description: controls.description.value.trim() || null,
      priceBam: controls.priceBam.value!,
      durationMinutes: controls.durationMinutes.value!,
    };

    // create returns { id } and update returns nothing; we don't need either result here
    let request$: Observable<unknown>;
    if (this.isEditMode) {
      const command: UpdateDentalServiceCommand = {
        ...common,
        isActive: controls.isActive.value,
      };
      request$ = this.api.update(this.id!, command);
    } else {
      request$ = this.api.create(common);
    }

    this.isSaving = true;
    this.errorMessage = null;

    request$.subscribe({
      next: () => {
        this.isSaving = false;
        const messageKey = this.isEditMode
          ? "DENTAL_SERVICES.UPDATED"
          : "DENTAL_SERVICES.SAVED";
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
    this.router.navigate(["/admin/catalog/dental-services"]);
  }
}
