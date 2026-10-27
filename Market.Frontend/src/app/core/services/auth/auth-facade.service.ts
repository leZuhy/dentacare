// src/app/core/services/auth/auth-facade.service.ts
import { Injectable, inject, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, of, tap, catchError, map } from 'rxjs';
import { jwtDecode } from 'jwt-decode';

import { AuthApiService } from '../../../api-services/auth/auth-api.service';
import {
  ChangePasswordCommand,
  ChangePasswordCommandDto,
  LoginCommand,
  LoginCommandDto,
  LogoutCommand,
  RefreshTokenCommand,
  RefreshTokenCommandDto,
  RegisterCommand,
  RegisterCommandDto,
} from '../../../api-services/auth/auth-api.model';

import { AuthStorageService } from './auth-storage.service';
import { isSafeReturnUrl } from './return-url';
import { CurrentUserDto } from './current-user.dto';
import { JwtPayloadDto } from './jwt-payload.dto';

/**
 * Glavni auth servis (façade).
 * - priča sa AuthApiService (HTTP)
 * - priča sa AuthStorageService (localStorage)
 * - dekodira JWT i drži CurrentUser kao signal
 *
 * Koristi se u:
 * - interceptoru (getAccessToken, refresh)
 * - guardovima (isAuthenticated, isAdmin)
 * - komponentama (login, registracija, promjena lozinke, logout, navbar)
 *
 * Ovdje su samo pozivi koji mijenjaju tokene. Ostali auth pozivi (zaboravljena lozinka,
 * profil /api/auth/me) idu iz komponente direktno preko AuthApiService.
 */
@Injectable({ providedIn: 'root' })
export class AuthFacadeService {
  private api = inject(AuthApiService);
  private storage = inject(AuthStorageService);
  private router = inject(Router);

  // === REACTIVE STATE: current user ===

  private _currentUser = signal<CurrentUserDto | null>(null);

  /** readonly signal za UI – čita se kao auth.currentUser() */
  currentUser = this._currentUser.asReadonly();

  /** computed signali nad current userom */
  isAuthenticated = computed(() => !!this._currentUser());
  isAdmin = computed(() => this._currentUser()?.isAdmin ?? false);
  isManager = computed(() => this._currentUser()?.isManager ?? false);
  isEmployee = computed(() => this._currentUser()?.isEmployee ?? false);

  /** Istek access tokena (ms od epohe), iz JWT "exp" claima. */
  private accessTokenExpiresAtMs: number | null = null;

  constructor() {
    // pokušaj inicijalizacije iz postojećeg access tokena
    this.initializeFromToken();
  }

  // =========================================================
  // PUBLIC API
  // =========================================================

  /**
   * Login korisnika (email + password).
   * Snima tokene u storage, dekodira JWT i popunjava current user state.
   */
  login(payload: LoginCommand): Observable<void> {
    return this.api.login(payload).pipe(
      tap((response: LoginCommandDto) => {
        this.storage.saveLogin(response);           // access + refresh + expiries
        this.decodeAndSetUser(response.accessToken); // popuni _currentUser
      }),
      map(() => void 0)
    );
  }

  /**
   * Registracija novog kupca. Backend vraća tokene (kao login), pa je korisnik odmah prijavljen.
   */
  register(payload: RegisterCommand): Observable<void> {
    return this.api.register(payload).pipe(
      tap((response: RegisterCommandDto) => {
        this.storage.saveLogin(response);
        this.decodeAndSetUser(response.accessToken);
      }),
      map(() => void 0)
    );
  }

  /**
   * Promjena lozinke prijavljenog korisnika.
   * Server opoziva SVE sesije (ostali uređaji su odjavljeni) i vraća nove tokene za ovu –
   * snimamo ih isto kao kod refresh-a, pa korisnik ovdje ostaje prijavljen.
   */
  changePassword(payload: ChangePasswordCommand): Observable<void> {
    return this.api.changePassword(payload).pipe(
      tap((response: ChangePasswordCommandDto) => {
        this.storage.saveRefresh(response);
        this.decodeAndSetUser(response.accessToken);
      }),
      map(() => void 0)
    );
  }

