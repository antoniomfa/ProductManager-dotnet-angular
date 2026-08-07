import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { ProductApiService } from '../../../../core/api/product-api.service';
import { ProductDetail as ProductDetailModel } from '../../../../core/models/product.model';
import { NotificationService } from '../../../../core/services/notification.service';
import { ConfirmDialog, ConfirmDialogData } from '../../../../shared/components/confirm-dialog';
import {
  StockAdjustDialog,
  StockAdjustDialogData,
  StockAdjustDialogResult,
  StockAdjustMode,
} from '../../components/stock-adjust-dialog';

/** Shows one product and its stock history. */
@Component({
  selector: 'app-product-detail',
  imports: [
    CurrencyPipe,
    DatePipe,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatDividerModule,
    MatTooltipModule,
  ],
  templateUrl: './product-detail.html',
  styleUrl: './product-detail.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductDetail {
  private readonly api = inject(ProductApiService);
  private readonly dialog = inject(MatDialog);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);

  /** Comes from the route, so it arrives as a string. */
  readonly id = input.required<string>();

  protected readonly product = signal<ProductDetailModel | undefined>(undefined);
  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);

  protected readonly movementColumns = [
    'occurredAt',
    'type',
    'quantityChange',
    'resultingStock',
    'reason',
  ];

  // Keyed by string because rows come out of the Material table untyped.
  protected readonly stockLabels: Record<string, string> = {
    OutOfStock: 'out of stock',
    LowStock: 'low stock',
    InStock: 'in stock',
  };

  constructor() {
    // Reloads when the route id changes.
    effect(() => {
      const productId = Number(this.id());
      void this.load(productId);
    });
  }

  protected openAddStock(): void {
    void this.adjustStock('add');
  }

  protected openRemoveStock(): void {
    void this.adjustStock('remove');
  }

  protected confirmDelete(): void {
    const current = this.product();
    if (!current) {
      return;
    }

    const data: ConfirmDialogData = {
      title: 'Delete product?',
      message: `"${current.name}" will no longer appear in the catalogue.`,
      confirmLabel: 'Delete',
      destructive: true,
    };

    this.dialog
      .open(ConfirmDialog, { data, width: '420px' })
      .afterClosed()
      .subscribe(async (confirmed) => {
        if (!confirmed) {
          return;
        }

        try {
          await firstValueFrom(this.api.deleteProduct(current.id));
          this.notifications.success(`Deleted "${current.name}".`);
          await this.router.navigate(['/products']);
        } catch {
          // The error interceptor already showed a message.
        }
      });
  }

  private async load(productId: number): Promise<void> {
    if (!Number.isFinite(productId)) {
      this.notFound.set(true);
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.notFound.set(false);

    try {
      this.product.set(await firstValueFrom(this.api.getProduct(productId)));
    } catch {
      this.product.set(undefined);
      this.notFound.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  private async adjustStock(mode: StockAdjustMode): Promise<void> {
    const current = this.product();
    if (!current) {
      return;
    }

    const data: StockAdjustDialogData = {
      mode,
      productName: current.name,
      currentStock: current.stockQuantity,
    };

    const result = await firstValueFrom(
      this.dialog
        .open<StockAdjustDialog, StockAdjustDialogData, StockAdjustDialogResult | undefined>(
          StockAdjustDialog,
          { data },
        )
        .afterClosed(),
    );

    if (!result) {
      return;
    }

    try {
      const request =
        mode === 'add'
          ? this.api.addToStock(current.id, result.quantity, result.reason)
          : this.api.decrementStock(current.id, result.quantity, result.reason);

      const outcome = await firstValueFrom(request);
      this.notifications.success(`Stock ${outcome.previousStock} to ${outcome.newStock}.`);

      // Reload so the history shows the new entry.
      await this.load(current.id);
    } catch {
      // The error interceptor already showed a message.
    }
  }
}
