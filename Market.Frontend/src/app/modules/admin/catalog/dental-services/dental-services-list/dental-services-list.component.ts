import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { DentalServicesApiService } from '../../../../../api-services/dental-services/dental-services-api.service';
import {
  ListDentalServiceQueryDto,
  ListDentalServiceRequest
} from '../../../../../api-services/dental-services/dental-services-api.model';
import { ServiceCategoriesApiService } from '../../../../../api-services/service-categories/service-categories-api.service';
import {
  ListServiceCategoryQueryDto,
  ListServiceCategoryRequest
} from '../../../../../api-services/service-categories/service-categories-api.model';
import { BaseListPagedComponent } from '../../../../../core/components/base-classes/base-list-paged-component';
import { allItemsPaging } from '../../../../../core/models/paging/paging-utils';
import { getErrorMessage } from '../../../../../core/interceptors/error-logging-interceptor.service';
import { ToasterService } from '../../../../../core/services/toaster.service';

/**
 * Dental services list (/admin/catalog/dental-services): filter by name, category and status, paging.
 * Add and edit are on a separate page (DentalServiceUpsertComponent).
 * There is no delete: a service is turned off in the edit form.
 */
@Component({
  selector: 'app-dental-services-list',
  standalone: false,
  templateUrl: './dental-services-list.component.html',
  styleUrl: './dental-services-list.component.scss'
})
export class DentalServicesListComponent
  extends BaseListPagedComponent<ListDentalServiceQueryDto, ListDentalServiceRequest>
  implements OnInit
{
  private api = inject(DentalServicesApiService);
  private categoriesApi = inject(ServiceCategoriesApiService);
  private toaster = inject(ToasterService);
  private destroyRef = inject(DestroyRef);

  readonly displayedColumns = ['name', 'categoryName', 'priceBam', 'durationMinutes', 'isActive', 'actions'];

  readonly searchFilter = new FormControl<string>('', { nonNullable: true });
  /** null = all categories */
  readonly categoryFilter = new FormControl<number | null>(null);
  /** null = all, true = only active, false = only inactive */
  readonly statusFilter = new FormControl<boolean | null>(null);

  /** options for the category dropdown */
  categories: ListServiceCategoryQueryDto[] = [];

  constructor() {
    super();
    this.request = new ListDentalServiceRequest();
  }

  ngOnInit(): void {
    this.initList();
    this.loadCategories();

    // wait until the user stops typing, so we don't send a request for every letter
    this.searchFilter.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((search) => {
        this.request.search = search.trim() || null;
        this.request.paging.page = 1;
        this.loadPagedData();
      });

    this.categoryFilter.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((categoryId) => {
        this.request.categoryId = categoryId;
        this.request.paging.page = 1;
        this.loadPagedData();
      });

    this.statusFilter.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((isActive) => {
        this.request.isActive = isActive;
        this.request.paging.page = 1;
        this.loadPagedData();
      });
  }

  protected loadPagedData(): void {
    this.startLoading();

    this.api
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

  /** all categories for the filter (one request, up to 1000 items) */
  private loadCategories(): void {
    const request = new ListServiceCategoryRequest();
    request.paging = allItemsPaging;

    this.categoriesApi
      .list(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => (this.categories = response.items),
        // the list still works without the filter, so only show a message
        error: (err) => this.toaster.error(getErrorMessage(err))
      });
  }
}
