import { inject, Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { buildHttpParams } from "../../core/models/build-http-params";
import { CreatedResponse } from "../../core/models/created-response";
import {
  GetLoyaltyTierByIdQueryDto,
  ListLoyaltyTierRequest,
  ListLoyaltyTierResponse,
  UpsertLoyaltyTierCommand,
} from "./loyalty-tiers-api.model";

/**
 * Loyalty tiers (Admin only). One method per endpoint of LoyaltyTiersController.
 */

@Injectable({
  providedIn: "root",
})
export class LoyaltyTiersApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/loyalty-tiers`;
  private http = inject(HttpClient);
  /** GET list */

  list(request?: ListLoyaltyTierRequest): Observable<ListLoyaltyTierResponse> {
    const params = request ? buildHttpParams(request) : undefined;
    return this.http.get<ListLoyaltyTierResponse>(this.baseUrl, { params });
  }

  /** GET id */
  getById(id: number): Observable<GetLoyaltyTierByIdQueryDto> {
    return this.http.get<GetLoyaltyTierByIdQueryDto>(`${this.baseUrl}/${id}`);
  }

  /**POST */
  create(payload: UpsertLoyaltyTierCommand): Observable<CreatedResponse> {
    return this.http.post<CreatedResponse>(this.baseUrl, payload);
  }

  /**PUT */
  update(id: number, payload: UpsertLoyaltyTierCommand): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, payload);
  }

  /**DELETE */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
