import { inject, Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { buildHttpParams } from "../../core/models/build-http-params";
import { CreatedResponse } from "../../core/models/created-response";
import {
  CreateWorkingHoursCommand,
  GetWorkingHoursByIdQueryDto,
  ListWorkingHoursRequest,
  ListWorkingHoursResponse,
  UpdateWorkingHoursCommand,
} from "./working-hours-api.model";

/**
 * Working hours of dentists (Admin only). One method per endpoint of WorkingHoursController.
 */
@Injectable({
  providedIn: "root",
})
export class WorkingHoursApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/working-hours`;
  private http = inject(HttpClient);

  /** GET api/working-hours?dentistId=&dayOfWeek=&paging... */
  list(request?: ListWorkingHoursRequest): Observable<ListWorkingHoursResponse> {
    const params = request ? buildHttpParams(request) : undefined;
    return this.http.get<ListWorkingHoursResponse>(this.baseUrl, { params });
  }

  /** GET api/working-hours/{id} -> 404 if it does not exist */
  getById(id: number): Observable<GetWorkingHoursByIdQueryDto> {
    return this.http.get<GetWorkingHoursByIdQueryDto>(`${this.baseUrl}/${id}`);
  }

  /** POST api/working-hours -> 201 { id } */
  create(payload: CreateWorkingHoursCommand): Observable<CreatedResponse> {
    return this.http.post<CreatedResponse>(this.baseUrl, payload);
  }

  /** PUT api/working-hours/{id} -> 204 */
  update(id: number, payload: UpdateWorkingHoursCommand): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, payload);
  }

  /** DELETE api/working-hours/{id} -> 204 */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
