import { Injectable, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { AuthTokens, User } from '../models';
import { environment } from '../../../environments/environment';

/**
 * Service responsible for all authentication operations in FreshMart.
 * Handles email/password login, Google OAuth2, registration, token storage,
 * token refresh, logout, and JWT claim extraction.
 * Tokens are persisted in localStorage (browser only — SSR-safe via PLATFORM_ID).
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private platformId = inject(PLATFORM_ID);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/auth`;

  /**
   * Lazily resolves the NotificationService to avoid a circular dependency
   * (NotificationService → AuthService → NotificationService).
   * Stored on the global window object by the NotificationService itself.
   */
  private get notifService() {
    return (window as any).__notifService as import('./notification.service').NotificationService | undefined;
  }

  /**
   * Authenticates the user with email and password.
   * On success, stores the JWT access token and refresh token in localStorage
   * and initialises the SignalR notification connection.
   * @param email - User's registered email address.
   * @param password - User's plain-text password.
   * @returns Observable of AuthTokens containing accessToken, refreshToken, role, and userId.
   */
  login(email: string, password: string): Observable<AuthTokens> {
    return this.http.post<AuthTokens>(`${this.baseUrl}/login`, { email, password }).pipe(
      tap(tokens => {
        this.storeTokens(tokens);
        this.notifService?.init(tokens.accessToken);
      })
    );
  }

  /**
   * Authenticates or registers the user via Google OAuth2.
   * Sends the Google ID token to the backend for verification.
   * On success, stores tokens and initialises the notification connection.
   * @param idToken - Google OAuth2 ID token or access token from the Google Identity SDK.
   * @returns Observable of AuthTokens.
   */
  googleLogin(idToken: string): Observable<AuthTokens> {
    return this.http.post<AuthTokens>(`${this.baseUrl}/google`, { idToken }).pipe(
      tap(tokens => {
        this.storeTokens(tokens);
        this.notifService?.init(tokens.accessToken);
      })
    );
  }

  /**
   * Registers a new customer account.
   * Does not log the user in automatically — the caller should redirect to login.
   * @param data - Registration data including email, password, name, and optional phone.
   * @returns Observable of the created user's ID, email, and default role.
   */
  register(data: {
    email: string; password: string; firstName: string;
    lastName: string; phoneNumber?: string;
  }): Observable<{ userId: string; email: string; role: string }> {
    return this.http.post<any>(`${this.baseUrl}/register`, data);
  }

  /**
   * Logs out the current user.
   * Calls the backend to invalidate the refresh token, disconnects SignalR,
   * clears tokens from localStorage, and redirects to the login page.
   */
  logout(): void {
    const refreshToken = this.getRefreshToken();
    if (refreshToken) {
      this.http.post(`${this.baseUrl}/logout`, { refreshToken }).subscribe({ error: () => {} });
    }
    this.notifService?.disconnect();
    this.clearTokens();
    this.router.navigate(['/auth/login']);
  }

  /**
   * Silently refreshes the JWT access token using the stored refresh token.
   * Called automatically by the auth interceptor on 401 responses.
   * Stores the new tokens on success.
   * @returns Observable of the new AuthTokens.
   */
  refreshToken(): Observable<AuthTokens> {
    return this.http.post<AuthTokens>(`${this.baseUrl}/refresh`,
      { refreshToken: this.getRefreshToken() }).pipe(
      tap(tokens => this.storeTokens(tokens))
    );
  }

  /**
   * Fetches the authenticated user's full profile from the backend.
   * Used to populate the profile page and keep the UI in sync after profile updates.
   * @returns Observable of the User profile object.
   */
  getProfile(): Observable<User> {
    return this.http.get<User>(`${this.baseUrl}/me`);
  }

  /**
   * Returns the stored JWT access token from localStorage.
   * Returns null in SSR environments where localStorage is unavailable.
   */
  getAccessToken(): string | null {
    if (!isPlatformBrowser(this.platformId)) return null;
    return localStorage.getItem('access_token');
  }

  /**
   * Returns the stored refresh token from localStorage.
   * Returns null in SSR environments.
   */
  getRefreshToken(): string | null {
    if (!isPlatformBrowser(this.platformId)) return null;
    return localStorage.getItem('refresh_token');
  }

  /**
   * Checks whether the current access token is present and not expired.
   * Decodes the JWT payload to read the `exp` claim without a library.
   * Returns false in SSR environments.
   */
  isAuthenticated(): boolean {
    if (!isPlatformBrowser(this.platformId)) return false;
    const token = this.getAccessToken();
    if (!token) return false;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.exp * 1000 > Date.now();
    } catch { return false; }
  }

  /**
   * Extracts the user's first name from the JWT access token payload.
   * Tries the standard `given_name` claim first, then the ASP.NET Core claim URI fallback.
   * Returns null if the token is missing or the claim is not present.
   */
  getUserName(): string | null {
    if (!isPlatformBrowser(this.platformId)) return null;
    const token = this.getAccessToken();
    if (!token) return null;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      // JwtRegisteredClaimNames.GivenName maps to 'given_name'
      return payload['given_name']
        ?? payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname']
        ?? payload['name']
        ?? null;
    } catch { return null; }
  }

  /**
   * Extracts the user's role from the JWT access token payload.
   * Uses the ASP.NET Core role claim URI as the key.
   * Returns null if the token is missing or the role claim is absent.
   */
  getUserRole(): string | null {
    if (!isPlatformBrowser(this.platformId)) return null;
    const token = this.getAccessToken();
    if (!token) return null;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? null;
    } catch { return null; }
  }

  /**
   * Persists the access token and refresh token to localStorage.
   * No-op in SSR environments.
   */
  private storeTokens(tokens: AuthTokens): void {
    if (!isPlatformBrowser(this.platformId)) return;
    localStorage.setItem('access_token', tokens.accessToken);
    localStorage.setItem('refresh_token', tokens.refreshToken);
  }

  /**
   * Removes both tokens from localStorage on logout or session expiry.
   * No-op in SSR environments.
   */
  private clearTokens(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
  }
}
