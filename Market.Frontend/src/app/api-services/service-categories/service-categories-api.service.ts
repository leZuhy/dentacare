import { inject, Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { buildHttpParams } from "../../core/models/build-http-params";
import { CreatedResponse } from "../../core/models/created-response";
import {
  GetServiceCategoryByIdQueryDto,
  ListServiceCategoryRequest,
  ListServiceCategoryResponse,
  UpsertServiceCategoryCommand,
} from "./service-categories-api.model";

/**
 * Dental service categories (Admin only). One method per endpoint of ServiceCategoriesController.
 */
@Injectable({
  providedIn: "root",
})
export class ServiceCategoriesApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/service-categories`;
  private http = inject(HttpClient);

  /** GET api/service-categories?search=&paging... */
  list(
    request?: ListServiceCategoryRequest,
  ): Observable<ListServiceCategoryResponse> {
    const params = request ? buildHttpParams(request) : undefined;
    return this.http.get<ListServiceCategoryResponse>(this.baseUrl, { params });
  }

  /** GET api/service-categories/{id} -> 404 if it does not exist */
  getById(id: number): Observable<GetServiceCategoryByIdQueryDto> {
    return this.http.get<GetServiceCategoryByIdQueryDto>(
      `${this.baseUrl}/${id}`,
    );
  }

  /** POST api/service-categories -> 201 { id } */
  create(payload: UpsertServiceCategoryCommand): Observable<CreatedResponse> {
    return this.http.post<CreatedResponse>(this.baseUrl, payload);
  }

  /** PUT api/service-categories/{id} -> 204 */
  update(id: number, payload: UpsertServiceCategoryCommand): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, payload);
  }

  /** DELETE api/service-categories/{id} -> 204 */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
