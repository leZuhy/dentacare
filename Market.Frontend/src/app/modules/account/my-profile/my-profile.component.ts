import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, FormGroupDirective, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { AuthApiService } from '../../../api-services/auth/auth-api.service';
import { GetMeQueryDto, UpdateMeCommand } from '../../../api-services/auth/auth-api.model';
import { BaseFormComponent } from '../../../core/components/base-classes/base-form-component';
import { applyServerErrors } from '../../../core/forms/server-errors';
import {
  AppValidators,
  CrossFieldErrorStateMatcher,
  PASSWORD_MIN_LENGTH
} from '../../../core/forms/validators';
import { getErrorMessage } from '../../../core/interceptors/error-logging-interceptor.service';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';
import { ToasterService } from '../../../core/services/toaster.service';
import { USER_CONSTRAINTS } from '../user-constraints';

/** Forma (a): lični podaci. Imena kontrola = imena polja API-ja (PUT /api/auth/me). */
interface ProfileForm {
  firstname: FormControl<string>;
  lastname: FormControl<string>;
  address: FormControl<string>;
  city: FormControl<string>;
}

/** Forma (b): promjena lozinke (POST /api/auth/change-password). */
interface ChangePasswordForm {
  currentPassword: FormControl<string>;
  newPassword: FormControl<string>;
  /** samo na frontendu – ne šalje se */
  confirmPassword: FormControl<string>;
}

/**
 * "Moj profil" – ista stranica za admina (/admin/profile) i kupca (/client/profile).
 * Deklarisana je u AccountModule (vidi account-module.ts).
 *
 * PRIMJER: dvije nezavisne forme na jednoj stranici.
 * (a) profil – BaseFormComponent: this.form, loadData() = GET /api/auth/me, save() = PUT /api/auth/me
 *     (email se samo prikazuje; adresu i grad koristi narudžba)
 * (b) lozinka – vlastita forma, stanje (isChangingPassword, passwordError) i submit.
 *     Server vraća NOVE tokene i odjavljuje ostale uređaje; tokene snima AuthFacadeService
 *     (kao kod refresh-a), pa ovaj korisnik ostaje prijavljen.
 */
@Component({
  selector: 'app-my-profile',
  standalone: false,
  templateUrl: './my-profile.component.html',
  styleUrl: './my-profile.component.scss'
})
export class MyProfileComponent
  extends BaseFormComponent<GetMeQueryDto, FormGroup<ProfileForm>>
  implements OnInit
{
  private authApi = inject(AuthApiService);
  private auth = inject(AuthFacadeService);
  private toaster = inject(ToasterService);
  private translate = inject(TranslateService);
  private destroyRef = inject(DestroyRef);

  private readonly limits = USER_CONSTRAINTS;
  protected readonly passwordMinLength = PASSWORD_MIN_LENGTH;
  /** polje "Ponovite lozinku" je u grešci i kad je greška 'passwordMismatch' na grupi */
  protected readonly passwordMatcher = new CrossFieldErrorStateMatcher('passwordMismatch');

  // === (b) promjena lozinke ===
  isChangingPassword = false;
  passwordError: string | null = null;

  readonly passwordForm = new FormGroup<ChangePasswordForm>(
    {
      currentPassword: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required]
      }),
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
    this.form = this.createProfileForm();
    this.initForm(true); // profil uvijek postoji → "edit" → loadData()
  }

  // === (a) profil ===

  protected loadData(): void {
    this.startLoading();

    this.authApi
      .getMe()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (me) => {
          this.model = me;
          // reset (a ne setValue): forma je poslije učitavanja "čista" (pristine)
          this.form.reset({
            firstname: me.firstname,
            lastname: me.lastname,
            address: me.address ?? '',
            city: me.city ?? ''
          });
          this.stopLoading();
        },
        error: (err) => this.stopLoading(getErrorMessage(err))
      });
  }

  retry(): void {
    this.loadData();
  }

  protected save(): void {
    if (this.isLoading) {
      return;
    }

    const value = this.form.getRawValue();
    const command: UpdateMeCommand = {
      firstname: value.firstname.trim(),
      lastname: value.lastname.trim(),
      // prazno polje → null (adresa i grad nisu obavezni)
      address: value.address.trim() || null,
      city: value.city.trim() || null
    };

    this.startLoading();
    this.authApi.updateMe(command).subscribe({
      next: () => {
        this.stopLoading();
        // snimljeno → forma je ponovo "čista", dugme "Sačuvaj" se gasi do sljedeće izmjene
        this.form.markAsPristine();
        this.toaster.success(this.translate.instant('ACCOUNT.PROFILE.SAVED'));
      },
      error: (err) => this.stopLoading(applyServerErrors(this.form, err))
    });
  }

  // === (b) promjena lozinke ===

  /**
   * formDirective = <form #passwordFormDirective="ngForm"> iz templatea. Poslije uspjeha zovemo
   * formDirective.resetForm(), a ne samo passwordForm.reset(): resetForm() briše i "submitted",
   * pa prazna obavezna polja ne pocrvene odmah nakon uspješne promjene.
   */
  changePassword(formDirective: FormGroupDirective): void {
    this.passwordForm.markAllAsTouched();
    if (this.passwordForm.invalid || this.isChangingPassword) {
      return;
    }

    const value = this.passwordForm.getRawValue();
    this.isChangingPassword = true;
    this.passwordError = null;

    this.auth
      .changePassword({ currentPassword: value.currentPassword, newPassword: value.newPassword })
      .subscribe({
        next: () => {
          this.isChangingPassword = false;
          formDirective.resetForm();
          this.toaster.success(this.translate.instant('ACCOUNT.CHANGE_PASSWORD.SUCCESS'), 5000);
        },
        error: (err) => {
          this.isChangingPassword = false;
          // pogrešna trenutna lozinka → 400 na "currentPassword" → poruka ispod tog polja
          this.passwordError = applyServerErrors(this.passwordForm, err);
        }
      });
  }

  private createProfileForm(): FormGroup<ProfileForm> {
    return new FormGroup<ProfileForm>({
      firstname: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(this.limits.firstnameMaxLength)]
      }),
      lastname: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(this.limits.lastnameMaxLength)]
      }),
      address: new FormControl('', {
        nonNullable: true,
        validators: [Validators.maxLength(this.limits.addressMaxLength)]
      }),
      city: new FormControl('', {
        nonNullable: true,
        validators: [Validators.maxLength(this.limits.cityMaxLength)]
      })
    });
  }
}
