import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { NotificationService } from '../services/notification.service';
import { describeHttpError } from './problem-details';

/**
 * Shows a message whenever a request fails, then rethrows so the caller can still react.
 */
export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const notifications = inject(NotificationService);

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      notifications.error(describeHttpError(error));

      return throwError(() => error);
    }),
  );
};
