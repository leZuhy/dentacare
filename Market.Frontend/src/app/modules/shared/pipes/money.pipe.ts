import { formatCurrency } from '@angular/common';
import { inject, LOCALE_ID, Pipe, PipeTransform } from '@angular/core';
import { CurrencyService } from '../../../core/services/currency/currency.service';

/**
 * Iznos u KM → tekst u valuti koju je kupac odabrao u headeru (npr. "51,13 €").
 *
 *   {{ product.price | money }}
 *
 * Format (decimalni zarez, simbol iza iznosa) je po bs-BA lokalu (vidi core/locale/app-locale.ts),
 * a broj decimala i simbol dolaze iz liste valuta sa servera.
 *
 * Zašto je pipe "impure" (pure: false):
 * rezultat ne zavisi samo od ulaza (iznosa) nego i od odabrane valute, koja je GLOBALNO stanje
 * (signal u CurrencyService). Pure pipe se ponovo izvršava samo kad se promijeni ulaz, pa bi
 * nakon promjene valute u headeru i dalje pokazivao staru valutu. Impure pipe se izvršava pri
 * svakoj provjeri promjena (isto rade AsyncPipe i TranslatePipe) – zato ispod pamtimo zadnji
 * rezultat i formatiramo ponovo samo kad se promijene iznos ili valuta.
 * Alternativa bez impure pipe-a: {{ product.price | money: currency.selected() }} u SVAKOM
 * templateu (pure pipe sa valutom kao argumentom) – ispravno, ali lako se zaboravi argument.
 *
 * Napomena: ovo je SAMO prikaz. Narudžba se obračunava u KM, a tačan iznos računa server.
 */
@Pipe({
  name: 'money',
  standalone: false,
  pure: false
})
export class MoneyPipe implements PipeTransform {
  private currency = inject(CurrencyService);
  private locale = inject(LOCALE_ID);

  private lastKey: string | null = null;
  private lastResult = '';

  constructor() {
    // lista valuta se učitava jednom za cijelu aplikaciju (idempotentno)
    this.currency.ensureLoaded();
  }

  transform(amountInBase: number | null | undefined): string {
    if (amountInBase === null || amountInBase === undefined || Number.isNaN(amountInBase)) {
      return '';
    }

    // čitanje signala → Angular zna da ovaj template zavisi od odabrane valute
    const selected = this.currency.selected();

    const key = `${amountInBase}|${selected.code}|${selected.rateToBase}`;
    if (key !== this.lastKey) {
      const digits = `1.${selected.decimalPlaces}-${selected.decimalPlaces}`;
      this.lastResult = formatCurrency(
        this.currency.fromBase(amountInBase),
        this.locale,
        selected.symbol,
        selected.code,
        digits
      );
      this.lastKey = key;
    }
    return this.lastResult;
  }
}
