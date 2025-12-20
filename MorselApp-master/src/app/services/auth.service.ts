import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, of } from 'rxjs';
import { LoginRequest, LoginResponse, User } from '../models/auth.model';
import { AppConfig } from '../config';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly API_URL = AppConfig.api.baseUrl;
  private readonly STORAGE_KEYS = AppConfig.storage;

  private currentUser = signal<User | null>(null);

  readonly user = this.currentUser.asReadonly();
  readonly isLoggedIn = computed(() => !!this.getApiKey());

  constructor(private http: HttpClient, private router: Router) {
    this.loadUserFromStorage();
  }

  private getStorage(): Storage {
    // Use localStorage if "Remember Me" was checked, otherwise sessionStorage
    const rememberMe = localStorage.getItem(this.STORAGE_KEYS.rememberMe) === 'true';
    return rememberMe ? localStorage : sessionStorage;
  }

  private loadUserFromStorage(): void {
    // Check both storages for existing session
    let userJson = localStorage.getItem(this.STORAGE_KEYS.user);
    if (!userJson) {
      userJson = sessionStorage.getItem(this.STORAGE_KEYS.user);
    }

    if (userJson) {
      try {
        this.currentUser.set(JSON.parse(userJson));
      } catch {
        this.currentUser.set(null);
      }
    }
  }

  getApiKey(): string | null {
    // Check both storages
    return localStorage.getItem(this.STORAGE_KEYS.apiKey) ||
           sessionStorage.getItem(this.STORAGE_KEYS.apiKey);
  }

  login(email: string, password: string, rememberMe: boolean = false): Observable<LoginResponse> {
    const request: LoginRequest = { email, password };

    return this.http.post<LoginResponse>(`${this.API_URL}${AppConfig.api.auth.login}`, request).pipe(
      tap(response => {
        if (response.success) {
          // Store remember me preference in localStorage
          localStorage.setItem(this.STORAGE_KEYS.rememberMe, String(rememberMe));

          // Get the appropriate storage based on remember me
          const storage = rememberMe ? localStorage : sessionStorage;

          // Clear both storages first
          this.clearAllStorage();

          // Store in the appropriate storage
          storage.setItem(this.STORAGE_KEYS.apiKey, response.apiKey);
          storage.setItem(this.STORAGE_KEYS.user, JSON.stringify(response.user));

          // Keep remember me preference in localStorage
          localStorage.setItem(this.STORAGE_KEYS.rememberMe, String(rememberMe));

          this.currentUser.set(response.user);
        }
      }),
      catchError(error => {
        console.error('Login error:', error);

        let errorMessage = 'Login failed. Please try again.';

        if (error.status === 0) {
          errorMessage = 'Unable to connect to server. Please check your connection.';
        } else if (error.status === 401) {
          errorMessage = 'Invalid email or password. Please try again.';
        } else if (error.status === 403) {
          errorMessage = 'Account is locked. Please contact support.';
        } else if (error.status === 404) {
          errorMessage = 'User not found. Please check your email.';
        } else if (error.status === 429) {
          errorMessage = 'Too many login attempts. Please try again later.';
        } else if (error.status >= 500) {
          errorMessage = 'Server error. Please try again later.';
        } else if (error.error?.message) {
          errorMessage = error.error.message;
        }

        return of({
          success: false,
          message: errorMessage,
          apiKey: '',
          user: {} as User
        });
      })
    );
  }

  private clearAllStorage(): void {
    // Clear from both storages
    localStorage.removeItem(this.STORAGE_KEYS.apiKey);
    localStorage.removeItem(this.STORAGE_KEYS.user);
    sessionStorage.removeItem(this.STORAGE_KEYS.apiKey);
    sessionStorage.removeItem(this.STORAGE_KEYS.user);
  }

  logout(): void {
    this.clearAllStorage();
    // Don't clear remember password credentials - keep them for next login
    // Only clear the session/auth data
    this.currentUser.set(null);
    // Set flag so login page knows user just logged out
    sessionStorage.setItem('just_logged_out', 'true');
    this.router.navigate(['/login']);
  }

  isAuthenticated(): boolean {
    return !!this.getApiKey();
  }
}
