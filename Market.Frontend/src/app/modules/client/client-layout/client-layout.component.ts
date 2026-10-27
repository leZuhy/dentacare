import { Component } from '@angular/core';

/**
 * Layout klijentskog dijela (/client/...): isti header kao javni dio + stranica.
 * Nove stranice za kupce dodaj kao children u client-routing-module.ts.
 */
@Component({
  selector: 'app-client-layout',
  standalone: false,
  templateUrl: './client-layout.component.html',
  styleUrl: './client-layout.component.scss'
})
export class ClientLayoutComponent {}
