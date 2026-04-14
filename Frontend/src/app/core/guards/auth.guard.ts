import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { catchError, map, of } from 'rxjs';

/**
 * Route guard that protects routes requiring authentication.
 *
 * Activation logic:
 * 1. If the access token is present and not expired, allow navigation immediately.
 * 2. If the access token is expired but a refresh token exists, attempt a silent refresh.
 *    - On success: allow navigation.
 *    - On failure: log out and redirect to `/auth/login`.
 * 3. If no tokens are available, redirect to `/auth/login`.
 *
 * Used on all routes that require the user to be logged in (e.g., cart, orders, profile).
 */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  // Token still valid — allow immediately
  if (auth.isAuthenticated()) return true;

  // Token expired but refresh token exists — try silent refresh
  const refreshToken = auth.getRefreshToken();
  if (refreshToken) {
    return auth.refreshToken().pipe(
      map(() => true),
      catchError(() => {
        auth.logout();
        return of(router.createUrlTree(['/auth/login']));
      })
    );
  }

  // No tokens at all — redirect to login
  return router.createUrlTree(['/auth/login']);
};
