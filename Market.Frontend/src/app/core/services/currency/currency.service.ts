import { computed, inject, Injectable, signal } from '@angular/core';
import { CurrenciesApiService } from '../../../api-services/currencies/currencies-api.service';
import { ListCurrenciesQueryDto } from '../../../api-services/currencies/currencies-api.model';

/** Valuta u kojoj kupac želi VIDJETI cijene. */
export interface DisplayCurrency {
  code: string;
  name: string;
  symbol: string;
  decimalPlaces: number;
  /** koliko KM vrijedi 1 jedinica ove valute */
  rateToBase: number;
  isBase: boolean;
}

/** Bazna valuta – koristi se dok se lista valuta ne učita (ili ako učitavanje ne uspije). */
export const BASE_CURRENCY: DisplayCurrency = {
  code: 'BAM',
  name: 'Konvertibilna marka',
  symbol: 'KM',
  decimalPlaces: 2,
  rateToBase: 1,
  isBase: true
};

/**
 * Odabrana valuta za PRIKAZ cijena (signal) + preračun iz KM.
 *
 * - Lista valuta (GET /Currencies) se učitava jednom, na prvu upotrebu (ensureLoaded).
 * - Odabir se pamti u localStorage; default je KM.
 * - fromBase() je SAMO za prikaz: cijene, korpa i narudžbe su u KM, a server računa iznose.
 *   Tačna konverzija za određeni datum ide preko GET /Currencies/convert (stranica /converter).
 *
 * U templateu se ne koristi direktno – za cijene koristi pipe: {{ product.price | money }}.
 */
@Injectable({ providedIn: 'root' })
export class CurrencyService {
  private api = inject(CurrenciesApiService);

  private readonly STORAGE_KEY = 'currency';

  private readonly _currencies = signal<DisplayCurrency[]>([BASE_CURRENCY]);
  private readonly _selectedCode = signal<string>(this.readStoredCode() ?? BASE_CURRENCY.code);
  private loadState: 'idle' | 'loading' | 'loaded' = 'idle';

  /** Valute koje se mogu odabrati (samo one koje imaju kurs). */
  readonly currencies = this._currencies.asReadonly();

  /** Odabrana valuta; dok lista nije učitana (ili odabrana valuta više ne postoji) → KM. */
  readonly selected = computed<DisplayCurrency>(
    () => this._currencies().find((c) => c.code === this._selectedCode()) ?? BASE_CURRENCY
  );

  /** true kad se cijene prikazuju u KM (tada nema napomene o informativnom iznosu). */
  readonly isBaseSelected = computed(() => this.selected().isBase);

  /** Učitava listu valuta ako još nije učitana. Može se zvati više puta. */
  ensureLoaded(): void {
    if (this.loadState !== 'idle') {
      return;
    }
    this.load();
  }

  /** Ponovo učitava kurseve (npr. nakon što admin doda kurs ili osvježi kursnu listu). */
  reload(): void {
    if (this.loadState === 'loading') {
      return;
    }
    this.load();
  }

  select(code: string): void {
    this._selectedCode.set(code);
    try {
      localStorage.setItem(this.STORAGE_KEY, code);
    } catch {
      // localStorage nije dostupan (privatni mod, blokiran) → odabir važi samo do refresha
    }
  }

  /** Iznos u KM → iznos u odabranoj valuti (nezaokruženo; zaokružuje pipe pri prikazu). */
  fromBase(amountInBase: number): number {
    return amountInBase / this.selected().rateToBase;
  }

  private load(): void {
    this.loadState = 'loading';
    this.api.list().subscribe({
      next: (items) => {
        const usable = items
          .filter((c) => c.isBase || (c.rateToBase !== null && c.rateToBase > 0))
          .map((c) => this.toDisplayCurrency(c));
        // bazna valuta je uvijek u listi, i kad je server ne vrati na prvom mjestu
        this._currencies.set(usable.some((c) => c.isBase) ? usable : [BASE_CURRENCY, ...usable]);
        this.loadState = 'loaded';
      },
      error: (err) => {
        // bez liste valuta cijene ostaju u KM; sljedeći ensureLoaded() pokušava ponovo
        this.loadState = 'idle';
        console.error('Load currencies error:', err);
      }
    });
  }

  private toDisplayCurrency(dto: ListCurrenciesQueryDto): DisplayCurrency {
    return {
      code: dto.code,
      name: dto.name,
      symbol: dto.symbol,
      decimalPlaces: dto.decimalPlaces,
      rateToBase: dto.isBase ? 1 : (dto.rateToBase as number),
      isBase: dto.isBase
    };
  }

  private readStoredCode(): string | null {
    try {
      return localStorage.getItem(this.STORAGE_KEY);
    } catch {
      return null;
    }
  }
}
