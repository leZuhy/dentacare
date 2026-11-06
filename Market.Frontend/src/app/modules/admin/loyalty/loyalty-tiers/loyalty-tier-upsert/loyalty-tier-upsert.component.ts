import { Component, DestroyRef, inject, OnInit } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormControl, FormGroup, Validators } from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import { TranslateService } from "@ngx-translate/core";
import { HttpErrorResponse } from "@angular/common/http";
import { Observable } from "rxjs";
import { LoyaltyTiersApiService } from "../../../../../api-services/loyalty-tiers/loyalty-tiers-api.service";
import {
  GetLoyaltyTierByIdQueryDto,
  UpsertLoyaltyTierCommand,
} from "../../../../../api-services/loyalty-tiers/loyalty-tiers-api.model";
import { BaseFormComponent } from "../../../../../core/components/base-classes/base-form-component";
import { applyServerErrors } from "../../../../../core/forms/server-errors";
import { AppValidators } from "../../../../../core/forms/validators";
import { getErrorMessage } from "../../../../../core/interceptors/error-logging-interceptor.service";
import { ToasterService } from "../../../../../core/services/toaster.service";

// same limits as LoyaltyTierEntity.Constraints on the backend
const NAME_MAX_LENGTH = 50;
const MIN_POINTS_MAX = 1_000_000;
const DISCOUNT_MAX = 40;
const DISCOUNT_MAX_DECIMALS = 2;
const BASE_TIER_MIN_POINTS = 0;

interface LoyaltyTierForm {
  name: FormControl<string>;
  minPoints: FormControl<number | null>;
  discountPercent: FormControl<number | null>;
}

/**
 * Add (/admin/loyalty/loyalty-tiers/new) and edit (/admin/loyalty/loyalty-tiers/:id/edit)
 * of a loyalty tier. Edit mode is when the route has an id.
 * The base tier (0 points) keeps its points, so that field is locked when editing it.
 */

@Component({
  selector: "app-loyalty-tier-upsert",
  standalone: false,
  templateUrl: "./loyalty-tier-upsert.component.html",
  styleUrl: "./loyalty-tier-upsert.component.scss",
})
export class LoyaltyTierUpsertComponent
  extends BaseFormComponent<
    GetLoyaltyTierByIdQueryDto,
    FormGroup<LoyaltyTierForm>
  >
  implements OnInit
{
  private api = inject(LoyaltyTiersApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private translate = inject(TranslateService);
  private destroyRef = inject(DestroyRef);

  readonly nameMaxLength = NAME_MAX_LENGTH;

  private id: number | null = null;

  isBaseTier = false;

  isSaving = false;

  ngOnInit(): void {
    this.form = new FormGroup<LoyaltyTierForm>({
      name: new FormControl("", {
        nonNullable: true,
        validators: [
          Validators.required,
          Validators.maxLength(NAME_MAX_LENGTH),
        ],
      }),
      minPoints: new FormControl<number | null>(null, {
        validators: [
          Validators.required,
          Validators.min(0),
          Validators.max(MIN_POINTS_MAX),
          AppValidators.integer,
        ],
      }),
      discountPercent: new FormControl<number | null>(null, {
        validators: [
          Validators.required,
          Validators.min(0),
          Validators.max(DISCOUNT_MAX),
          AppValidators.maxDecimals(DISCOUNT_MAX_DECIMALS),
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
        next: (tier) => {
          this.model = tier;
          this.form.patchValue({
            name: tier.name,
            minPoints: tier.minPoints,
            discountPercent: tier.discountPercent,
          });

          // the base tier keeps 0 points (the backend checks this too)
          this.isBaseTier = tier.minPoints === BASE_TIER_MIN_POINTS;
          if (this.isBaseTier) {
            this.form.controls.minPoints.disable();
          }

          this.stopLoading();
        },
        error: (err: HttpErrorResponse) => {
          if (err.status === 404) {
            // deleted in the meantime: no form for a missing record
            this.toaster.error(getErrorMessage(err));
            this.router.navigate(["/admin/loyalty/loyalty-tiers"], {
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

    const controls = this.form.controls;
    const command: UpsertLoyaltyTierCommand = {
      name: controls.name.value.trim(),
      // required passed, so these are not null; a disabled control still has its value
      minPoints: controls.minPoints.value!,
      discountPercent: controls.discountPercent.value!,
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
          ? "LOYALTY_TIERS.UPDATED"
          : "LOYALTY_TIERS.SAVED";
        this.toaster.success(this.translate.instant(messageKey));
        this.goBack();
      },
      error: (err) => {
        this.isSaving = false;
        // e.g. duplicate points or discount out of order -> error under that field
        this.errorMessage = applyServerErrors(this.form, err);
      },
    });
  }

  goBack(): void {
    this.router.navigate(["/admin/loyalty/loyalty-tiers"]);
  }
}
