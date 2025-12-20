import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const apiKey = authService.getApiKey();

  // Skip adding API key for login request
  if (req.url.includes('/Auth/login')) {
    return next(req);
  }

  // Add X-API-Key header if we have an API key
  if (apiKey) {
    const authReq = req.clone({
      setHeaders: {
        'X-API-Key': apiKey
      }
    });
    return next(authReq);
  }

  return next(req);
};
