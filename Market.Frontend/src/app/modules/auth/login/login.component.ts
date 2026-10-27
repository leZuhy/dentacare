import { Component, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { BaseComponent } from '../../../core/components/base-classes/base-component';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';
import { LoginCommand } from '../../../api-services/auth/auth-api.model';
import { CurrentUserService } from '../../../core/services/auth/current-user.service';
import { isSafeReturnUrl } from '../../../core/services/auth/return-url';
import { getErrorMessage } from '../../../core/interceptors/error-logging-interceptor.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-login',
  standalone: false,
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent extends BaseComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthFacadeService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private currentUser = inject(CurrentUserService);
  hidePassword = true;

  // Seedani admin (Market.API DynamicDataSeeder) popunjen je samo u development buildu,
  // radi bržeg testiranja. U production/testing buildu polja su prazna.
  form = this.fb.group({
    email: [environment.production ? '' : 'admin@market.local', [Validators.required, Validators.email]],
    password: [environment.production ? '' : 'Admin123!', [Validators.required]],
  });

  onSubmit(): void {
    if (this.form.invalid || this.isLoading) return;

    this.startLoading();

    const payload: LoginCommand = {
      email: this.form.value.email ?? '',
      password: this.form.value.password ?? '',
      fingerprint: null,
    };

    this.auth.login(payload).subscribe({
      next: () => {
        this.stopLoading();
        // ?returnUrl=... postavlja guard (myAuthGuard). Vjerujemo mu samo ako je putanja
        // unutar aplikacije; inače korisnik ide na svoju početnu stranicu.
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        if (isSafeReturnUrl(returnUrl)) {
          this.router.navigateByUrl(returnUrl);
        } else {
          this.router.navigate([this.currentUser.getDefaultRoute()]);
        }
      },
      error: (err: HttpErrorResponse) => {
        // 401 → poruka sa backenda ("Pogrešan email ili lozinka."),
        // ostalo (npr. server nije dostupan) → opšta poruka po statusu
        this.stopLoading(getErrorMessage(err));
        if (err.status !== 401) {
          console.error('Login error:', err);
        }
      },
    });
  }
}
