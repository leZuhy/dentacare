import { BasePagedQuery } from "../../core/models/paging/base-paged-query";
import { PageResult } from "../../core/models/paging/page-result";

// === QUERIES (READ, anonymous) ===

/** Sort options for public services; sent to the API as text. */
export type PublicServiceSort = "name" | "priceAsc" | "priceDesc";

/**
 * Query parameters for GET api/public/services
 * Corresponds to: ListPublicServicesQuery.cs
 */
export class ListPublicServicesRequest extends BasePagedQuery {
  /** part of the service name, case-insensitive */
  search?: string | null;
  categoryId?: number | null;
  /** upper price limit in KM (BAM) */
  maxPriceBam?: number | null;
  sort: PublicServiceSort = "name";
}

/**
 * Response item for GET api/public/services (only active services)
 * Corresponds to: ListPublicServicesQueryDto.cs
 */
export interface ListPublicServicesQueryDto {
  id: number;
  name: string;
  description: string | null;
  categoryId: number;
  categoryName: string;
  priceBam: number;
  durationMinutes: number;
}

export type ListPublicServicesResponse = PageResult<ListPublicServicesQueryDto>;

/**
 * Query parameters for GET api/public/dentists
 * Corresponds to: ListPublicDentistsQuery.cs
 */
export class ListPublicDentistsRequest extends BasePagedQuery {
  /** part of the first name and last name */
  search?: string | null;
  specializationId?: number | null;
}

/**
 * Response item for GET api/public/dentists (no email)
 * Corresponds to: ListPublicDentistsQueryDto.cs
 */
export interface ListPublicDentistsQueryDto {
  id: number;
  firstname: string;
  lastname: string;
  specializationId: number;
  specializationName: string;
  bio: string | null;
}

export type ListPublicDentistsResponse = PageResult<ListPublicDentistsQueryDto>;

/**
 * Item for GET api/public/service-categories and GET api/public/specializations
 * (only the ones that are in use, for filter dropdowns)
 */
export interface PublicLookupDto {
  id: number;
  name: string;
}
