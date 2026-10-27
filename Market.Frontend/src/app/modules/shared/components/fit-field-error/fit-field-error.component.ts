import { Component, Input } from '@angular/core';
import { AbstractControl } from '@angular/forms';
import {
  getValidationMessage,
  ValidationMessage
} from '../../../../core/forms/validation-messages';

/**
 * Prevedena poruka za PRVU grešku kontrole forme (required, maxlength, min, server, ...).
 *
 * Unutar mat-form-field (mat-error se sam prikaže kad je polje neispravno i dodirnuto):
 *   <mat-error><app-fit-field-error [control]="form.controls.name" /></mat-error>
 *
 * Poruka specifična za formu (ključ greške → ključ prevoda):
 *   <app-fit-field-error [control]="form.controls.name"
 *                        [messages]="{ nameTaken: 'PRODUCTS.FORM.NAME_TAKEN' }" />
 *
 * Poruke i ključevi prevoda su u core/forms/validation-messages.ts.
 */
@Component({
  selector: 'app-fit-field-error',
  standalone: false,
  templateUrl: './fit-field-error.component.html'
})
export class FitFieldErrorComponent {
  @Input({ required: true }) control!: AbstractControl | null;
  @Input() messages: Record<string, string> = {};

  get message(): ValidationMessage | null {
    return getValidationMessage(this.control?.errors, this.messages);
  }
}
