/** One hour in milliseconds (JavaScript dates are counted in milliseconds). */
const HOUR_MS = 60 * 60 * 1000;

/**
 * true when less than minHours are left until the start of the appointment,
 * i.e. it can no longer be cancelled (or moved).
 *
 * Only for display (the browser clock can be wrong): the server checks the same rule
 * again (Clinic:CancellationMinHoursBefore -> 409). A pure function (no HTTP, no state),
 * so the patient's details page and the dentist's schedule use the same code.
 *
 * @param startUtc start of the appointment, UTC string from the server ("...Z")
 * @param minHours environment.clinic.cancellationMinHoursBefore
 * @param nowMs current time in ms (default: now); a parameter so it can be tested
 */
export function isTooLateToCancel(startUtc: string, minHours: number, nowMs: number = Date.now()): boolean {
  const msLeft = Date.parse(startUtc) - nowMs;
  return msLeft < minHours * HOUR_MS;
}
