import { Component, inject } from '@angular/core';
import { CartService } from '../../../../core/services/cart/cart.service';
import { CART_MAX_QUANTITY, CartItem } from '../../../../core/services/cart/cart.models';

/**
 * Lista stavki korpe sa izmjenom količine i uklanjanjem.
 * Koriste je stranica korpe (/cart) i prvi korak checkouta (/checkout).
 * Stanje je u CartService (signal), pa obje stranice uvijek prikazuju istu korpu.
 */
@Component({
  selector: 'app-cart-items',
  standalone: false,
  templateUrl: './cart-items.component.html',
  styleUrl: './cart-items.component.scss'
})
export class CartItemsComponent {
  protected cart = inject(CartService);
  protected readonly maxQuantity = CART_MAX_QUANTITY;

  increment(item: CartItem): void {
    this.cart.setQuantity(item.productId, item.quantity + 1);
  }

  decrement(item: CartItem): void {
    this.cart.setQuantity(item.productId, item.quantity - 1);
  }

  /** Upisana količina; servis je ograniči na 1..999. */
  onQuantityChange(item: CartItem, input: HTMLInputElement): void {
    const quantity = input.valueAsNumber; // NaN za prazno polje
    if (Number.isFinite(quantity)) {
      this.cart.setQuantity(item.productId, quantity);
    }
    // nevažeći unos (prazno, 0, 5000) → polje prikazuje stvarnu količinu iz korpe.
    // Ručno, jer [value] se ne osvježi kad je količina u korpi ostala ista kao prije.
    const current = this.cart.items().find((i) => i.productId === item.productId);
    input.value = String(current?.quantity ?? item.quantity);
  }

  remove(item: CartItem): void {
    this.cart.remove(item.productId);
  }
}
