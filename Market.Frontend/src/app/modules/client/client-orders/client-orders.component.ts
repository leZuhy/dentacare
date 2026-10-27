import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { OrdersApiService } from '../../../api-services/orders/orders-api.service';
import { ListOrdersQueryDto, ListOrdersRequest } from '../../../api-services/orders/orders-api.models';
import { OrderStatusHelper } from '../../../api-services/orders/order-status.helper';
import { BaseListPagedComponent } from '../../../core/components/base-classes/base-list-paged-component';
import { CurrencyService } from '../../../core/services/currency/currency.service';
import { getErrorMessage } from '../../../core/interceptors/error-logging-interceptor.service';

/**
 * Moje narudžbe (/client/orders). Backend kupcu vraća samo njegove narudžbe.
 */
@Component({
  selector: 'app-client-orders',
  standalone: false,
  templateUrl: './client-orders.component.html',
  styleUrl: './client-orders.component.scss'
})
export class ClientOrdersComponent
  extends BaseListPagedComponent<ListOrdersQueryDto, ListOrdersRequest>
  implements OnInit
{
  private ordersApi = inject(OrdersApiService);
  private destroyRef = inject(DestroyRef);
  protected currency = inject(CurrencyService);

  /** naziv, boja i ikona statusa – na jednom mjestu za cijelu aplikaciju */
  protected readonly statusHelper = OrderStatusHelper;

  readonly displayedColumns = ['referenceNumber', 'orderedAt', 'status', 'totalAmount', 'actions'];

  constructor() {
    super();
    this.request = new ListOrdersRequest();
  }

  ngOnInit(): void {
    this.initList();
  }

  protected loadPagedData(): void {
    this.startLoading();

    this.ordersApi
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
}
