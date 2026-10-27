import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { AuthApiService } from '../../../api-services/auth/auth-api.service';
import { BaseComponent } from '../../../core/components/base-classes/base-component';
import { applyServerErrors } from '../../../core/forms/server-errors';
import { environment } from '../../../../environments/environment';
import { USER_CONSTRAINTS } from '../../account/user-constraints';

interface ForgotPasswordForm {
  email: FormControl<string>;
}

/**
 * Zaboravljena lozinka (/auth/forgot-password): korisnik upiše email, a backend pošalje link
 * /auth/reset-password?email=...&token=... (vidi ResetPasswordComponent).
 *
 * Backend UVIJEK odgovara 204 – i kad nalog ne postoji – pa i stranica uvijek prikaže istu
 * poruku. Tako se preko ove forme ne može saznati koji emailovi su registrovani.
 */
@Component({
  selector: 'app-forgot-password',
  standalone: false,
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss',
})
export class ForgotPasswordComponent extends BaseComponent {
  private authApi = inject(AuthApiService);

  /** U razvoju (ng serve) backend ne šalje email nego ga ispiše u svoj log – to piše i na stranici. */
  protected readonly isProduction = environment.production;

  /** true nakon odgovora servera → umjesto forme se prikazuje poruka */
  sent = false;

  readonly form = new FormGroup<ForgotPasswordForm>({
    email: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.email,
        Validators.maxLength(USER_CONSTRAINTS.emailMaxLength)
      ]
    })
  });

  onSubmit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.isLoading) {
      return;
    }

    this.startLoading();
    this.authApi.forgotPassword({ email: this.form.controls.email.value.trim() }).subscribe({
      next: () => {
        this.stopLoading();
        this.sent = true;
      },
      // 400 (neispravan email) ili server nedostupan – link sigurno nije poslan
      error: (err) => this.stopLoading(applyServerErrors(this.form, err))
    });
  }
}
