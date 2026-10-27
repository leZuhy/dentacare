import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { buildHttpParams } from '../../core/models/build-http-params';
import { GetCatalogHomeQueryDto, GetCatalogHomeRequest } from './catalog-api.model';

@Injectable({
  providedIn: 'root'
})
export class CatalogApiService {
  private readonly baseUrl = `${environment.apiUrl}/Catalog`;
  private http = inject(HttpClient);

  /**
   * GET /Catalog/home (anonimno)
   * Podaci za javnu početnu stranicu: promocije, kategorije, najnoviji proizvodi, brojke.
   */
  getHome(request?: GetCatalogHomeRequest): Observable<GetCatalogHomeQueryDto> {
    const params = request ? buildHttpParams(request) : undefined;
    return this.http.get<GetCatalogHomeQueryDto>(`${this.baseUrl}/home`, { params });
  }
}
