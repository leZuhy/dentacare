import { Component, DestroyRef, inject, OnInit } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormControl } from "@angular/forms";
import { TranslateService } from "@ngx-translate/core";
import {
  debounceTime,
  distinctUntilChanged,
  filter,
  switchMap,
  tap,
} from "rxjs";
import { LoyaltyTiersApiService } from "../../../../../api-services/loyalty-tiers/loyalty-tiers-api.service";
import {
  ListLoyaltyTierQueryDto,
  ListLoyaltyTierRequest,
} from "../../../../../api-services/loyalty-tiers/loyalty-tiers-api.model";
import { BaseListPagedComponent } from "../../../../../core/components/base-classes/base-list-paged-component";
import { getErrorMessage } from "../../../../../core/interceptors/error-logging-interceptor.service";
import { ToasterService } from "../../../../../core/services/toaster.service";
import { DialogHelperService } from "../../../../shared/services/dialog-helper.service";
import { DialogButton } from "../../../../shared/models/dialog-config.model";

/**
 * Loyalty tiers list (/admin/loyalty/loyalty-tiers): filter by name, paging, delete.
 * Add and edit are on a separate page (LoyaltyTierUpsertComponent).
 */

@Component({
  selector: "app-loyalty-tiers-list",
  standalone: false,
  templateUrl: "./loyalty-tiers-list.component.html",
  styleUrl: "./loyalty-tiers-list.component.scss",
})
export class LoyaltyTiersListComponent
  extends BaseListPagedComponent<
    ListLoyaltyTierQueryDto,
    ListLoyaltyTierRequest
  >
  implements OnInit
{
  private api = inject(LoyaltyTiersApiService);
  private dialogHelper = inject(DialogHelperService);
  private toaster = inject(ToasterService);
  private translate = inject(TranslateService);
  private destroyRef = inject(DestroyRef);

  readonly displayedColumns = [
    "name",
    "minPoints",
    "discountPercent",
    "actions",
  ];

  readonly searchFilter = new FormControl<string>("", { nonNullable: true });

  constructor() {
    super();
    this.request = new ListLoyaltyTierRequest();
  }

  ngOnInit(): void {
    this.initList();

    this.searchFilter.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
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
        error: (err) => this.stopLoading(getErrorMessage(err)),
      });
  }

  onDelete(tier: ListLoyaltyTierQueryDto): void {
    this.dialogHelper
      .confirm("LOYALTY_TIERS.DELETE_TITLE", "LOYALTY_TIERS.DELETE_MESSAGE", {
        name: tier.name,
      })
      .pipe(
        filter((result) => result?.button === DialogButton.YES),
        tap(() => this.startLoading()),
        switchMap(() => this.api.delete(tier.id)),
      )
      .subscribe({
        next: () => {
          this.toaster.success(this.translate.instant("LOYALTY_TIERS.DELETED"));
          this.loadPagedData();
        },
        error: (err) => {
          this.stopLoading();
          this.toaster.error(getErrorMessage(err));
        },
      });
  }
}
