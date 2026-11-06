import { NgModule } from "@angular/core";
import { RouterModule, Routes } from "@angular/router";

import { AdminLayoutComponent } from "./admin-layout/admin-layout.component";
import { MyProfileComponent } from "../account/my-profile/my-profile.component";
import { AdminSettingsComponent } from "./admin-settings/admin-settings.component";
import { ExchangeRatesComponent } from "./finance/exchange-rates/exchange-rates.component";
import { SpecializationsListComponent } from "./catalog/specializations/specializations-list/specializations-list.component";
import { SpecializationUpsertComponent } from "./catalog/specializations/specialization-upsert/specialization-upsert.component";
import { ServiceCategoriesListComponent } from "./catalog/service-categories/service-categories-list/service-categories-list.component";
import { ServiceCategoryUpsertComponent } from "./catalog/service-categories/service-category-upsert/service-category-upsert.component";
import { DentalServicesListComponent } from "./catalog/dental-services/dental-services-list/dental-services-list.component";
import { DentalServiceUpsertComponent } from "./catalog/dental-services/dental-service-upsert/dental-service-upsert.component";
import { DentistsListComponent } from "./dentists/dentists-list/dentists-list.component";
import { DentistUpsertComponent } from "./dentists/dentist-upsert/dentist-upsert.component";
import { WorkingHoursListComponent } from "./working-hours/working-hours-list/working-hours-list.component";
import { WorkingHoursUpsertComponent } from "./working-hours/working-hours-upsert/working-hours-upsert.component";
import { LoyaltyTiersListComponent } from "./loyalty/loyalty-tiers/loyalty-tiers-list/loyalty-tiers-list.component";
import { LoyaltyTierUpsertComponent } from "./loyalty/loyalty-tiers/loyalty-tier-upsert/loyalty-tier-upsert.component";

const routes: Routes = [
  {
    path: "",
    component: AdminLayoutComponent,
    children: [
      // CATALOG
      {
        path: "catalog/specializations",
        component: SpecializationsListComponent,
      },
      {
        path: "catalog/specializations/new",
        component: SpecializationUpsertComponent,
      },
      {
        path: "catalog/specializations/:id/edit",
        component: SpecializationUpsertComponent,
      },
      {
        path: "catalog/service-categories",
        component: ServiceCategoriesListComponent,
      },
      {
        path: "catalog/service-categories/new",
        component: ServiceCategoryUpsertComponent,
      },
      {
        path: "catalog/service-categories/:id/edit",
        component: ServiceCategoryUpsertComponent,
      },
      {
        path: "catalog/dental-services",
        component: DentalServicesListComponent,
      },
      {
        path: "catalog/dental-services/new",
        component: DentalServiceUpsertComponent,
      },
      {
        path: "catalog/dental-services/:id/edit",
        component: DentalServiceUpsertComponent,
      },

      // CLINIC
      {
        path: "dentists",
        component: DentistsListComponent,
      },
      {
        path: "dentists/new",
        component: DentistUpsertComponent,
      },
      {
        path: "dentists/:id/edit",
        component: DentistUpsertComponent,
      },
      {
        path: "working-hours",
        component: WorkingHoursListComponent,
      },
      {
        path: "working-hours/new",
        component: WorkingHoursUpsertComponent,
      },
      {
        path: "working-hours/:id/edit",
        component: WorkingHoursUpsertComponent,
      },

      // LOYALTY
      {
        path: "loyalty/loyalty-tiers",
        component: LoyaltyTiersListComponent,
      },
      {
        path: "loyalty/loyalty-tiers/new",
        component: LoyaltyTierUpsertComponent,
      },
      {
        path: "loyalty/loyalty-tiers/:id/edit",
        component: LoyaltyTierUpsertComponent,
      },

      // FINANCE
      {
        path: "exchange-rates",
        component: ExchangeRatesComponent,
      },

      // POSTAVKE APLIKACIJE (prazna stranica – ovdje idu postavke vaše aplikacije)
      {
        path: "settings",
        component: AdminSettingsComponent,
      },

      // MOJ PROFIL (ista stranica kao /client/profile, deklarisana u AccountModule)
      {
        path: "profile",
        component: MyProfileComponent,
      },

      // default admin route → /admin/exchange-rates
      {
        path: "",
        redirectTo: "exchange-rates",
        pathMatch: "full",
      },
    ],
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class AdminRoutingModule {}
