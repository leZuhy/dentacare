import { ParamMap } from '@angular/router';
import {
  AppointmentPeriod,
  AppointmentStatus
} from '../../../api-services/appointments/appointments-api.model';
import { MY_APPOINTMENTS_CONSTRAINTS as LIMITS } from './my-appointments-constraints';

/**
 * Reading the "My appointments" filters from the URL (?period=past&status=Completed).
 *
 * Same idea as public-catalog-query-params.ts: the URL can be edited by anyone, so an unknown
 * value (period=abc, status=xyz) is replaced with the default and never sent to the server
 * (which would return 400). page/pageSize are read with readPaging from that file.
 */

/** Period: one of the allowed values, otherwise the default ("upcoming"). */
export function readPeriod(params: ParamMap, key = 'period'): AppointmentPeriod {
  const raw = params.get(key) as AppointmentPeriod | null;
  return raw !== null && LIMITS.periodValues.includes(raw) ? raw : LIMITS.defaultPeriod;
}

/** Status: one of the allowed values (exact letter case, as the server returns it), otherwise null = all. */
export function readStatus(params: ParamMap, key = 'status'): AppointmentStatus | null {
  const raw = params.get(key) as AppointmentStatus | null;
  return raw !== null && LIMITS.statusValues.includes(raw) ? raw : null;
}
