import {NgModule} from '@angular/core';

import {ClientRoutingModule} from './client-routing-module';
import {SharedModule} from '../shared/shared-module';
import {ClientLayoutComponent} from './client-layout/client-layout.component';
import {ClientHomeComponent} from './client-home/client-home.component';
import {AccountModule} from '../account/account-module';
import {MyAppointmentsComponent} from './appointments/my-appointments/my-appointments.component';
import {MyAppointmentDetailsComponent} from './appointments/my-appointment-details/my-appointment-details.component';
import {RescheduleAppointmentDialogComponent} from './appointments/reschedule-appointment-dialog/reschedule-appointment-dialog.component';
import {DentistScheduleComponent} from './schedule/dentist-schedule/dentist-schedule.component';


@NgModule({
  declarations: [
    ClientLayoutComponent,
    ClientHomeComponent,
    MyAppointmentsComponent,
    MyAppointmentDetailsComponent,
    RescheduleAppointmentDialogComponent,
    DentistScheduleComponent
  ],
  imports: [
    SharedModule,
    ClientRoutingModule,
    // "Moj profil" (/client/profile) – ista stranica kao /admin/profile
    AccountModule
  ]
})
export class ClientModule { }
