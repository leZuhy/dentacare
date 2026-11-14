import { Pipe, PipeTransform } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { APP_LOCALE } from '../../../core/locale/app-locale';

/** What to show: only the time, only the date, or both. */
export type ClinicTimeFormat = 'time' | 'date' | 'dateTime';

/**
 * UTC time from the server ("2026-11-16T07:00:00Z") -> text in the CLINIC time zone.
 *
 *   {{ slot.startUtc | clinicTime }}             -> 08:00
 *   {{ slot.startUtc | clinicTime: 'date' }}     -> ponedjeljak, 16. novembar 2026.
 *   {{ slot.startUtc | clinicTime: 'dateTime' }} -> ponedjeljak, 16. novembar 2026. u 08:00
 *
 * Why not DatePipe: its time zone parameter is an offset ('+0100'), not a zone name,
 * so it would show the wrong hour after daylight saving time changes.
 * Clinic zone and not the browser's zone: the patient comes to the clinic at the clinic's time.
 *
 * Only for display. The value sent back to the server is always the original string.
 */
@Pipe({
  name: 'clinicTime',
  standalone: false,
})
export class ClinicTimePipe implements PipeTransform {
  private readonly timeZone = environment.clinic.timeZoneId;

  private readonly timeFormatter = new Intl.DateTimeFormat(APP_LOCALE, {
    timeZone: this.timeZone,
    hour: '2-digit',
    minute: '2-digit',
    hourCycle: 'h23',
  });

  private readonly dateFormatter = new Intl.DateTimeFormat(APP_LOCALE, {
    timeZone: this.timeZone,
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });

  transform(utc: string | null | undefined, format: ClinicTimeFormat = 'time'): string {
    if (!utc) {
      return '';
    }

    const moment = new Date(utc);
    if (Number.isNaN(moment.getTime())) {
      return '';
    }

    const time = this.timeFormatter.format(moment);
    const date = this.dateFormatter.format(moment);

    switch (format) {
      case 'date':
        return date;
      case 'dateTime':
        return `${date} u ${time}`;
      default:
        return time;
    }
  }
}
