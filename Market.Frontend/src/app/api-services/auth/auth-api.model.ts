// === COMMANDS (WRITE) ===

/**
 * Command for POST /api/auth/login
 * Corresponds to: LoginCommand.cs
 */
export interface LoginCommand {
  email: string;
  password: string;
  fingerprint?: string | null;
}

/**
 * Response for POST /api/auth/login
 * Corresponds to: LoginCommandDto.cs (isti oblik kao odgovor na refresh)
 */
export interface LoginCommandDto {
  accessToken: string;
  refreshToken: string;
  /**
   * ISO string (UTC) when access token expires
   * Example: "2025-12-02T23:59:59Z"
   */
  accessTokenExpiresAtUtc: string;
  /**
   * ISO string (UTC) when refresh token expires
   */
  refreshTokenExpiresAtUtc: string;
}

/**
 * Command for POST /api/auth/refresh
 * Corresponds to: RefreshTokenCommand.cs
 */
export interface RefreshTokenCommand {
  refreshToken: string;
  fingerprint?: string | null;
}

/**
 * Response for POST /api/auth/refresh
 * Corresponds to: RefreshTokenCommandDto.cs
 */
export interface RefreshTokenCommandDto {
  accessToken: string;
  refreshToken: string;
  /**
   * ISO string (UTC) when access token expires
   */
  accessTokenExpiresAtUtc: string;
  /**
   * ISO string (UTC) when refresh token expires
   */
  refreshTokenExpiresAtUtc: string;
}

/**
 * Command for POST /api/auth/logout
 * Corresponds to: LogoutCommand.cs
 */
export interface LogoutCommand {
  refreshToken: string;
}

/**
 * Command for POST /api/auth/register (anonimno)
 * Corresponds to: RegisterCommand.cs
 * Novi korisnik je kupac (bez uloga). Greške po polju: email ("Email je već registrovan."), password, ...
 */
export interface RegisterCommand {
  firstname: string;
  lastname: string;
  email: string;
  /** najmanje 8 znakova, bar jedno slovo i jedna cifra (AppValidators.password) */
  password: string;
  address?: string | null;
  city?: string | null;
}

/**
 * Response for POST /api/auth/register → 201 + tokeni (korisnik je odmah prijavljen).
 * Isti oblik kao odgovor na login.
 */
export type RegisterCommandDto = LoginCommandDto;

/**
 * Command for POST /api/auth/forgot-password (anonimno) → uvijek 204,
 * i kad nalog ne postoji (odgovor ne otkriva koji emailovi su registrovani).
 * Corresponds to: ForgotPasswordCommand.cs
 */
export interface ForgotPasswordCommand {
  email: string;
}

/**
 * Command for POST /api/auth/reset-password (anonimno) → 204.
 * email i token dolaze iz linka u emailu (/auth/reset-password?email=...&token=...).
 * Nevažeći/istekao/iskorišten token → 400 sa greškom na polju "token".
 * Corresponds to: ResetPasswordCommand.cs
 */
export interface ResetPasswordCommand {
  email: string;
  token: string;
  newPassword: string;
}

/**
 * Response for GET /api/auth/me – profil prijavljenog korisnika.
 * Corresponds to: GetMeQueryDto.cs
 */
export interface GetMeQueryDto {
  id: number;
  email: string;
  firstname: string;
  lastname: string;
  /** null = korisnik još nije unio adresu */
  address: string | null;
  city: string | null;
  isAdmin: boolean;
  isManager: boolean;
  isEmployee: boolean;
}

/**
 * Command for PUT /api/auth/me → 204. Email se ovdje ne mijenja.
 * Corresponds to: UpdateMeCommand.cs
 */
export interface UpdateMeCommand {
  firstname: string;
  lastname: string;
  address?: string | null;
  city?: string | null;
}

/**
 * Command for POST /api/auth/change-password → 200 + NOVI tokeni.
 * Pogrešna trenutna lozinka → 400 na polju "currentPassword".
 * Corresponds to: ChangePasswordCommand.cs
 */
export interface ChangePasswordCommand {
  currentPassword: string;
  newPassword: string;
}

/**
 * Response for POST /api/auth/change-password: novi par tokena za trenutnu sesiju
 * (server je opozvao sve ostale sesije). Isti oblik kao odgovor na refresh.
 */
export type ChangePasswordCommandDto = RefreshTokenCommandDto;
