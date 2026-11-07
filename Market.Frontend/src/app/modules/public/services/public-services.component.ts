import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, ParamMap, Params, Router } from '@angular/router';
import { catchError, debounceTime, EMPTY, filter, Subject, switchMap, tap } from 'rxjs';
import { PublicCatalogApiService } from '../../../api-services/public-catalog/public-catalog-api.service';
import {
  ListPublicServicesQueryDto,
  ListPublicServicesRequest,
  PublicLookupDto,
  PublicServiceSort
} from '../../../api-services/public-catalog/public-catalog-api.model';
import { BaseListPagedComponent } from '../../../core/components/base-classes/base-list-paged-component';
import { PageResult } from '../../../core/models/paging/page-result';
import { getErrorMessage } from '../../../core/interceptors/error-logging-interceptor.service';
import { PUBLIC_CATALOG_CONSTRAINTS } from '../public-catalog-constraints';
import {
  readMaxPrice,
  readPaging,
  readPositiveInt,
  readSearch,
  readSort,
  urlValue
} from '../public-catalog-query-params';

/** Control names = URL parameter names = API parameter names. */
interface ServicesFilterForm {
  search: FormControl<string>;
  categoryId: FormControl<number | null>;
  maxPriceBam: FormControl<number | null>;
  sort: FormControl<PublicServiceSort>;
}

/**
 * Public list of services (/services, anonymous): cards with search, category, max price and sort.
 *
 * The URL is the only source of truth:
 *   URL → queryParamMap → request (invalid values → defaults) → form (emitEvent: false) → load
 *   filter / page change → router.navigate (only the URL changes) → queryParamMap emits again → load
 * So F5, the Back button and a shared link show the same filters and page.
 */
