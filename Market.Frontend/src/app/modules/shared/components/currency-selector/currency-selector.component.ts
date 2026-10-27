import { Component, inject, OnInit } from '@angular/core';
import { CurrencyService } from '../../../../core/services/currency/currency.service';

/**
 * Izbor valute za prikaz cijena (header javnog dijela i klijentskog dijela).
 * Odabir se pamti u CurrencyService (signal + localStorage); cijene prati pipe "money".
 */
@Component({
  selector: 'app-currency-selector',
  standalone: false,
  templateUrl: './currency-selector.component.html',
  styleUrl: './currency-selector.component.scss'
})
export class CurrencySelectorComponent implements OnInit {
  protected currency = inject(CurrencyService);

  ngOnInit(): void {
    this.currency.ensureLoaded();
  }
}
