import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AdminLayoutComponent } from './admin-layout/admin-layout.component';
import { ProductsComponent } from './catalogs/products/products.component';
import { ProductsAddComponent } from './catalogs/products/products-add/products-add.component';
import { ProductsEditComponent } from './catalogs/products/products-edit/products-edit.component';
import { ProductCategoriesComponent } from './catalogs/product-categories/product-categories.component';
import {AdminOrdersComponent} from './orders/admin-orders.component';
import {MyProfileComponent} from '../account/my-profile/my-profile.component';
import {AdminSettingsComponent} from './admin-settings/admin-settings.component';
import {PromotionsComponent} from './catalogs/promotions/promotions.component';
import {PromotionUpsertComponent} from './catalogs/promotions/promotion-upsert/promotion-upsert.component';
import {OrderEditComponent} from './orders/order-edit/order-edit.component';
import {ExchangeRatesComponent} from './finance/exchange-rates/exchange-rates.component';
import {unsavedChangesGuard} from '../shared/guards/unsaved-changes.guard';

const routes: Routes = [
  {
    path: '',
    component: AdminLayoutComponent,
    children: [
      // PRODUCTS
      {
        path: 'products',
        component: ProductsComponent,
      },
      {
        path: 'products/add',
        component: ProductsAddComponent,
      },
      {
        path: 'products/:id/edit',
        component: ProductsEditComponent,
      },

      // PRODUCT CATEGORIES
      {
        path: 'product-categories',
        component: ProductCategoriesComponent,
      },

      // PROMOTIONS (baneri na početnoj stranici)
      {
        path: 'promotions',
        component: PromotionsComponent,
      },
      {
        path: 'promotions/add',
        component: PromotionUpsertComponent,
      },
      {
        path: 'promotions/:id/edit',
        component: PromotionUpsertComponent,
      },

      // ORDERS
      {
        path: 'orders',
        component: AdminOrdersComponent,
      },
      {
        // canDeactivate: pita prije napuštanja stranice sa nesačuvanim izmjenama
        path: 'orders/:id/edit',
        component: OrderEditComponent,
        canDeactivate: [unsavedChangesGuard],
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

      // default admin route → /admin/products
      {
        path: '',
        redirectTo: 'products',
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
