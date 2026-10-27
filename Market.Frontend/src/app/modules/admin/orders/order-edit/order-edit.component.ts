import { Component, DestroyRef, HostListener, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormArray,
  FormControl,
  FormGroup,
  ValidationErrors,
  Validators
} from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { switchMap, tap } from 'rxjs';
import { OrdersApiService } from '../../../../api-services/orders/orders-api.service';
import {
  GetByIdOrderQueryDtoItem,
  GetOrderByIdQueryDto,
  OrderStatusType,
  UpdateOrderCommand
} from '../../../../api-services/orders/orders-api.models';
import { OrderStatusHelper } from '../../../../api-services/orders/order-status.helper';
import { BaseComponent } from '../../../../core/components/base-classes/base-component';
import { applyServerErrors } from '../../../../core/forms/server-errors';
import { AppValidators } from '../../../../core/forms/validators';
import { getErrorMessage } from '../../../../core/interceptors/error-logging-interceptor.service';
import { ToasterService } from '../../../../core/services/toaster.service';
import { HasUnsavedChanges } from '../../../shared/guards/unsaved-changes.guard';
import {
  isProductOption,
  ProductOption,
  ProductPickerValue,
  productSelectedValidator
} from './product-autocomplete/product-autocomplete.component';

/** Jedna stavka narudžbe u formi. */
interface OrderItemForm {
  /** 0 = nova stavka; > 0 = postojeća stavka (backend je ažurira) */
  id: FormControl<number>;
  product: FormControl<ProductPickerValue>;
  quantity: FormControl<number | null>;
}

interface OrderEditForm {
  note: FormControl<string>;
  items: FormArray<FormGroup<OrderItemForm>>;
}

/**
 * Validator na FormArray-u (cross-row): isti proizvod ne smije biti u dvije stavke
 * (backend to isto odbija). Greška je na nizu: { duplicateProduct: true }.
 */
function uniqueProductsValidator(control: AbstractControl): ValidationErrors | null {
  const ids = (control.value as { product: ProductPickerValue }[])
    .map((item) => item.product)
    .filter(isProductOption)
    .map((product) => product.id);
  return new Set(ids).size === ids.length ? null : { duplicateProduct: true };
}

/**
 * Izmjena narudžbe (/admin/orders/:id/edit) – samo za status Draft.
 *
 * PRIMJERI na ovoj stranici:
 * - tipizirani FormArray stavki (dodavanje/uklanjanje redova, validator na nizu)
 * - autocomplete sa pretragom na serveru (ProductAutocompleteComponent) – i u postojećim stavkama:
 *   stavci se može promijeniti proizvod (backend joj tada daje trenutnu cijenu novog proizvoda)
 * - zaštita nesačuvanih izmjena: CanDeactivate guard (unsavedChangesGuard u admin-routing-module.ts)
 *   + 'beforeunload' za refresh/zatvaranje taba
 *
 * Iznosi u formi su PROCJENA (cijena × količina). Popuste i konačne iznose računa server –
 * nakon snimanja stranica ponovo učita narudžbu i prikaže iznose sa servera.
 */
@Component({
  selector: 'app-order-edit',
  standalone: false,
  templateUrl: './order-edit.component.html',
  styleUrl: './order-edit.component.scss'
})
export class OrderEditComponent extends BaseComponent implements OnInit, HasUnsavedChanges {
  private ordersApi = inject(OrdersApiService);
  private route = inject(ActivatedRoute);
  private toaster = inject(ToasterService);
  private translate = inject(TranslateService);
  private destroyRef = inject(DestroyRef);

  protected readonly statusHelper = OrderStatusHelper;
  readonly noteMaxLength = 500;

  orderId!: number;
  /** narudžba kako je na serveru (zadnje učitavanje / snimanje) */
  order: GetOrderByIdQueryDto | null = null;
  isSaving = false;
  /** greška pri snimanju (greška učitavanja je u errorMessage iz BaseComponent) */
  saveError: string | null = null;

