import { Component, inject, Input, OnInit } from '@angular/core';
import { AbstractControl, FormControl, ValidationErrors } from '@angular/forms';
import {
  catchError,
  debounceTime,
  distinctUntilChanged,
  filter,
  finalize,
  map,
  Observable,
  of,
  startWith,
  switchMap,
  tap
} from 'rxjs';
import { ProductsApiService } from '../../../../../api-services/products/products-api.service';
import { ListProductsQueryDto } from '../../../../../api-services/products/products-api.models';
import { PageRequest } from '../../../../../core/models/paging/page-request';

/** Odabrani proizvod (vrijednost kontrole kad korisnik izabere stavku iz liste). */
export interface ProductOption {
  id: number;
  name: string;
  /** cijena u KM */
  price: number;
  isEnabled: boolean;
}

/** Dok korisnik kuca, vrijednost kontrole je tekst; tek izbor iz liste daje ProductOption. */
export type ProductPickerValue = ProductOption | string | null;

export function isProductOption(value: ProductPickerValue): value is ProductOption {
  return typeof value === 'object' && value !== null && typeof value.id === 'number';
}

/** Validator: proizvod mora biti IZABRAN iz liste (upisan tekst nije dovoljan). */
export function productSelectedValidator(control: AbstractControl): ValidationErrors | null {
  const value = control.value as ProductPickerValue;
  if (value === null || value === '') {
    return null; // prazno polje hvata Validators.required
  }
  return isProductOption(value) ? null : { productNotSelected: true };
}

/** Koliko prijedloga tražimo od servera. */
const SUGGESTIONS = 10;

/**
 * PRIMJER: AUTOCOMPLETE SA PRETRAGOM NA SERVERU (mat-autocomplete).
 *
 * Umjesto da se svih 1000+ proizvoda učita u mat-select, server vraća 10 prijedloga za
 * upisani tekst: GET /Products?search=<tekst>&paging.page=1&paging.pageSize=10.
 *
 * - debounceTime(300): zahtjev ide tek kad korisnik zastane sa kucanjem
 * - switchMap: novi tekst otkazuje prethodni zahtjev (stari odgovor ne može pregaziti noviji)
 * - async pipe u templateu: pretplata se sama ugasi kad se red (komponenta) ukloni
 * - displayWith: u polju se vidi naziv proizvoda, a vrijednost kontrole je cijeli ProductOption
 * - dok korisnik kuca, vrijednost je tekst → productSelectedValidator javlja grešku dok se
 *   proizvod ne izabere iz liste
 * - postojeća stavka (već izabran proizvod) nema prijedloga dok korisnik ne počne kucati;
 *   upisom drugog naziva i izborom iz liste stavci se mijenja proizvod
 */
@Component({
  selector: 'app-product-autocomplete',
  standalone: false,
  templateUrl: './product-autocomplete.component.html',
  styleUrl: './product-autocomplete.component.scss'
})
export class ProductAutocompleteComponent implements OnInit {
  /** kontrola iz forme roditelja (validatori: required + productSelectedValidator) */
  @Input({ required: true }) control!: FormControl<ProductPickerValue>;

  private api = inject(ProductsApiService);

  options$!: Observable<ProductOption[]>;
  isSearching = false;

  ngOnInit(): void {
    this.options$ = this.control.valueChanges.pipe(
      // prazan novi red odmah nudi prvih 10 proizvoda; postojeći red (već izabran proizvod) ne traži
      startWith(this.control.value ?? ''),
      // traži samo dok je vrijednost tekst (izbor iz liste postavlja objekat)
      filter((value): value is string => typeof value === 'string'),
      map((text) => text.trim()),
      debounceTime(300),
      distinctUntilChanged(),
      tap(() => (this.isSearching = true)),
      switchMap((search) =>
        this.api.list({ search: search || null, paging: new PageRequest(1, SUGGESTIONS) }).pipe(
          map((response) => response.items.map(toProductOption)),
          // greška pretrage → prazna lista prijedloga, polje i dalje radi
          catchError(() => of<ProductOption[]>([])),
          finalize(() => (this.isSearching = false))
        )
      )
    );
  }

  /** Šta se prikazuje u polju za izabranu vrijednost. */
  displayWith = (value: ProductPickerValue): string =>
    isProductOption(value) ? value.name : (value ?? '');
}

function toProductOption(dto: ListProductsQueryDto): ProductOption {
  return { id: dto.id, name: dto.name, price: dto.price, isEnabled: dto.isEnabled };
}
