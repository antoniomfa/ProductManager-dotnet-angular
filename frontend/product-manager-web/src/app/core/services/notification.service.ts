import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

/** Shows short messages at the bottom of the screen. */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);

  success(message: string): void {
    this.snackBar.open(message, 'Dismiss', {
      duration: 4000,
      panelClass: 'app-snackbar-success',
    });
  }

  error(message: string): void {
    // Errors stay on screen longer, because the user has to read and act on them.
    this.snackBar.open(message, 'Dismiss', {
      duration: 8000,
      panelClass: 'app-snackbar-error',
    });
  }
}
