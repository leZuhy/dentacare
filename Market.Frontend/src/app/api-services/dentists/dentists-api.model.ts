import { BasePagedQuery } from "../../core/models/paging/base-paged-query";
import { PageResult } from "../../core/models/paging/page-result";

// === QUERIES (READ) ===

/**
 * Query parameters for GET api/dentists (Admin)
 * Corresponds to: ListDentistQuery.cs
 */

export class ListDentistRequest extends BasePagedQuery {
  /** part of the first name, last name or email, case-insensitive */
  search?: string | null;
  /** null = all specializations */
  specializationId?: number | null;
}

/**
 * Response item for GET api/dentists
 * Corresponds to: ListDentistQueryDto.cs
 */

export interface ListDentistQueryDto {
  id: number;
  firstname: string;
  lastname: string;
  email: string;
  specializationName: string;
}

export type ListDentistResponse = PageResult<ListDentistQueryDto>;

/**
 * Response for GET api/dentists/{id}
 * Corresponds to: GetDentistByIdQueryDto.cs
 */
export interface GetDentistByIdQueryDto {
  id: number;
  firstname: string;
  lastname: string;
  email: string;
  specializationId: number;
  specializationName: string;
  bio?: string | null;
}

// === COMMANDS (WRITE) ===

/**
 * Body for POST api/dentists
 * Corresponds to: CreateDentistCommand.cs
 * (creates the user account too, so email and password are needed only here)
 */
export interface CreateDentistCommand {
  firstname: string;
  lastname: string;
  email: string;
  password: string;
  specializationId: number;
  bio?: string | null;
}

/**
 * Body for PUT api/dentists/{id}
 * Corresponds to: UpdateDentistCommand.cs
 * (the id goes in the URL; email and password cannot be changed here)
 */
export interface UpdateDentistCommand {
  firstname: string;
  lastname: string;
  specializationId: number;
  bio?: string | null;
}
