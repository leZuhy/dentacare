import { BasePagedQuery } from "../../core/models/paging/base-paged-query";
import { PageResult } from "../../core/models/paging/page-result";

// === QUERIES (READ) ===

/**
 * Query parameters for GET api/service-categories (Admin)
 * Corresponds to: ListServiceCategoryQuery.cs
 */

export class ListServiceCategoryRequest extends BasePagedQuery {
  /** part of the name, case-insensitive */
  search?: string | null;
}

/**
 * Response item for GET api/service-categories
 * Corresponds to: ListServiceCategoryQueryDto.cs
 */

export interface ListServiceCategoryQueryDto {
  id: number;
  name: string;
}

export type ListServiceCategoryResponse = PageResult<ListServiceCategoryQueryDto>;

/**
 * Response for GET api/service-categories/{id}
 * Corresponds to: GetServiceCategoryByIdQueryDto.cs
 */
export interface GetServiceCategoryByIdQueryDto {
  id: number;
  name: string;
}

// === COMMANDS (WRITE) ===

/**
 * Body for POST api/service-categories and PUT api/service-categories/{id}
 * Corresponds to: CreateServiceCategoryCommand.cs / UpdateServiceCategoryCommand.cs
 * (the id for PUT goes in the URL, not in the body)
 */

export interface UpsertServiceCategoryCommand {
  name: string;
}
