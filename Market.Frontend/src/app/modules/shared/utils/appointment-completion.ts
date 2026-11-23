/**
 * true when the appointment has already started (start <= now), i.e. the dentist may complete it.
 *
 * Only for display (the browser clock can be wrong): the server checks the same rule
 * again (StartUtc <= now from TimeProvider -> otherwise 409). A pure function (no HTTP, no state).
 *
 * @param startUtc start of the appointment, UTC string from the server ("...Z")
 * @param nowMs current time in ms (default: now); a parameter so it can be tested
 */
export function hasAppointmentStarted(startUtc: string, nowMs: number = Date.now()): boolean {
  return Date.parse(startUtc) <= nowMs;
}
