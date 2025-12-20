import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../services';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);

  private readonly REMEMBER_KEY = 'morsel_remember_password';
  private readonly CREDENTIALS_KEY = 'morsel_saved_credentials';

  email: string = '';
  password: string = '';
  rememberMe: boolean = false;
  errorMessage: string = '';
  showPassword: boolean = false;

  ngOnInit() {
    // Load saved credentials if "Remember Password" was checked
    this.loadSavedCredentials();

    // Check if user is already authenticated (and not just logged out)
    const justLoggedOut = sessionStorage.getItem('just_logged_out') === 'true';
    if (this.authService.isAuthenticated() && !justLoggedOut) {
      this.router.navigate(['/home']);
      return;
    }

    // Clear the logout flag
    sessionStorage.removeItem('just_logged_out');
  }

  private loadSavedCredentials(): void {
    const rememberMe = localStorage.getItem(this.REMEMBER_KEY) === 'true';
    this.rememberMe = rememberMe;

    if (rememberMe) {
      const savedCredentials = localStorage.getItem(this.CREDENTIALS_KEY);
      if (savedCredentials) {
        try {
          const credentials = JSON.parse(atob(savedCredentials));
          this.email = credentials.email || '';
          this.password = credentials.password || '';
          // Don't auto-login, just fill the fields
        } catch {
          // Invalid saved data, clear it
          this.clearSavedCredentials();
        }
      }
    }
  }

  private saveCredentials(): void {
    if (this.rememberMe && this.email && this.password) {
      // Encode credentials (not encryption, but obfuscation for basic security)
      const credentials = btoa(JSON.stringify({ email: this.email, password: this.password }));
      localStorage.setItem(this.CREDENTIALS_KEY, credentials);
      localStorage.setItem(this.REMEMBER_KEY, 'true');
    }
  }

  private clearSavedCredentials(): void {
    localStorage.removeItem(this.CREDENTIALS_KEY);
    localStorage.removeItem(this.REMEMBER_KEY);
  }

  onRememberMeChange(): void {
    if (!this.rememberMe) {
      // Immediately clear saved credentials when unchecked
      this.clearSavedCredentials();
    }
  }

  togglePasswordVisibility() {
    this.showPassword = !this.showPassword;
  }

  onSignIn() {
    if (!this.email || !this.password) {
      this.errorMessage = 'Please enter email and password';
      return;
    }

    this.errorMessage = '';

    this.authService.login(this.email, this.password, this.rememberMe).subscribe(response => {
      if (response.success) {
        // Save credentials if remember password is checked
        if (this.rememberMe) {
          this.saveCredentials();
        } else {
          this.clearSavedCredentials();
        }
        this.router.navigate(['/home']);
      } else {
        this.errorMessage = response.message || 'Login failed. Please try again.';
      }
    });
  }
}
