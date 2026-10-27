import { Component } from '@angular/core';

/**
 * Postavke aplikacije (ruta /admin/settings) – namjerno prazna stranica.
 * Ovdje se dodaju postavke vaše aplikacije (npr. podaci o firmi, podrazumijevana valuta,
 * pravila poslovanja). Lični podaci i lozinka su na stranici "Moj profil" (/admin/profile).
 */
@Component({
  selector: 'app-admin-settings',
  standalone: false,
  templateUrl: './admin-settings.component.html',
  styleUrl: './admin-settings.component.scss',
})
export class AdminSettingsComponent {}
