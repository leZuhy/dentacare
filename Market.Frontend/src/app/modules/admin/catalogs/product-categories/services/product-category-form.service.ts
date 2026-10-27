import { Injectable } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import {GetProductCategoryByIdQueryDto} from '../../../../../api-services/product-categories/product-categories-api.model';

/** Tipizirana forma kategorije: form.controls.name.value je string, ne any. */
export interface ProductCategoryForm {
  name: FormControl<string>;
  isEnabled: FormControl<boolean>;
}

/**
 * Service for creating and managing product category forms.
 * Used in modal dialog for both create and edit operations.
 * Poruke grešaka prikazuje <app-fit-field-error> (core/forms/validation-messages.ts).
 */
@Injectable()
export class ProductCategoryFormService {
  /** isto kao ProductCategoryEntity.Constraints.NameMaxLength na backendu */
  readonly nameMaxLength = 100;

  /**
   * Create a product category form with validation.
   * If category data is provided, form is pre-filled (edit mode).
   */
  createCategoryForm(category?: GetProductCategoryByIdQueryDto): FormGroup<ProductCategoryForm> {
    return new FormGroup<ProductCategoryForm>({
      name: new FormControl(category?.name ?? '', {
        nonNullable: true,
        validators: [
          Validators.required,
          Validators.minLength(2),
          Validators.maxLength(this.nameMaxLength)
        ]
      }),
      // nova kategorija je aktivna; backend vraća 409 ako se gasi kategorija sa aktivnim proizvodima
      isEnabled: new FormControl(category?.isEnabled ?? true, { nonNullable: true })
    });
  }
}
