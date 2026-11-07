import { PublicServiceSort } from '../../api-services/public-catalog/public-catalog-api.model';

/**
 * Public catalog filter limits – the same as the validators of
 * ListPublicServicesQuery / ListPublicDentistsQuery on the backend.
 * If a limit changes, change it in both places.
 */
export const PUBLIC_CATALOG_CONSTRAINTS = {
  searchMaxLength: 100,
  maxPriceMin: 0.01,
  maxPriceMax: 10000,
  /** the same options as the page size dropdown in fit-paginator-bar */
  pageSizes: [5, 10, 20, 50] as readonly number[],
  defaultPageSize: 10,
  defaultSort: 'name' as PublicServiceSort,
  sortValues: ['name', 'priceAsc', 'priceDesc'] as readonly PublicServiceSort[],
} as const;
