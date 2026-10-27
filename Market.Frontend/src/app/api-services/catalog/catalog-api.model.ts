// === QUERIES (READ) ===

/**
 * Query parameters for GET /Catalog/home
 * Corresponds to: GetCatalogHomeQuery.cs (backend ograničava: kategorije 1..50, proizvodi 1..100)
 */
export interface GetCatalogHomeRequest {
  categoryLimit?: number | null;
  productLimit?: number | null;
}

/**
 * Response for GET /Catalog/home (javna početna stranica)
 * Corresponds to: GetCatalogHomeQueryDto.cs
 */
export interface GetCatalogHomeQueryDto {
  /** omogućene kategorije, po nazivu */
  categories: GetCatalogHomeQueryDtoCategory[];
  /** najnoviji omogućeni proizvodi u omogućenim kategorijama */
  newestProducts: GetCatalogHomeQueryDtoProduct[];
  /** aktivne promocije unutar perioda važenja, po sortOrder */
  promotions: GetCatalogHomeQueryDtoPromotion[];
  stats: GetCatalogHomeQueryDtoStats;
}

export interface GetCatalogHomeQueryDtoCategory {
  id: number;
  name: string;
}

export interface GetCatalogHomeQueryDtoProduct {
  id: number;
  name: string;
  description: string | null;
  /** cijena u KM (bazna valuta) */
  price: number;
  categoryId: number;
  categoryName: string;
  /** relativna putanja, npr. "/uploads/products/{guid}.webp" (prikaz: pipe apiUrl) ili null */
  imageUrl: string | null;
}

export interface GetCatalogHomeQueryDtoPromotion {
  id: number;
  title: string;
  /** apsolutni http(s) URL slike */
  imageUrl: string;
  /** apsolutni http(s) URL ili ruta aplikacije ("/search?...") ili null */
  targetUrl: string | null;
  sortOrder: number;
}

export interface GetCatalogHomeQueryDtoStats {
  totalCategories: number;
  totalProducts: number;
}
