import { AbstractControl, FormGroupDirective, NgForm, ValidationErrors, ValidatorFn } from '@angular/forms';
import { ErrorStateMatcher } from '@angular/material/core';

/** Najmanja dužina lozinke – isto pravilo kao na backendu (registracija, nova lozinka). */
export const PASSWORD_MIN_LENGTH = 8;

/**
 * Validatori koje Angular nema (dopuna za Validators.required, Validators.min, ...).
 *
 * Svaki validator vraća null kad je vrijednost ispravna ili PRAZNA – obaveznost se
 * zadaje posebno sa Validators.required (isto kao Angularovi validatori).
 * Poruke za ove greške su u core/forms/validation-messages.ts (ključevi VALIDATION.*).
 *
 * Primjer:
 *   sortOrder: new FormControl(0, { nonNullable: true,
 *     validators: [Validators.required, Validators.min(0), AppValidators.integer] })
 */
export class AppValidators {
  /** Cijeli broj (bez decimala). Greška: { integer: true } */
  static integer(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    if (isEmpty(value)) {
      return null;
    }
    return Number.isInteger(Number(value)) ? null : { integer: true };
  }

  /** Apsolutni http(s) URL, npr. "https://example.com/slika.jpg". Greška: { url: true } */
  static absoluteHttpUrl(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    if (isEmpty(value)) {
      return null;
    }
    return isAbsoluteHttpUrl(String(value)) ? null : { url: true };
  }

  /**
   * Apsolutni http(s) URL ili ruta aplikacije koja počinje sa "/" (npr. "/search?categoryId=2").
   * "//nesto" nije ruta nego adresa drugog servera. Greška: { urlOrPath: true }
   */
  static httpUrlOrAppPath(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    if (isEmpty(value)) {
      return null;
    }
    const text = String(value).trim();
    const isAppPath = text.startsWith('/') && !text.startsWith('//') && !text.includes('\\');
    return isAppPath || isAbsoluteHttpUrl(text) ? null : { urlOrPath: true };
  }

  /** Najviše `max` decimala (npr. kurs sa 6 decimala). Greška: { maxDecimals: { max } } */
  static maxDecimals(max: number): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const value = control.value;
      if (isEmpty(value) || Number.isNaN(Number(value))) {
        return null;
      }
      // decimale brojimo iz tekstualnog zapisa ("1.955830" → 6); eksponent (1e-7) nije dozvoljen
      const text = String(value);
      if (/e/i.test(text)) {
        return { maxDecimals: { max } };
      }
      const decimals = text.includes('.') ? text.split('.')[1].replace(/0+$/, '').length : 0;
      return decimals <= max ? null : { maxDecimals: { max } };
    };
  }

  /**
   * CROSS-FIELD validator – ide na FormGroup, ne na jedno polje, jer poredi DVA polja:
   * kraj (endKey) mora biti NAKON početka (startKey). Ako jedno polje nije popunjeno → ispravno.
   * Greška je na grupi: { dateRange: true }. Da se prikaže ispod polja "kraj", polje koristi
   * CrossFieldErrorStateMatcher (ispod).
   *
   * Primjer:
   *   this.form = new FormGroup({ startsAtUtc: ..., endsAtUtc: ... },
   *     { validators: AppValidators.dateRange('startsAtUtc', 'endsAtUtc') });
   */
  static dateRange(startKey: string, endKey: string): ValidatorFn {
    return (group: AbstractControl): ValidationErrors | null => {
      const start = group.get(startKey)?.value as Date | null | undefined;
      const end = group.get(endKey)?.value as Date | null | undefined;
      if (!(start instanceof Date) || !(end instanceof Date)) {
        return null;
      }
      return end.getTime() > start.getTime() ? null : { dateRange: true };
    };
  }

  /**
   * Lozinka po pravilu backenda: najmanje PASSWORD_MIN_LENGTH znakova, bar jedno slovo i bar
   * jedna cifra (\p{L} = bilo koje slovo, i č, ć, š...). Server provjerava isto pravilo –
   * ovdje je samo da korisnik grešku vidi odmah. Greška: { password: { minLength } }
   */
  static password(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    // bez trim-a: razmaci su dio lozinke
    if (value === null || value === undefined || value === '') {
      return null;
    }
    const text = String(value);
    const valid = text.length >= PASSWORD_MIN_LENGTH && /\p{L}/u.test(text) && /\d/.test(text);
    return valid ? null : { password: { minLength: PASSWORD_MIN_LENGTH } };
  }

  /**
   * CROSS-FIELD validator (na grupi, kao dateRange): polje "ponovite lozinku" (confirmKey) mora
   * biti isto kao lozinka (passwordKey). Dok potvrda nije upisana → ispravno (to hvata required).
   * Greška je na grupi: { passwordMismatch: true }; ispod polja potvrde je prikazuje
   * CrossFieldErrorStateMatcher('passwordMismatch').
   *
   * Primjer:
   *   new FormGroup({ newPassword: ..., confirmPassword: ... },
   *     { validators: AppValidators.passwordsMatch('newPassword', 'confirmPassword') });
   */
  static passwordsMatch(passwordKey: string, confirmKey: string): ValidatorFn {
    return (group: AbstractControl): ValidationErrors | null => {
      const password = group.get(passwordKey)?.value;
      const confirm = group.get(confirmKey)?.value;
      if (!confirm) {
        return null;
      }
      return password === confirm ? null : { passwordMismatch: true };
    };
  }
}

/**
 * Material polje (mat-form-field) prikazuje mat-error samo kad je SAMA kontrola neispravna.
 * Greška cross-field validatora je na roditeljskoj grupi, pa ovaj matcher kaže polju da je
 * u grešci i kad grupa ima zadatu grešku (npr. 'dateRange' ili 'passwordMismatch').
 *
 * Primjer (template):
 *   <input matInput [matDatepicker]="endPicker" formControlName="endsAtUtc"
 *          [errorStateMatcher]="dateRangeMatcher" />
 */
export class CrossFieldErrorStateMatcher implements ErrorStateMatcher {
  constructor(private readonly groupErrorKey: string) {}

  isErrorState(control: AbstractControl | null, form: FormGroupDirective | NgForm | null): boolean {
    if (!control) {
      return false;
    }
    const shown = control.touched || !!form?.submitted;
    const groupError = !!control.parent?.hasError(this.groupErrorKey);
    return shown && (control.invalid || groupError);
  }
}

function isEmpty(value: unknown): boolean {
  return value === null || value === undefined || (typeof value === 'string' && value.trim() === '');
}

function isAbsoluteHttpUrl(text: string): boolean {
  try {
    const url = new URL(text.trim());
    return url.protocol === 'http:' || url.protocol === 'https:';
  } catch {
    return false;
  }
}
