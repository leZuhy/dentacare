import { Component, inject } from '@angular/core';
import { CartService } from '../../../core/services/cart/cart.service';
import { CurrencyService } from '../../../core/services/currency/currency.service';

/**
 * Korpa (/cart, anonimno). Narudžba se pravi na /checkout, koji traži prijavu:
 * neprijavljen korisnik ide na login i vraća se na checkout (myAuthGuard + returnUrl).
 */
@Component({
  selector: 'app-cart-page',
  standalone: false,
  templateUrl: './cart-page.component.html',
  styleUrl: './cart-page.component.scss'
})
export class CartPageComponent {
  protected cart = inject(CartService);
  protected currency = inject(CurrencyService);
}
