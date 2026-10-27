import { PageRequest } from './page-request';

/**
 * Utility functions for creating common paging configurations.
 *
 * Konstante ispod su zamrznute (Object.freeze) jer ih dijele svi koji ih koriste:
 * koristi ih samo za jednokratne pozive (npr. dropdown). Lista sa paginacijom mijenja
 * page/pageSize, pa treba svoj objekat: new PageRequest() ili createPaging().
 */

/**
 * Default paging (10 items per page).
 */
export const defaultPaging: Readonly<PageRequest> = Object.freeze(new PageRequest(1, 10));

/**
 * Paging for fetching all items (1000 items – backend maximum).
 * Use for dropdowns and small datasets.
 */
export const allItemsPaging: Readonly<PageRequest> = Object.freeze(new PageRequest(1, 1000));

/**
 * Large page size (100 items).
 */
export const largePaging: Readonly<PageRequest> = Object.freeze(new PageRequest(1, 100));

/**
 * Small page size (5 items).
 */
export const smallPaging: Readonly<PageRequest> = Object.freeze(new PageRequest(1, 5));

/**
 * Create a custom paging configuration.
 */
export function createPaging(page: number = 1, pageSize: number = 10): PageRequest {
  return new PageRequest(page, pageSize);
}
