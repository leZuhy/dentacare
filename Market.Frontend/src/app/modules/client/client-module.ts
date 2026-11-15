import {NgModule} from '@angular/core';

import {ClientRoutingModule} from './client-routing-module';
import {SharedModule} from '../shared/shared-module';
import {ClientLayoutComponent} from './client-layout/client-layout.component';
import {ClientHomeComponent} from './client-home/client-home.component';
import {AccountModule} from '../account/account-module';
import {MyAppointmentsComponent} from './appointments/my-appointments/my-appointments.component';
import {MyAppointmentDetailsComponent} from './appointments/my-appointment-details/my-appointment-details.component';


@NgModule({
  declarations: [
    ClientLayoutComponent,
    ClientHomeComponent,
    MyAppointmentsComponent,
    MyAppointmentDetailsComponent
  ],
  imports: [
    SharedModule,
    ClientRoutingModule,
    // "Moj profil" (/client/profile) – ista stranica kao /admin/profile
    AccountModule
  ]
})
export class ClientModule { }
