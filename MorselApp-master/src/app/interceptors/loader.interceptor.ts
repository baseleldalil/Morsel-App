import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { LoaderService } from '../services/loader.service';

export const loaderInterceptor: HttpInterceptorFn = (req, next) => {
  const loaderService = inject(LoaderService);

  // Skip loader for certain requests if needed
  const skipLoader = req.headers.has('X-Skip-Loader') ||
    req.url.includes('/whatsapp/') ||
    req.url.includes(':5111/');

  if (!skipLoader) {
    loaderService.show();
  }

  return next(req).pipe(
    finalize(() => {
      if (!skipLoader) {
        loaderService.hide();
      }
    })
  );
};
