import { Component, inject } from '@angular/core';
import { CurrentUserService } from '../../../core/services/auth/current-user.service';

/**
 * Početna stranica za prijavljene korisnike koji nisu admin (ruta /client).
 * Ovdje (i kao nove child rute u client-routing-module.ts) idu funkcionalnosti za kupce.
 */
@Component({
  selector: 'app-client-home',
  standalone: false,
  templateUrl: './client-home.component.html',
  styleUrl: './client-home.component.scss',
})
export class ClientHomeComponent {
  protected currentUser = inject(CurrentUserService).currentUser;
}
