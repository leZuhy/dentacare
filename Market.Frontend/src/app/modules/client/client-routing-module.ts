import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ClientLayoutComponent } from './client-layout/client-layout.component';
import { ClientHomeComponent } from './client-home/client-home.component';
import { ClientOrdersComponent } from './client-orders/client-orders.component';
import { ClientOrderDetailsComponent } from './client-order-details/client-order-details.component';
import { MyProfileComponent } from '../account/my-profile/my-profile.component';

// Rute ispod /client (guard u app-routing-module.ts: bilo koji prijavljeni korisnik)
const routes: Routes = [
  {
    path: '',
    component: ClientLayoutComponent,
    children: [
      // default → /client
      { path: '', component: ClientHomeComponent, pathMatch: 'full' },
      { path: 'orders', component: ClientOrdersComponent },
      { path: 'orders/:id', component: ClientOrderDetailsComponent },
      // ista stranica kao /admin/profile (deklarisana u AccountModule)
      { path: 'profile', component: MyProfileComponent },
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ClientRoutingModule { }
