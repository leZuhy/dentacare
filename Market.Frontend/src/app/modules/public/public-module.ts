import {NgModule} from '@angular/core';

import {PublicRoutingModule} from './public-routing-module';
import {PublicLayoutComponent} from './public-layout/public-layout.component';
import {LandingComponent} from './landing/landing.component';
import {SharedModule} from '../shared/shared-module';
import {ConverterComponent} from './converter/converter.component';


@NgModule({
  declarations: [
    PublicLayoutComponent,
    LandingComponent,
    ConverterComponent
  ],
  imports: [
    SharedModule,
    PublicRoutingModule,
  ]
})
export class PublicModule { }
