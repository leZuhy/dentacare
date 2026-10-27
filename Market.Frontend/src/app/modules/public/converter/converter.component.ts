import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { CurrenciesApiService } from '../../../api-services/currencies/currencies-api.service';
import { ConvertCurrencyQueryDto } from '../../../api-services/currencies/currencies-api.model';
import { BaseComponent } from '../../../core/components/base-classes/base-component';
import { applyServerErrors } from '../../../core/forms/server-errors';
import { toUtcDateIso } from '../../../core/forms/utc-date';
import { CurrencyService } from '../../../core/services/currency/currency.service';

/** Imena kontrola = imena parametara API-ja, pa applyServerErrors greške mapira automatski. */
interface ConverterForm {
  amount: FormControl<number | null>;
  from: FormControl<string>;
  to: FormControl<string>;
  /** opcioni datum kursa; prazno = kurs koji važi sada */
  atUtc: FormControl<Date | null>;
}

/**
 * Konvertor valuta (/converter, anonimno).
 *
 * Za razliku od pipe-a "money" (koji preračunava lokalno, samo za prikaz cijena), ovdje
 * računa SERVER (GET /Currencies/convert): vraća iznos, efektivni kurs i datum kursa.
 */
@Component({
  selector: 'app-converter',
  standalone: false,
  templateUrl: './converter.component.html',
  styleUrl: './converter.component.scss'
})
export class ConverterComponent extends BaseComponent implements OnInit {
  private api = inject(CurrenciesApiService);
  private destroyRef = inject(DestroyRef);
  protected currency = inject(CurrencyService);

  readonly maxAmount = 1_000_000_000_000;
  readonly today = new Date();

  readonly form = new FormGroup<ConverterForm>({
    amount: new FormControl<number | null>(100, [
      Validators.required,
      Validators.min(0),
      Validators.max(this.maxAmount)
    ]),
    from: new FormControl('EUR', { nonNullable: true, validators: [Validators.required] }),
    to: new FormControl('BAM', { nonNullable: true, validators: [Validators.required] }),
    atUtc: new FormControl<Date | null>(null)
  });

  result: ConvertCurrencyQueryDto | null = null;

  ngOnInit(): void {
    this.currency.ensureLoaded();
  }

  swap(): void {
    const { from, to } = this.form.getRawValue();
    this.form.patchValue({ from: to, to: from });
  }

  convert(): void {
    if (this.form.invalid || this.isLoading) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.startLoading();

    this.api
      .convert({
        amount: value.amount ?? 0,
        from: value.from,
        to: value.to,
        // odabrani kalendarski dan kao UTC ponoć (vidi core/forms/utc-date.ts)
        atUtc: toUtcDateIso(value.atUtc)
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.result = result;
          this.stopLoading();
        },
        error: (err) => {
          this.result = null;
          this.stopLoading(applyServerErrors(this.form, err));
        }
      });
  }

  /** DecimalPipe format za iznos u valuti (broj decimala valute sa servera; default 2). */
  digitsOf(code: string): string {
    const decimals = this.currency.currencies().find((c) => c.code === code)?.decimalPlaces ?? 2;
    return `1.${decimals}-${decimals}`;
  }

  symbolOf(code: string): string {
    return this.currency.currencies().find((c) => c.code === code)?.symbol ?? code;
  }
}
