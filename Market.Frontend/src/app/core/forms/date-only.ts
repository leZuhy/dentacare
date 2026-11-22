import { ParamMap } from '@angular/router';

/**
 * Conversion between a JS Date (what mat-datepicker gives) and "yyyy-MM-dd" (DateOnly on the backend).
 *
 * Why not toISOString(): it converts to UTC first. In Sarajevo (UTC+1) midnight of 20.04.1995
 * is 19.04.1995 23:00 UTC, so the backend would get the previous day.
 * Here the date is built from local year/month/day, without any time zone.
 */

/** Date → "yyyy-MM-dd" */
export function toDateOnly(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

/** "yyyy-MM-dd" → Date (local midnight of that day) */
export function fromDateOnly(text: string): Date {
  const [year, month, day] = text.split('-').map(Number);
  return new Date(year, month - 1, day);
}

/**
 * "yyyy-MM-dd" from the URL that is a real calendar day -> Date (local midnight), otherwise null.
 * The URL can be edited by anyone, so the value is checked before it is used
 * (same idea as public-catalog-query-params.ts). Used by /book and /client/schedule.
 */
export function readDateOnly(params: ParamMap, key: string): Date | null {
  const raw = params.get(key);
  if (raw === null || !/^\d{4}-\d{2}-\d{2}$/.test(raw)) {
    return null;
  }

  const date = fromDateOnly(raw);
  // "2026-02-31" -> JavaScript quietly makes it 3 March; converting back shows it was not a real day
  return toDateOnly(date) === raw ? date : null;
}

/**
 * Formats a moment as "yyyy-MM-dd" in the given time zone.
 * The en-CA locale writes dates exactly as yyyy-MM-dd, so no manual padding is needed.
 */
function formatDateOnlyInZone(moment: Date, timeZoneId: string): string {
  return new Intl.DateTimeFormat('en-CA', {
    timeZone: timeZoneId,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).format(moment);
}

/**
 * Today in the clinic (not in the browser's zone), as local midnight of that day.
 * Used for mat-datepicker [min] and [max].
 */
export function clinicToday(timeZoneId: string): Date {
  return fromDateOnly(formatDateOnlyInZone(new Date(), timeZoneId));
}

/**
 * The clinic calendar day of a UTC time from the server, as "yyyy-MM-dd".
 * "2026-11-16T23:30:00Z" is already 17.11. in Sarajevo -> "2026-11-17".
 * Used to group free slots by day.
 */
export function utcToClinicDateOnly(utc: string, timeZoneId: string): string {
  return formatDateOnlyInZone(new Date(utc), timeZoneId);
}

/** date + n calendar days (local midnight). Month and year roll over by themselves. */
export function addDays(date: Date, days: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
}
