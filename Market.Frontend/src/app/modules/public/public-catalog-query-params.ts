import { ParamMap } from '@angular/router';
import { PublicServiceSort } from '../../api-services/public-catalog/public-catalog-api.model';
import { PageRequest } from '../../core/models/paging/page-request';
import { PUBLIC_CATALOG_CONSTRAINTS as LIMITS } from './public-catalog-constraints';

/**
 * Reading filters from the URL (?search=...&categoryId=...&page=...).
 *
 * The URL can be typed or edited by anyone, so every value is checked here.
 * An invalid value (sort=abc, page=-1, categoryId=xyz, maxPriceBam=0) is replaced with
 * the default (null = "no filter"), so it is never sent to the server (which would return 400).
 */

/** the largest value of a C# int (ids and page numbers are int on the backend) */
const MAX_INT = 2_147_483_647;

/** Whole number > 0 (e.g. categoryId, page), otherwise null. */
export function readPositiveInt(params: ParamMap, key: string): number | null {
  const raw = params.get(key);
  // only digits: rejects "", "abc", "-1", "1.5", "1e3"
  if (raw === null || !/^\d+$/.test(raw)) return null;

  const value = Number(raw);
  return value >= 1 && value <= MAX_INT ? value : null;
}

/** Search text: trimmed, at most searchMaxLength characters, otherwise null. */
export function readSearch(params: ParamMap, key = 'search'): string | null {
  const value = params.get(key)?.trim() ?? '';
  if (value === '' || value.length > LIMITS.searchMaxLength) return null;
  return value;
}

/** Max price in KM: number in [maxPriceMin, maxPriceMax], otherwise null. */
export function readMaxPrice(params: ParamMap, key = 'maxPriceBam'): number | null {
  const raw = params.get(key);
  if (raw === null || raw.trim() === '') return null;

  // Number("100abc") is NaN, so text that is not a number is rejected
  const value = Number(raw);
  if (!Number.isFinite(value)) return null;
  return value >= LIMITS.maxPriceMin && value <= LIMITS.maxPriceMax ? value : null;
}

/** Sort: one of the allowed values, otherwise the default ("name"). */
export function readSort(params: ParamMap, key = 'sort'): PublicServiceSort {
  const raw = params.get(key) as PublicServiceSort | null;
  return raw !== null && LIMITS.sortValues.includes(raw) ? raw : LIMITS.defaultSort;
}

/** page (>= 1, default 1) and pageSize (one of the paginator options, default 10). */
export function readPaging(params: ParamMap): PageRequest {
  const page = readPositiveInt(params, 'page') ?? 1;
  const size = readPositiveInt(params, 'pageSize');
  const pageSize = size !== null && LIMITS.pageSizes.includes(size) ? size : LIMITS.defaultPageSize;
  return new PageRequest(page, pageSize);
}

/** Value for the URL: the default value is removed from the URL (null), so links stay short. */
export function urlValue<T>(value: T | null | undefined, defaultValue: T | null = null): T | null {
  return value === undefined || value === null || value === defaultValue ? null : value;
}
