import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ClientLayoutComponent } from './client-layout/client-layout.component';
import { ClientHomeComponent } from './client-home/client-home.component';
import { MyProfileComponent } from '../account/my-profile/my-profile.component';
import { MyAppointmentsComponent } from './appointments/my-appointments/my-appointments.component';
import { MyAppointmentDetailsComponent } from './appointments/my-appointment-details/my-appointment-details.component';
import { DentistScheduleComponent } from './schedule/dentist-schedule/dentist-schedule.component';
import { myAuthData, myAuthGuard } from '../../core/guards/my-auth-guard';

// Rute ispod /client (guard u app-routing-module.ts: bilo koji prijavljeni korisnik)
const routes: Routes = [
  {
    path: '',
    component: ClientLayoutComponent,
    children: [
      // default → /client
      { path: '', component: ClientHomeComponent, pathMatch: 'full' },
      // ista stranica kao /admin/profile (deklarisana u AccountModule)
      { path: 'profile', component: MyProfileComponent },
      // "Moji termini" – samo pacijent (admin i stomatolog idu na svoju početnu rutu)
      {
        path: 'appointments',
        component: MyAppointmentsComponent,
        canActivate: [myAuthGuard],
        data: myAuthData({ requirePatient: true })
      },
      {
        path: 'appointments/:id',
        component: MyAppointmentDetailsComponent,
        canActivate: [myAuthGuard],
        data: myAuthData({ requirePatient: true })
      },
      // "Moj raspored" – samo stomatolog (?date=yyyy-MM-dd, bez date = danas)
      {
        path: 'schedule',
        component: DentistScheduleComponent,
        canActivate: [myAuthGuard],
        data: myAuthData({ requireDentist: true })
      },
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ClientRoutingModule { }
