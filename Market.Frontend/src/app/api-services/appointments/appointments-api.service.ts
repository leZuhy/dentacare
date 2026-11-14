import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateAppointmentCommand, CreateAppointmentCommandDto } from './appointments-api.model';

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
}
