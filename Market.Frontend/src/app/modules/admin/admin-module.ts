import {NgModule} from '@angular/core';

import {AdminRoutingModule} from './admin-routing-module';
import {AdminLayoutComponent} from './admin-layout/admin-layout.component';
import {SharedModule} from '../shared/shared-module';
import {AccountModule} from '../account/account-module';
import { ExchangeRatesComponent } from './finance/exchange-rates/exchange-rates.component';
import { AdminSettingsComponent } from './admin-settings/admin-settings.component';
import { SpecializationsListComponent } from './catalog/specializations/specializations-list/specializations-list.component';
import { SpecializationUpsertComponent } from './catalog/specializations/specialization-upsert/specialization-upsert.component';
import { ServiceCategoriesListComponent } from './catalog/service-categories/service-categories-list/service-categories-list.component';
import { ServiceCategoryUpsertComponent } from './catalog/service-categories/service-category-upsert/service-category-upsert.component';


@NgModule({
  declarations: [
    AdminLayoutComponent,
    ExchangeRatesComponent,
    AdminSettingsComponent,
    SpecializationsListComponent,
    SpecializationUpsertComponent,
    ServiceCategoriesListComponent,
    ServiceCategoryUpsertComponent,
  ],
  imports: [
    AdminRoutingModule,
    SharedModule,
    // "Moj profil" (/admin/profile) – ista stranica kao /client/profile
    AccountModule,
  ]
})
export class AdminModule { }
