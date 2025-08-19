import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable, PLATFORM_ID } from '@angular/core';
import { BehaviorSubject, catchError, map, Observable, of, switchMap, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, LoginRequest, RegisterRequest, User } from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private platformId = inject(PLATFORM_ID);
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  currentUser$ = this.currentUserSubject.asObservable();

  get token(): string | null {
    if (!isPlatformBrowser(this.platformId)) return null;
  const raw = localStorage.getItem(environment.storage.tokenKey);
  if (!raw) return null;
  let t = raw.trim();
  if (t.startsWith('"') && t.endsWith('"')) t = t.slice(1, -1);
  if (t.toLowerCase().startsWith('bearer ')) t = t.slice(7).trim();
  return t || null;
  }

  get isAuthenticated(): boolean {
    return !!this.token;
  }

  get currentUser(): User | null {
    return this.currentUserSubject.value;
  }

  initFromStorage(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const userRaw = localStorage.getItem(environment.storage.userKey);
    if (userRaw) {
      try { this.currentUserSubject.next(JSON.parse(userRaw)); } catch {}
    }
  }

  login(payload: LoginRequest): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${environment.apiUrl}/auth/login`, payload).pipe(
      tap(res => {
        if (res.success && res.data && isPlatformBrowser(this.platformId)) {
          localStorage.setItem(environment.storage.tokenKey, res.data);
        }
      }),
      switchMap(res => {
        if (res.success && res.data) {
          return this.refreshMe().pipe(map(() => res));
        }
        return of(res);
      })
    );
  }

  register(payload: RegisterRequest): Observable<ApiResponse<User>> {
    return this.http.post<ApiResponse<User>>(`${environment.apiUrl}/auth/register`, payload).pipe(
      tap(res => {
        if (res.success && res.data) {
          if (isPlatformBrowser(this.platformId)) {
            localStorage.setItem(environment.storage.userKey, JSON.stringify(res.data));
          }
          this.currentUserSubject.next(res.data);
        }
      })
    );
  }

  refreshMe(): Observable<ApiResponse<User>> {
    return this.http.get<ApiResponse<User>>(`${environment.apiUrl}/auth/me`).pipe(
      tap(res => {
        if (res.success && res.data) {
          if (isPlatformBrowser(this.platformId)) {
            localStorage.setItem(environment.storage.userKey, JSON.stringify(res.data));
          }
          this.currentUserSubject.next(res.data);
        }
      }),
      catchError((error: any) => {
        // If token is invalid/expired, clear it and redirect to login
        if (error.status === 401) {
          this.logout();
        }
        return of({ success: false, message: error.message || 'Authentication failed', data: undefined } as ApiResponse<User>);
      })
    );
  }

  superAdminExists(): Observable<ApiResponse<boolean>> {
    return this.http.get<ApiResponse<boolean>>(`${environment.apiUrl}/auth/superadmin-exists`);
  }

  logout(): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem(environment.storage.tokenKey);
      localStorage.removeItem(environment.storage.userKey);
    }
    this.currentUserSubject.next(null);
  }
}
