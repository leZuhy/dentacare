import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { RegisterCommand } from '../../../api-services/auth/auth-api.model';
import { BaseComponent } from '../../../core/components/base-classes/base-component';
import { applyServerErrors } from '../../../core/forms/server-errors';
import {
  AppValidators,
  CrossFieldErrorStateMatcher,
  PASSWORD_MIN_LENGTH
} from '../../../core/forms/validators';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';
import { CurrentUserService } from '../../../core/services/auth/current-user.service';
import { isSafeReturnUrl } from '../../../core/services/auth/return-url';
import { USER_CONSTRAINTS } from '../../account/user-constraints';

/** Tipizirana forma registracije. Imena kontrola = imena polja API-ja (za applyServerErrors). */
interface RegisterForm {
  firstname: FormControl<string>;
  lastname: FormControl<string>;
  email: FormControl<string>;
  password: FormControl<string>;
  /** samo na frontendu – ne šalje se */
  confirmPassword: FormControl<string>;
  address: FormControl<string>;
  city: FormControl<string>;
}

/**
 * Registracija kupca (/auth/register).
 *
 * - pravilo lozinke kao na backendu (AppValidators.password) + hint ispod polja
 * - CROSS-FIELD validator "lozinke se ne podudaraju" (AppValidators.passwordsMatch na grupi),
 *   greška se prikazuje ispod polja potvrde (CrossFieldErrorStateMatcher)
 * - greške servera po polju (npr. "Email je već registrovan.") → applyServerErrors
 * - backend odmah vraća tokene: snimaju se kao kod logina, pa i preusmjerenje radi kao login
 *   (?returnUrl ako je siguran, inače početna stranica korisnika)
 */
@Component({
  selector: 'app-register',
  standalone: false,
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent extends BaseComponent {
  private auth = inject(AuthFacadeService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private currentUser = inject(CurrentUserService);

  private readonly limits = USER_CONSTRAINTS;
  protected readonly passwordMinLength = PASSWORD_MIN_LENGTH;
  /** polje "Ponovite lozinku" je u grešci i kad je greška 'passwordMismatch' na grupi */
  protected readonly passwordMatcher = new CrossFieldErrorStateMatcher('passwordMismatch');

  readonly form = new FormGroup<RegisterForm>(
    {
      firstname: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(this.limits.firstnameMaxLength)]
      }),
      lastname: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(this.limits.lastnameMaxLength)]
      }),
      email: new FormControl('', {
        nonNullable: true,
        validators: [
          Validators.required,
          Validators.email,
          Validators.maxLength(this.limits.emailMaxLength)
        ]
      }),
      password: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, AppValidators.password]
      }),
      confirmPassword: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required]
      }),
      address: new FormControl('', {
        nonNullable: true,
        validators: [Validators.maxLength(this.limits.addressMaxLength)]
      }),
      city: new FormControl('', {
        nonNullable: true,
        validators: [Validators.maxLength(this.limits.cityMaxLength)]
      })
    },
    // CROSS-FIELD: validator na GRUPI, jer poredi dva polja
    { validators: AppValidators.passwordsMatch('password', 'confirmPassword') }
  );

  onSubmit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.isLoading) {
      return;
    }

    const value = this.form.getRawValue();
    const payload: RegisterCommand = {
      firstname: value.firstname.trim(),
      lastname: value.lastname.trim(),
      email: value.email.trim(),
      password: value.password,
      // prazno polje → null (adresa i grad nisu obavezni)
      address: value.address.trim() || null,
      city: value.city.trim() || null
    };

    this.startLoading();
    this.auth.register(payload).subscribe({
      next: () => {
        this.stopLoading();
        // isto kao login: ?returnUrl samo ako je putanja unutar aplikacije
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        if (isSafeReturnUrl(returnUrl)) {
          this.router.navigateByUrl(returnUrl);
        } else {
          this.router.navigate([this.currentUser.getDefaultRoute()]);
        }
      },
      // "email" → greška ispod polja Email; opšta poruka ide u banner iznad forme
      error: (err) => this.stopLoading(applyServerErrors(this.form, err))
    });
  }
}
