import {Component, DestroyRef, inject, OnInit} from '@angular/core';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {FormGroup} from '@angular/forms';
import {ActivatedRoute, Router} from '@angular/router';
import {TranslateService} from '@ngx-translate/core';
import {forkJoin} from 'rxjs';
import {ProductForm, ProductFormService} from '../services/product-form.service';
import {BaseFormComponent} from '../../../../../core/components/base-classes/base-form-component';
import {GetProductByIdQueryDto, UpdateProductCommand} from '../../../../../api-services/products/products-api.models';
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
  selector: 'app-products-edit',
  standalone: false,
  templateUrl: './products-edit.component.html',
  styleUrl: './products-edit.component.scss',
  providers: [ProductFormService]
})
export class ProductsEditComponent
  extends BaseFormComponent<GetProductByIdQueryDto, FormGroup<ProductForm>>
  implements OnInit {

  private api = inject(ProductsApiService);
  private categoriesApi = inject(ProductCategoriesApiService);
  protected formService = inject(ProductFormService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private translate = inject(TranslateService);
  private destroyRef = inject(DestroyRef);

  productId!: number;
  categories: ListProductCategoriesQueryDto[] = [];
  /** slika se mijenja odvojeno od forme (ProductImageComponent) */
  imageUrl: string | null = null;

  ngOnInit(): void {
    this.productId = +this.route.snapshot.params['id'];
    this.initForm(true); // Edit mode
  }

  protected loadData(): void {
    this.startLoading();

    // Load product and categories in parallel
    forkJoin({
      product: this.api.getById(this.productId),
      // dropdown treba SVE kategorije → allItemsPaging (default paging je 10 po stranici)
      categories: this.categoriesApi.list({ onlyEnabled: true, paging: allItemsPaging })
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ product, categories }) => {
          this.model = product;
          this.imageUrl = product.imageUrl;
          this.categories = categories.items;
          this.form = this.formService.createProductForm(product);
          this.stopLoading();
        },
        error: (err) => {
          this.stopLoading();
          this.toaster.error(this.translate.instant('PRODUCTS.DIALOGS.ERROR_LOAD'));
          console.error('Load product error:', err);
          this.router.navigate(['/admin/products']);
        }
      });
  }

  protected save(): void {
    if (this.form.invalid || this.isLoading) {
      return;
    }

    this.startLoading();

    const value = this.form.getRawValue();
    const command: UpdateProductCommand = {
      name: value.name.trim(),
      description: value.description.trim() || null,
      price: value.price ?? 0,
      // PUT traži oba polja (bez njih 400)
      stockQuantity: value.stockQuantity ?? 0,
      categoryId: value.categoryId ?? 0,
      isEnabled: value.isEnabled
    };

    this.api.update(this.productId, command).subscribe({
      next: () => {
        this.stopLoading();
        this.toaster.success(this.translate.instant('PRODUCTS.DIALOGS.SUCCESS_UPDATE'));
        this.router.navigate(['/admin/products']);
      },
      error: (err) => {
        // greške polja idu u mat-error, opšta poruka u error-banner; forma ostaje popunjena
        this.stopLoading(applyServerErrors(this.form, err));
        console.error('Update product error:', err);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/admin/products']);
  }
}
