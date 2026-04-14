import { HttpInterceptorFn, HttpErrorResponse, HttpRequest, HttpHandlerFn } from '@angular/common/http';
import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

/**
 * Module-level flag to prevent concurrent token refresh attempts.
 * When a 401 triggers a refresh, subsequent 401s wait for the first refresh to complete
 * rather than each spawning their own refresh request.
 */
let isRefreshing = false;

/**
 * Clones the request and attaches a Bearer token to the Authorization header.
 * @param req - The original HTTP request.
 * @param token - The JWT access token to attach.
 * @returns A cloned request with the Authorization header set.
 */
function addToken(req: HttpRequest<unknown>, token: string) {
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

/**
 * Angular functional HTTP interceptor that handles JWT authentication.
 *
 * Responsibilities:
 * 1. Attaches the stored JWT access token to every outgoing request (browser only).
 * 2. On 401 Unauthorized responses, attempts a silent token refresh using the refresh token.
 * 3. Retries the original request with the new access token after a successful refresh.
 * 4. Redirects to the login page if the refresh fails or no refresh token is available.
 *
 * Skips refresh logic for `/auth/refresh` and `/auth/login` requests to prevent infinite loops.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const platformId = inject(PLATFORM_ID);

  // Only attach token in browser — localStorage is unavailable on SSR
  if (isPlatformBrowser(platformId)) {
    const token = auth.getAccessToken();
    if (token) {
      req = addToken(req, token);
    }
  }

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      // Only handle 401 in browser, skip refresh/login requests to avoid loops
      if (err.status === 401 && isPlatformBrowser(platformId)
          && !req.url.includes('/auth/refresh')
          && !req.url.includes('/auth/login')) {

        const refreshToken = auth.getRefreshToken();

        // If we have a refresh token, try to refresh silently
        if (refreshToken && !isRefreshing) {
          isRefreshing = true;
          return auth.refreshToken().pipe(
            switchMap(tokens => {
              isRefreshing = false;
              // Retry the original request with the new token
              return next(addToken(req, tokens.accessToken));
            }),
            catchError(refreshErr => {
              // Refresh failed — session truly expired, redirect to login
              isRefreshing = false;
              auth.logout();
              router.navigate(['/auth/login']);
              return throwError(() => refreshErr);
            })
          );
        }

        // No refresh token available — redirect to login
        auth.logout();
        router.navigate(['/auth/login']);
      }
      return throwError(() => err);
    })
  );
};
