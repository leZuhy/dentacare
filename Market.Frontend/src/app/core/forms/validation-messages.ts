import { ValidationErrors } from '@angular/forms';

/**
 * Poruka za grešku validacije:
 *  - key/params → ključ prevoda (public/i18n/*.json) i parametri za TranslatePipe
 *  - text       → gotov tekst (poruka sa backenda, vidi core/forms/server-errors.ts)
 */
export interface ValidationMessage {
  key?: string;
  params?: Record<string, unknown>;
  text?: string;
}

type MessageFactory = (error: any) => ValidationMessage;

/**
 * Ključ greške (Angular / AppValidators / Material) → poruka.
 * Novi validator = nova linija ovdje + ključ u bs.json i en.json.
 */
const DEFAULT_MESSAGES: Record<string, MessageFactory> = {
  required: () => ({ key: 'VALIDATION.REQUIRED' }),
  email: () => ({ key: 'VALIDATION.EMAIL' }),
  minlength: (e) => ({ key: 'VALIDATION.MIN_LENGTH', params: { min: e.requiredLength } }),
  maxlength: (e) => ({ key: 'VALIDATION.MAX_LENGTH', params: { max: e.requiredLength } }),
  min: (e) => ({ key: 'VALIDATION.MIN_VALUE', params: { min: e.min } }),
  max: (e) => ({ key: 'VALIDATION.MAX_VALUE', params: { max: e.max } }),
  pattern: () => ({ key: 'VALIDATION.PATTERN' }),
  // AppValidators (core/forms/validators.ts)
  integer: () => ({ key: 'VALIDATION.INTEGER' }),
  url: () => ({ key: 'VALIDATION.URL' }),
  urlOrPath: () => ({ key: 'VALIDATION.URL_OR_PATH' }),
  maxDecimals: (e) => ({ key: 'VALIDATION.MAX_DECIMALS', params: { max: e.max } }),
  dateRange: () => ({ key: 'VALIDATION.DATE_RANGE' }),
  password: (e) => ({ key: 'VALIDATION.PASSWORD', params: { min: e.minLength } }),
  passwordMismatch: () => ({ key: 'VALIDATION.PASSWORD_MATCH' }),
  // mat-datepicker: tekst u polju nije datum
  matDatepickerParse: () => ({ key: 'VALIDATION.DATE' }),
};

/**
 * Vraća poruku za PRVU grešku kontrole (ili null ako grešaka nema).
 *
 * @param errors   control.errors
 * @param custom   poruke specifične za formu: ključ greške → ključ prevoda,
 *                 npr. { nameTaken: 'PRODUCTS.FORM.NAME_TAKEN' } (imaju prednost nad default-ima)
 *
 * U templateu se ne poziva direktno – koristi <app-fit-field-error [control]="...">.
 */
export function getValidationMessage(
  errors: ValidationErrors | null | undefined,
  custom: Record<string, string> = {}
): ValidationMessage | null {
  if (!errors) {
    return null;
  }

  // greška sa backenda ima prednost: to je ono što je server odbio
  if (typeof errors['server'] === 'string') {
    return { text: errors['server'] };
  }

  const errorKey = Object.keys(errors)[0];
  if (!errorKey) {
    return null;
  }

  if (custom[errorKey]) {
    return { key: custom[errorKey] };
  }

  const factory = DEFAULT_MESSAGES[errorKey];
  return factory ? factory(errors[errorKey]) : { key: 'VALIDATION.CUSTOM' };
}
