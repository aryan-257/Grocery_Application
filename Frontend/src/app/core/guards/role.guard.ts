import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Factory function that creates a role-based route guard.
 * Returns a `CanActivateFn` that allows navigation only if the authenticated
 * user's role is included in the provided list of allowed roles.
 *
 * Activation logic:
 * 1. If the user is not authenticated, redirect to `/auth/login`.
 * 2. If the user's role is in the allowed roles list, allow navigation.
 * 3. Otherwise, redirect to `/unauthorized`.
 *
 * @param allowedRoles - One or more role strings that are permitted to access the route
 *                       (e.g., `'Admin'`, `'StoreManager'`).
 * @returns A `CanActivateFn` that enforces the role restriction.
 *
 * @example
 * // In route configuration:
 * { path: 'admin', canActivate: [roleGuard('Admin')] }
 * { path: 'manager', canActivate: [roleGuard('Admin', 'StoreManager')] }
 */
export function roleGuard(...allowedRoles: string[]): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    if (!auth.isAuthenticated()) return router.createUrlTree(['/auth/login']);
    const role = auth.getUserRole();
    if (role && allowedRoles.includes(role)) return true;
    return router.createUrlTree(['/unauthorized']);
  };
}
