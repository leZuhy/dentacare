import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { buildHttpParams } from '../../core/models/build-http-params';
import { CreatedResponse } from '../../core/models/created-response';
import {
  ConvertCurrencyQueryDto,
  ConvertCurrencyRequest,
  CreateExchangeRateCommand,
  ListCurrenciesQueryDto,
  ListExchangeRatesRequest,
  ListExchangeRatesResponse,
  RefreshExchangeRatesCommandDto
} from './currencies-api.model';

/**
 * Valute i kursna lista. Cijene i narudžbe su u KM; konverzija je samo za prikaz.
 * Za prikaz cijena u odabranoj valuti koristi CurrencyService + pipe "money", ne ovaj servis direktno.
 */
@Injectable({
  providedIn: 'root'
})
export class CurrenciesApiService {
  private readonly baseUrl = `${environment.apiUrl}/Currencies`;
  private http = inject(HttpClient);

  /**
   * GET /Currencies (anonimno) – omogućene valute sa kursom koji važi sada
   */
  list(): Observable<ListCurrenciesQueryDto[]> {
    return this.http.get<ListCurrenciesQueryDto[]>(this.baseUrl);
  }

  /**
   * GET /Currencies/convert?amount=&from=&to=&atUtc= (anonimno)
   */
  convert(request: ConvertCurrencyRequest): Observable<ConvertCurrencyQueryDto> {
    const params = buildHttpParams(request);
    return this.http.get<ConvertCurrencyQueryDto>(`${this.baseUrl}/convert`, { params });
  }

  /**
   * GET /Currencies/rates?currencyCode=&paging... (Admin)
   */
  listRates(request?: ListExchangeRatesRequest): Observable<ListExchangeRatesResponse> {
    const params = request ? buildHttpParams(request) : undefined;
    return this.http.get<ListExchangeRatesResponse>(`${this.baseUrl}/rates`, { params });
  }

  /**
   * POST /Currencies/rates (Admin) → 201 { id }
   */
  createRate(payload: CreateExchangeRateCommand): Observable<CreatedResponse> {
    return this.http.post<CreatedResponse>(`${this.baseUrl}/rates`, payload);
  }

  /**
   * DELETE /Currencies/rates/{id} (Admin) → 204
   */
  deleteRate(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/rates/${id}`);
  }

  /**
   * POST /Currencies/rates/refresh (Admin) – preuzima kurseve od vanjskog izvora (ECB).
   * 409 ako izvor nije uključen u konfiguraciji backenda (poruka kaže kako se uključuje).
   */
  refreshRates(): Observable<RefreshExchangeRatesCommandDto> {
    return this.http.post<RefreshExchangeRatesCommandDto>(`${this.baseUrl}/rates/refresh`, {});
  }
}
