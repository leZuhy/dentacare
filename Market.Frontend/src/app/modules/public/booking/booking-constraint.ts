/**
 * Booking stepper limits.
 * MAX_RANGE_DAYS is the same as FreeSlotCalculator.MaxRangeDays on the backend
 * (GET api/public/free-slots accepts at most 14 days, both dates included).
 * If it changes, change it in both places.
 */
export const BOOKING_CONSTRAINTS = {
  /** Max days in one free-slots request (backend validator). */
  maxRangeDays: 14,
  /** How many days the stepper shows at once, starting from the picked date. */
  daysShown: 7,
} as const;
