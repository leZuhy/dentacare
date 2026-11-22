import { Component, DestroyRef, inject, OnInit, ViewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Params, Router } from '@angular/router'; // NEW: Params, Router
import { forkJoin, Subscription } from 'rxjs'; // NEW
import { PublicCatalogApiService } from '../../../api-services/public-catalog/public-catalog-api.service';
import {
  ListPublicDentistsQueryDto, // NEW
  ListPublicDentistsRequest, // NEW
  ListPublicServicesQueryDto,
  ListPublicFreeSlotsQueryDto,
  ListPublicServicesRequest,
} from '../../../api-services/public-catalog/public-catalog-api.model';
import { allItemsPaging } from '../../../core/models/paging/paging-utils';
import { getErrorMessage } from '../../../core/interceptors/error-logging-interceptor.service';
import { readPositiveInt } from '../public-catalog-query-params';
import {BOOKING_CONSTRAINTS} from './booking-constraint';
import {environment} from '../../../../environments/environment';
import {addDays, clinicToday, readDateOnly, toDateOnly, utcToClinicDateOnly} from '../../../core/forms/date-only';
import { HttpErrorResponse } from '@angular/common/http';
import { MatStepper } from '@angular/material/stepper';
import { AppointmentsApiService } from '../../../api-services/appointments/appointments-api.service';
import { CreateAppointmentCommandDto } from '../../../api-services/appointments/appointments-api.model';
import { CurrentUserService } from '../../../core/services/auth/current-user.service';
import { ToasterService } from '../../../core/services/toaster.service';
import {AppValidators} from '../../../core/forms/validators';


/** Step 1. Same rule as CreateAppointmentCommandValidator: DentalServiceId > 0. */
interface ServiceStepForm {
  dentalServiceId: FormControl<number | null>;
}

/** Step 2. Same rule as CreateAppointmentCommandValidator: DentistId > 0. */
interface DentistStepForm { // NEW
  dentistId: FormControl<number | null>;
}

/**
 * Step 3. date = first day shown (clinic calendar day from the datepicker),
 * startUtc = chosen slot, exactly as GET api/public/free-slots returned it.
 */
interface SlotStepForm {
  date: FormControl<Date | null>;
  startUtc: FormControl<string | null>;
}

/** One day in the slot list. Days without free slots are shown too. */
interface SlotDay {
  date: Date;
  slots: ListPublicFreeSlotsQueryDto[];
}


/**
 * Booking stepper (/book, public): service -> dentist -> date and slot -> confirmation.
 * Steps 1–3 work for anyone (free slots are public), confirming needs a logged-in patient.
 * The choices live in the URL (?serviceId=&dentistId=&date=&start=), so they survive
 * a page refresh and the redirect to login and back.
 * The forms are the source of truth; the URL is read once on open and then only written.
 */
@Component({
  selector: 'app-booking',
  standalone: false,
  templateUrl: './booking.component.html',
  styleUrl: './booking.component.scss'
})
export class BookingComponent implements OnInit {
  private catalogApi = inject(PublicCatalogApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router); // NEW
  private destroyRef = inject(DestroyRef);
  private appointmentsApi = inject(AppointmentsApiService);
  private toaster = inject(ToasterService);
  /** who is logged in (signals: isAuthenticated(), isPatient()); read in the HTML */
  readonly currentUser = inject(CurrentUserService);

  /** <mat-stepper #stepper> from the HTML; undefined until the lists are loaded */
  @ViewChild('stepper') stepper?: MatStepper;

  // === step 1: service ===
  readonly serviceForm = new FormGroup<ServiceStepForm>({
    dentalServiceId: new FormControl<number | null>(null, [Validators.required, Validators.min(1)]),
  });

  // === step 2: dentist === NEW
  readonly dentistForm = new FormGroup<DentistStepForm>({
    dentistId: new FormControl<number | null>(null, [Validators.required, Validators.min(1)]),
  });