  readonly form = new FormGroup<OrderEditForm>({
    note: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(this.noteMaxLength)]
    }),
    items: new FormArray<FormGroup<OrderItemForm>>([], {
      // required na FormArray-u: bar jedna stavka (prazan niz je "prazna vrijednost")
      validators: [Validators.required, uniqueProductsValidator]
    })
  });

  get items(): FormArray<FormGroup<OrderItemForm>> {
    return this.form.controls.items;
  }

  get isDraft(): boolean {
    return this.order?.status === OrderStatusType.Draft;
  }

  ngOnInit(): void {
    this.orderId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadOrder();
  }

  loadOrder(): void {
    this.startLoading();

    this.ordersApi
      .getById(this.orderId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (order) => {
          this.fillForm(order);
          this.stopLoading();
        },
        error: (err) => this.stopLoading(getErrorMessage(err))
      });
  }

  addItem(): void {
    this.items.push(this.createItem());
    // programske izmjene ne označavaju formu kao izmijenjenu – za guard to radimo ručno
    this.form.markAsDirty();
  }

  removeItem(index: number): void {
    this.items.removeAt(index);
    this.form.markAsDirty();
  }

  /** Procjena za red: cijena × količina (null dok proizvod nije izabran). */
  lineTotal(item: FormGroup<OrderItemForm>): number | null {
    const product = item.controls.product.value;
    const quantity = item.controls.quantity.value;
    if (!isProductOption(product) || !quantity || quantity < 0) {
      return null;
    }
    return product.price * quantity;
  }

  unitPrice(item: FormGroup<OrderItemForm>): number | null {
    const product = item.controls.product.value;
    return isProductOption(product) ? product.price : null;
  }

  /** Procjena ukupnog iznosa BEZ popusta (popuste računa server). */
  get previewSubtotal(): number {
    return this.items.controls.reduce((sum, item) => sum + (this.lineTotal(item) ?? 0), 0);
  }

  /**
   * Iznos stavke sa servera (zadnje snimanje) – samo za snimljenu stavku kojoj proizvod nije
   * promijenjen. Ako je izabran drugi proizvod, snimljeni iznos se odnosi na stari proizvod.
   */
  serverItem(item: FormGroup<OrderItemForm>): GetByIdOrderQueryDtoItem | undefined {
    const id = item.controls.id.value;
    const saved = id > 0 ? this.order?.items.find((i) => i.id === id) : undefined;
    const product = item.controls.product.value;
    return saved && isProductOption(product) && product.id === saved.product.productId
      ? saved
      : undefined;
  }

  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.isSaving) {
      return;
    }

    const value = this.form.getRawValue();
    const command: UpdateOrderCommand = {
      note: value.note.trim() || null,
      items: value.items.map((item) => ({
        id: item.id,
        // validator garantuje da je proizvod izabran
        productId: isProductOption(item.product) ? item.product.id : 0,
        quantity: item.quantity ?? 0
      }))
    };

    this.isSaving = true;
    this.saveError = null;
    let saved = false;

    this.ordersApi
      .update(this.orderId, command)
      .pipe(
        tap(() => {
          saved = true;
          this.toaster.success(this.translate.instant('ORDERS.EDIT.SAVED'));
        }),
        // ponovo učitaj: popuste, cijene i ukupan iznos je izračunao server
        switchMap(() => this.ordersApi.getById(this.orderId))
      )
      .subscribe({
        next: (order) => {
          this.isSaving = false;
          this.fillForm(order);
        },
        error: (err) => {
          this.isSaving = false;
          if (saved) {
            // izmjene SU snimljene, samo ponovno učitavanje nije uspjelo
            this.form.markAsPristine();
            this.saveError = getErrorMessage(err);
          } else {
            // "items[0].quantity" → greška ispod količine u prvom redu; ostalo u banner
            this.saveError = applyServerErrors(this.form, err);
          }
        }
      });
  }

  /** CanDeactivate guard pita samo kad ima nesačuvanih izmjena. */
  hasUnsavedChanges(): boolean {
    return this.form.dirty && !this.isSaving;
  }

  /** Refresh ili zatvaranje taba – browser prikaže svoje (neprevodivo) upozorenje. */
  @HostListener('window:beforeunload', ['$event'])
  onBeforeUnload(event: BeforeUnloadEvent): void {
    if (this.hasUnsavedChanges()) {
      event.preventDefault();
    }
  }

  private fillForm(order: GetOrderByIdQueryDto): void {
    this.order = order;
    this.form.controls.note.setValue(order.note ?? '');
    this.items.clear();
    order.items.forEach((item) => this.items.push(this.createItem(item)));
    // učitano stanje je "čisto" – guard ne pita dok korisnik nešto ne promijeni
    this.form.markAsPristine();
    this.form.markAsUntouched();
  }

  private createItem(item?: GetByIdOrderQueryDtoItem): FormGroup<OrderItemForm> {
    // postojeća stavka: proizvod sa cijenom iz narudžbe. Kad korisnik izabere drugi proizvod,
    // vrijednost kontrole postaje taj ProductOption, pa i procjena (unitPrice, lineTotal) prati
    // njegovu trenutnu cijenu – istu koju će backend upisati u stavku.
    const product: ProductOption | null = item
      ? {
          id: item.product.productId,
          name: item.product.productName ?? '',
          price: item.unitPrice,
          isEnabled: true
        }
      : null;

    return new FormGroup<OrderItemForm>({
      id: new FormControl(item?.id ?? 0, { nonNullable: true }),
      product: new FormControl<ProductPickerValue>(product, [
        Validators.required,
        productSelectedValidator
      ]),
      quantity: new FormControl<number | null>(item?.quantity ?? 1, [
        Validators.required,
        Validators.min(1),
        AppValidators.integer
      ])
    });
  }
}
