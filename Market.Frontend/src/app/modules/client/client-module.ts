import {NgModule} from '@angular/core';

import {ClientRoutingModule} from './client-routing-module';
import {SharedModule} from '../shared/shared-module';
import {ClientLayoutComponent} from './client-layout/client-layout.component';
import {ClientHomeComponent} from './client-home/client-home.component';
import {ClientOrdersComponent} from './client-orders/client-orders.component';
import {ClientOrderDetailsComponent} from './client-order-details/client-order-details.component';
import {AccountModule} from '../account/account-module';


@NgModule({
  declarations: [
    ClientLayoutComponent,
    ClientHomeComponent,
    ClientOrdersComponent,
    ClientOrderDetailsComponent
  ],
  imports: [
    SharedModule,
    ClientRoutingModule,
    // "Moj profil" (/client/profile) – ista stranica kao /admin/profile
    AccountModule
  ]
})
export class ClientModule { }
