import { Component, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  standalone: false,
  styleUrl: './app.component.scss'
})
export class AppComponent {
  private translate = inject(TranslateService);

  protected readonly title = 'Market';

  constructor() {
    // Inicijalizacija translate servisa
    this.translate.addLangs(['en', 'bs']);
    this.translate.setFallbackLang('bs');

    // Učitaj jezik iz localStorage ili koristi default
    const savedLang = localStorage.getItem('language') || 'bs';

    this.translate.use(savedLang).subscribe({
      error: (error) => {
        console.error(`Error loading translations (/i18n/${savedLang}.json):`, error);
      }
    });
  }
}
