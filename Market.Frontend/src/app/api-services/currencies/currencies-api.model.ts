import { BasePagedQuery } from '../../core/models/paging/base-paged-query';
import { PageResult } from '../../core/models/paging/page-result';

// Bazna valuta je BAM (KM). rateToBase = koliko KM vrijedi 1 jedinica valute (EUR = 1.95583).

// === QUERIES (READ) ===

/**
 * Response item for GET /Currencies (anonimno; samo omogućene valute, bazna prva)
 * Corresponds to: ListCurrenciesQueryDto.cs
 */
export interface ListCurrenciesQueryDto {
  /** ISO 4217, npr. "BAM", "EUR" */
  code: string;
  name: string;
  /** npr. "KM", "€" */
  symbol: string;
  /** na koliko decimala se zaokružuje iznos u ovoj valuti */
  decimalPlaces: number;
  isBase: boolean;
  /** kurs koji važi sada (1 za baznu); null = valuta još nema kurs */
  rateToBase: number | null;
  /** od kada važi trenutni kurs (ISO, UTC); null za baznu i za valutu bez kursa */
  rateValidFromUtc: string | null;
}

/**
 * Query parameters for GET /Currencies/convert (anonimno)
 * Corresponds to: ConvertCurrencyQuery.cs
 */
export interface ConvertCurrencyRequest {
  amount: number;
  from: string;
  to: string;
  /** kursevi koji su važili u tom trenutku (ISO, UTC); bez njega → sada */
  atUtc?: string | null;
}

/**
 * Response for GET /Currencies/convert – server je izvor istine za konverziju
 * Corresponds to: ConvertCurrencyQueryDto.cs
 */
export interface ConvertCurrencyQueryDto {
  amount: number;
  from: string;
  to: string;
  /** zaokruženo na decimale ciljne valute */
  convertedAmount: number;
  /** efektivni kurs from → to (6 decimala) */
  rate: number;
  /** od kada važe primijenjeni kursevi (ISO, UTC) */
  rateDateUtc: string;
}

/**
 * Query parameters for GET /Currencies/rates (Admin)
 * Corresponds to: ListExchangeRatesQuery.cs
 */
export class ListExchangeRatesRequest extends BasePagedQuery {
  currencyCode?: string | null;
}

/**
 * Response item for GET /Currencies/rates (najnoviji prvi)
 * Corresponds to: ListExchangeRatesQueryDto.cs
 */
export interface ListExchangeRatesQueryDto {
  id: number;
  currencyCode: string;
  rateToBase: number;
  /** ISO string (UTC) */
  validFromUtc: string;
  /** npr. "Ručni unos", "Frankfurter (ECB)" */
  source: string;
}

/**
 * Paged response for GET /Currencies/rates
 */
export type ListExchangeRatesResponse = PageResult<ListExchangeRatesQueryDto>;

// === COMMANDS (WRITE) ===

/**
 * Command for POST /Currencies/rates (Admin) – kursevi se ne mijenjaju, dodaje se novi
 * Corresponds to: CreateExchangeRateCommand.cs
 */
export interface CreateExchangeRateCommand {
  /** valuta koja nije bazna, npr. "USD" */
  currencyCode: string;
  /** > 0, najviše 6 decimala */
  rateToBase: number;
  /** ISO string (UTC) */
  validFromUtc: string;
}

/**
 * Response for POST /Currencies/rates/refresh (Admin)
 * Corresponds to: RefreshExchangeRatesCommandDto.cs
 */
export interface RefreshExchangeRatesCommandDto {
  /** broj novih kurseva */
  updated: number;
  /** izvor, npr. "Frankfurter (ECB)" */
  source: string;
  /** omogućene valute koje izvor ne objavljuje (nisu ažurirane) */
  skipped: string[];
}
