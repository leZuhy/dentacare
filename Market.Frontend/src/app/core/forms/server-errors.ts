import { HttpErrorResponse } from '@angular/common/http';
import { FormGroup } from '@angular/forms';
import { getErrorMessage } from '../interceptors/error-logging-interceptor.service';
import { ErrorDto } from '../models/error-dto';

/**
 * Prikazuje grešku sa backenda u reaktivnoj formi.
 *
 * 1) Za svaku stavku iz ErrorDto.errors postavlja grešku { server: poruka } na kontrolu
 *    čija putanja odgovara polju (camelCase, npr. "name" ili "items[0].quantity").
 *    Polja kojih nema u formi se preskaču – njihova poruka je ionako u ErrorDto.message.
 * 2) Vraća opštu poruku (ErrorDto.message) koju komponenta prikaže u formi.
 *
 * Greška "server" nestaje čim korisnik promijeni vrijednost kontrole
 * (Angular tada ponovo pokreće validatore).
 *
 * Primjer (komponenta):
 *   error: (err) => this.stopLoading(applyServerErrors(this.form, err))
 *
 * Primjer (template, uz mat-error):
 *   @if (form.get('name')?.hasError('server')) {
 *     <mat-error>{{ form.get('name')?.getError('server') }}</mat-error>
 *   }
 */
export function applyServerErrors(form: FormGroup, error: HttpErrorResponse): string {
  const fieldErrors = (error.error as Partial<ErrorDto> | null)?.errors;

  if (Array.isArray(fieldErrors)) {
    // ako backend vrati više poruka za isto polje, prikazujemo ih zajedno
    const messagesByPath = new Map<string, string[]>();

    for (const fieldError of fieldErrors) {
      if (!fieldError?.field || !fieldError.message) {
        continue;
      }
      const path = toControlPath(fieldError.field);
      messagesByPath.set(path, [...(messagesByPath.get(path) ?? []), fieldError.message]);
    }

    messagesByPath.forEach((messages, path) => {
      const control = form.get(path);
      if (!control) {
        return; // polje ne postoji u formi
      }
      control.setErrors({ ...(control.errors ?? {}), server: messages.join(' ') });
      control.markAsTouched(); // da se mat-error odmah prikaže
    });
  }

  return getErrorMessage(error);
}

/**
 * "items[0].quantity" → "items.0.quantity" (format koji razumije FormGroup.get()).
 */
function toControlPath(field: string): string {
  return field.replace(/\[(\d+)\]/g, '.$1');
}
