import { BasePagedQuery } from "../../core/models/paging/base-paged-query";
import { PageResult } from "../../core/models/paging/page-result";

// === QUERIES (READ) ===

/**
 * Query parameters for GET api/specializations (Admin)
 * Corresponds to: ListSpecializationQuery.cs
 */

export class ListSpecializationRequest extends BasePagedQuery {
  /** part of the name, case-insensitive */
  search?: string | null;
}

/**
 * Response item for GET api/specializations
 * Corresponds to: ListSpecializationQueryDto.cs
 */

export interface ListSpecializationQueryDto {
  id: number;
  name: string;
}

export type ListSpecializationResponse = PageResult<ListSpecializationQueryDto>;

/**
 * Response for GET api/specializations/{id}
 * Corresponds to: GetSpecializationByIdQueryDto.cs
 */
export interface GetSpecializationByIdQueryDto {
  id: number;
  name: string;
}

// === COMMANDS (WRITE) ===

/**
 * Body for POST api/specializations and PUT api/specializations/{id}
 * Corresponds to: CreateSpecializationCommand.cs / UpdateSpecializationCommand.cs
 * (the id for PUT goes in the URL, not in the body)
 */

export interface UpsertSpecializationCommand {
  name: string;
}
