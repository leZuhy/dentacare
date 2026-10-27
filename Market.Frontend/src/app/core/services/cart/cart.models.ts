/**
 * Stavka korpe (čuva se u localStorage).
 *
 * name/price/imageUrl su kopija u trenutku dodavanja – samo za prikaz. Narudžba šalje samo
 * productId i quantity, a cijenu (i popuste) računa server po trenutnoj cijeni proizvoda.
 */
export interface CartItem {
  productId: number;
  name: string;
  /** cijena u KM u trenutku dodavanja */
  price: number;
  quantity: number;
  imageUrl: string | null;
}

/** Ono što je potrebno da se proizvod doda u korpu (odgovara DTO-ima proizvoda). */
export interface CartProduct {
  id: number;
  name: string;
  price: number;
  imageUrl?: string | null;
}

export const CART_MAX_QUANTITY = 999;
