import { Component, inject, signal, OnInit, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { environment } from '../../../../environments/environment';

const ROLE_ROUTES: Record<string, string> = {
  Admin: '/admin/dashboard', StoreManager: '/manager/dashboard',
  DeliveryDriver: '/delivery', Customer: '/products',
};

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="min-h-screen bg-gray-50 dark:bg-gray-950 flex items-center justify-center p-4">
      <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl shadow-sm p-8 w-full max-w-md">
        <div class="text-center mb-7">
          <span class="text-4xl">🛒</span>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-white mt-2">FreshMart</h1>
          <p class="text-gray-500 dark:text-gray-400 text-sm mt-1">Sign in to your account</p>
        </div>

        @if (error()) {
          <div class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-400 rounded-lg px-4 py-3 mb-4 text-sm">
            {{ error() }}
          </div>
        }

        <button type="button" (click)="googleSignIn()" [disabled]="loading()"
          class="w-full flex items-center justify-center gap-3 border border-gray-300 dark:border-gray-600 rounded-lg px-4 py-2.5 text-sm font-medium text-gray-700 dark:text-gray-200 bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700 transition disabled:opacity-50 mb-4">
          <svg class="w-5 h-5" viewBox="0 0 24 24">
            <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
            <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
            <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l3.66-2.84z"/>
            <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
          </svg>
          Continue with Google
        </button>

        <div class="flex items-center gap-3 mb-4">
          <div class="flex-1 h-px bg-gray-200 dark:bg-gray-700"></div>
          <span class="text-xs text-gray-400">or</span>
          <div class="flex-1 h-px bg-gray-200 dark:bg-gray-700"></div>
        </div>

        <form (ngSubmit)="submit()" #f="ngForm" class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Email</label>
            <input type="email" name="email" [(ngModel)]="email" required
              class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-transparent transition" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Password</label>
            <input type="password" name="password" [(ngModel)]="password" required
              class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-transparent transition" />
          </div>
          <button type="submit" [disabled]="loading() || f.invalid"
            class="w-full bg-green-600 hover:bg-green-700 disabled:opacity-50 text-white py-2.5 rounded-lg font-medium transition">
            {{ loading() ? 'Signing in...' : 'Sign in' }}
          </button>
        </form>

        <p class="text-center text-sm text-gray-500 dark:text-gray-400 mt-4">
          No account? <a routerLink="/auth/register" class="text-green-600 dark:text-green-400 font-medium hover:underline">Register</a>
        </p>

        <div class="mt-6 border-t border-gray-100 dark:border-gray-800 pt-5">
          <p class="text-xs text-gray-400 text-center mb-3">Demo accounts</p>
          <div class="grid grid-cols-2 gap-2">
            @for (demo of demos; track demo.label) {
              <button type="button" (click)="fillDemo(demo.email, demo.password)"
                class="text-xs border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2 hover:bg-gray-50 dark:hover:bg-gray-800 text-gray-600 dark:text-gray-300 text-left transition">
                <span class="font-semibold block">{{ demo.label }}</span>
                <span class="text-gray-400 dark:text-gray-500">{{ demo.email }}</span>
              </button>
            }
          </div>
        </div>
      </div>
    </div>
  `
})
export class Login implements OnInit {
  private auth = inject(AuthService);
  private router = inject(Router);
  private platformId = inject(PLATFORM_ID);

  email = ''; password = '';
  loading = signal(false);
  error = signal('');

  demos = [
    { label: '👑 Admin',    email: 'admin@grocery.com',    password: 'Admin@123' },
    { label: '🏪 Manager',  email: 'manager@grocery.com',  password: 'Manager@123' },
    { label: '🚚 Driver',   email: 'driver@grocery.com',   password: 'Driver@123' },
    { label: '🛒 Customer', email: 'customer@grocery.com', password: 'Customer@123' },
  ];

  ngOnInit() {
    if (!isPlatformBrowser(this.platformId)) return;
    if (!(window as any).google) {
      const s = document.createElement('script');
      s.src = 'https://accounts.google.com/gsi/client';
      s.async = true;
      document.head.appendChild(s);
    }
  }

  googleSignIn() {
    if (!isPlatformBrowser(this.platformId)) return;
    this.error.set('');

    const launch = () => {
      const google = (window as any).google;
      // Use OAuth2 token client — opens a real popup, works on HTTP
      const client = google.accounts.oauth2.initTokenClient({
        client_id: environment.googleClientId,
        scope: 'openid email profile',
        callback: (tokenRes: any) => {
          if (tokenRes.error) { this.error.set('Google sign-in was cancelled.'); return; }
          this.loading.set(true);
          this.auth.googleLogin(tokenRes.access_token).subscribe({
            next: (t) => this.router.navigate([ROLE_ROUTES[t.role] ?? '/products']),
            error: (e) => { this.error.set(e.error?.error ?? 'Google sign-in failed'); this.loading.set(false); }
          });
        },
      });
      client.requestAccessToken({ prompt: 'select_account' });
    };

    if ((window as any).google?.accounts?.oauth2) {
      launch();
    } else {
      // Script still loading — poll for up to 5s
      let waited = 0;
      const poll = setInterval(() => {
        waited += 200;
        if ((window as any).google?.accounts?.oauth2) { clearInterval(poll); launch(); }
        else if (waited >= 5000) { clearInterval(poll); this.error.set('Google Sign-In failed to load. Please refresh.'); }
      }, 200);
    }
  }

  fillDemo(email: string, password: string) { this.email = email; this.password = password; }

  submit() {
    this.loading.set(true); this.error.set('');
    this.auth.login(this.email, this.password).subscribe({
      next: (t) => this.router.navigate([ROLE_ROUTES[t.role] ?? '/products']),
      error: (e) => { this.error.set(e.error?.error ?? 'Login failed'); this.loading.set(false); }
    });
  }
}
