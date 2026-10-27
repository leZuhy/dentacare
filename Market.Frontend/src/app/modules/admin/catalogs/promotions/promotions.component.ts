import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { debounceTime, distinctUntilChanged, filter, map, switchMap, tap } from 'rxjs';
import { PromotionsApiService } from '../../../../api-services/promotions/promotions-api.service';
import {
  ListPromotionsQueryDto,
  ListPromotionsRequest
} from '../../../../api-services/promotions/promotions-api.model';
import { BaseListPagedComponent } from '../../../../core/components/base-classes/base-list-paged-component';
import { getErrorMessage } from '../../../../core/interceptors/error-logging-interceptor.service';
import { ToasterService } from '../../../../core/services/toaster.service';
import { DialogHelperService } from '../../../shared/services/dialog-helper.service';
import { DialogButton } from '../../../shared/models/dialog-config.model';

/**
 * Promocije – lista (/admin/promotions): pretraga, filter aktivne/neaktivne, straničenje, brisanje.
 * Dodavanje i izmjena su na posebnoj stranici (PromotionUpsertComponent).
 */
@Component({
  selector: 'app-promotions',
  standalone: false,
  templateUrl: './promotions.component.html',
  styleUrl: './promotions.component.scss'
})
export class PromotionsComponent
  extends BaseListPagedComponent<ListPromotionsQueryDto, ListPromotionsRequest>
  implements OnInit
{
  private api = inject(PromotionsApiService);
  private dialogHelper = inject(DialogHelperService);
  private toaster = inject(ToasterService);
  private translate = inject(TranslateService);
  private destroyRef = inject(DestroyRef);

  readonly displayedColumns = ['image', 'title', 'isActive', 'period', 'sortOrder', 'actions'];

  readonly searchControl = new FormControl('', { nonNullable: true });
  /** null = sve, true = aktivne, false = neaktivne */
  readonly activeControl = new FormControl<boolean | null>(null);

  constructor() {
    super();
    this.request = new ListPromotionsRequest();
  }

  ngOnInit(): void {
    this.initList();

    this.searchControl.valueChanges
      .pipe(
        map((value) => value.trim()),
        debounceTime(400),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((search) => {
        this.request.search = search || null;
        this.goToFirstPage();
      });

    this.activeControl.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((isActive) => {
        this.request.isActive = isActive;
        this.goToFirstPage();
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

  onDelete(promotion: ListPromotionsQueryDto): void {
    this.dialogHelper
      .confirmDelete(promotion.title)
      .pipe(
        filter((result) => result?.button === DialogButton.DELETE),
        tap(() => this.startLoading()),
        switchMap(() => this.api.delete(promotion.id))
      )
      .subscribe({
        next: () => {
          this.toaster.success(this.translate.instant('PROMOTIONS.DELETED'));
          this.loadPagedData();
        },
        error: (err) => {
          this.stopLoading();
          this.toaster.error(getErrorMessage(err));
        }
      });
  }

  private goToFirstPage(): void {
    this.request.paging.page = 1;
    this.loadPagedData();
  }
}
