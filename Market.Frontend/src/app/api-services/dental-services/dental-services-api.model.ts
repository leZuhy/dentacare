import { BasePagedQuery } from "../../core/models/paging/base-paged-query";
import { PageResult } from "../../core/models/paging/page-result";

// === QUERIES (READ) ===

/**
 * Query parameters for GET api/dental-services (Admin)
 * Corresponds to: ListDentalServiceQuery.cs
 */
export class ListDentalServiceRequest extends BasePagedQuery {
  /** part of the name, case-insensitive */
  search?: string | null;
  /** null = all categories */
  categoryId?: number | null;
  /** null = all, true = only active, false = only inactive */
  isActive?: boolean | null;
}

/**
 * Response item for GET api/dental-services
 * Corresponds to: ListDentalServiceQueryDto.cs
 */
export interface ListDentalServiceQueryDto {
  id: number;
  categoryId: number;
  categoryName: string;
  name: string;
  priceBam: number;
  durationMinutes: number;
  isActive: boolean;
}

export type ListDentalServiceResponse = PageResult<ListDentalServiceQueryDto>;

/**
 * Response for GET api/dental-services/{id}
 * Corresponds to: GetDentalServiceByIdQueryDto.cs
 */
export interface GetDentalServiceByIdQueryDto {
  id: number;
  categoryId: number;
  categoryName: string;
  name: string;
  description: string | null;
  priceBam: number;
  durationMinutes: number;
  isActive: boolean;
}

// === COMMANDS (WRITE) ===

/**
 * Body for POST api/dental-services
 * Corresponds to: CreateDentalServiceCommand.cs
 * (there is no isActive: a new service is always active)
 */
export interface CreateDentalServiceCommand {
  categoryId: number;
  name: string;
  description: string | null;
  priceBam: number;
  durationMinutes: number;
}

/**
 * Body for PUT api/dental-services/{id}
 * Corresponds to: UpdateDentalServiceCommand.cs
 * (the id goes in the URL, not in the body)
 */
export interface UpdateDentalServiceCommand {
  categoryId: number;
  name: string;
  description: string | null;
  priceBam: number;
  durationMinutes: number;
  isActive: boolean;
}
