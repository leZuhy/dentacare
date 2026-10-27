import { platformBrowser } from '@angular/platform-browser';
import { AppModule } from './app/app-module';

// zone.js se ne importuje ovdje nego preko "polyfills" u angular.json –
// tako build zna da aplikacija koristi zone.js (vidi provideZoneChangeDetection u AppModule).
platformBrowser()
  .bootstrapModule(AppModule)
  .catch(err => console.error(err));
