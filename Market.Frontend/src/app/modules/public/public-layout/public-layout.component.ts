import { Component } from '@angular/core';

/**
 * Layout javnog dijela: zaglavlje + stranica (router-outlet) + podnožje.
 * Nove javne stranice dodaj kao children u public-routing-module.ts.
 */
@Component({
  selector: 'app-public-layout',
  standalone: false,
  templateUrl: './public-layout.component.html',
  styleUrl: './public-layout.component.scss',
})
export class PublicLayoutComponent {
  currentYear = new Date().getFullYear();
}
