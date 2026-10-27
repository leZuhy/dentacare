import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { debounceTime, distinctUntilChanged, map, Subscription } from 'rxjs';
import { ProductsApiService } from '../../../api-services/products/products-api.service';
import {
  ListProductsQueryDto,
  ListProductsRequest
} from '../../../api-services/products/products-api.models';
import { ProductCategoriesApiService } from '../../../api-services/product-categories/product-categories-api.service';
import { ListProductCategoriesQueryDto } from '../../../api-services/product-categories/product-categories-api.model';
import { BaseListPagedComponent } from '../../../core/components/base-classes/base-list-paged-component';
import { allItemsPaging } from '../../../core/models/paging/paging-utils';
import { getErrorMessage } from '../../../core/interceptors/error-logging-interceptor.service';

/**
 * Javna pretraga proizvoda (/search, anonimno).
 *
 * Filteri su u adresi: /search?search=mlijeko&categoryId=2. Adresa je izvor istine –
 * link sa početne stranice, refresh i back/forward daju isti rezultat. Polja forme samo
 * mijenjaju adresu, a promjena adrese učitava podatke.
 */
@Component({
  selector: 'app-search-products',
  standalone: false,
  templateUrl: './search-products.component.html',
  styleUrl: './search-products.component.scss',
})
export class SearchProductsComponent
  extends BaseListPagedComponent<ListProductsQueryDto, ListProductsRequest>
  implements OnInit
{
  private productsApi = inject(ProductsApiService);
  private categoriesApi = inject(ProductCategoriesApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  readonly searchControl = new FormControl('', { nonNullable: true });
  readonly categoryControl = new FormControl<number | null>(null);

  categories: ListProductCategoriesQueryDto[] = [];

  /** zahtjev koji je u toku – novi zahtjev ga otkazuje (stariji odgovor ne smije pregaziti noviji) */
  private loadSubscription?: Subscription;

  constructor() {
    super();
    this.request = new ListProductsRequest();
  }

  ngOnInit(): void {
    this.loadCategories();

    // 1) adresa → filteri → učitavanje
    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => {
        const search = params.get('search') ?? '';
        const categoryId = toPositiveInt(params.get('categoryId'));

        // emitEvent: false → ne pokreće valueChanges ispod (nema petlje adresa ↔ forma)
        this.searchControl.setValue(search, { emitEvent: false });
        this.categoryControl.setValue(categoryId, { emitEvent: false });

        this.request.search = search || null;
        this.request.categoryId = categoryId;
        this.request.paging.page = 1;
        this.loadPagedData();
      });

    // 2) kucanje u pretrazi → adresa (tek kad korisnik zastane 400 ms)
    this.searchControl.valueChanges
      .pipe(
        map((value) => value.trim()),
        debounceTime(400),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((search) => this.updateUrl({ search: search || null }));

    // 3) izbor kategorije → adresa
    this.categoryControl.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((categoryId) => this.updateUrl({ categoryId }));
  }

  protected loadPagedData(): void {
    this.startLoading();

    this.loadSubscription?.unsubscribe();
    this.loadSubscription = this.productsApi
      .list(this.request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.handlePageResult(response);
          this.stopLoading();
        },
        error: (err) => this.stopLoading(getErrorMessage(err))
      });
  }

  clearFilters(): void {
    this.updateUrl({ search: null, categoryId: null });
  }

  private loadCategories(): void {
    // dropdown treba SVE kategorije → allItemsPaging
    this.categoriesApi
      .list({ onlyEnabled: true, paging: allItemsPaging })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => (this.categories = response.items),
        // bez kategorija pretraga i dalje radi – filter samo ostaje prazan
        error: (err) => console.error('Load categories error:', err)
      });
  }

  /** null u queryParams uklanja parametar iz adrese. replaceUrl: kucanje ne puni historiju. */
  private updateUrl(queryParams: Params): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }
}

function toPositiveInt(value: string | null): number | null {
  const n = Number(value);
  return value && Number.isInteger(n) && n > 0 ? n : null;
}