  /**
   * Logout korisnika:
   * - lokalno očisti state i tokene
   * - pokuša invalidirati refresh token na serveru (bez drame na error)
   */
  logout(): Observable<void> {
    const refreshToken = this.storage.getRefreshToken();

    // 1) lokalno očisti (optimistic logout)
    this.clearUserState();

    // 2) nema refresh tokena → nema ni API poziva
    if (!refreshToken) {
      return of(void 0);
    }

    const payload: LogoutCommand = { refreshToken };

    // 3) pokušaj server-side logout, ignoriši greške
    return this.api.logout(payload).pipe(catchError(() => of(void 0)));
  }

  /**
   * Refresh access tokena – koristi refresh token.
   * Poziva interceptor kada dobije 401.
   */
  refresh(payload: RefreshTokenCommand): Observable<RefreshTokenCommandDto> {
    return this.api.refresh(payload).pipe(
      tap((response: RefreshTokenCommandDto) => {
        this.storage.saveRefresh(response);           // snimi nove tokene
        this.decodeAndSetUser(response.accessToken);  // update current usera
      })
    );
  }

  /**
   * Utility za guardove/interceptore – očisti auth state i prebaci na /auth/login.
   * Trenutna stranica ide kao returnUrl, pa se korisnik nakon ponovne prijave vraća na nju.
   */
  redirectToLogin(): void {
    this.clearUserState();
    const returnUrl = this.router.url;
    this.router.navigate(['/auth/login'], {
      queryParams: isSafeReturnUrl(returnUrl) && returnUrl !== '/' ? { returnUrl } : {}
    });
  }

  /**
   * Da li korisnik ima sesiju koju još može koristiti (koristi je guard):
   * - nije prijavljen → ne
   * - ima refresh token koji nije istekao → da (interceptor će na 401 obnoviti access token)
   * - nema (važećeg) refresh tokena → access token mora još važiti
   */
  hasValidSession(): boolean {
    if (!this._currentUser()) {
      return false;
    }

    const refreshToken = this.storage.getRefreshToken();
    if (refreshToken && !this.isInPast(this.storage.getRefreshTokenExpiresAtUtc())) {
      return true;
    }

    return !this.isInPast(this.accessTokenExpiresAtMs);
  }

  // =========================================================
  // GETTERI ZA INTERCEPTOR
  // =========================================================

  /**
   * Access token za Authorization header.
   */
  getAccessToken(): string | null {
    return this.storage.getAccessToken();
  }

  /**
   * Refresh token za refresh poziv.
   */
  getRefreshToken(): string | null {
    return this.storage.getRefreshToken();
  }

  // =========================================================
  // PRIVATE HELPERS
  // =========================================================

  /**
   * Na startu aplikacije (konstruktor) – pokušaj obnoviti stanje iz postojećeg tokena.
   */
  private initializeFromToken(): void {
    const token = this.storage.getAccessToken();
    if (token) {
      this.decodeAndSetUser(token);
    }
  }

  /**
   * Dekodiraj JWT i postavi current user state.
   */
  private decodeAndSetUser(token: string): void {
    try {
      const payload = jwtDecode<JwtPayloadDto>(token);

      const user: CurrentUserDto = {
        userId: Number(payload.sub),
        email: payload.email,
        isAdmin: payload.is_admin === 'true',
        isManager: payload.is_manager === 'true',
        isEmployee: payload.is_employee === 'true',
        tokenVersion: Number(payload.ver),
      };

      this._currentUser.set(user);
      this.accessTokenExpiresAtMs = payload.exp ? payload.exp * 1000 : null;
    } catch (error) {
      console.error('Failed to decode JWT token:', error);
      this._currentUser.set(null);
      this.accessTokenExpiresAtMs = null;
    }
  }

  /**
   * Očisti user state + sve tokene iz storage-a.
   */
  private clearUserState(): void {
    this._currentUser.set(null);
    this.accessTokenExpiresAtMs = null;
    this.storage.clear();
  }

  /**
   * true ako je trenutak (ISO string ili ms) prošao; nepoznat istek (null) se ne smatra isteklim.
   */
  private isInPast(value: string | number | null): boolean {
    if (value === null) {
      return false;
    }
    const time = typeof value === 'number' ? value : Date.parse(value);
    return !Number.isNaN(time) && time <= Date.now();
  }
}
