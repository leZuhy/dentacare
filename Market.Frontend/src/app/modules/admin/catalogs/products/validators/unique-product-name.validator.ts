import { AbstractControl, AsyncValidatorFn, ValidationErrors } from '@angular/forms';
import { catchError, map, Observable, of, switchMap, timer } from 'rxjs';
import { ProductsApiService } from '../../../../../api-services/products/products-api.service';

/**
 * PRIMJER: ASYNC VALIDATOR – naziv proizvoda mora biti jedinstven (pita server).
 *
 *   name: new FormControl('', {
 *     validators: [Validators.required, ...],
 *     asyncValidators: [uniqueProductNameValidator(api, product?.id)]
 *   })
 *
 * Kako radi:
 * - Angular pokreće async validator tek kad SVI sinhroni validatori prođu (prazno polje ne ide na server).
 * - Dok čeka odgovor, kontrola (i forma) su u stanju PENDING → template prikazuje "Provjera naziva…",
 *   a dugme za snimanje je onemogućeno (form.pending).
 * - Kad se vrijednost promijeni, Angular OTKAŽE prethodni poziv validatora. Zato timer(400)
 *   prije HTTP poziva radi kao debounce: dok korisnik kuca, zahtjev se ni ne pošalje.
 * - Greška: { nameTaken: true } → poruka PRODUCTS.FORM.NAME_TAKEN.
 *
 * Server je i dalje izvor istine: create/update ponovo provjeravaju naziv (400 na polju "name"),
 * jer neko drugi može zauzeti naziv između provjere i snimanja.
 *
 * @param excludeId kod izmjene: id proizvoda koji se uređuje (sam sebi nije duplikat)
 */
export function uniqueProductNameValidator(
  api: ProductsApiService,
  excludeId?: number | null
): AsyncValidatorFn {
  return (control: AbstractControl): Observable<ValidationErrors | null> => {
    const name = String(control.value ?? '').trim();

    // nepromijenjen naziv (npr. odmah po otvaranju forme za izmjenu) → nema poziva
    if (!name || control.pristine) {
      return of(null);
    }

    return timer(400).pipe(
      switchMap(() => api.checkNameAvailable({ name, excludeId: excludeId ?? null })),
      map((response) => (response.available ? null : { nameTaken: true })),
      // provjera nije uspjela (mreža, server) → ne blokiraj formu; server provjerava pri snimanju
      catchError(() => of(null))
    );
  };
}
