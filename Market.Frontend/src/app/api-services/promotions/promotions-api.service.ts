import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { buildHttpParams } from '../../core/models/build-http-params';
import { CreatedResponse } from '../../core/models/created-response';
import {
  GetPromotionByIdQueryDto,
  ListPromotionsRequest,
  ListPromotionsResponse,
  UpsertPromotionCommand
} from './promotions-api.model';

/**
 * Promocije (baneri na početnoj stranici) – svi endpointi traže Admin ulogu.
 */
@Injectable({
  providedIn: 'root'
})
export class PromotionsApiService {
  private readonly baseUrl = `${environment.apiUrl}/Promotions`;
  private http = inject(HttpClient);

  /**
   * GET /Promotions
   */
  list(request?: ListPromotionsRequest): Observable<ListPromotionsResponse> {
    const params = request ? buildHttpParams(request) : undefined;
    return this.http.get<ListPromotionsResponse>(this.baseUrl, { params });
  }

  /**
   * GET /Promotions/{id}
   */
  getById(id: number): Observable<GetPromotionByIdQueryDto> {
    return this.http.get<GetPromotionByIdQueryDto>(`${this.baseUrl}/${id}`);
  }

  /**
   * POST /Promotions → 201 { id }
   */
  create(payload: UpsertPromotionCommand): Observable<CreatedResponse> {
    return this.http.post<CreatedResponse>(this.baseUrl, payload);
  }

  /**
   * PUT /Promotions/{id} → 204
   */
  update(id: number, payload: UpsertPromotionCommand): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, payload);
  }

  /**
   * DELETE /Promotions/{id} → 204
   */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
