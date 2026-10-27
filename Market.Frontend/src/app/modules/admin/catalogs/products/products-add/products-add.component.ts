import {Component, DestroyRef, inject, OnInit} from '@angular/core';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {FormGroup} from '@angular/forms';
import {Router} from '@angular/router';
import {TranslateService} from '@ngx-translate/core';
import {ProductForm, ProductFormService} from '../services/product-form.service';
import {CreateProductCommand, GetProductByIdQueryDto} from '../../../../../api-services/products/products-api.models';
import {BaseFormComponent} from '../../../../../core/components/base-classes/base-form-component';
import {ProductsApiService} from '../../../../../api-services/products/products-api.service';
import {
  ProductCategoriesApiService
} from '../../../../../api-services/product-categories/product-categories-api.service';
import {ToasterService} from '../../../../../core/services/toaster.service';
import {
  ListProductCategoriesQueryDto
} from '../../../../../api-services/product-categories/product-categories-api.model';
import {allItemsPaging} from '../../../../../core/models/paging/paging-utils';
import {applyServerErrors} from '../../../../../core/forms/server-errors';

@Component({
  selector: 'app-products-add',
  standalone: false,
  templateUrl: './products-add.component.html',
  styleUrl: './products-add.component.scss',
  providers: [ProductFormService]
})
export class ProductsAddComponent
  extends BaseFormComponent<GetProductByIdQueryDto, FormGroup<ProductForm>>
  implements OnInit {

  private api = inject(ProductsApiService);
  private categoriesApi = inject(ProductCategoriesApiService);
  protected formService = inject(ProductFormService);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private translate = inject(TranslateService);
  private destroyRef = inject(DestroyRef);

  categories: ListProductCategoriesQueryDto[] = [];

  ngOnInit(): void {
    this.initForm(false); // Add mode
    this.loadCategories();
  }

  protected loadData(): void {
    // Not needed in add mode
  }

  protected save(): void {
    if (this.form.invalid || this.isLoading) {
      return;
    }

    this.startLoading();

    const value = this.form.getRawValue();
    const command: CreateProductCommand = {
      name: value.name.trim(),
      description: value.description.trim() || null,
      price: value.price ?? 0,
      stockQuantity: value.stockQuantity ?? 0,
      categoryId: value.categoryId ?? 0,
      isEnabled: value.isEnabled
    };

    this.api.create(command).subscribe({
      next: ({ id }) => {
        this.stopLoading();
        // slika se dodaje na stranici za izmjenu (upload traži id proizvoda)
        this.toaster.success(this.translate.instant('PRODUCTS.DIALOGS.SUCCESS_CREATE_ADD_IMAGE'));
        this.router.navigate(['/admin/products', id, 'edit']);
      },
      error: (err) => {
        // greške polja idu u mat-error, opšta poruka u error-banner; forma ostaje popunjena
        this.stopLoading(applyServerErrors(this.form, err));
        console.error('Create product error:', err);
      }
    });
  }

  private loadCategories(): void {
    // dropdown treba SVE kategorije → allItemsPaging (default paging je 10 po stranici)
    this.categoriesApi
      .list({ onlyEnabled: true, paging: allItemsPaging })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.categories = response.items;
        },
        error: (err) => {
          this.toaster.error(this.translate.instant('PRODUCTS.DIALOGS.ERROR_LOAD_CATEGORIES'));
          console.error('Load categories error:', err);
        }
      });
  }

  protected override initForm(isEdit: boolean): void {
    super.initForm(isEdit);
    this.form = this.formService.createProductForm();
  }

  onCancel(): void {
    this.router.navigate(['/admin/products']);
  }
}
