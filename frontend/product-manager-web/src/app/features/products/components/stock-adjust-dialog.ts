import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';

/** Which direction stock is moving. */
export type StockAdjustMode = 'add' | 'remove';

/** Values passed into the dialog. */
export interface StockAdjustDialogData {
  readonly mode: StockAdjustMode;
  readonly productName: string;
  readonly currentStock: number;
}

/** Values returned when the dialog is confirmed. */
export interface StockAdjustDialogResult {
  readonly quantity: number;
  readonly reason?: string;
}

/** Asks for a quantity and an optional reason before changing a product's stock. */
@Component({
  selector: 'app-stock-adjust-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>
      {{ data.mode === 'add' ? 'Add stock' : 'Remove stock' }}
    </h2>

    <mat-dialog-content>
      <p class="context">
        <strong>{{ data.productName }}</strong>
        <span class="context__stock">{{ data.currentStock }} in stock</span>
      </p>

      <form [formGroup]="form" class="form" (ngSubmit)="confirm()">
        <mat-form-field>
          <mat-label>Quantity</mat-label>
          <input
            matInput
            type="number"
            formControlName="quantity"
            min="1"
            cdkFocusInitial
            required
          />
          <mat-icon matSuffix>{{ data.mode === 'add' ? 'add' : 'remove' }}</mat-icon>
          @if (form.controls.quantity.hasError('required')) {
            <mat-error>Enter a quantity.</mat-error>
          } @else if (form.controls.quantity.hasError('min')) {
            <mat-error>Quantity must be at least 1.</mat-error>
          } @else if (form.controls.quantity.hasError('max')) {
            <mat-error>Only {{ data.currentStock }} in stock.</mat-error>
          }
        </mat-form-field>

        <mat-form-field>
          <mat-label>Reason (optional)</mat-label>
          <input matInput formControlName="reason" maxlength="250" />
          <mat-hint>Saved with the stock movement.</mat-hint>
        </mat-form-field>
      </form>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button matButton (click)="dialogRef.close()">Cancel</button>
      <button matButton="filled" [disabled]="form.invalid" (click)="confirm()">
        {{ data.mode === 'add' ? 'Add' : 'Remove' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .context {
      display: flex;
      justify-content: space-between;
      align-items: baseline;
      gap: 1rem;
      margin: 0 0 1rem;
    }

    .context__stock {
      color: var(--mat-sys-on-surface-variant);
      font: var(--mat-sys-body-small);
      white-space: nowrap;
    }

    .form {
      display: flex;
      flex-direction: column;
      min-width: 320px;
    }

    @media (max-width: 599px) {
      .form {
        min-width: unset;
      }
    }
  `,
})
export class StockAdjustDialog {
  protected readonly dialogRef =
    inject<MatDialogRef<StockAdjustDialog, StockAdjustDialogResult>>(MatDialogRef);
  protected readonly data = inject<StockAdjustDialogData>(MAT_DIALOG_DATA);

  private readonly formBuilder = inject(FormBuilder);

  protected readonly form = this.formBuilder.nonNullable.group({
    // Removing more than is in stock is blocked here as well as on the server.
    quantity: [
      1,
      this.data.mode === 'remove'
        ? [Validators.required, Validators.min(1), Validators.max(this.data.currentStock)]
        : [Validators.required, Validators.min(1)],
    ],
    reason: [''],
  });

  protected confirm(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { quantity, reason } = this.form.getRawValue();
    const trimmedReason = reason.trim();

    this.dialogRef.close({
      quantity: Number(quantity),
      reason: trimmedReason.length > 0 ? trimmedReason : undefined,
    });
  }
}
