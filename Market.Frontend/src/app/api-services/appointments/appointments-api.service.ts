import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { buildHttpParams } from '../../core/models/build-http-params';
import {
  CompleteAppointmentCommand,
  CreateAppointmentCommand,
  CreateAppointmentCommandDto,
  GetDentistScheduleQueryDto,
  GetDentistScheduleRequest,
  GetMyAppointmentByIdQueryDto,
  ListMyAppointmentsRequest,
  ListMyAppointmentsResponse,
  ListRescheduleFreeSlotsQueryDto,
  ListRescheduleFreeSlotsRequest,
  RescheduleAppointmentCommand
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

  /**
   * GET api/appointments/schedule?date=yyyy-MM-dd -> one day of the logged-in dentist
   * (working hours + appointments, cancelled ones included). Dentist only (others get 403).
   * 400 when date is missing or invalid.
   */
  getDentistSchedule(date: string): Observable<GetDentistScheduleQueryDto> {
    const request: GetDentistScheduleRequest = { date };
    return this.http.get<GetDentistScheduleQueryDto>(`${this.baseUrl}/schedule`, {
      params: buildHttpParams(request),
    });
  }

  /**
   * POST api/appointments/{id}/cancel (no body) -> 204 (Patient or Dentist, own appointment only).
   * 404 when it does not exist OR belongs to someone else.
   * 409 when it is not Scheduled or starts in less than Clinic:CancellationMinHoursBefore hours.
   */
  cancel(id: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/cancel`, null);
  }

  /**
   * GET api/appointments/{id}/free-slots?fromDate=&toDate= -> free slots for moving
   * the logged-in patient's appointment (the appointment does not block itself).
   * 400 for invalid dates, 404 for a missing or someone else's appointment,
   * 409 when it is not Scheduled or starts in less than Clinic:CancellationMinHoursBefore hours.
   */
  getRescheduleFreeSlots(
    id: number,
    request: ListRescheduleFreeSlotsRequest
  ): Observable<ListRescheduleFreeSlotsQueryDto[]> {
    return this.http.get<ListRescheduleFreeSlotsQueryDto[]>(`${this.baseUrl}/${id}/free-slots`, {
      params: buildHttpParams(request),
    });
  }

  /**
   * POST api/appointments/{id}/reschedule -> 204 (Patient only, own appointment only).
   * 404 when it does not exist OR belongs to someone else.
   * 409 when the slot is no longer free, the patient is busy at that time, it is too late
   * (less than CancellationMinHoursBefore hours before the OLD start), etc. The message is in Bosnian.
   */
  reschedule(id: number, payload: RescheduleAppointmentCommand): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/reschedule`, payload);
  }

  /**
   * POST api/appointments/{id}/complete -> 204 (Dentist only, own appointment only).
   * 400 for invalid findings/therapy/service ids (field errors, see applyServerErrors).
   * 404 when it does not exist OR belongs to another dentist.
   * 409 when it is not Scheduled, has not started yet, or a chosen service is no longer active.
   */
  complete(id: number, payload: CompleteAppointmentCommand): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/complete`, payload);
  }
}
