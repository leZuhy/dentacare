import { inject, Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { buildHttpParams } from "../../core/models/build-http-params";
import { CreatedResponse } from "../../core/models/created-response";
import {
  CreateDentalServiceCommand,
  GetDentalServiceByIdQueryDto,
  ListDentalServiceRequest,
  ListDentalServiceResponse,
  UpdateDentalServiceCommand,
} from "./dental-services-api.model";

/**
 * Dental services (Admin only). One method per endpoint of DentalServicesController.
 * There is no delete: a service is turned off with isActive in the update.
 */
@Injectable({
  providedIn: "root",
})
export class DentalServicesApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/dental-services`;
  private http = inject(HttpClient);

  /** GET api/dental-services?search=&categoryId=&isActive=&paging... */
  list(request?: ListDentalServiceRequest): Observable<ListDentalServiceResponse> {
    const params = request ? buildHttpParams(request) : undefined;
    return this.http.get<ListDentalServiceResponse>(this.baseUrl, { params });
  }

  /** GET api/dental-services/{id} -> 404 if it does not exist */
  getById(id: number): Observable<GetDentalServiceByIdQueryDto> {
    return this.http.get<GetDentalServiceByIdQueryDto>(`${this.baseUrl}/${id}`);
  }

  /** POST api/dental-services -> 201 { id } */
  create(payload: CreateDentalServiceCommand): Observable<CreatedResponse> {
    return this.http.post<CreatedResponse>(this.baseUrl, payload);
  }

  /** PUT api/dental-services/{id} -> 204 */
  update(id: number, payload: UpdateDentalServiceCommand): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, payload);
  }
}
