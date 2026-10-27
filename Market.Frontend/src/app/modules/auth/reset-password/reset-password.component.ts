import { Component, inject, OnInit } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { AuthApiService } from '../../../api-services/auth/auth-api.service';
import { BaseComponent } from '../../../core/components/base-classes/base-component';
import { applyServerErrors } from '../../../core/forms/server-errors';
import {
  AppValidators,
  CrossFieldErrorStateMatcher,
  PASSWORD_MIN_LENGTH
} from '../../../core/forms/validators';
import { ErrorDto } from '../../../core/models/error-dto';
import { ToasterService } from '../../../core/services/toaster.service';

/** Imena kontrola = imena polja API-ja (greška servera na "newPassword" ide ispod polja). */
interface ResetPasswordForm {
  newPassword: FormControl<string>;
  /** samo na frontendu – ne šalje se */
  confirmPassword: FormControl<string>;
}

/**
 * Nova lozinka (/auth/reset-password?email=...&token=...) – stranica na koju vodi link iz emaila
 * (vidi ForgotPasswordComponent).
 *
 * Link je nevažeći ako u adresi nema emaila ili tokena, ili ako server odbije token
 * (400 sa greškom na polju "token": istekao, već iskorišten, pogrešan). U oba slučaja stranica
 * umjesto forme prikaže isto stanje greške sa linkom za novi zahtjev.
 */
@Component({
  selector: 'app-reset-password',
  standalone: false,
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.scss',
})
export class ResetPasswordComponent extends BaseComponent implements OnInit {
  private authApi = inject(AuthApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private translate = inject(TranslateService);

  protected readonly passwordMinLength = PASSWORD_MIN_LENGTH;
  /** polje "Ponovite lozinku" je u grešci i kad je greška 'passwordMismatch' na grupi */
  protected readonly passwordMatcher = new CrossFieldErrorStateMatcher('passwordMismatch');

  email = '';
  private token = '';
  /** nema emaila/tokena u adresi ili ih je server odbio */
  linkInvalid = false;

  readonly form = new FormGroup<ResetPasswordForm>(
    {
      newPassword: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, AppValidators.password]
      }),
      confirmPassword: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required]
      })
    },
    { validators: AppValidators.passwordsMatch('newPassword', 'confirmPassword') }
  );

  ngOnInit(): void {
    // queryParamMap vraća već dekodirane vrijednosti (link u emailu je URL-enkodiran)
    const params = this.route.snapshot.queryParamMap;
    this.email = params.get('email')?.trim() ?? '';
    this.token = params.get('token')?.trim() ?? '';
    this.linkInvalid = !this.email || !this.token;
  }

  onSubmit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.isLoading || this.linkInvalid) {
      return;
    }

    this.startLoading();
    this.authApi
      .resetPassword({
        email: this.email,
        token: this.token,
        newPassword: this.form.controls.newPassword.value
      })
      .subscribe({
        next: () => {
          this.stopLoading();
          // server je odjavio sve sesije korisnika → prijava novom lozinkom
          this.toaster.success(this.translate.instant('AUTH.RESET_PASSWORD.SUCCESS'));
          this.router.navigate(['/auth/login']);
        },
        error: (err: HttpErrorResponse) => {
          if (hasTokenError(err)) {
            // forma tu ne pomaže – treba novi link
            this.linkInvalid = true;
            this.stopLoading();
            return;
          }
          // npr. greška na "newPassword" → ispod polja; ostalo u banner
          this.stopLoading(applyServerErrors(this.form, err));
        }
      });
  }
}

/** 400 sa greškom na polju "token" = link je istekao, iskorišten ili pogrešan. */
function hasTokenError(err: HttpErrorResponse): boolean {
  const errors = (err.error as Partial<ErrorDto> | null)?.errors;
  return (
    err.status === 400 &&
    Array.isArray(errors) &&
    errors.some((e) => e?.field?.toLowerCase() === 'token')
  );
}
