import {NgModule} from '@angular/core';

import {PublicRoutingModule} from './public-routing-module';
import {PublicLayoutComponent} from './public-layout/public-layout.component';
import {SearchProductsComponent} from './search-products/search-products.component';
import {LandingComponent} from './landing/landing.component';
import {SharedModule} from '../shared/shared-module';
import {ProductCardComponent} from './components/product-card/product-card.component';
import {PromotionsCarouselComponent} from './components/promotions-carousel/promotions-carousel.component';
import {CartItemsComponent} from './components/cart-items/cart-items.component';
import {CartPageComponent} from './cart-page/cart-page.component';
import {CheckoutComponent} from './checkout/checkout.component';
import {ConverterComponent} from './converter/converter.component';


@NgModule({
  declarations: [
    PublicLayoutComponent,
    LandingComponent,
    SearchProductsComponent,
    CartPageComponent,
    CheckoutComponent,
    ConverterComponent,
    // dijelovi stranica
    ProductCardComponent,
    PromotionsCarouselComponent,
    CartItemsComponent
  ],
  imports: [
    SharedModule,
    PublicRoutingModule,
  ]
})
export class PublicModule { }
