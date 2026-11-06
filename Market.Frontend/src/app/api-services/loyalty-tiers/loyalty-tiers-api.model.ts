import { BasePagedQuery } from "../../core/models/paging/base-paged-query";
import { PageResult } from "../../core/models/paging/page-result";

// === QUERIES (READ) ===

/**
 * Query parameters for GET api/loyalty-tiers (Admin)
 * Corresponds to: ListLoyaltyTierQuery.cs
 */

export class ListLoyaltyTierRequest extends BasePagedQuery {
  search?: string | null;
}

/**
 * Response item for GET api/loyalty-tiers
 * Corresponds to: ListLoyaltyTierQueryDto.cs
 */

export interface ListLoyaltyTierQueryDto {
  id: number;
  name: string;
  minPoints: number;
  discountPercent: number;
}

export type ListLoyaltyTierResponse = PageResult<ListLoyaltyTierQueryDto>;

/**
 * Response for GET api/loyalty-tiers/{id}
 * Corresponds to: GetLoyaltyTierByIdQueryDto.cs
 */

export interface GetLoyaltyTierByIdQueryDto {
  id: number;
  name: string;
  minPoints: number;
  discountPercent: number;
}

// === COMMANDS (WRITE) ===

/**
 * Body for POST api/loyalty-tiers and PUT api/loyalty-tiers/{id}
 * Corresponds to: CreateLoyaltyTierCommand.cs / UpdateLoyaltyTierCommand.cs
 * (the id for PUT goes in the URL, not in the body)
 */

export interface UpsertLoyaltyTierCommand {
  name: string;
  minPoints: number;
  discountPercent: number;
}
