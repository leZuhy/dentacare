import { BasePagedQuery } from '../../core/models/paging/base-paged-query';
import { PageResult } from '../../core/models/paging/page-result';

/**
 * Status of an appointment, exactly as the server returns it (enum name of AppointmentStatus.cs).
 * The same value is sent back as a filter: the backend accepts enum names in any letter case.
 */
export type AppointmentStatus = 'Scheduled' | 'Cancelled' | 'Completed';

/**
 * Role of the user who cancelled the appointment, exactly as the server returns it
 * (enum name of Role.cs).
 */
export type CancelledByRole = 'Patient' | 'Dentist' | 'Admin';

/** Which appointments to list: from now on, before now, or all (backend default: "upcoming"). */
export type AppointmentPeriod = 'upcoming' | 'past' | 'all';

// === COMMANDS (WRITE, Patient only) ===

/**
 * Body for POST api/appointments
 * Corresponds to: CreateAppointmentCommand.cs
 * There is no patientId: the backend takes the patient from the access token.
 */
export interface CreateAppointmentCommand {
  dentistId: number;
  dentalServiceId: number;
  /** start of a free slot, exactly as GET api/public/free-slots returned it (UTC, ends with "Z") */
  startUtc: string;
}

/**
 * Response of POST api/appointments (201), shown on the confirmation card.
 * Corresponds to: CreateAppointmentCommandDto.cs
 */
export interface CreateAppointmentCommandDto {
  id: number;
  startUtc: string;
  endUtc: string;
  dentistFullName: string;
  dentalServiceName: string;
  priceBam: number;
}

/**
 * Body for POST api/appointments/{id}/reschedule (204, no response body)
 * Corresponds to: RescheduleAppointmentCommand.cs
 * The appointment id goes into the route, not into the body.
 * Only the time changes: same dentist, same service, same duration.
 */
export interface RescheduleAppointmentCommand {
  /** new start, exactly as GET api/appointments/{id}/free-slots returned it (UTC, ends with "Z") */
  startUtc: string;
}

/**
 * Body for POST api/appointments/{id}/complete (204, no response body, Dentist only)
 * Corresponds to: CompleteAppointmentCommand.cs
 * The appointment id goes into the route, not into the body.
 * Limits: MEDICAL_RECORD_CONSTRAINTS (= MedicalRecordEntity.Constraints on the backend).
 */
export interface CompleteAppointmentCommand {
  /** required, not only spaces */
  findings: string;
  /** optional; null = no therapy */
  therapy: string | null;
  /** extra performed services; each id is one procedure, the same id may repeat; [] = none */
  additionalDentalServiceIds: number[];
}

// === QUERIES (READ, Patient only, only the patient's own appointments) ===

/**
 * Query for GET api/appointments/{id}/free-slots?fromDate=&toDate=
 * Corresponds to: ListRescheduleFreeSlotsQuery.cs
 * The id goes into the route. Dentist and duration are taken from the appointment on the server.
 * Not paged: max 14 days (BOOKING_CONSTRAINTS.maxRangeDays).
 */
export interface ListRescheduleFreeSlotsRequest {
  /** first day, local clinic date "yyyy-MM-dd" (DateOnly on the backend) */
  fromDate: string;
  /** last day, included, "yyyy-MM-dd"; null = same day as fromDate */
  toDate?: string | null;
}

/**
 * One free slot for moving the appointment.
 * Corresponds to: ListRescheduleFreeSlotsQueryDto.cs
 * UTC strings ending with "Z"; startUtc is sent back to .../reschedule exactly as received.
 */
export interface ListRescheduleFreeSlotsQueryDto {
  startUtc: string;
  endUtc: string;
}

/**
 * Query for GET api/appointments/my?period=&status=&paging.page=&paging.pageSize=
 * Corresponds to: ListMyAppointmentsQuery.cs
 * There is no patientId: the backend takes the patient from the access token.
 */
export class ListMyAppointmentsRequest extends BasePagedQuery {
  period: AppointmentPeriod = 'upcoming';
  /** null = all statuses (the parameter is not sent) */
  status?: AppointmentStatus | null;
}

/**
 * Response item for GET api/appointments/my
 * Corresponds to: ListMyAppointmentsQueryDto.cs
 */
export interface ListMyAppointmentsQueryDto {
  id: number;
  /** UTC, ends with "Z" */
  startUtc: string;
  endUtc: string;
  status: AppointmentStatus;
  dentistFullName: string;
  dentalServiceNames: string[];
  /** sum of the copied prices (PriceAtTimeBam), without the loyalty discount */
  totalPriceBam: number;
}

export type ListMyAppointmentsResponse = PageResult<ListMyAppointmentsQueryDto>;

/**
 * Response of GET api/appointments/my/{id}
 * Corresponds to: GetMyAppointmentByIdQueryDto.cs
 */
export interface GetMyAppointmentByIdQueryDto {
  id: number;
  startUtc: string;
  endUtc: string;
  status: AppointmentStatus;
  /** UTC; filled only for a cancelled appointment */
  cancelledAtUtc: string | null;
  /** who cancelled it; null while the appointment is not cancelled */
  cancelledByRole: CancelledByRole | null;
  dentistFullName: string;
  dentistSpecializationName: string;
  /** one row = one procedure (the same service can appear more than once) */
  services: GetMyAppointmentByIdServiceDto[];
  /** sum of priceAtTimeBam, without the loyalty discount (that goes on the invoice) */
  totalPriceBam: number;
}

/** One service of the appointment with the price copied at booking/completion time. */
export interface GetMyAppointmentByIdServiceDto {
  name: string;
  priceAtTimeBam: number;
}

// === DENTIST SCHEDULE (READ, Dentist only, only the dentist's own appointments) ===

/**
 * Query for GET api/appointments/schedule?date=
 * Corresponds to: GetDentistScheduleQuery.cs
 * There is no dentistId: the backend takes the dentist from the access token.
 */
export interface GetDentistScheduleRequest {
  /** local clinic date "yyyy-MM-dd" (DateOnly on the backend); required (400 without it) */
  date: string;
}

/**
 * Response of GET api/appointments/schedule
 * Corresponds to: GetDentistScheduleQueryDto.cs
 */
export interface GetDentistScheduleQueryDto {
  /** the same day that was asked for, "yyyy-MM-dd" */
  date: string;
  /** sorted by start; empty = the dentist does not work that day */
  workingHours: DentistScheduleWorkingHoursDto[];
  /** sorted by startUtc; cancelled appointments are included too */
  appointments: DentistScheduleAppointmentDto[];
}

/**
 * One working hours interval, in LOCAL clinic time (TimeOnly on the backend, e.g. "08:00:00").
 * Corresponds to: DentistScheduleWorkingHoursDto.cs
 */
export interface DentistScheduleWorkingHoursDto {
  startTime: string;
  endTime: string;
}

/**
 * One appointment in the dentist's day.
 * Corresponds to: DentistScheduleAppointmentDto.cs
 */
export interface DentistScheduleAppointmentDto {
  id: number;
  /** UTC, ends with "Z" */
  startUtc: string;
  endUtc: string;
  status: AppointmentStatus;
  patientFullName: string;
  /** null when the patient has not entered a phone number */
  patientPhone: string | null;
  dentalServiceNames: string[];
  /** sum of the copied prices (PriceAtTimeBam), without the loyalty discount */
  totalPriceBam: number;
}
