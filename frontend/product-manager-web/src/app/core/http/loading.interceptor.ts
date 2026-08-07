import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';

import { LoadingService } from '../services/loading.service';

/** Counts requests in and out so the progress bar knows when to show. */
export const loadingInterceptor: HttpInterceptorFn = (request, next) => {
  const loading = inject(LoadingService);

  loading.start();

  // finalize, not tap, so the count also drops when a request fails.
  return next(request).pipe(finalize(() => loading.stop()));
};
