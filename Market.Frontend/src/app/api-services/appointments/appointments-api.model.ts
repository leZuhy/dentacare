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
