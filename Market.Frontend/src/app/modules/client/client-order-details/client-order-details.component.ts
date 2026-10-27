import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { OrdersApiService } from '../../../api-services/orders/orders-api.service';
import { GetOrderByIdQueryDto } from '../../../api-services/orders/orders-api.models';
import { OrderStatusHelper } from '../../../api-services/orders/order-status.helper';
import { BaseComponent } from '../../../core/components/base-classes/base-component';
import { CurrencyService } from '../../../core/services/currency/currency.service';
import { getErrorMessage } from '../../../core/interceptors/error-logging-interceptor.service';

/**
 * Detalji narudžbe kupca (/client/orders/:id). Backend vraća 404 za tuđu narudžbu.
 */
@Component({
  selector: 'app-client-order-details',
  standalone: false,
  templateUrl: './client-order-details.component.html',
  styleUrl: './client-order-details.component.scss'
})
export class ClientOrderDetailsComponent extends BaseComponent implements OnInit {
  private ordersApi = inject(OrdersApiService);
  private route = inject(ActivatedRoute);
  private destroyRef = inject(DestroyRef);
  protected currency = inject(CurrencyService);

  protected readonly statusHelper = OrderStatusHelper;

  order: GetOrderByIdQueryDto | null = null;

  ngOnInit(): void {
    // paramMap (a ne snapshot): ista komponenta se ponovo koristi ako se promijeni samo :id
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      this.loadOrder(Number(params.get('id')));
    });
  }

  loadOrder(id: number): void {
    this.startLoading();
    this.order = null;

    this.ordersApi
      .getById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (order) => {
          this.order = order;
          this.stopLoading();
        },
        error: (err) => this.stopLoading(getErrorMessage(err))
      });
  }

  retry(): void {
    this.loadOrder(Number(this.route.snapshot.paramMap.get('id')));
  }
}
