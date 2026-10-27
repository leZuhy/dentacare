import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CatalogApiService } from '../../../api-services/catalog/catalog-api.service';
import { GetCatalogHomeQueryDto } from '../../../api-services/catalog/catalog-api.model';
import { BaseComponent } from '../../../core/components/base-classes/base-component';
import { CurrentUserService } from '../../../core/services/auth/current-user.service';
import { getErrorMessage } from '../../../core/interceptors/error-logging-interceptor.service';

/**
 * Početna (landing) stranica javnog dijela aplikacije.
 * Ruta: '' unutar PublicLayoutComponent (vidi public-routing-module.ts).
 *
 * Podaci dolaze sa GET /Catalog/home (anonimno): promocije, kategorije, najnoviji proizvodi, brojke.
 */
@Component({
  selector: 'app-landing',
  standalone: false,
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss',
})
export class LandingComponent extends BaseComponent implements OnInit {
  private catalogApi = inject(CatalogApiService);
  private destroyRef = inject(DestroyRef);
  protected currentUser = inject(CurrentUserService);

  home: GetCatalogHomeQueryDto | null = null;

  ngOnInit(): void {
    this.loadHome();
  }

  loadHome(): void {
    this.startLoading();

    this.catalogApi
      .getHome({ categoryLimit: 8, productLimit: 8 })
      // korisnik napusti stranicu prije odgovora → odgovor se više ne obrađuje
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (home) => {
          this.home = home;
          this.stopLoading();
        },
        error: (err) => {
          this.stopLoading(getErrorMessage(err));
        }
      });
  }
}
