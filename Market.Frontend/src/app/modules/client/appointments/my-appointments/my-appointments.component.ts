import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup } from '@angular/forms';
import { ActivatedRoute, ParamMap, Params, Router } from '@angular/router';
import { catchError, EMPTY, Subject, switchMap, tap } from 'rxjs';
import { AppointmentsApiService } from '../../../../api-services/appointments/appointments-api.service';
import {
  AppointmentPeriod,
  AppointmentStatus,
  ListMyAppointmentsQueryDto,
  ListMyAppointmentsRequest
} from '../../../../api-services/appointments/appointments-api.model';
import { BaseListPagedComponent } from '../../../../core/components/base-classes/base-list-paged-component';
import { PageResult } from '../../../../core/models/paging/page-result';
import { getErrorMessage } from '../../../../core/interceptors/error-logging-interceptor.service';
import { PUBLIC_CATALOG_CONSTRAINTS } from '../../../public/public-catalog-constraints';
import { readPaging, urlValue } from '../../../public/public-catalog-query-params';
import { MY_APPOINTMENTS_CONSTRAINTS } from '../my-appointments-constraints';
import { readPeriod, readStatus } from '../my-appointments-query-params';

/** Control names = URL parameter names = API parameter names. */
interface MyAppointmentsFilterForm {
  period: FormControl<AppointmentPeriod>;
  /** null = all statuses */
  status: FormControl<AppointmentStatus | null>;
}

/**
 * "My appointments" (/client/appointments, Patient only): the logged-in patient's appointments
 * with a period filter (upcoming / past / all) and a status filter.
 *
 * The URL is the only source of truth (the same flow as /services):
 *   URL → queryParamMap → request (invalid values → defaults) → form (emitEvent: false) → load
 *   filter / page change → router.navigate (only the URL changes) → queryParamMap emits again → load
 * So F5, the Back button and the "Back to my appointments" button keep the filters and page.
 */
@Component({
  selector: 'app-my-appointments',
  standalone: false,
  templateUrl: './my-appointments.component.html',
  styleUrl: './my-appointments.component.scss'
})
export class MyAppointmentsComponent
  extends BaseListPagedComponent<ListMyAppointmentsQueryDto, ListMyAppointmentsRequest>
  implements OnInit
{
  private api = inject(AppointmentsApiService);
  private route = inject(ActivatedRoute);
  /** protected: the HTML passes router.url to the details page (see "listUrl") */
  protected router = inject(Router);
  private destroyRef = inject(DestroyRef);

  readonly limits = MY_APPOINTMENTS_CONSTRAINTS;

  /** period value → translation key for the button toggle */
  readonly periodOptions: { value: AppointmentPeriod; label: string }[] = [
    { value: 'upcoming', label: 'CLIENT.APPOINTMENTS.PERIOD.UPCOMING' },
    { value: 'past', label: 'CLIENT.APPOINTMENTS.PERIOD.PAST' },
    { value: 'all', label: 'CLIENT.APPOINTMENTS.PERIOD.ALL' }
  ];

  readonly filters = new FormGroup<MyAppointmentsFilterForm>({
    period: new FormControl<AppointmentPeriod>(this.limits.defaultPeriod, { nonNullable: true }),
    status: new FormControl<AppointmentStatus | null>(null)
  });

  /** every next() = "load the list for this.request" (see ngOnInit, step 1) */
  private readonly load$ = new Subject<void>();

  constructor() {
    super();
    this.request = new ListMyAppointmentsRequest();
  }

  /** true when a filter differs from the default (enables "Clear filters") */
  get hasActiveFilters(): boolean {
    return this.request.period !== this.limits.defaultPeriod || this.request.status != null;
  }

  ngOnInit(): void {
    // 1) load: switchMap cancels the previous HTTP request when a new one starts,
    //    so a slow old response can never overwrite the newer list
    this.load$
      .pipe(
        tap(() => this.startLoading()),
        switchMap(() =>
          this.api.listMy(this.request).pipe(
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
          { period: this.request.period, status: this.request.status ?? null },
          { emitEvent: false }
        );
        this.loadPagedData();
      });

    // 3) form → URL (a toggle and a select change once per click, so no debounce is needed)
    const { period, status } = this.filters.controls;

    period.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => this.applyFilters({ period: urlValue(value, this.limits.defaultPeriod) }));

    status.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => this.applyFilters({ status: urlValue(value) }));
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
    this.navigate({ pageSize: urlValue(size, PUBLIC_CATALOG_CONSTRAINTS.defaultPageSize), page: null });
  }

  /**
   * The page from the URL does not exist (e.g. page=99)
   * → go to the last page with replaceUrl, so the Back button does not return to page=99.
   */
  protected override handlePageResult(result: PageResult<ListMyAppointmentsQueryDto>): void {
    const lastPage = Math.max(1, result.totalPages);
    if (result.items.length === 0 && this.paging.page > lastPage) {
      this.navigate({ page: urlValue(lastPage, 1) }, true);
      return;
    }
    super.handlePageResult(result);
  }

  clearFilters(): void {
    // pageSize stays, everything else goes back to default
    this.navigate({ period: null, status: null, page: null });
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

  private readRequest(params: ParamMap): ListMyAppointmentsRequest {
    const request = new ListMyAppointmentsRequest();
    request.period = readPeriod(params);
    request.status = readStatus(params);
    request.paging = readPaging(params);
    return request;
  }
}
