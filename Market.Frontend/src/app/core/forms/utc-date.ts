/**
 * Kalendarski datum (bez vremena) između mat-datepickera i API-ja.
 *
 * mat-datepicker vraća Date u LOKALNOJ ponoći odabranog dana, a API radi sa UTC vremenom
 * ("...Z"). Da isti dan ne "pobjegne" za jedan dan zbog vremenske zone, datum šaljemo kao
 * UTC ponoć tog kalendarskog dana:
 *
 *   odabrano 15.10.2026. → "2026-10-15T00:00:00.000Z"
 *
 * i obrnuto, "2026-10-15T00:00:00Z" sa servera → Date za 15.10.2026. u datepickeru.
 * Za prikaz takvih datuma koristi DatePipe sa zonom 'UTC': {{ d | date: 'dd.MM.yyyy' : 'UTC' }}
 */

/** Date iz datepickera → ISO string (UTC ponoć istog kalendarskog dana). null ostaje null. */
export function toUtcDateIso(date: Date | null | undefined): string | null {
  if (!date) {
    return null;
  }
  return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate())).toISOString();
}

/** ISO string sa servera → Date (lokalna ponoć istog UTC kalendarskog dana) za datepicker. */
export function fromUtcDateIso(iso: string | null | undefined): Date | null {
  if (!iso) {
    return null;
  }
  const utc = new Date(iso);
  if (Number.isNaN(utc.getTime())) {
    return null;
  }
  return new Date(utc.getUTCFullYear(), utc.getUTCMonth(), utc.getUTCDate());
}
