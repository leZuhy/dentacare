import { inject, Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { buildHttpParams } from "../../core/models/build-http-params";
import { CreatedResponse } from "../../core/models/created-response";
import {
  GetSpecializationByIdQueryDto,
  ListSpecializationRequest,
  ListSpecializationResponse,
  UpsertSpecializationCommand,
} from "./specializations-api.model";

/**
 * Dentist specializations (Admin only). One method per endpoint of SpecializationsController.
 */
@Injectable({
  providedIn: "root",
})
export class SpecializationsApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/specializations`;
  private http = inject(HttpClient);

  /** GET api/specializations?search=&paging... */
  list(
    request?: ListSpecializationRequest,
  ): Observable<ListSpecializationResponse> {
    const params = request ? buildHttpParams(request) : undefined;
    return this.http.get<ListSpecializationResponse>(this.baseUrl, { params });
  }

  /** GET api/specializations/{id} -> 404 if it does not exist */
  getById(id: number): Observable<GetSpecializationByIdQueryDto> {
    return this.http.get<GetSpecializationByIdQueryDto>(
      `${this.baseUrl}/${id}`,
    );
  }

  /** POST api/specializations -> 201 { id } */
  create(payload: UpsertSpecializationCommand): Observable<CreatedResponse> {
    return this.http.post<CreatedResponse>(this.baseUrl, payload);
  }

  /** PUT api/specializations/{id} -> 204 */
  update(id: number, payload: UpsertSpecializationCommand): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, payload);
  }

  /** DELETE api/specializations/{id} -> 204 */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
