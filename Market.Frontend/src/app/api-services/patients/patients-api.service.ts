import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import {
  GetMyPatientProfileQueryDto,
  UpdateMyPatientProfileCommand,
} from "./patients-api.model";

/**
 * Patient's own data (Patient role only). One method per endpoint of PatientsController.
 */
@Injectable({
  providedIn: "root",
})
export class PatientsApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/patients`;
  private http = inject(HttpClient);

  /** GET /api/patients/me - phone and date of birth of the logged-in patient. */
  getMyProfile(): Observable<GetMyPatientProfileQueryDto> {
    return this.http.get<GetMyPatientProfileQueryDto>(`${this.baseUrl}/me`);
  }

  /** PUT /api/patients/me -> 204. */
  updateMyProfile(payload: UpdateMyPatientProfileCommand): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/me`, payload);
  }
}
