import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { PromotionsApiService } from '../../../../../api-services/promotions/promotions-api.service';
import {
  GetPromotionByIdQueryDto,
  UpsertPromotionCommand
} from '../../../../../api-services/promotions/promotions-api.model';
import { BaseFormComponent } from '../../../../../core/components/base-classes/base-form-component';
import { applyServerErrors } from '../../../../../core/forms/server-errors';
import { fromUtcDateIso, toUtcDateIso } from '../../../../../core/forms/utc-date';
import { AppValidators, CrossFieldErrorStateMatcher } from '../../../../../core/forms/validators';
import { getErrorMessage } from '../../../../../core/interceptors/error-logging-interceptor.service';
import { ToasterService } from '../../../../../core/services/toaster.service';

/**
 * Tipizirana forma promocije. Imena kontrola = imena polja API-ja (startsAtUtc, endsAtUtc),
 * pa applyServerErrors grešku servera na "endsAtUtc" prikaže ispod polja "Kraj".
 */
interface PromotionForm {
  title: FormControl<string>;
  imageUrl: FormControl<string>;
  targetUrl: FormControl<string>;
  isActive: FormControl<boolean>;
  sortOrder: FormControl<number | null>;
  /** Date iz datepickera (kalendarski dan); u API ide kao UTC ISO string */
  startsAtUtc: FormControl<Date | null>;
  endsAtUtc: FormControl<Date | null>;
}

/**
 * Dodavanje i izmjena promocije (/admin/promotions/add, /admin/promotions/:id/edit).
 *
 * PRIMJERI na ovoj formi:
 * - URL validatori (AppValidators.absoluteHttpUrl, httpUrlOrAppPath)
 * - PERIOD sa dva mat-datepickera + CROSS-FIELD validator (AppValidators.dateRange na grupi):
 *   kraj mora biti nakon početka; greška se prikazuje ispod polja "Kraj"
 *   (CrossFieldErrorStateMatcher), a ista greška sa servera stiže na polje endsAtUtc.
 */
@Component({
  selector: 'app-promotion-upsert',
  standalone: false,
  templateUrl: './promotion-upsert.component.html',
  styleUrl: './promotion-upsert.component.scss'
})
export class PromotionUpsertComponent
  extends BaseFormComponent<GetPromotionByIdQueryDto, FormGroup<PromotionForm>>
  implements OnInit
{
  private api = inject(PromotionsApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private translate = inject(TranslateService);
  private destroyRef = inject(DestroyRef);

  /** isto kao PromotionEntity.Constraints na backendu */
  readonly titleMaxLength = 120;
  readonly urlMaxLength = 500;

  /** polje "Kraj" je u grešci i kad je greška 'dateRange' na grupi */
  readonly dateRangeMatcher = new CrossFieldErrorStateMatcher('dateRange');

  promotionId: number | null = null;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    this.promotionId = id ? Number(id) : null;

    this.form = this.createForm();
    this.initForm(this.promotionId !== null);
  }

  protected loadData(): void {
    this.startLoading();

    this.api
      .getById(this.promotionId!)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (promotion) => {
          this.model = promotion;
          this.form.setValue({
            title: promotion.title,
            imageUrl: promotion.imageUrl,
            targetUrl: promotion.targetUrl ?? '',
            isActive: promotion.isActive,
            sortOrder: promotion.sortOrder,
            startsAtUtc: fromUtcDateIso(promotion.startsAtUtc),
            endsAtUtc: fromUtcDateIso(promotion.endsAtUtc)
          });
          this.stopLoading();
        },
        error: (err) => {
          this.stopLoading();
          this.toaster.error(getErrorMessage(err));
          this.router.navigate(['/admin/promotions']);
        }
      });
  }

  protected save(): void {
    if (this.isLoading) {
      return;
    }

    const value = this.form.getRawValue();
    const command: UpsertPromotionCommand = {
      title: value.title.trim(),
      imageUrl: value.imageUrl.trim(),
      targetUrl: value.targetUrl.trim() || null,
      isActive: value.isActive,
      sortOrder: value.sortOrder ?? 0,
      startsAtUtc: toUtcDateIso(value.startsAtUtc),
      endsAtUtc: toUtcDateIso(value.endsAtUtc)
    };

    const request$: Observable<unknown> = this.isEditMode
      ? this.api.update(this.promotionId!, command)
      : this.api.create(command);

    this.startLoading();
    request$.subscribe({
      next: () => {
        this.stopLoading();
        this.toaster.success(this.translate.instant('PROMOTIONS.SAVED'));
        this.router.navigate(['/admin/promotions']);
      },
      error: (err) => this.stopLoading(applyServerErrors(this.form, err))
    });
  }

  /** Slika iz polja imageUrl, samo ako je URL ispravan (za pregled ispod polja). */
  get imagePreview(): string | null {
    const control = this.form.controls.imageUrl;
    return control.value && control.valid ? control.value : null;
  }

  private createForm(): FormGroup<PromotionForm> {
    return new FormGroup<PromotionForm>(
      {
        title: new FormControl('', {
          nonNullable: true,
          validators: [Validators.required, Validators.maxLength(this.titleMaxLength)]
        }),
        imageUrl: new FormControl('', {
          nonNullable: true,
          validators: [
            Validators.required,
            Validators.maxLength(this.urlMaxLength),
            AppValidators.absoluteHttpUrl
          ]
        }),
        targetUrl: new FormControl('', {
          nonNullable: true,
          validators: [Validators.maxLength(this.urlMaxLength), AppValidators.httpUrlOrAppPath]
        }),
        isActive: new FormControl(true, { nonNullable: true }),
        sortOrder: new FormControl<number | null>(0, [
          Validators.required,
          Validators.min(0),
          AppValidators.integer
        ]),
        startsAtUtc: new FormControl<Date | null>(null),
        endsAtUtc: new FormControl<Date | null>(null)
      },
      // CROSS-FIELD: validator na GRUPI, jer poredi dva polja
      { validators: AppValidators.dateRange('startsAtUtc', 'endsAtUtc') }
    );
  }
}
