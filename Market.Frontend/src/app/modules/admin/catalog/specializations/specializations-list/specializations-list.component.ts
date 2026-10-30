import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { debounceTime, distinctUntilChanged, filter, switchMap, tap } from 'rxjs';
import { SpecializationsApiService } from '../../../../../api-services/specializations/specializations-api.service';
import {
  ListSpecializationQueryDto,
  ListSpecializationRequest
} from '../../../../../api-services/specializations/specializations-api.model';
import { BaseListPagedComponent } from '../../../../../core/components/base-classes/base-list-paged-component';
import { getErrorMessage } from '../../../../../core/interceptors/error-logging-interceptor.service';
import { ToasterService } from '../../../../../core/services/toaster.service';
import { DialogHelperService } from '../../../../shared/services/dialog-helper.service';
import { DialogButton } from '../../../../shared/models/dialog-config.model';

/**
 * Specializations list (/admin/catalog/specializations): filter by name, paging, delete.
 * Add and edit are on a separate page (SpecializationUpsertComponent).
 */
@Component({
  selector: 'app-specializations-list',
  standalone: false,
  templateUrl: './specializations-list.component.html',
  styleUrl: './specializations-list.component.scss'
})
export class SpecializationsListComponent
  extends BaseListPagedComponent<ListSpecializationQueryDto, ListSpecializationRequest>
  implements OnInit
{
  private api = inject(SpecializationsApiService);
  private dialogHelper = inject(DialogHelperService);
  private toaster = inject(ToasterService);
  private translate = inject(TranslateService);
  private destroyRef = inject(DestroyRef);

  readonly displayedColumns = ['name', 'actions'];

  readonly searchFilter = new FormControl<string>('', { nonNullable: true });

  constructor() {
    super();
    this.request = new ListSpecializationRequest();
  }

  ngOnInit(): void {
    this.initList();

    // wait until the user stops typing, so we don't send a request for every letter
    this.searchFilter.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((search) => {
        this.request.search = search.trim() || null;
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

  onDelete(specialization: ListSpecializationQueryDto): void {
    this.dialogHelper
      .confirm('SPECIALIZATIONS.DELETE_TITLE', 'SPECIALIZATIONS.DELETE_MESSAGE', {
        name: specialization.name
      })
      .pipe(
        filter((result) => result?.button === DialogButton.YES),
        tap(() => this.startLoading()),
        switchMap(() => this.api.delete(specialization.id))
      )
      .subscribe({
        next: () => {
          this.toaster.success(this.translate.instant('SPECIALIZATIONS.DELETED'));
          this.loadPagedData();
        },
        error: (err) => {
          this.stopLoading();
          this.toaster.error(getErrorMessage(err));
        }
      });
  }
}
