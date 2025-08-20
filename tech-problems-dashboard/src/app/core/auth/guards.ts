import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { UserRole } from './auth.models';
import { AuthService } from './auth.service';

// Protect routes, optionally enforcing allowed roles via route data: { roles: UserRole[] }
export const authGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  
  if (!auth.isAuthenticated) {
    return router.createUrlTree(['/login']);
  }
  
  // If we have a token but no current user, try to refresh user data
  if (auth.token && !auth.currentUser) {
    return auth.refreshMe().pipe(
      map(res => {
        if (!res.success) {
          // Token is invalid, redirect to login
          return router.createUrlTree(['/login']);
        }
        
        // Check role permissions if specified
        const roles = (route?.data?.['roles'] as UserRole[] | undefined) ?? undefined;
        if (!roles || roles.length === 0) return true;
        
        const user = auth.currentUser;
        if (user && roles.includes(user.role)) return true;
        
        return router.createUrlTree(['/login']);
      }),
      catchError(() => of(router.createUrlTree(['/login'])))
    );
  }
  
  // User is authenticated and we have user data, check roles
  const roles = (route?.data?.['roles'] as UserRole[] | undefined) ?? undefined;
  if (!roles || roles.length === 0) return true;
  
  const user = auth.currentUser;
  if (user && roles.includes(user.role)) return true;
  
  return router.createUrlTree(['/login']);
};

// Deny access to guests-only routes if the user is authenticated (e.g., /login)
export const guestOnlyGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated) return router.createUrlTree(['/dashboard']);
  return true;
};

// Allow superadmin signup only if no superadmin exists
export const superadminSignupGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.superAdminExists().pipe(
    map(res => {
      if (!res.success) return false;
      // Use res.exists for backend compatibility
      if ((res as any).exists === false) return true; // allow signup when no superadmin
      return router.createUrlTree(['/login']);
    }),
    catchError(() => of(router.createUrlTree(['/login'])))
  );
};
