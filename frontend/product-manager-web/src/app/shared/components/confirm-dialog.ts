import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';

/** Text and styling for a confirmation prompt. */
export interface ConfirmDialogData {
  readonly title: string;
  readonly message: string;
  readonly confirmLabel?: string;
  readonly cancelLabel?: string;
  /** Shows the confirm button in red. */
  readonly destructive?: boolean;
}

/** Yes/no prompt. Closes with true when confirmed, false otherwise. */
@Component({
  selector: 'app-confirm-dialog',
  imports: [MatDialogModule, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>

    <mat-dialog-content>
      <p>{{ data.message }}</p>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button matButton (click)="dialogRef.close(false)">
        {{ data.cancelLabel ?? 'Cancel' }}
      </button>
      <button
        matButton="filled"
        [class.destructive]="data.destructive"
        cdkFocusInitial
        (click)="dialogRef.close(true)"
      >
        {{ data.confirmLabel ?? 'Confirm' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .destructive {
      --mat-button-filled-container-color: var(--mat-sys-error);
      --mat-button-filled-label-text-color: var(--mat-sys-on-error);
    }
  `,
})
export class ConfirmDialog {
  protected readonly dialogRef = inject(MatDialogRef<ConfirmDialog, boolean>);
  protected readonly data = inject<ConfirmDialogData>(MAT_DIALOG_DATA);
}
