import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Location } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, EMPTY, Subject, switchMap, tap } from 'rxjs';
import { AppointmentsApiService } from '../../../../api-services/appointments/appointments-api.service';
import { GetMyAppointmentByIdQueryDto } from '../../../../api-services/appointments/appointments-api.model';
import { BaseComponent } from '../../../../core/components/base-classes/base-component';
import { getErrorMessage } from '../../../../core/interceptors/error-logging-interceptor.service';

/** Address of the list; the "Back" button never leaves it (see backToList). */
const LIST_URL = '/client/appointments';

/**
 * Details of one appointment of the logged-in patient (/client/appointments/:id, Patient only).
 *
 * The server returns 404 both for a missing appointment and for someone else's appointment,
 * so the page shows the same "not found" message in both cases.
 */
@Component({
  selector: 'app-my-appointment-details',
  standalone: false,
  templateUrl: './my-appointment-details.component.html',
  styleUrl: './my-appointment-details.component.scss'
})
export class MyAppointmentDetailsComponent extends BaseComponent implements OnInit {
  private api = inject(AppointmentsApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private location = inject(Location);
  private destroyRef = inject(DestroyRef);

  appointment: GetMyAppointmentByIdQueryDto | null = null;
  /** true after a 404 (or an id in the URL that is not a number) */
  notFound = false;

  /** columns of the services table */
  readonly displayedColumns = ['name', 'price'];

  private id: number | null = null;
  /** every next() = "load the appointment this.id" (also used by "Try again") */
  private readonly load$ = new Subject<void>();

  ngOnInit(): void {
    // switchMap: when the id changes, the old request is cancelled
    this.load$
      .pipe(
        tap(() => {
          this.startLoading();
          this.notFound = false;
        }),
        switchMap(() =>
          this.api.getMyById(this.id!).pipe(
            catchError((err: HttpErrorResponse) => {
              this.appointment = null;
              if (err.status === 404) {
                this.notFound = true;
                this.stopLoading();
              } else {
                this.stopLoading(getErrorMessage(err));
              }
              return EMPTY;
            })
          )
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((appointment) => {
        this.appointment = appointment;
        this.stopLoading();
      });

    // paramMap (and not snapshot), so the page also reacts if only :id changes
    this.route.paramMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => {
        const raw = params.get('id') ?? '';
        // only digits, e.g. "abc" or "-1" is not sent to the server: it cannot exist anyway
        this.id = /^\d+$/.test(raw) && Number(raw) >= 1 ? Number(raw) : null;
        if (this.id === null) {
          this.appointment = null;
          this.notFound = true;
          return;
        }
        this.load();
      });
  }

  load(): void {
    this.load$.next();
  }

  /**
   * Back to the list WITH the filters it had: the list sends its URL in the router state
   * (state.listUrl). The browser keeps that state in history, so it also works after F5.
   * The value is used only if it is the list address; otherwise (e.g. the details page was
   * opened from a link or a bookmark) we go to the list without filters.
   */
  backToList(): void {
    const state = this.location.getState() as { listUrl?: unknown } | null;
    const listUrl = state?.listUrl;
    const isListUrl =
      typeof listUrl === 'string' && (listUrl === LIST_URL || listUrl.startsWith(`${LIST_URL}?`));

    this.router.navigateByUrl(isListUrl ? listUrl : LIST_URL);
  }
}