  // === step 3: date and slot ===
  private readonly timeZoneId = environment.clinic.timeZoneId;
  readonly maxBookingDaysAhead = environment.clinic.maxBookingDaysAhead;
  /** today in the clinic; the datepicker does not offer earlier days */
  readonly minDate = clinicToday(this.timeZoneId);
  /** last day that can be booked (same rule as Clinic:MaxBookingDaysAhead on the backend) */
  readonly maxDate = addDays(this.minDate, this.maxBookingDaysAhead);

  readonly slotForm = new FormGroup<SlotStepForm>({
    date: new FormControl<Date | null>(null, Validators.required),
    startUtc: new FormControl<string | null>(null, [Validators.required, AppValidators.utcIsoString]),
  });

  days: SlotDay[] = [];
  isLoadingSlots = false;
  slotsError: string | null = null;
  /** the running free-slots request, so it can be cancelled when a new one starts */
  private slotsRequest?: Subscription;
  /** "start" from the URL; chosen only if it is among the free slots that the server returns */
  private startFromUrl: string | null = null;
  /** true when the slot from the URL (or, later, from a 409) is no longer free */
  slotNoLongerFree = false;

  // === step 4: confirmation ===
  isSubmitting = false;
  /** the booked appointment (201); while set, the success card replaces the stepper */
  booked: CreateAppointmentCommandDto | null = null;

  // === lists for steps 1 and 2 ===
  services: ListPublicServicesQueryDto[] = [];
  dentists: ListPublicDentistsQueryDto[] = []; // NEW
  isLoading = false; // RENAMED (was isLoadingServices)
  loadError: string | null = null; // RENAMED (was servicesError)

  ngOnInit(): void {
    this.loadLists();
    this.watchChoices(); // NEW
  }

