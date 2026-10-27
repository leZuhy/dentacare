import { PageResult } from '../../models/paging/page-result';
import {BaseListComponent} from './base-list-component';
import {BasePagedQuery} from '../../models/paging/base-paged-query';

export abstract class BaseListPagedComponent<TItem, TRequest extends BasePagedQuery>
  extends BaseListComponent<TItem> {

  constructor() {
    super();
  }

  request!: TRequest;
  totalItems = 0;
  totalPages = 0;

  get paging(){
    return this.request.paging;
  }

  protected abstract loadPagedData(): void;

  protected override loadData(): void {
    this.loadPagedData();
  }

  protected handlePageResult(result: PageResult<TItem>) {
    this.items = result.items;
    this.totalItems = result.totalItems;
    this.totalPages = result.totalPages;

    // Tražena stranica više ne postoji (npr. obrisan je zadnji red na zadnjoj stranici)
    // → učitaj zadnju postojeću stranicu umjesto prikaza prazne tabele.
    const lastPage = Math.max(1, result.totalPages);
    if (result.items.length === 0 && this.paging.page > lastPage) {
      this.goToPage(lastPage);
    }
  }

  goToPage(page: number): void {
    if (page < 1 || (this.totalPages && page > this.totalPages)) return;
    this.paging.page = page;
    this.loadPagedData();
  }

  nextPage() { this.goToPage(this.paging.page + 1); }
  prevPage() { this.goToPage(this.paging.page - 1); }

  changePageSize(size: number) {
    this.paging.pageSize = size;
    this.paging.page = 1;
    this.loadPagedData();
  }
}
