import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';
import { map } from 'rxjs';
import { DialogButton } from '../models/dialog-config.model';
import { DialogHelperService } from '../services/dialog-helper.service';

/**
 * Komponenta koja zna da li ima nesačuvane izmjene (najčešće: form.dirty && !saving).
 */
export interface HasUnsavedChanges {
  hasUnsavedChanges(): boolean;
}

/**
 * CanDeactivate guard: prije napuštanja stranice sa nesačuvanim izmjenama pita korisnika
 * (fit-confirm-dialog preko DialogHelperService). "Da" → napušta stranicu, "Ne" → ostaje.
 *
 * Ruta:
 *   { path: 'orders/:id/edit', component: OrderEditComponent, canDeactivate: [unsavedChangesGuard] }
 *
 * Guard pokriva navigaciju UNUTAR aplikacije (link, back dugme browsera). Zatvaranje taba ili
 * refresh hvata komponenta preko 'window:beforeunload' (vidi OrderEditComponent).
 */
export const unsavedChangesGuard: CanDeactivateFn<HasUnsavedChanges> = (component) => {
  if (!component.hasUnsavedChanges()) {
    return true;
  }

  return inject(DialogHelperService)
    .confirmUnsavedChanges()
    .pipe(map((result) => result?.button === DialogButton.YES));
};
