import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { debounceTime, distinctUntilChanged, filter, switchMap, tap } from 'rxjs';
import { DentistsApiService } from '../../../../api-services/dentists/dentists-api.service';
import {
  ListDentistQueryDto,
  ListDentistRequest
} from '../../../../api-services/dentists/dentists-api.model';
import { SpecializationsApiService } from '../../../../api-services/specializations/specializations-api.service';
import {
  ListSpecializationQueryDto,
  ListSpecializationRequest
} from '../../../../api-services/specializations/specializations-api.model';
import { BaseListPagedComponent } from '../../../../core/components/base-classes/base-list-paged-component';
import { allItemsPaging } from '../../../../core/models/paging/paging-utils';
import { getErrorMessage } from '../../../../core/interceptors/error-logging-interceptor.service';
import { ToasterService } from '../../../../core/services/toaster.service';
import { DialogHelperService } from '../../../shared/services/dialog-helper.service';
import { DialogButton } from '../../../shared/models/dialog-config.model';

/**
 * Dentists list (/admin/dentists): filter by text and specialization, paging, delete.
 * Add and edit are on a separate page (DentistUpsertComponent).
 */
@Component({
  selector: 'app-dentists-list',
  standalone: false,
  templateUrl: './dentists-list.component.html',
  styleUrl: './dentists-list.component.scss'
})
export class DentistsListComponent
  extends BaseListPagedComponent<ListDentistQueryDto, ListDentistRequest>
  implements OnInit
{
  private api = inject(DentistsApiService);
  private specializationsApi = inject(SpecializationsApiService);
  private dialogHelper = inject(DialogHelperService);
  private toaster = inject(ToasterService);
  private translate = inject(TranslateService);
  private destroyRef = inject(DestroyRef);

  readonly displayedColumns = ['fullName', 'email', 'specializationName', 'actions'];

  readonly searchFilter = new FormControl<string>('', { nonNullable: true });
  /** null = all specializations */
  readonly specializationFilter = new FormControl<number | null>(null);

  /** options for the specialization dropdown */
  specializations: ListSpecializationQueryDto[] = [];

  constructor() {
    super();
    this.request = new ListDentistRequest();
  }

  ngOnInit(): void {
    this.initList();
    this.loadSpecializations();

    // wait until the user stops typing, so we don't send a request for every letter
    this.searchFilter.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((search) => {
        this.request.search = search.trim() || null;
        this.request.paging.page = 1;
        this.loadPagedData();
      });

    this.specializationFilter.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((specializationId) => {
        this.request.specializationId = specializationId;
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

  /** all specializations for the filter (one request, up to 1000 items) */
  private loadSpecializations(): void {
    const request = new ListSpecializationRequest();
    request.paging = allItemsPaging;

    this.specializationsApi
      .list(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => (this.specializations = response.items),
        // the list still works without the filter, so only show a message
        error: (err) => this.toaster.error(getErrorMessage(err))
      });
  }

  onDelete(dentist: ListDentistQueryDto): void {
    this.dialogHelper
      .confirm('DENTISTS.DELETE_TITLE', 'DENTISTS.DELETE_MESSAGE', {
        name: `${dentist.firstname} ${dentist.lastname}`
      })
      .pipe(
        filter((result) => result?.button === DialogButton.YES),
        tap(() => this.startLoading()),
        switchMap(() => this.api.delete(dentist.id))
      )
      .subscribe({
        next: () => {
          this.toaster.success(this.translate.instant('DENTISTS.DELETED'));
          this.loadPagedData();
        },
        error: (err) => {
          this.stopLoading();
          this.toaster.error(getErrorMessage(err));
        }
      });
  }
}
