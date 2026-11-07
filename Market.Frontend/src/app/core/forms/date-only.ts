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
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

/** "yyyy-MM-dd" → Date (local midnight of that day) */
export function fromDateOnly(text: string): Date {
  const [year, month, day] = text.split("-").map(Number);
  return new Date(year, month - 1, day);
}
