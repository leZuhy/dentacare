import { BasePagedQuery } from '../../core/models/paging/base-paged-query';
import { PageResult } from '../../core/models/paging/page-result';

// === QUERIES (READ) ===

/**
 * Query parameters for GET /Promotions (Admin)
 * Corresponds to: ListPromotionsQuery.cs
 */
export class ListPromotionsRequest extends BasePagedQuery {
  /** pretraga po naslovu */
  search?: string | null;
  /** true = samo aktivne, false = samo neaktivne, null = sve */
  isActive?: boolean | null;
}

/**
 * Response item for GET /Promotions (sortirano po sortOrder, pa id)
 * Corresponds to: ListPromotionsQueryDto.cs
 */
export interface ListPromotionsQueryDto {
  id: number;
  title: string;
  imageUrl: string;
  targetUrl: string | null;
  isActive: boolean;
  /** ISO string (UTC) ili null = bez ograničenja */
  startsAtUtc: string | null;
  /** ISO string (UTC) ili null = bez ograničenja */
  endsAtUtc: string | null;
  sortOrder: number;
}

/**
 * Response for GET /Promotions/{id} (isti oblik kao stavka liste)
 * Corresponds to: GetPromotionByIdQueryDto.cs
 */
export type GetPromotionByIdQueryDto = ListPromotionsQueryDto;

/**
 * Paged response for GET /Promotions
 */
export type ListPromotionsResponse = PageResult<ListPromotionsQueryDto>;

// === COMMANDS (WRITE) ===

/**
 * Command for POST /Promotions and PUT /Promotions/{id}
 * Corresponds to: CreatePromotionCommand.cs / UpdatePromotionCommand.cs
 */
export interface UpsertPromotionCommand {
  title: string;
  imageUrl: string;
  targetUrl?: string | null;
  isActive: boolean;
  /** ISO string (UTC) ili null */
  startsAtUtc?: string | null;
  /** ISO string (UTC) ili null; ako su oba datuma zadana, mora biti nakon startsAtUtc */
  endsAtUtc?: string | null;
  sortOrder: number;
}
