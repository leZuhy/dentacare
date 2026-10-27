import { computed, Injectable, signal } from '@angular/core';
import { CART_MAX_QUANTITY, CartItem, CartProduct } from './cart.models';

/**
 * Korpa kupca: stanje u signalu, kopija u localStorage (ostaje nakon refresha).
 *
 * Komponente čitaju signale (items(), itemCount(), total()) i mijenjaju korpu samo kroz
 * metode ovog servisa. Stanje se nikad ne mijenja "u mjestu" – uvijek se postavlja novi niz,
 * pa signal javi promjenu svima koji ga čitaju (badge u headeru, stranica korpe, checkout).
 */
@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly STORAGE_KEY = 'market_cart_v1';

  private readonly _items = signal<CartItem[]>(this.readStorage());

  /** stavke korpe (readonly) */
  readonly items = this._items.asReadonly();

  /** broj različitih proizvoda (badge u headeru) */
  readonly itemCount = computed(() => this._items().length);

  /** ukupna količina svih stavki */
  readonly totalQuantity = computed(() =>
    this._items().reduce((sum, item) => sum + item.quantity, 0)
  );

  /** ukupno u KM po cijenama iz korpe (informativno – konačni iznos računa server) */
  readonly total = computed(() =>
    this._items().reduce((sum, item) => sum + item.price * item.quantity, 0)
  );

  readonly isEmpty = computed(() => this._items().length === 0);

  /** Dodaje proizvod; ako je već u korpi, povećava količinu. */
  add(product: CartProduct, quantity = 1): void {
    const existing = this._items().find((i) => i.productId === product.id);

    if (existing) {
      this.setQuantity(product.id, existing.quantity + quantity);
      return;
    }

    this.update([
      ...this._items(),
      {
        productId: product.id,
        name: product.name,
        price: product.price,
        quantity: clampQuantity(quantity),
        imageUrl: product.imageUrl ?? null
      }
    ]);
  }

  /** Postavlja tačnu količinu (1..999). */
  setQuantity(productId: number, quantity: number): void {
    this.update(
      this._items().map((item) =>
        item.productId === productId ? { ...item, quantity: clampQuantity(quantity) } : item
      )
    );
  }

  remove(productId: number): void {
    this.update(this._items().filter((item) => item.productId !== productId));
  }

  clear(): void {
    this.update([]);
  }

  private update(items: CartItem[]): void {
    this._items.set(items);
    try {
      localStorage.setItem(this.STORAGE_KEY, JSON.stringify(items));
    } catch {
      // localStorage nije dostupan ili je pun → korpa radi do refresha stranice
    }
  }

  /**
   * localStorage može sadržavati bilo šta (stara verzija aplikacije, ručna izmjena),
   * pa zadržavamo samo ispravne stavke.
   */
  private readStorage(): CartItem[] {
    try {
      const parsed: unknown = JSON.parse(localStorage.getItem(this.STORAGE_KEY) ?? '[]');
      if (!Array.isArray(parsed)) {
        return [];
      }
      return parsed
        .filter(
          (i): i is CartItem =>
            !!i &&
            Number.isInteger(i.productId) &&
            typeof i.name === 'string' &&
            typeof i.price === 'number' &&
            Number.isInteger(i.quantity)
        )
        .map((i) => ({ ...i, quantity: clampQuantity(i.quantity), imageUrl: i.imageUrl ?? null }));
    } catch {
      return [];
    }
  }
}

function clampQuantity(quantity: number): number {
  const whole = Math.trunc(Number(quantity));
  return Number.isFinite(whole) ? Math.min(Math.max(whole, 1), CART_MAX_QUANTITY) : 1;
}
