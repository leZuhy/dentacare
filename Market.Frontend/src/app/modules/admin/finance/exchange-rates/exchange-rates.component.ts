import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormControl, FormGroup, FormGroupDirective, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { filter, switchMap, tap } from 'rxjs';
import { CurrenciesApiService } from '../../../../api-services/currencies/currencies-api.service';
import {
  CreateExchangeRateCommand,
  ListCurrenciesQueryDto,
  ListExchangeRatesQueryDto,
  ListExchangeRatesRequest
} from '../../../../api-services/currencies/currencies-api.model';
import { BaseListPagedComponent } from '../../../../core/components/base-classes/base-list-paged-component';
import { applyServerErrors } from '../../../../core/forms/server-errors';
import { toUtcDateIso } from '../../../../core/forms/utc-date';
import { AppValidators } from '../../../../core/forms/validators';
import { getErrorMessage } from '../../../../core/interceptors/error-logging-interceptor.service';
import { CurrencyService } from '../../../../core/services/currency/currency.service';
import { ToasterService } from '../../../../core/services/toaster.service';
import { DialogHelperService } from '../../../shared/services/dialog-helper.service';
import { DialogButton, DialogType } from '../../../shared/models/dialog-config.model';

interface ExchangeRateForm {
  currencyCode: FormControl<string | null>;
  /** koliko KM vrijedi 1 jedinica valute; > 0, najviše 6 decimala */
  rateToBase: FormControl<number | null>;
  validFromUtc: FormControl<Date | null>;
}

/**
 * Kursna lista (/admin/exchange-rates): istorija kurseva, ručni unos, brisanje i
 * preuzimanje kurseva sa ECB-a (POST /Currencies/rates/refresh).
 *
 * Kursevi se ne mijenjaju: novi kurs važi od svog datuma, a stari ostaje za konverzije u prošlosti.
 */
@Component({
  selector: 'app-exchange-rates',
  standalone: false,
  templateUrl: './exchange-rates.component.html',
  styleUrl: './exchange-rates.component.scss'
})
export class ExchangeRatesComponent
  extends BaseListPagedComponent<ListExchangeRatesQueryDto, ListExchangeRatesRequest>
  implements OnInit
{
  private api = inject(CurrenciesApiService);
  private currencyService = inject(CurrencyService);
  private dialogHelper = inject(DialogHelperService);
  private toaster = inject(ToasterService);
  private translate = inject(TranslateService);
  private destroyRef = inject(DestroyRef);

  readonly displayedColumns = ['currencyCode', 'rateToBase', 'validFromUtc', 'source', 'actions'];

  /** valute koje mogu imati kurs (sve osim bazne KM), i one koje još nemaju kurs */
  currencies: ListCurrenciesQueryDto[] = [];

  readonly currencyFilter = new FormControl<string | null>(null);

  readonly rateForm = new FormGroup<ExchangeRateForm>({
    currencyCode: new FormControl<string | null>(null, [Validators.required]),
    rateToBase: new FormControl<number | null>(null, [
      Validators.required,
      // najmanji pozitivan broj sa 6 decimala
      Validators.min(0.000001),
      AppValidators.maxDecimals(6)
    ]),
    validFromUtc: new FormControl<Date | null>(new Date(), [Validators.required])
  });

  isSavingRate = false;
  rateFormError: string | null = null;
  isRefreshing = false;

  constructor() {
    super();
    this.request = new ListExchangeRatesRequest();
  }

  ngOnInit(): void {
    this.initList();
    this.loadCurrencies();

    this.currencyFilter.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((code) => {
        this.request.currencyCode = code;
        this.request.paging.page = 1;
        this.loadPagedData();
      });
  }

  protected loadPagedData(): void {
    this.startLoading();

    this.api
      .listRates(this.request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.handlePageResult(response);
          this.stopLoading();
        },
        error: (err) => this.stopLoading(getErrorMessage(err))
      });
  }

  /**
   * FormGroupDirective (iz templatea) je potreban za resetForm(): reset() same forme ne briše
   * "submitted" stanje, pa bi prazna obavezna polja odmah bila crvena.
   */
  addRate(formDirective: FormGroupDirective): void {
    if (this.rateForm.invalid || this.isSavingRate) {
      this.rateForm.markAllAsTouched();
      return;
    }

    const value = this.rateForm.getRawValue();
    const command: CreateExchangeRateCommand = {
      currencyCode: value.currencyCode ?? '',
      rateToBase: value.rateToBase ?? 0,
      // kalendarski dan kao UTC ponoć (core/forms/utc-date.ts)
      validFromUtc: toUtcDateIso(value.validFromUtc) ?? ''
    };

    this.isSavingRate = true;
    this.rateFormError = null;

    this.api.createRate(command).subscribe({
      next: () => {
        this.isSavingRate = false;
        this.toaster.success(this.translate.instant('EXCHANGE_RATES.SAVED'));
        // ista valuta ostaje odabrana (često se unosi više kurseva iste valute)
        formDirective.resetForm({
          currencyCode: value.currencyCode,
          rateToBase: null,
          validFromUtc: new Date()
        });
        this.onRatesChanged();
      },
      error: (err) => {
        this.isSavingRate = false;
        this.rateFormError = applyServerErrors(this.rateForm, err);
      }
    });
  }

  onDelete(rate: ListExchangeRatesQueryDto): void {
    this.dialogHelper
      .confirm('EXCHANGE_RATES.DELETE_TITLE', 'EXCHANGE_RATES.DELETE_MESSAGE', {
        code: rate.currencyCode
      })
      .pipe(
        filter((result) => result?.button === DialogButton.YES),
        tap(() => this.startLoading()),
        switchMap(() => this.api.deleteRate(rate.id))
      )
      .subscribe({
        next: () => {
          this.toaster.success(this.translate.instant('EXCHANGE_RATES.DELETED'));
          this.onRatesChanged();
        },
        error: (err) => {
          this.stopLoading();
          this.toaster.error(getErrorMessage(err));
        }
      });
  }

  /** "Osvježi s ECB": backend preuzme kurseve; 409 = izvor kurseva nije uključen u konfiguraciji. */
  refreshFromProvider(): void {
    this.isRefreshing = true;

    this.api.refreshRates().subscribe({
      next: (result) => {
        this.isRefreshing = false;
        const skipped = result.skipped.length
          ? ' ' +
            this.translate.instant('EXCHANGE_RATES.REFRESH_SKIPPED', {
              codes: result.skipped.join(', ')
            })
          : '';
        this.toaster.success(
          this.translate.instant('EXCHANGE_RATES.REFRESH_DONE', {
            updated: result.updated,
            source: result.source
          }) + skipped,
          6000
        );
        this.onRatesChanged();
      },
      error: (err: HttpErrorResponse) => {
        this.isRefreshing = false;
        // poruka sa backenda kaže kako se izvor uključuje (appsettings)
        this.dialogHelper
          .open({
            type: err.status === 409 ? DialogType.WARNING : DialogType.ERROR,
            titleKey: 'EXCHANGE_RATES.REFRESH_FAILED',
            message: getErrorMessage(err),
            buttons: [{ type: DialogButton.OK, color: 'primary' }]
          })
          .subscribe();
      }
    });
  }

  private onRatesChanged(): void {
    this.loadPagedData();
    // javni dio (pipe "money", konvertor) treba nove kurseve
    this.currencyService.reload();
  }

  private loadCurrencies(): void {
    this.api
      .list()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (currencies) => (this.currencies = currencies.filter((c) => !c.isBase)),
        error: (err) => this.toaster.error(getErrorMessage(err))
      });
  }
}
