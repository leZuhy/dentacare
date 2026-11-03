import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { filter, switchMap, tap } from 'rxjs';
import { WorkingHoursApiService } from '../../../../api-services/working-hours/working-hours-api.service';
import {
  DayOfWeekNumber,
  ListWorkingHoursQueryDto,
  ListWorkingHoursRequest,
  toDisplayTime,
  WORK_DAYS
} from '../../../../api-services/working-hours/working-hours-api.model';
import { DentistsApiService } from '../../../../api-services/dentists/dentists-api.service';
import {
  ListDentistQueryDto,
  ListDentistRequest
} from '../../../../api-services/dentists/dentists-api.model';
import { BaseListPagedComponent } from '../../../../core/components/base-classes/base-list-paged-component';
import { allItemsPaging } from '../../../../core/models/paging/paging-utils';
import { getErrorMessage } from '../../../../core/interceptors/error-logging-interceptor.service';
import { ToasterService } from '../../../../core/services/toaster.service';
import { DialogHelperService } from '../../../shared/services/dialog-helper.service';
import { DialogButton } from '../../../shared/models/dialog-config.model';

/**
 * Working hours list (/admin/working-hours): filter by dentist and day, paging, delete.
 * Add and edit are on a separate page (WorkingHoursUpsertComponent).
 */
@Component({
  selector: 'app-working-hours-list',
  standalone: false,
  templateUrl: './working-hours-list.component.html',
  styleUrl: './working-hours-list.component.scss'
})
export class WorkingHoursListComponent
  extends BaseListPagedComponent<ListWorkingHoursQueryDto, ListWorkingHoursRequest>
  implements OnInit
{
  private api = inject(WorkingHoursApiService);
  private dentistsApi = inject(DentistsApiService);
  private dialogHelper = inject(DialogHelperService);
  private toaster = inject(ToasterService);
  private translate = inject(TranslateService);
  private destroyRef = inject(DestroyRef);

  readonly displayedColumns = ['dentistFullName', 'dayOfWeek', 'startTime', 'endTime', 'actions'];

  /** days for the filter, Monday first */
  readonly days = WORK_DAYS;
  /** used in the template to show "08:00" instead of "08:00:00" */
  readonly toDisplayTime = toDisplayTime;

  /** null = all dentists */
  readonly dentistFilter = new FormControl<number | null>(null);
  /** null = all days */
  readonly dayFilter = new FormControl<DayOfWeekNumber | null>(null);

  /** options for the dentist dropdown */
  dentists: ListDentistQueryDto[] = [];

  constructor() {
    super();
    this.request = new ListWorkingHoursRequest();
  }

  ngOnInit(): void {
    this.initList();
    this.loadDentists();

    this.dentistFilter.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((dentistId) => {
        this.request.dentistId = dentistId;
        this.request.paging.page = 1;
        this.loadPagedData();
      });

    this.dayFilter.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((dayOfWeek) => {
        this.request.dayOfWeek = dayOfWeek;
        this.request.paging.page = 1;
        this.loadPagedData();
      });
  }

  protected loadPagedData(): void {
    this.startLoading();

    this.api
      .list(this.request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.handlePageResult(response);
          this.stopLoading();
        },
        error: (err) => this.stopLoading(getErrorMessage(err))
      });
  }

  /** all dentists for the filter (one request, up to 1000 items) */
  private loadDentists(): void {
    const request = new ListDentistRequest();
    request.paging = allItemsPaging;

    this.dentistsApi
      .list(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => (this.dentists = response.items),
        // the list still works without the filter, so only show a message
        error: (err) => this.toaster.error(getErrorMessage(err))
      });
  }

  onDelete(row: ListWorkingHoursQueryDto): void {
    // e.g. "Ana Kovač, Ponedjeljak 08:00–12:00"; the day name must be translated here,
    // because the dialog only inserts the text into the message
    const day = this.translate.instant(`WORKING_HOURS.DAYS.${row.dayOfWeek}`);
    const name = `${row.dentistFullName}, ${day} ${toDisplayTime(row.startTime)}–${toDisplayTime(row.endTime)}`;

    this.dialogHelper
      .confirm('WORKING_HOURS.DELETE_TITLE', 'WORKING_HOURS.DELETE_MESSAGE', { name })
      .pipe(
        filter((result) => result?.button === DialogButton.YES),
        tap(() => this.startLoading()),
        switchMap(() => this.api.delete(row.id))
      )
      .subscribe({
        next: () => {
          this.toaster.success(this.translate.instant('WORKING_HOURS.DELETED'));
          this.loadPagedData();
        },
        error: (err) => {
          this.stopLoading();
          this.toaster.error(getErrorMessage(err));
        }
      });
  }
}
