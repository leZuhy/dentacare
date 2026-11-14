import { ParamMap } from '@angular/router';
import { fromDateOnly, toDateOnly } from '../../../core/forms/date-only';

/**
 * "yyyy-MM-dd" from the URL that is a real calendar day -> Date (local midnight), otherwise null.
 * The URL can be edited by anyone, so the value is checked before it is used
 * (same idea as public-catalog-query-params.ts).
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
