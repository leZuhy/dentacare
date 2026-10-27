import { Injectable } from '@angular/core';
import {
  LoginCommandDto,
  RefreshTokenCommandDto
} from '../../../api-services/auth/auth-api.model';

/**
 * Low-level service for managing auth tokens in localStorage.
 * Should not be used directly in components - use AuthFacadeService instead.
 */
@Injectable({
  providedIn: 'root'
})
export class AuthStorageService {
  private readonly ACCESS_TOKEN_KEY = 'accessToken';
  private readonly REFRESH_TOKEN_KEY = 'refreshToken';
  private readonly ACCESS_EXPIRES_KEY = 'accessTokenExpiresAtUtc';
  private readonly REFRESH_EXPIRES_KEY = 'refreshTokenExpiresAtUtc';

  /**
   * Save login response to localStorage.
   */
  saveLogin(response: LoginCommandDto): void {
    this.saveTokens(response);
  }

  /**
   * Save refresh response to localStorage.
   */
  saveRefresh(response: RefreshTokenCommandDto): void {
    this.saveTokens(response);
  }

  /**
   * Clear all auth data from localStorage.
   */
  clear(): void {
    localStorage.removeItem(this.ACCESS_TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.ACCESS_EXPIRES_KEY);
    localStorage.removeItem(this.REFRESH_EXPIRES_KEY);
  }

  /**
   * Get access token from localStorage.
   */
  getAccessToken(): string | null {
    return localStorage.getItem(this.ACCESS_TOKEN_KEY);
  }

  /**
   * Get refresh token from localStorage.
   */
  getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  /**
   * Get refresh token expiry (ISO string, UTC) from localStorage.
   */
  getRefreshTokenExpiresAtUtc(): string | null {
    return localStorage.getItem(this.REFRESH_EXPIRES_KEY);
  }

  /**
   * Check if user has access token.
   */
  hasToken(): boolean {
    return !!this.getAccessToken();
  }

  /**
   * Login i refresh vraćaju isti oblik odgovora (tokeni + oba isteka).
   */
  private saveTokens(response: LoginCommandDto | RefreshTokenCommandDto): void {
    localStorage.setItem(this.ACCESS_TOKEN_KEY, response.accessToken);
    localStorage.setItem(this.REFRESH_TOKEN_KEY, response.refreshToken);
    localStorage.setItem(this.ACCESS_EXPIRES_KEY, response.accessTokenExpiresAtUtc);
    localStorage.setItem(this.REFRESH_EXPIRES_KEY, response.refreshTokenExpiresAtUtc);
  }
}
