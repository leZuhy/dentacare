import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { AuthApiService } from '../../../api-services/auth/auth-api.service';
import { GetMeQueryDto } from '../../../api-services/auth/auth-api.model';
import { OrdersApiService } from '../../../api-services/orders/orders-api.service';
import { CreateOrderCommand } from '../../../api-services/orders/orders-api.models';
import { BaseComponent } from '../../../core/components/base-classes/base-component';
import { applyServerErrors } from '../../../core/forms/server-errors';
import { getErrorMessage } from '../../../core/interceptors/error-logging-interceptor.service';
import { CartService } from '../../../core/services/cart/cart.service';
import { CurrencyService } from '../../../core/services/currency/currency.service';
import { ToasterService } from '../../../core/services/toaster.service';

/** Forma drugog koraka (tipizirana: form.controls.note.value je string). */
interface CheckoutDetailsForm {
  note: FormControl<string>;
  confirmed: FormControl<boolean>;
}

/**
 * PRIMJER: Material stepper (mat-stepper) – narudžba u tri koraka.
 *
 *   1. Korpa      – pregled i izmjena količina (CartItemsComponent); korak je završen kad korpa nije prazna
 *   2. Napomena   – forma detailsForm; [stepControl] ne pušta dalje dok forma nije ispravna
 *   3. Potvrda    – adresa za dostavu iz profila (GET /api/auth/me, samo prikaz),
 *                   POST /Orders → /client/orders/{id}
 *
 * Stepper je "linear": korak se ne može preskočiti dok prethodni nije završen.
 * Ruta /checkout traži prijavu (myAuthGuard u public-routing-module.ts) – neprijavljen kupac
 * ide na login i nakon prijave se vraća ovdje (returnUrl).
 *
 * Narudžba nema svoju adresu: backend uz narudžbu vraća adresu i grad iz profila kupca. Zato
 * se ovdje adresa samo prikazuje, a mijenja se na stranici "Moj profil" (/client/profile).
 */
@Component({
  selector: 'app-checkout',
  standalone: false,
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.scss'
})
export class CheckoutComponent extends BaseComponent implements OnInit {
  protected cart = inject(CartService);
  protected currency = inject(CurrencyService);
  private ordersApi = inject(OrdersApiService);
  private authApi = inject(AuthApiService);
  private destroyRef = inject(DestroyRef);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private translate = inject(TranslateService);

  readonly noteMaxLength = 500;

  readonly detailsForm = new FormGroup<CheckoutDetailsForm>({
    note: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(this.noteMaxLength)]
    }),
    // checkbox "Potvrđujem narudžbu" – requiredTrue: mora biti označen
    confirmed: new FormControl(false, { nonNullable: true, validators: [Validators.requiredTrue] })
  });

  /** profil kupca (adresa za dostavu); null dok se učitava ili ako učitavanje nije uspjelo */
  profile: GetMeQueryDto | null = null;
  profileError: string | null = null;

  /** adresa je potpuna kad su unesena oba polja */
  get hasDeliveryAddress(): boolean {
    return !!this.profile?.address && !!this.profile?.city;
  }

  ngOnInit(): void {
    this.authApi
      .getMe()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profile) => (this.profile = profile),
        // adresa je samo informacija – narudžba se može poslati i bez nje
        error: (err) => (this.profileError = getErrorMessage(err))
      });
  }

  submit(): void {
    if (this.cart.isEmpty() || this.isLoading) {
      return;
    }
    if (this.detailsForm.invalid) {
      this.detailsForm.markAllAsTouched();
      return;
    }

    this.startLoading();

    const payload: CreateOrderCommand = {
      note: this.detailsForm.controls.note.value.trim() || null,
      // šaljemo samo proizvod i količinu – cijene i popuste računa server
      items: this.cart.items().map((item) => ({
        productId: item.productId,
        quantity: item.quantity
      }))
    };

    // Namjerno bez takeUntilDestroyed: i ako korisnik ode sa stranice, narudžba je na serveru
    // kreirana, pa korpu treba isprazniti.
    this.ordersApi.create(payload).subscribe({
      next: ({ id }) => {
        // isLoading ostaje true do odlaska na detalje (inače bi se na trenutak vidjela prazna korpa)
        this.cart.clear();
        this.toaster.success(this.translate.instant('CHECKOUT.SUCCESS'));
        this.router.navigate(['/client/orders', id]);
      },
      error: (err) => {
        // greška polja "note" ide ispod polja; ostalo (npr. nema dovoljno zaliha) u banner
        this.stopLoading(applyServerErrors(this.detailsForm, err));
      }
    });
  }
}
