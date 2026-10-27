import { Component, inject } from '@angular/core';
import { CurrentUserService } from '../../../../core/services/auth/current-user.service';
import { CartService } from '../../../../core/services/cart/cart.service';

/**
 * Zaglavlje prodavnice: logo, pretraga, konvertor, korpa (badge), valuta, nalog.
 * Koriste ga PublicLayoutComponent (/, /search, /cart, ...) i ClientLayoutComponent (/client/...).
 */
@Component({
  selector: 'app-store-header',
  standalone: false,
  templateUrl: './store-header.component.html',
  styleUrl: './store-header.component.scss'
})
export class StoreHeaderComponent {
  protected currentUser = inject(CurrentUserService);
  protected cart = inject(CartService);
}
