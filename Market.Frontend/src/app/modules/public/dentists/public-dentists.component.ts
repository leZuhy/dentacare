import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, ParamMap, Params, Router } from '@angular/router';
import { catchError, debounceTime, EMPTY, filter, Subject, switchMap, tap } from 'rxjs';
import { PublicCatalogApiService } from '../../../api-services/public-catalog/public-catalog-api.service';
import {
  ListPublicDentistsQueryDto,
  ListPublicDentistsRequest,
  PublicLookupDto
} from '../../../api-services/public-catalog/public-catalog-api.model';
import { BaseListPagedComponent } from '../../../core/components/base-classes/base-list-paged-component';
import { PageResult } from '../../../core/models/paging/page-result';
import { getErrorMessage } from '../../../core/interceptors/error-logging-interceptor.service';
import { PUBLIC_CATALOG_CONSTRAINTS } from '../public-catalog-constraints';
import { readPaging, readPositiveInt, readSearch, urlValue } from '../public-catalog-query-params';

/** Control names = URL parameter names = API parameter names. */
interface DentistsFilterForm {
  search: FormControl<string>;
  specializationId: FormControl<number | null>;
}

/**
 * Public list of dentists (/dentists, anonymous): cards with search and specialization.
 * Works the same way as PublicServicesComponent: the URL is the only source of truth.
 */
@Component({
  selector: 'app-public-dentists',
  standalone: false,
  templateUrl: './public-dentists.component.html',
  styleUrl: './public-dentists.component.scss'
})
export class PublicDentistsComponent
  extends BaseListPagedComponent<ListPublicDentistsQueryDto, ListPublicDentistsRequest>
  implements OnInit
{
  private api = inject(PublicCatalogApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  readonly limits = PUBLIC_CATALOG_CONSTRAINTS;

  readonly filters = new FormGroup<DentistsFilterForm>({
    search: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(this.limits.searchMaxLength)]
    }),
    /** null = all specializations */
    specializationId: new FormControl<number | null>(null)
  });

  /** options for the specialization dropdown */
  specializations: PublicLookupDto[] = [];

  /** every next() = "load the list for this.request" (see ngOnInit, step 1) */
  private readonly load$ = new Subject<void>();

  constructor() {
    super();
    this.request = new ListPublicDentistsRequest();
  }

  /** true when at least one filter is set (enables "Clear filters") */
  get hasActiveFilters(): boolean {
    return !!this.request.search || this.request.specializationId != null;
  }

  ngOnInit(): void {
    this.loadSpecializations();

    // 1) load: switchMap cancels the previous HTTP request when a new one starts
    this.load$
      .pipe(
        tap(() => this.startLoading()),
        switchMap(() =>
          this.api.listDentists(this.request).pipe(
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

    // 2) URL → request → form (without events) → load
    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => {
        this.request = this.readRequest(params);
        this.filters.setValue(
          {
            search: this.request.search ?? '',
            specializationId: this.request.specializationId ?? null
          },
          { emitEvent: false }
        );
        this.loadPagedData();
      });

    // 3) form → URL
    const { search, specializationId } = this.filters.controls;

    // debounce: wait until the user stops typing; "distinct" is checked against the URL
    // (this.request), because the form is also filled from the URL without events
    search.valueChanges
      .pipe(
        debounceTime(300),
        filter(() => search.valid),
        filter((value) => (value.trim() || null) !== (this.request.search ?? null)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((value) => this.applyFilters({ search: value.trim() || null }));

    specializationId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => this.applyFilters({ specializationId: value }));
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

  /** The page from the URL does not exist → last page, replaceUrl so Back still works. */
  protected override handlePageResult(result: PageResult<ListPublicDentistsQueryDto>): void {
    const lastPage = Math.max(1, result.totalPages);
    if (result.items.length === 0 && this.paging.page > lastPage) {
      this.navigate({ page: urlValue(lastPage, 1) }, true);
      return;
    }
    super.handlePageResult(result);
  }

  clearFilters(): void {
    // pageSize stays, everything else goes back to default
    this.navigate({ search: null, specializationId: null, page: null });
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

  private readRequest(params: ParamMap): ListPublicDentistsRequest {
    const request = new ListPublicDentistsRequest();
    request.search = readSearch(params);
    request.specializationId = readPositiveInt(params, 'specializationId');
    request.paging = readPaging(params);
    return request;
  }

  private loadSpecializations(): void {
    this.api
      .listSpecializations()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (specializations) => (this.specializations = specializations),
        // the list still works without this filter
        error: () => (this.specializations = [])
      });
  }
}
