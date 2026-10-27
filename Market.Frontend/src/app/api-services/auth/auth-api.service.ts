import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  LoginCommand,
  LoginCommandDto,
  RefreshTokenCommand,
  RefreshTokenCommandDto,
  LogoutCommand,
  RegisterCommand,
  RegisterCommandDto,
  ForgotPasswordCommand,
  ResetPasswordCommand,
  GetMeQueryDto,
  UpdateMeCommand,
  ChangePasswordCommand,
  ChangePasswordCommandDto
} from './auth-api.model';

/**
 * Pozivi koji vraćaju tokene (login, refresh, register, change-password) iz komponenti idu
 * preko AuthFacadeService – on tokene snimi i osvježi prijavljenog korisnika.
 */
@Injectable({
  providedIn: 'root'
})
export class AuthApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/auth`;
  private http = inject(HttpClient);

  /**
   * POST /api/auth/login
   * Authenticate user and get access/refresh tokens.
   */
  login(payload: LoginCommand): Observable<LoginCommandDto> {
    return this.http.post<LoginCommandDto>(`${this.baseUrl}/login`, payload);
  }

  /**
   * POST /api/auth/refresh
   * Refresh access token using refresh token.
   */
  refresh(payload: RefreshTokenCommand): Observable<RefreshTokenCommandDto> {
    return this.http.post<RefreshTokenCommandDto>(`${this.baseUrl}/refresh`, payload);
  }

  /**
   * POST /api/auth/logout
   * Invalidate refresh token and logout user.
   */
  logout(payload: LogoutCommand): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/logout`, payload);
  }

  /**
   * POST /api/auth/register → 201 + tokeni (novi kupac je odmah prijavljen).
   */
  register(payload: RegisterCommand): Observable<RegisterCommandDto> {
    return this.http.post<RegisterCommandDto>(`${this.baseUrl}/register`, payload);
  }

  /**
   * POST /api/auth/forgot-password → uvijek 204 (šalje link za promjenu lozinke ako nalog postoji).
   */
  forgotPassword(payload: ForgotPasswordCommand): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/forgot-password`, payload);
  }

  /**
   * POST /api/auth/reset-password → 204 (nova lozinka; sve sesije korisnika se odjavljuju).
   */
  resetPassword(payload: ResetPasswordCommand): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/reset-password`, payload);
  }

  /**
   * GET /api/auth/me – profil prijavljenog korisnika.
   */
  getMe(): Observable<GetMeQueryDto> {
    return this.http.get<GetMeQueryDto>(`${this.baseUrl}/me`);
  }

  /**
   * PUT /api/auth/me → 204 (ime, prezime, adresa, grad).
   */
  updateMe(payload: UpdateMeCommand): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/me`, payload);
  }

  /**
   * POST /api/auth/change-password → 200 + novi tokeni (ostale sesije su odjavljene).
   */
  changePassword(payload: ChangePasswordCommand): Observable<ChangePasswordCommandDto> {
    return this.http.post<ChangePasswordCommandDto>(`${this.baseUrl}/change-password`, payload);
  }
}
