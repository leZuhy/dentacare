import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest
} from '@angular/common/http';
import { inject, Injector } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Observable, throwError } from 'rxjs';
import { catchError, finalize, map, shareReplay, switchMap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { AuthFacadeService } from '../services/auth/auth-facade.service';
import { ToasterService } from '../services/toaster.service';

/**
 * Auth endpointi koje interceptor ne dira (bez tokena, bez refresh logike):
 * - login/refresh: 401 ovdje znači "pogrešni podaci / nevažeći refresh token",
 *   a ne "istekao access token" – ne smije pokrenuti refresh.
 * - logout: backend ga dozvoljava anonimno (sesiju prepoznaje po refresh tokenu
 *   u tijelu zahtjeva), a AuthFacadeService.logout() ionako prvo obriše tokene.
 * - register, forgot-password, reset-password: anonimni, kao login (token im ne treba).
 * /api/auth/me i /api/auth/change-password NISU ovdje – traže prijavu, pa idu sa tokenom.
 */
const SKIPPED_AUTH_PATHS = [
  '/api/auth/login',
  '/api/auth/refresh',
  '/api/auth/logout',
  '/api/auth/register',
  '/api/auth/forgot-password',
  '/api/auth/reset-password'
];

/**
 * Refresh koji je trenutno u toku – dijele ga svi zahtjevi koji dobiju 401 u isto vrijeme
 * (single-flight). null = nema refresh-a u toku.
 */
let refreshInFlight$: Observable<string> | null = null;

/**
 * HTTP interceptor that:
 * 1. Adds Authorization header with access token – ONLY for requests to our API (environment.apiUrl)
 * 2. Handles 401 errors by refreshing the token (one shared refresh call for concurrent requests)
 * 3. Retries the failed request once with the new token
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const path = getApiPath(req.url);

  // 1) Token se nikad ne šalje drugim domenima (npr. CDN, tuđi API, /i18n/*.json),
  //    a auth endpointi idu bez tokena.
  if (path === null || SKIPPED_AUTH_PATHS.includes(path)) {
    return next(req);
  }

  const auth = inject(AuthFacadeService);
  const injector = inject(Injector);

  // 2) Add Authorization header if token exists
  const usedToken = auth.getAccessToken();

  // 3) Handle 401 → refresh → retry (samo jednom)
  return next(withToken(req, usedToken)).pipe(
    catchError((err: unknown) => {
      if (!(err instanceof HttpErrorResponse) || err.status !== 401) {
        return throwError(() => err);
      }

      // U storage-u je već NOVIJI access token od onog s kojim je zahtjev poslan:
      // token je u međuvremenu obnovio drugi zahtjev ili DRUGI TAB (svi tabovi dijele
      // localStorage) → samo ponovi zahtjev jednom sa tim tokenom, bez novog refresh-a.
      // Važno: backend ponovno korištenje već rotiranog refresh tokena tretira kao krađu
      // i gasi SVE sesije korisnika – zato refresh zovemo samo ako je u storage-u još
      // uvijek isti token koji je dobio 401.
      const currentToken = auth.getAccessToken();
      if (currentToken && currentToken !== usedToken) {
        return next(withToken(req, currentToken));
      }

      return refreshAccessToken(auth, injector).pipe(
        // refresh nije uspio → pozivalac dobija originalnu 401 grešku svog zahtjeva
        catchError(() => throwError(() => err)),
        // Ponovljeni zahtjev ide direktno dalje (next), ne kroz ovaj interceptor,
        // pa se ne može vrtiti u krug. Njegova greška (400, 404, 409...) se samo
        // prosljeđuje komponenti – korisnik se zbog nje NE odjavljuje.
        switchMap((newToken) => next(withToken(req, newToken)))
      );
    })
  );
};

/**
 * Vraća novi access token. Ako je refresh već u toku, vraća isti (dijeljeni) poziv.
 * Ako refresh ne uspije → odjava i redirect na /auth/login, a svi koji čekaju dobiju grešku.
 */
function refreshAccessToken(auth: AuthFacadeService, injector: Injector): Observable<string> {
  if (refreshInFlight$) {
    return refreshInFlight$;
  }

  const refreshToken = auth.getRefreshToken();
  if (!refreshToken) {
    sessionExpired(auth, injector);
    return throwError(() => new Error('No refresh token'));
  }

  refreshInFlight$ = auth.refresh({ refreshToken, fingerprint: null }).pipe(
    map((res) => res.accessToken),
    // catchError je SAMO na refresh pozivu: ne valja refresh token → odjava
    catchError((error: unknown) => {
      sessionExpired(auth, injector);
      return throwError(() => error);
    }),
    // refresh je završen (uspješno ili ne) → sljedeći 401 smije pokrenuti novi
    finalize(() => {
      refreshInFlight$ = null;
    }),
    // jedan HTTP poziv za sve koji čekaju; ne prekida se ako neko od njih odustane
    // (bitno: server je možda već rotirao refresh token)
    shareReplay({ bufferSize: 1, refCount: false })
  );

  return refreshInFlight$;
}

/**
 * Sesija se ne može obnoviti → očisti tokene, idi na /auth/login i obavijesti korisnika.
 */
function sessionExpired(auth: AuthFacadeService, injector: Injector): void {
  auth.redirectToLogin();
  const translate = injector.get(TranslateService);
  injector.get(ToasterService).warning(translate.instant('ERRORS.LOGIN_REQUIRED'));
}

function withToken(req: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  return token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;
}

/**
 * Putanja zahtjeva relativno na environment.apiUrl (npr. "/api/auth/login"),
 * ili null ako zahtjev ne ide na naš API.
 */
function getApiPath(url: string): string | null {
  const apiUrl = environment.apiUrl.replace(/\/+$/, '');
  if (url !== apiUrl && !url.startsWith(apiUrl + '/') && !url.startsWith(apiUrl + '?')) {
    return null;
  }
  return url.substring(apiUrl.length).split('?')[0].toLowerCase();
}
