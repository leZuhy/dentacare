import { inject, Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { buildHttpParams } from "../../core/models/build-http-params";
import {
  ListPublicDentistsRequest,
  ListPublicDentistsResponse,
  ListPublicServicesRequest,
  ListPublicServicesResponse,
  PublicLookupDto,
} from "./public-catalog-api.model";

/**
 * Public catalog (anonymous). One method per endpoint of PublicCatalogController.
 */
@Injectable({
  providedIn: "root",
})
export class PublicCatalogApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/public`;
  private http = inject(HttpClient);

  /** GET api/public/services?search=&categoryId=&maxPriceBam=&sort=&paging... */
  listServices(request: ListPublicServicesRequest): Observable<ListPublicServicesResponse> {
    return this.http.get<ListPublicServicesResponse>(`${this.baseUrl}/services`, {
      params: buildHttpParams(request),
    });
  }

  /** GET api/public/dentists?search=&specializationId=&paging... */
  listDentists(request: ListPublicDentistsRequest): Observable<ListPublicDentistsResponse> {
    return this.http.get<ListPublicDentistsResponse>(`${this.baseUrl}/dentists`, {
      params: buildHttpParams(request),
    });
  }

  /** GET api/public/service-categories -> categories with at least one active service */
  listServiceCategories(): Observable<PublicLookupDto[]> {
    return this.http.get<PublicLookupDto[]>(`${this.baseUrl}/service-categories`);
  }

  /** GET api/public/specializations -> specializations with at least one active dentist */
  listSpecializations(): Observable<PublicLookupDto[]> {
    return this.http.get<PublicLookupDto[]>(`${this.baseUrl}/specializations`);
  }
}
