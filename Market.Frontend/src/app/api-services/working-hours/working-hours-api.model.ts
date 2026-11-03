import { BasePagedQuery } from "../../core/models/paging/base-paged-query";
import { PageResult } from "../../core/models/paging/page-result";

/**
 * Day of the week as a number, same as .NET DayOfWeek (0 = Sunday, 1 = Monday ... 6 = Saturday).
 */
export type DayOfWeekNumber = 0 | 1 | 2 | 3 | 4 | 5 | 6;

/**
 * Order of days in dropdowns: Monday first, Sunday last.
 * The name of each day is the translation key WORKING_HOURS.DAYS.<number>.
 */
export const WORK_DAYS: readonly DayOfWeekNumber[] = [1, 2, 3, 4, 5, 6, 0];

// === QUERIES (READ) ===

/**
 * Query parameters for GET api/working-hours (Admin)
 * Corresponds to: ListWorkingHoursQuery.cs
 */
export class ListWorkingHoursRequest extends BasePagedQuery {
  /** null = all dentists */
  dentistId?: number | null;
  /** null = all days */
  dayOfWeek?: DayOfWeekNumber | null;
}

/**
 * Response item for GET api/working-hours
 * Corresponds to: ListWorkingHoursQueryDto.cs
 * (times are TimeOnly on the backend, so they arrive as "HH:mm:ss")
 */
export interface ListWorkingHoursQueryDto {
  id: number;
  dentistId: number;
  dentistFullName: string;
  dayOfWeek: DayOfWeekNumber;
  startTime: string;
  endTime: string;
}

export type ListWorkingHoursResponse = PageResult<ListWorkingHoursQueryDto>;

/**
 * Response for GET api/working-hours/{id}
 * Corresponds to: GetWorkingHoursByIdQueryDto.cs
 */
export interface GetWorkingHoursByIdQueryDto {
  id: number;
  dentistId: number;
  dentistFullName: string;
  dayOfWeek: DayOfWeekNumber;
  startTime: string;
  endTime: string;
}

// === COMMANDS (WRITE) ===

/**
 * Body for POST api/working-hours
 * Corresponds to: CreateWorkingHoursCommand.cs
 * (times are sent as "HH:mm:ss")
 */
export interface CreateWorkingHoursCommand {
  dentistId: number;
  dayOfWeek: DayOfWeekNumber;
  startTime: string;
  endTime: string;
}

/**
 * Body for PUT api/working-hours/{id}
 * Corresponds to: UpdateWorkingHoursCommand.cs
 * (the id goes in the URL; the dentist cannot be changed)
 */
export interface UpdateWorkingHoursCommand {
  dayOfWeek: DayOfWeekNumber;
  startTime: string;
  endTime: string;
}

/** "08:00:00" -> "08:00" (for display and for input type="time") */
export function toDisplayTime(time: string): string {
  return time.slice(0, 5);
}

/**
 * "08:00" -> "08:00:00" (the format the backend expects).
 * Seconds are added only when the value is "HH:mm" (5 characters),
 * so a value that already has seconds is sent unchanged.
 */
export function toApiTime(time: string): string {
  return time.length === 5 ? `${time}:00` : time;
}
