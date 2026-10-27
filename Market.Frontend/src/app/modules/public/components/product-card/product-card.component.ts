import { Component, inject, Input } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { CartService } from '../../../../core/services/cart/cart.service';
import { ToasterService } from '../../../../core/services/toaster.service';

/**
 * Podaci za karticu proizvoda – odgovaraju i proizvodu sa /Catalog/home i sa /Products.
 */
export interface ProductCardItem {
  id: number;
  name: string;
  description?: string | null;
  /** cijena u KM */
  price: number;
  categoryName: string;
  imageUrl: string | null;
  /** nepoznato (undefined) → dugme "Dodaj u korpu" je uvijek omogućeno */
  stockQuantity?: number;
}

/**
 * Kartica proizvoda (početna stranica, pretraga): slika, naziv, cijena u odabranoj valuti,
 * dodavanje u korpu.
 */
@Component({
  selector: 'app-product-card',
  standalone: false,
  templateUrl: './product-card.component.html',
  styleUrl: './product-card.component.scss'
})
export class ProductCardComponent {
  @Input({ required: true }) product!: ProductCardItem;

  private cart = inject(CartService);
  private toaster = inject(ToasterService);
  private translate = inject(TranslateService);

  get outOfStock(): boolean {
    return this.product.stockQuantity !== undefined && this.product.stockQuantity <= 0;
  }

  addToCart(): void {
    this.cart.add(this.product);
    this.toaster.success(
      this.translate.instant('CART.ADDED', { name: this.product.name }),
      2000
    );
  }
}