@Component({
  selector: 'app-public-services',
  standalone: false,
  templateUrl: './public-services.component.html',
  styleUrl: './public-services.component.scss'
})
export class PublicServicesComponent
  extends BaseListPagedComponent<ListPublicServicesQueryDto, ListPublicServicesRequest>
  implements OnInit
{
  private api = inject(PublicCatalogApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  readonly limits = PUBLIC_CATALOG_CONSTRAINTS;

  /** sort value → translation key for the dropdown */
  readonly sortOptions: { value: PublicServiceSort; label: string }[] = [
    { value: 'name', label: 'PUBLIC.SERVICES.SORT_NAME' },
    { value: 'priceAsc', label: 'PUBLIC.SERVICES.SORT_PRICE_ASC' },
    { value: 'priceDesc', label: 'PUBLIC.SERVICES.SORT_PRICE_DESC' }
  ];

  readonly filters = new FormGroup<ServicesFilterForm>({
    search: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(this.limits.searchMaxLength)]
    }),
    /** null = all categories */
    categoryId: new FormControl<number | null>(null),
    /** null = no price limit */
    maxPriceBam: new FormControl<number | null>(null, [
      Validators.min(this.limits.maxPriceMin),
      Validators.max(this.limits.maxPriceMax)
    ]),
    sort: new FormControl<PublicServiceSort>(this.limits.defaultSort, { nonNullable: true })
  });

  /** options for the category dropdown */
  categories: PublicLookupDto[] = [];

  /** every next() = "load the list for this.request" (see ngOnInit, step 1) */
  private readonly load$ = new Subject<void>();

  constructor() {
    super();
    this.request = new ListPublicServicesRequest();
  }

  /** true when at least one filter is set (enables "Clear filters") */
  get hasActiveFilters(): boolean {
    const r = this.request;
    return !!r.search || r.categoryId != null || r.maxPriceBam != null || r.sort !== this.limits.defaultSort;
  }

  ngOnInit(): void {
    this.loadCategories();

    // 1) load: switchMap cancels the previous HTTP request when a new one starts,
    //    so a slow old response can never overwrite the newer list
    this.load$
      .pipe(
        tap(() => this.startLoading()),
        switchMap(() =>
          this.api.listServices(this.request).pipe(
            catchError((err) => {
              this.items = [];
              this.stopLoading(getErrorMessage(err));
              return EMPTY;
            })
          )
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((result) => {
        this.handlePageResult(result);
        this.stopLoading();
      });

    // 2) URL → request → form → load (also runs once at the start, for the initial URL)
    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => {
        this.request = this.readRequest(params);
        // fill the form WITHOUT valueChanges, otherwise step 3 would navigate again
        this.filters.setValue(
          {
            search: this.request.search ?? '',
            categoryId: this.request.categoryId ?? null,
            maxPriceBam: this.request.maxPriceBam ?? null,
            sort: this.request.sort
          },
          { emitEvent: false }
        );
        this.loadPagedData();
      });

    // 3) form → URL
    const { search, categoryId, maxPriceBam, sort } = this.filters.controls;

    // wait until the user stops typing, so we don't send a request for every letter.
    // "Distinct" is checked against this.request (= the URL), not with distinctUntilChanged:
    // distinctUntilChanged remembers the last TYPED value, but the form is also filled from
    // the URL (Back button) without events, so it would wrongly skip typing the same text again.
    search.valueChanges
      .pipe(
        debounceTime(300),
        filter(() => search.valid),
        filter((value) => (value.trim() || null) !== (this.request.search ?? null)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((value) => this.applyFilters({ search: value.trim() || null }));

    categoryId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => this.applyFilters({ categoryId: value }));

    // a number is also typed letter by letter ("1", "10", "100"), so it gets the same debounce
    maxPriceBam.valueChanges
      .pipe(
        tap(() => maxPriceBam.markAsTouched()), // show mat-error while typing, not only on blur
        debounceTime(300),
        filter(() => maxPriceBam.valid),
        filter((value) => (value ?? null) !== (this.request.maxPriceBam ?? null)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((value) => this.applyFilters({ maxPriceBam: value }));

    sort.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => this.applyFilters({ sort: urlValue(value, this.limits.defaultSort) }));
  }

  /** Load the list for the current request (also used by the "Try again" button). */
  protected loadPagedData(): void {
    this.load$.next();
  }

  /** Paging only changes the URL; the list is loaded in step 2 of ngOnInit. */
  override goToPage(page: number): void {
    if (page < 1 || (this.totalPages && page > this.totalPages)) return;
    this.navigate({ page: urlValue(page, 1) });
  }

  override changePageSize(size: number): void {
    this.navigate({ pageSize: urlValue(size, this.limits.defaultPageSize), page: null });
  }

  /**
   * The page from the URL does not exist (e.g. page=99, or fewer results than before)
   * → go to the last page with replaceUrl, so the Back button does not return to page=99
   * (it would redirect again and the user could never go back).
   */
  protected override handlePageResult(result: PageResult<ListPublicServicesQueryDto>): void {
    const lastPage = Math.max(1, result.totalPages);
    if (result.items.length === 0 && this.paging.page > lastPage) {
      this.navigate({ page: urlValue(lastPage, 1) }, true);
      return;
    }
    super.handlePageResult(result);
  }

  clearFilters(): void {
    // pageSize stays, everything else goes back to default
    this.navigate({ search: null, categoryId: null, maxPriceBam: null, sort: null, page: null });
  }

  /** A filter change always goes back to page 1 (null = removed from the URL). */
  private applyFilters(changes: Params): void {
    this.navigate({ ...changes, page: null });
  }

  /** 'merge' keeps the other parameters in the URL; null removes a parameter. */
  private navigate(queryParams: Params, replaceUrl = false): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: 'merge',
      replaceUrl
    });
  }

  private readRequest(params: ParamMap): ListPublicServicesRequest {
    const request = new ListPublicServicesRequest();
    request.search = readSearch(params);
    request.categoryId = readPositiveInt(params, 'categoryId');
    request.maxPriceBam = readMaxPrice(params);
    request.sort = readSort(params);
    request.paging = readPaging(params);
    return request;
  }

  private loadCategories(): void {
    this.api
      .listServiceCategories()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (categories) => (this.categories = categories),
        // the list still works without this filter, so the error is only logged by the interceptor
        error: () => (this.categories = [])
      });
  }
}
