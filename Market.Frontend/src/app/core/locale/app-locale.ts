import { registerLocaleData } from '@angular/common';
import localeBs from '@angular/common/locales/bs';
import { Injectable } from '@angular/core';
import { MAT_NATIVE_DATE_FORMATS, MatDateFormats, NativeDateAdapter } from '@angular/material/core';

/**
 * Lokalizacija brojeva, datuma i valuta (DatePipe, DecimalPipe, CurrencyPipe, mat-datepicker).
 *
 * Aplikacija formatira uvijek po bosanskom (bs-BA): 1.234,56 KM, 19.10.2026
 * Jezik teksta (bs/en, TranslateService) se mijenja u toku rada, ali LOCALE_ID se u Angularu
 * zadaje samo jednom, pri pokretanju – zato formati ostaju bs-BA i kad je tekst na engleskom.
 *
 * Registracija i provideri su u app-module.ts.
 */
export const APP_LOCALE = 'bs-BA';

/** Učitava Angularove podatke za bosanski (nazivi mjeseci, decimalni zarez, "KM" za BAM). */
export function registerAppLocale(): void {
  registerLocaleData(localeBs, APP_LOCALE);
}

/** Format datuma u polju datepickera – isti kao {{ datum | date: 'dd.MM.yyyy' }} u tabelama. */
const INPUT_DATE_FORMAT = 'dd.MM.yyyy';

/**
 * Formati za mat-datepicker: polje prikazuje dd.MM.yyyy, ostalo (naziv mjeseca u kalendaru,
 * čitači ekrana) radi Intl po lokalu bs-BA.
 */
export const BS_DATE_FORMATS: MatDateFormats = {
  ...MAT_NATIVE_DATE_FORMATS,
  display: { ...MAT_NATIVE_DATE_FORMATS.display, dateInput: INPUT_DATE_FORMAT }
};

/**
 * mat-datepicker adapter za bosanski format datuma.
 *
 * NativeDateAdapter formatira preko Intl-a (bs-BA → "19. 10. 2026.", a browser bez podataka
 * za bs prikaže čak "2026-10-19"), a ručno upisan tekst parsira preko Date.parse(), koji ne
 * razumije "19.10.2026". Ovdje polje uvijek prikazuje i prihvata d.M.yyyy (sa ili bez razmaka
 * i završne tačke); sve ostalo radi NativeDateAdapter.
 */
@Injectable()
export class BsDateAdapter extends NativeDateAdapter {
  override parse(value: unknown, parseFormat?: unknown): Date | null {
    if (typeof value === 'string') {
      const match = /^\s*(\d{1,2})\.\s*(\d{1,2})\.\s*(\d{4})\.?\s*$/.exec(value);
      if (match) {
        const day = Number(match[1]);
        const month = Number(match[2]) - 1;
        const year = Number(match[3]);
        const date = new Date(year, month, day);
        // 31.02.2026 bi JS pretvorio u 03.03.2026 → takav unos je neispravan datum
        const sameDay =
          date.getFullYear() === year && date.getMonth() === month && date.getDate() === day;
        return sameDay ? date : this.invalid();
      }
    }
    return super.parse(value, parseFormat);
  }

  override format(date: Date, displayFormat: object | string): string {
    if (displayFormat === INPUT_DATE_FORMAT && this.isValid(date)) {
      const day = String(date.getDate()).padStart(2, '0');
      const month = String(date.getMonth() + 1).padStart(2, '0');
      return `${day}.${month}.${date.getFullYear()}`;
    }
    return super.format(date, displayFormat as object);
  }

  /** Sedmica u kalendaru počinje ponedjeljkom (neki browseri ne znaju to za bs-BA). */
  override getFirstDayOfWeek(): number {
    return 1;
  }
}
