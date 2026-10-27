import { PageResult } from '../../core/models/paging/page-result';
import { BasePagedQuery } from '../../core/models/paging/base-paged-query';

// === QUERIES (READ) ===

/**
 * Query parameters for GET /Products
 * Corresponds to: ListProductsQuery.cs
 *
 * Anonimni korisnik i kupac dobijaju samo omogućene proizvode u omogućenim kategorijama;
 * admin vidi sve.
 */
export class ListProductsRequest extends BasePagedQuery {
  /** pretraga po nazivu */
  search?: string | null;
  categoryId?: number | null;
}

/**
 * Response item for GET /Products
 * Corresponds to: ListProductsQueryDto.cs
 */
export interface ListProductsQueryDto {
  id: number;
  name: string;
  description?: string | null;
  /** cijena u KM (bazna valuta) */
  price: number;
  stockQuantity: number;
  categoryName: string;
  isEnabled: boolean;
  /** relativna putanja, npr. "/uploads/products/{guid}.webp" (prikaz: pipe apiUrl) ili null */
  imageUrl: string | null;
}

/**
 * Response for GET /Products/{id}
 * Corresponds to: GetProductByIdQueryDto.cs
 */
export interface GetProductByIdQueryDto {
  id: number;
  name: string;
  description?: string | null;
  price: number;
  stockQuantity: number;
  categoryName: string;
  categoryId: number;
  isEnabled: boolean;
  /** relativna putanja ili null */
  imageUrl: string | null;
}

/**
 * Paged response for GET /Products
 */
export type ListProductsResponse = PageResult<ListProductsQueryDto>;

/**
 * Query parameters for GET /Products/name-available (Admin)
 * Corresponds to: CheckProductNameAvailabilityQuery.cs
 */
export interface CheckProductNameRequest {
  name: string;
  /** kod izmjene: proizvod koji se ne računa (sam sebi nije duplikat) */
  excludeId?: number | null;
}

/**
 * Response for GET /Products/name-available
 */
export interface CheckProductNameResponse {
  available: boolean;
}

/**
 * Response for POST /Products/{id}/image
 * Corresponds to: UploadProductImageCommandDto.cs
 */
export interface UploadProductImageResponse {
  /** nova relativna putanja slike */
  imageUrl: string;
}

// === COMMANDS (WRITE) ===

/**
 * Command for POST /Products
 * Corresponds to: CreateProductCommand.cs
 */
export interface CreateProductCommand {
  name: string;
  description?: string | null;
  price: number;
  /** cijeli broj ≥ 0 */
  stockQuantity: number;
  categoryId: number;
  isEnabled: boolean;
}

/**
 * Command for PUT /Products/{id}
 * Corresponds to: UpdateProductCommand.cs
 */
export interface UpdateProductCommand {
  name: string;
  description?: string | null;
  price: number;
  /** cijeli broj ≥ 0 */
  stockQuantity: number;
  categoryId: number;
  isEnabled: boolean;
}
