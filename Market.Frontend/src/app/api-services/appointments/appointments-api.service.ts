import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { buildHttpParams } from '../../core/models/build-http-params';
import {
  CreateAppointmentCommand,
  CreateAppointmentCommandDto,
  GetMyAppointmentByIdQueryDto,
  ListMyAppointmentsRequest,
  ListMyAppointmentsResponse
} from './appointments-api.model';

/**
 * Appointments. One method per endpoint of AppointmentsController.
 */
@Injectable({
  providedIn: 'root',
})
export class AppointmentsApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/appointments`;
  private http = inject(HttpClient);

  /**
   * POST api/appointments -> 201 + confirmation (Patient only).
   * 409 when the slot was taken in the meantime or the patient is busy at that time.
   */
  create(payload: CreateAppointmentCommand): Observable<CreateAppointmentCommandDto> {
    return this.http.post<CreateAppointmentCommandDto>(this.baseUrl, payload);
  }

  /**
   * GET api/appointments/my?period=&status=&paging... -> the logged-in patient's appointments.
   * 400 for an unknown period or status.
   */
  listMy(request: ListMyAppointmentsRequest): Observable<ListMyAppointmentsResponse> {
    return this.http.get<ListMyAppointmentsResponse>(`${this.baseUrl}/my`, {
      params: buildHttpParams(request),
    });
  }

  /**
   * GET api/appointments/my/{id} -> details of one appointment of the logged-in patient.
   * 404 when it does not exist OR belongs to someone else (the same answer, so nothing is revealed).
   */
  getMyById(id: number): Observable<GetMyAppointmentByIdQueryDto> {
    return this.http.get<GetMyAppointmentByIdQueryDto>(`${this.baseUrl}/my/${id}`);
  }
}
