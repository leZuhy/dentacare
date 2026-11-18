import { addDays, toDateOnly, utcToClinicDateOnly } from '../../../core/forms/date-only';

/** Anything that has a UTC start (public free slots, reschedule free slots). */
export interface SlotWithStart {
  /** UTC, ends with "Z" */
  startUtc: string;
}

/** One day in a slot list. Days without free slots are included too (slots = []). */
export interface SlotDay<TSlot extends SlotWithStart> {
  /** local midnight of the clinic calendar day */
  date: Date;
  slots: TSlot[];
}

/**
 * One SlotDay for every day from fromDate to toDate (both included, empty ones too),
 * then every slot goes into the day it belongs to IN THE CLINIC
 * ("2026-11-16T23:30:00Z" is already 17.11. in Sarajevo).
 * Slots outside the range are ignored.
 *
 * A pure function (no HTTP, no state), so the same code serves every slot picker.
 */
export function groupSlotsByDay<TSlot extends SlotWithStart>(
  fromDate: Date,
  toDate: Date,
  slots: TSlot[],
  timeZoneId: string
): SlotDay<TSlot>[] {
  const byDay = new Map<string, SlotDay<TSlot>>();

  for (let day = fromDate; day <= toDate; day = addDays(day, 1)) {
    byDay.set(toDateOnly(day), { date: day, slots: [] });
  }

  for (const slot of slots) {
    const key = utcToClinicDateOnly(slot.startUtc, timeZoneId);
    byDay.get(key)?.slots.push(slot);
  }

  return Array.from(byDay.values());
}
