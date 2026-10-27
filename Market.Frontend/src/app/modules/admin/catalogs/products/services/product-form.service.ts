import { Injectable, inject } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import {GetProductByIdQueryDto} from '../../../../../api-services/products/products-api.models';
import {ProductsApiService} from '../../../../../api-services/products/products-api.service';
import {AppValidators} from '../../../../../core/forms/validators';
import {uniqueProductNameValidator} from '../validators/unique-product-name.validator';

/** Tipizirana forma proizvoda (isti oblik za dodavanje i izmjenu). */
export interface ProductForm {
  name: FormControl<string>;
  description: FormControl<string>;
  price: FormControl<number | null>;
  stockQuantity: FormControl<number | null>;
  categoryId: FormControl<number | null>;
  isEnabled: FormControl<boolean>;
}

/**
 * Service for creating and managing product forms.
 * Provides reusable form creation with validation for Add and Edit components.
 * Poruke grešaka prikazuje <app-fit-field-error> (core/forms/validation-messages.ts).
 */
@Injectable()
export class ProductFormService {
  private api = inject(ProductsApiService);

  /** isto kao ProductEntity.Constraints na backendu */
  readonly nameMaxLength = 150;
  readonly descriptionMaxLength = 1000;

  /**
   * Create a product form with validation.
   * If product data is provided, form is pre-filled (edit mode).
   */
  createProductForm(product?: GetProductByIdQueryDto): FormGroup<ProductForm> {
    return new FormGroup<ProductForm>({
      name: new FormControl(product?.name ?? '', {
        nonNullable: true,
        validators: [
          Validators.required,
          Validators.minLength(3),
          Validators.maxLength(this.nameMaxLength)
        ],
        // naziv mora biti jedinstven – provjera na serveru dok korisnik kuca (vidi validator)
        asyncValidators: [uniqueProductNameValidator(this.api, product?.id)]
      }),
      description: new FormControl(product?.description ?? '', {
        nonNullable: true,
        validators: [Validators.maxLength(this.descriptionMaxLength)]
      }),
      price: new FormControl<number | null>(product?.price ?? null, [
        Validators.required,
        Validators.min(0.01),
        Validators.max(1000000)
      ]),
      stockQuantity: new FormControl<number | null>(product?.stockQuantity ?? 0, [
        Validators.required,
        Validators.min(0),
        AppValidators.integer
      ]),
      categoryId: new FormControl<number | null>(product?.categoryId ?? null, [
        Validators.required
      ]),
      isEnabled: new FormControl(product?.isEnabled ?? true, { nonNullable: true })
    });
  }
}
