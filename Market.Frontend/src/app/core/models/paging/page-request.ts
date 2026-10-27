/**
 * Pagination parameters for API requests.
 * Corresponds to: PageRequest.cs (backend: page >= 1, pageSize 1..1000)
 *
 * Default je 10 zapisa po stranici (mora biti jedna od opcija u fit-paginator-bar).
 * Za dropdown koji treba sve zapise koristi allItemsPaging iz paging-utils.ts.
 */
export class PageRequest {
  page: number;
  pageSize: number;

  constructor(page: number = 1, pageSize: number = 10) {
    this.page = page;
    this.pageSize = pageSize;
  }
}
