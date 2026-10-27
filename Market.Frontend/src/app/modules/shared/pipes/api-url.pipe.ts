import { Pipe, PipeTransform } from '@angular/core';
import { environment } from '../../../../environments/environment';

/**
 * Relativna putanja fajla sa backenda → puna adresa (environment.apiUrl + putanja).
 *
 *   @if (product.imageUrl | apiUrl; as src) {
 *     <img [src]="src" [alt]="product.name" />
 *   } @else {
 *     <mat-icon>inventory_2</mat-icon>   <!-- placeholder -->
 *   }
 *
 * Backend vraća npr. "/uploads/products/{guid}.webp", a slike servira sa svoje adrese
 * (u developmentu https://localhost:7100), ne sa adrese Angular aplikacije.
 * Apsolutni URL (http/https) se vraća nepromijenjen; null/prazno → null.
 */
@Pipe({
  name: 'apiUrl',
  standalone: false
})
export class ApiUrlPipe implements PipeTransform {
  transform(path: string | null | undefined): string | null {
    if (!path) {
      return null;
    }
    if (/^https?:\/\//i.test(path)) {
      return path;
    }
    const base = environment.apiUrl.replace(/\/+$/, '');
    return `${base}${path.startsWith('/') ? '' : '/'}${path}`;
  }
}
