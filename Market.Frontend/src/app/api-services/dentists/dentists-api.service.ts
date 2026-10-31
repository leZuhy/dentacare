import { inject, Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { buildHttpParams } from "../../core/models/build-http-params";
import { CreatedResponse } from "../../core/models/created-response";
import {
  CreateDentistCommand,
  GetDentistByIdQueryDto,
  ListDentistRequest,
  ListDentistResponse,
  UpdateDentistCommand,
} from "./dentists-api.model";

/**
 * Dentists (Admin only). One method per endpoint of DentistsController.
 */
@Injectable({
  providedIn: "root",
})
export class DentistsApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/dentists`;
  private http = inject(HttpClient);

  /** GET api/dentists?search=&specializationId=&paging... */
  list(request?: ListDentistRequest): Observable<ListDentistResponse> {
    const params = request ? buildHttpParams(request) : undefined;
    return this.http.get<ListDentistResponse>(this.baseUrl, { params });
  }

  /** GET api/dentists/{id} -> 404 if it does not exist */
  getById(id: number): Observable<GetDentistByIdQueryDto> {
    return this.http.get<GetDentistByIdQueryDto>(`${this.baseUrl}/${id}`);
  }

  /** POST api/dentists -> 201 { id } */
  create(payload: CreateDentistCommand): Observable<CreatedResponse> {
    return this.http.post<CreatedResponse>(this.baseUrl, payload);
  }

  /** PUT api/dentists/{id} -> 204 */
  update(id: number, payload: UpdateDentistCommand): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, payload);
  }

  /** DELETE api/dentists/{id} -> 204 */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
