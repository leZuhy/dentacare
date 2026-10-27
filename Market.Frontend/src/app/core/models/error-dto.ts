/**
 * Standardni oblik greške koji vraća backend (Market.Shared/Dtos/ErrorDto.cs).
 *
 * code:
 *  - "validation.error"  (400) – popunjen je i niz errors
 *  - "auth.unauthorized" (401)
 *  - "auth.forbidden"    (403) – 403 koji vrati sama ASP.NET policy može imati PRAZNO tijelo
 *  - "entity.error"      (404 / 409)
 *  - "database.error"    (409)
 *  - "internal.error"    (500)
 */
export interface ErrorDto {
  code: string;
  message: string;
  traceId?: string | null;
  /** stack trace – samo u Development okruženju backenda */
  details?: string | null;
  errors?: FieldErrorDto[] | null;
}

/**
 * Greška jednog polja forme (Market.Shared/Dtos/FieldErrorDto.cs).
 */
export interface FieldErrorDto {
  /** camelCase putanja polja, npr. "name" ili "items[0].quantity" */
  field: string;
  message: string;
  errorCode?: string | null;
}
