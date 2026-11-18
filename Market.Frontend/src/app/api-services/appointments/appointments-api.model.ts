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

// === QUERIES (READ, Patient only, only the patient's own appointments) ===

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
