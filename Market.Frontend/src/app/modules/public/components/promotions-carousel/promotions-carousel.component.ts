import { Component, inject, Input } from '@angular/core';
import { Router, UrlTree } from '@angular/router';
import { GetCatalogHomeQueryDtoPromotion } from '../../../../api-services/catalog/catalog-api.model';

interface PromotionSlide {
  id: number;
  title: string;
  imageUrl: string;
  /** ruta aplikacije ("/search?categoryId=2") → routerLink */
  internalLink: UrlTree | null;
  /** apsolutni http(s) URL → otvara se u novom tabu */
  externalLink: string | null;
}

/**
 * Promocije sa početne stranice: jedan baner u isto vrijeme, strelice i tačkice za izbor.
 * targetUrl promocije je ili ruta aplikacije ili apsolutna adresa (backend to validira).
 */
@Component({
  selector: 'app-promotions-carousel',
  standalone: false,
  templateUrl: './promotions-carousel.component.html',
  styleUrl: './promotions-carousel.component.scss'
})
export class PromotionsCarouselComponent {
  private router = inject(Router);

  slides: PromotionSlide[] = [];
  index = 0;

  @Input({ required: true })
  set promotions(value: GetCatalogHomeQueryDtoPromotion[]) {
    this.slides = value.map((p) => this.toSlide(p));
    this.index = 0;
  }

  get current(): PromotionSlide | undefined {
    return this.slides[this.index];
  }

  previous(): void {
    this.index = (this.index - 1 + this.slides.length) % this.slides.length;
  }

  next(): void {
    this.index = (this.index + 1) % this.slides.length;
  }

  goTo(index: number): void {
    this.index = index;
  }

  private toSlide(p: GetCatalogHomeQueryDtoPromotion): PromotionSlide {
    const target = p.targetUrl?.trim() ?? '';
    const isAppPath = target.startsWith('/') && !target.startsWith('//');
    const isExternal = /^https?:\/\//i.test(target);

    return {
      id: p.id,
      title: p.title,
      imageUrl: p.imageUrl,
      // parseUrl: routerLink sa stringom bi "?" iz adrese pretvorio u dio putanje
      internalLink: isAppPath ? this.router.parseUrl(target) : null,
      externalLink: isExternal ? target : null
    };
  }
}
