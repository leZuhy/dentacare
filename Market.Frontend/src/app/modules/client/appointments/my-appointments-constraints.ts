import {
  AppointmentPeriod,
  AppointmentStatus
} from '../../../api-services/appointments/appointments-api.model';

/**
 * Allowed filter values of "My appointments" – the same as the validator of
 * ListMyAppointmentsQuery on the backend. If a value changes, change it in both places.
 */
export const MY_APPOINTMENTS_CONSTRAINTS = {
  defaultPeriod: 'upcoming' as AppointmentPeriod,
  periodValues: ['upcoming', 'past', 'all'] as readonly AppointmentPeriod[],
  statusValues: ['Scheduled', 'Cancelled', 'Completed'] as readonly AppointmentStatus[],
} as const;