  /** Services and dentists in parallel (max 1000 each), for the dropdowns. */
  loadLists(): void { // RENAMED (was loadServices)
    this.isLoading = true;
    this.loadError = null;

    const servicesRequest = new ListPublicServicesRequest();
    servicesRequest.paging = allItemsPaging;

    const dentistsRequest = new ListPublicDentistsRequest();
    dentistsRequest.paging = allItemsPaging;

    forkJoin({
      services: this.catalogApi.listServices(servicesRequest),
      dentists: this.catalogApi.listDentists(dentistsRequest),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.services = result.services.items;
          this.dentists = result.dentists.items;
          this.isLoading = false;
          this.applyChoicesFromUrl();
        },
        error: (err) => {
          this.isLoading = false;
          this.loadError = getErrorMessage(err);
        },
      });
  }

  /**
   * Once, when the page opens: ?serviceId=3&dentistId=1 -> pre-select them.
   * Only ids that exist in the loaded lists are taken (the URL can be edited by anyone).
   * emitEvent: false -> valueChanges does not fire, so the URL is not rewritten
   * (date and start from the URL must survive, see step 7: return from login).
   */
  private applyChoicesFromUrl(): void { // RENAMED (was applyServiceFromUrl)
    const params = this.route.snapshot.queryParamMap;

    const serviceId = readPositiveInt(params, 'serviceId');
    if (serviceId !== null && this.services.some((s) => s.id === serviceId)) {
      this.serviceForm.controls.dentalServiceId.setValue(serviceId, { emitEvent: false });
    }

    const dentistId = readPositiveInt(params, 'dentistId');
    if (dentistId !== null && this.dentists.some((d) => d.id === dentistId)) {
      this.dentistForm.controls.dentistId.setValue(dentistId, { emitEvent: false });
      // date and start only make sense when service and dentist are both known
      const serviceValue = this.serviceForm.controls.dentalServiceId.value;
      const dentistValue = this.dentistForm.controls.dentistId.value;
      if (serviceValue === null || dentistValue === null) {
        return;
      }

      const date = readDateOnly(params, 'date');
      if (date !== null && date >= this.minDate && date <= this.maxDate) {
        this.slotForm.controls.date.setValue(date, { emitEvent: false });
        this.startFromUrl = params.get('start');
        this.loadSlots();
      }
    }
  }

  /**
   * Every choice goes into the URL. A new service or dentist means the old date and
   * slot are no longer valid (other duration, other schedule), so they are removed.
   */
  private watchChoices(): void { // NEW
    this.serviceForm.controls.dentalServiceId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((serviceId) => {
        this.clearSlots();
        this.updateUrl({ serviceId, date: null, start: null });
      });

    this.dentistForm.controls.dentistId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((dentistId) => {
        this.clearSlots();
        this.updateUrl({ dentistId, date: null, start: null });
      });

    // a new date -> the old slot is no longer chosen, load the new days
    this.slotForm.controls.date.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((date) => {
        this.slotForm.controls.startUtc.setValue(null);
        const dateForUrl = date !== null && this.slotForm.controls.date.valid ? toDateOnly(date) : null;
        this.updateUrl({ date: dateForUrl, start: null });
        this.loadSlots();
      });
  }

  /**
   * Changes only the given query parameters and keeps the others (merge).
   * null removes a parameter. replaceUrl: the browser's Back button does not go
   * through every single choice, it leaves /book.
   */
  private updateUrl(params: Params): void { // NEW
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: params,
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  /**
   * Free slots from the chosen date, BOOKING_CONSTRAINTS.daysShown days (max 14 = backend rule),
   * but never after maxDate (the backend would not return those anyway).
   */
  loadSlots(): void {
    this.slotsRequest?.unsubscribe(); // cancel the previous request (C#: cts.Cancel())
    this.days = [];
    this.slotsError = null;
    this.slotNoLongerFree = false;

    const dentalServiceId = this.serviceForm.controls.dentalServiceId.value;
    const dentistId = this.dentistForm.controls.dentistId.value;
    const fromDate = this.slotForm.controls.date.value;

    // nothing to load: a choice is missing, or the date is outside [minDate, maxDate]
    if (
      dentalServiceId === null ||
      dentistId === null ||
      fromDate === null ||
      this.slotForm.controls.date.invalid
    ) {
      this.isLoadingSlots = false;
      return;
    }

    const dayCount = Math.min(BOOKING_CONSTRAINTS.daysShown, BOOKING_CONSTRAINTS.maxRangeDays);
    let toDate = addDays(fromDate, dayCount - 1);
    if (toDate > this.maxDate) {
      toDate = this.maxDate;
    }

    this.isLoadingSlots = true;
    this.slotsRequest = this.catalogApi
      .listFreeSlots({
        dentistId,
        dentalServiceId,
        fromDate: toDateOnly(fromDate),
        toDate: toDateOnly(toDate),
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (slots) => {
          this.days = this.groupByDay(fromDate, toDate, slots);
          this.isLoadingSlots = false;
          this.applyStartFromUrl(slots);
        },
        error: (err) => {
          this.isLoadingSlots = false;
          this.slotsError = getErrorMessage(err);
        },
      });
  }

  /** A new service or dentist: the old date, slot and list are no longer valid. */
  private clearSlots(): void {
    this.slotsRequest?.unsubscribe();
    this.slotForm.reset({ date: null, startUtc: null }, { emitEvent: false });
    this.days = [];
    this.isLoadingSlots = false;
    this.slotsError = null;
    this.startFromUrl = null;
    this.slotNoLongerFree = false;
  }

  /**
   * One SlotDay for every day from fromDate to toDate (empty ones too),
   * then every slot goes into the day it belongs to IN THE CLINIC
   * (23:30Z is already the next day in Sarajevo).
   */
  private groupByDay(fromDate: Date, toDate: Date, slots: ListPublicFreeSlotsQueryDto[]): SlotDay[] {
    const byDay = new Map<string, SlotDay>();

    for (let day = fromDate; day <= toDate; day = addDays(day, 1)) {
      byDay.set(toDateOnly(day), { date: day, slots: [] });
    }

    for (const slot of slots) {
      const key = utcToClinicDateOnly(slot.startUtc, this.timeZoneId);
      byDay.get(key)?.slots.push(slot);
    }

    return Array.from(byDay.values());
  }
  /** Click on a slot: chosen in the form and remembered in the URL. */
  selectSlot(slot: ListPublicFreeSlotsQueryDto): void {
    this.slotNoLongerFree = false;
    this.slotForm.controls.startUtc.setValue(slot.startUtc);
    this.updateUrl({ start: slot.startUtc });
  }

  /** The chosen slot (for the summary under the list), or undefined when none is chosen. */
  get selectedStartUtc(): string | null {
    return this.slotForm.controls.startUtc.value;
  }

  get selectedService(): ListPublicServicesQueryDto | undefined {
    return this.services.find((s) => s.id === this.serviceForm.controls.dentalServiceId.value);
  }

  get selectedDentist(): ListPublicDentistsQueryDto | undefined {
    return this.dentists.find((d) => d.id === this.dentistForm.controls.dentistId.value);
  }

  /** current address with all query parameters, used as returnUrl for login */
  get currentUrl(): string {
    return this.router.url;
  }

  /**
   * Only once, after the first load from the URL: the slot from ?start= is chosen
   * only if the server still offers it. The value that goes into the form is the
   * server's string, never the text from the URL.
   */
  private applyStartFromUrl(slots: ListPublicFreeSlotsQueryDto[]): void {
    if (this.startFromUrl === null) {
      return;
    }

    const slot = slots.find((s) => s.startUtc === this.startFromUrl);
    if (slot) {
      this.slotForm.controls.startUtc.setValue(slot.startUtc);
      this.goToStep(3); // NEW: everything is chosen -> confirmation
    } else {
      this.slotNoLongerFree = true;
      this.updateUrl({ start: null });
      this.goToStep(2); // NEW: choose another slot
    }
    this.startFromUrl = null;
  }

  /** "Confirm": POST api/appointments. Only for a logged-in patient (the backend checks it too). */
  confirm(): void {
    const dentalServiceId = this.serviceForm.controls.dentalServiceId.value;
    const dentistId = this.dentistForm.controls.dentistId.value;
    const startUtc = this.slotForm.controls.startUtc.value;

    if (
      this.isSubmitting ||
      !this.currentUser.isPatient() ||
      this.serviceForm.invalid ||
      this.dentistForm.invalid ||
      this.slotForm.invalid ||
      dentalServiceId === null ||
      dentistId === null ||
      startUtc === null
    ) {
      return;
    }

    this.isSubmitting = true;
    this.appointmentsApi
      .create({ dentistId, dentalServiceId, startUtc })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (appointment) => {
          this.isSubmitting = false;
          this.booked = appointment;
          // a refresh must not offer the same (now taken) slot again
          this.updateUrl({ date: null, start: null });
        },
        error: (err: HttpErrorResponse) => {
          this.isSubmitting = false;
          this.handleBookingError(err);
        },
      });
  }

  /**
   * 409: the slot was taken in the meantime, the patient already has an appointment
   *      at that time, or the date is more than MaxBookingDaysAhead days ahead
   *      -> server message, back to step 3, fresh free slots.
   * 404: the service was switched off or the dentist disabled in the meantime
   *      -> back to step 1, fresh lists.
   * 401/403 are already handled by the interceptors (login / "forbidden" toaster).
   */
  private handleBookingError(err: HttpErrorResponse): void {
    if (err.status === 401 || err.status === 403) {
      return;
    }

    this.toaster.error(getErrorMessage(err));

    if (err.status === 409) {
      this.slotForm.controls.startUtc.setValue(null);
      this.updateUrl({ start: null });
      this.goToStep(2);
      this.loadSlots();
    } else if (err.status === 404) {
      this.serviceForm.reset({ dentalServiceId: null }, { emitEvent: false });
      this.dentistForm.reset({ dentistId: null }, { emitEvent: false });
      this.clearSlots();
      this.updateUrl({ serviceId: null, dentistId: null, date: null, start: null });
      this.goToStep(0);
      this.loadLists();
    }
  }

  /** "Book another": everything from the start. */
  startOver(): void {
    this.booked = null;
    this.serviceForm.reset({ dentalServiceId: null }, { emitEvent: false });
    this.dentistForm.reset({ dentistId: null }, { emitEvent: false });
    this.clearSlots();
    this.updateUrl({ serviceId: null, dentistId: null, date: null, start: null });
  }

  private goToStep(index: number): void {
    if (this.stepper) {
      this.stepper.selectedIndex = index;
    }
  }
}
