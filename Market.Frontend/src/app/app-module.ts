import {
  DEFAULT_CURRENCY_CODE,
  LOCALE_ID,
  NgModule,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection
} from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import {HttpClient, provideHttpClient, withInterceptors} from '@angular/common/http';
import { DateAdapter, MAT_DATE_FORMATS, MAT_DATE_LOCALE } from '@angular/material/core';

import { AppRoutingModule } from './app-routing-module';
import { AppComponent } from './app.component';
import {authInterceptor} from './core/interceptors/auth-interceptor.service';
import {loadingBarInterceptor} from './core/interceptors/loading-bar-interceptor.service';
import {errorLoggingInterceptor} from './core/interceptors/error-logging-interceptor.service';
import {TranslateLoader, TranslateModule} from '@ngx-translate/core';
import {CustomTranslateLoader} from './core/services/custom-translate-loader';
import {materialModules} from './modules/shared/material-modules';
import {SharedModule} from './modules/shared/shared-module';
import { APP_LOCALE, BS_DATE_FORMATS, BsDateAdapter, registerAppLocale } from './core/locale/app-locale';

// Podaci za bs-BA (decimalni zarez, nazivi mjeseci, "KM") – prije prvog formatiranja
registerAppLocale();

@NgModule({
  declarations: [
    AppComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    TranslateModule.forRoot({
      loader: {
        provide: TranslateLoader,
        useFactory: (http: HttpClient) => new CustomTranslateLoader(http),
        deps: [HttpClient]
      }
    }),
    SharedModule,
    materialModules,
  ],
  providers: [
    provideBrowserGlobalErrorListeners(),
    // Aplikacija koristi zone.js (učitan preko "polyfills" u angular.json)
    provideZoneChangeDetection(),
    provideHttpClient(
      // Redoslijed je bitan: zahtjev prolazi kroz interceptore ovim redom,
      // a odgovor/greška obrnutim (errorLogging prvi vidi grešku sa servera).
      withInterceptors([
        loadingBarInterceptor,
        authInterceptor,
        errorLoggingInterceptor
      ])
    ),

    // === Lokalizacija (vidi core/locale/app-locale.ts) ===
    // DatePipe/DecimalPipe/CurrencyPipe formatiraju po bs-BA: 1.234,56 KM
    { provide: LOCALE_ID, useValue: APP_LOCALE },
    // {{ iznos | currency }} bez oznake valute → KM
    { provide: DEFAULT_CURRENCY_CODE, useValue: 'BAM' },
    // mat-datepicker: bosanski nazivi mjeseci, ponedjeljak prvi dan, polje "19.10.2026".
    // Adapter je zadan SAMO ovdje (root). Zato MatNativeDateModule nije u material-modules.ts:
    // lazy modul koji ga importuje bi dobio svoj NativeDateAdapter umjesto ovog.
    { provide: MAT_DATE_LOCALE, useValue: APP_LOCALE },
    { provide: DateAdapter, useClass: BsDateAdapter },
    { provide: MAT_DATE_FORMATS, useValue: BS_DATE_FORMATS },
  ],
  exports: [
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
