import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AdminLayoutComponent } from './admin-layout/admin-layout.component';
import {MyProfileComponent} from '../account/my-profile/my-profile.component';
import {AdminSettingsComponent} from './admin-settings/admin-settings.component';
import {ExchangeRatesComponent} from './finance/exchange-rates/exchange-rates.component';
import {SpecializationsListComponent} from './catalog/specializations/specializations-list/specializations-list.component';
import {SpecializationUpsertComponent} from './catalog/specializations/specialization-upsert/specialization-upsert.component';
import {ServiceCategoriesListComponent} from './catalog/service-categories/service-categories-list/service-categories-list.component';
import {ServiceCategoryUpsertComponent} from './catalog/service-categories/service-category-upsert/service-category-upsert.component';
import {DentistsListComponent} from './dentists/dentists-list/dentists-list.component';
import {DentistUpsertComponent} from './dentists/dentist-upsert/dentist-upsert.component';

const routes: Routes = [
  {
    path: '',
    component: AdminLayoutComponent,
    children: [
      // CATALOG
      {
        path: 'catalog/specializations',
        component: SpecializationsListComponent,
      },
      {
        path: 'catalog/specializations/new',
        component: SpecializationUpsertComponent,
      },
      {
        path: 'catalog/specializations/:id/edit',
        component: SpecializationUpsertComponent,
      },
      {
        path: 'catalog/service-categories',
        component: ServiceCategoriesListComponent,
      },
      {
        path: 'catalog/service-categories/new',
        component: ServiceCategoryUpsertComponent,
      },
      {
        path: 'catalog/service-categories/:id/edit',
        component: ServiceCategoryUpsertComponent,
      },

      // CLINIC
      {
        path: 'dentists',
        component: DentistsListComponent,
      },
      {
        path: 'dentists/new',
        component: DentistUpsertComponent,
      },
      {
        path: 'dentists/:id/edit',
        component: DentistUpsertComponent,
      },

      // FINANCE
      {
        path: 'exchange-rates',
        component: ExchangeRatesComponent,
      },

      // POSTAVKE APLIKACIJE (prazna stranica – ovdje idu postavke vaše aplikacije)
      {
        path: 'settings',
        component: AdminSettingsComponent,
      },

      // MOJ PROFIL (ista stranica kao /client/profile, deklarisana u AccountModule)
      {
        path: 'profile',
        component: MyProfileComponent,
      },

      // default admin route → /admin/exchange-rates
      {
        path: '',
        redirectTo: 'exchange-rates',
        pathMatch: 'full',
      },
    ],
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class AdminRoutingModule {}
