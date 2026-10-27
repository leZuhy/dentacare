// src/app/core/guards/my-auth-guard.ts
import { inject } from '@angular/core';
import { CanActivateFn, ActivatedRouteSnapshot, Router, RouterStateSnapshot } from '@angular/router';
import { AuthFacadeService } from '../services/auth/auth-facade.service';
import { CurrentUserService } from '../services/auth/current-user.service';

/**
 * Guard za rute koje traže prijavu i/ili ulogu.
 *
 * Pravila se zadaju u ruti preko helpera myAuthData():
 *   {
 *     path: 'admin',
 *     canActivate: [myAuthGuard],
 *     data: myAuthData({ requireAuth: true, requireAdmin: true }),
 *     ...
 *   }
 *
 * Guard može stajati i na jednoj child ruti (vidi /checkout u public-routing-module.ts).
 *
 * Neprijavljen korisnik ide na /auth/login?returnUrl=<tražena adresa>, a login ga poslije
 * vrati tamo (samo na putanju unutar aplikacije – vidi core/services/auth/return-url.ts).
 *
 * Napomena: guard samo štiti UI (navigaciju). Pravu zaštitu podataka radi backend
 * ([Authorize] / policy) – frontend se uvijek može zaobići.
 */
export const myAuthGuard: CanActivateFn = (
  route: ActivatedRouteSnapshot,
  state: RouterStateSnapshot
) => {
  const auth = inject(AuthFacadeService);
  const currentUser = inject(CurrentUserService);
  const router = inject(Router);

  // myAuthData() pakuje pravila pod ključ "auth"
  const authData = (route.data['auth'] ?? {}) as MyAuthRouteData;

  const requireAdmin = authData.requireAdmin === true;
  const requireManager = authData.requireManager === true;
  const requireEmployee = authData.requireEmployee === true;
  // traženje bilo koje uloge podrazumijeva i prijavu
  const requireAuth =
    authData.requireAuth === true || requireAdmin || requireManager || requireEmployee;

  // Ako ne traži auth → pusti (javne rute)
  if (!requireAuth) {
    return true;
  }

  // 1) user nije logiran ili mu je sesija istekla → login, uz adresu na koju se vraća
  const user = currentUser.snapshot;
  if (!user || !auth.hasValidSession()) {
    return router.createUrlTree(['/auth/login'], { queryParams: { returnUrl: state.url } });
  }

  // 2) role check – ako user nema traženu ulogu → njegova početna ruta
  if (
    (requireAdmin && !user.isAdmin) ||
    (requireManager && !user.isManager) ||
    (requireEmployee && !user.isEmployee)
  ) {
    return router.createUrlTree([currentUser.getDefaultRoute()]);
  }

  return true;
};

export interface MyAuthRouteData {
  requireAuth?: boolean;
  requireAdmin?: boolean;
  requireManager?: boolean;
  requireEmployee?: boolean;
}

export function myAuthData(data: MyAuthRouteData): { auth: MyAuthRouteData } {
  return { auth: data };
}
