import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';
import { catchError, debounceTime, firstValueFrom, of } from 'rxjs';

import { CategoryApiService } from '../../../../core/api/category-api.service';
import { ProductApiService } from '../../../../core/api/product-api.service';
import { Product } from '../../../../core/models/product.model';
import { NotificationService } from '../../../../core/services/notification.service';
import { ConfirmDialog, ConfirmDialogData } from '../../../../shared/components/confirm-dialog';
import {
  StockAdjustDialog,
  StockAdjustDialogData,
  StockAdjustDialogResult,
  StockAdjustMode,
} from '../../components/stock-adjust-dialog';

/** The products list screen. */
@Component({
  selector: 'app-product-list',
  imports: [
    CurrencyPipe,
    RouterLink,
    ReactiveFormsModule,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatMenuModule,
    MatTooltipModule,
  ],
  templateUrl: './product-list.html',
  styleUrl: './product-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductList implements OnInit {
  private readonly api = inject(ProductApiService);
  private readonly categoriesApi = inject(CategoryApiService);
  private readonly dialog = inject(MatDialog);
  private readonly notifications = inject(NotificationService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly products = signal<Product[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly loading = signal(false);
  protected readonly loaded = signal(false);

  // Only the plain product list is paged. Search and stock-level return every match at once.
  protected readonly isPaged = signal(true);

  protected readonly page = signal(1);
  protected readonly pageSize = signal(10);

  private sortBy = 'name';
  private sortDescending = false;

  protected readonly displayedColumns = ['id', 'name', 'category', 'price', 'stock', 'actions'];

  // Keyed by string because rows come out of the Material table untyped.
  protected readonly stockLabels: Record<string, string> = {
    OutOfStock: 'out of stock',
    LowStock: 'low stock',
    InStock: 'in stock',
  };

  protected readonly categories = toSignal(
    this.categoriesApi.getCategories().pipe(catchError(() => of([]))),
    { initialValue: [] },
  );

  protected readonly filters = this.formBuilder.nonNullable.group({
    search: [''],
    categoryId: [null as number | null],
    minStock: [null as number | null],
    maxStock: [null as number | null],
  });

  constructor() {
    // Debounced so a request is not sent on every keystroke.
    this.filters.valueChanges.pipe(debounceTime(400), takeUntilDestroyed()).subscribe(() => {
      this.page.set(1);
      void this.load();
    });
  }

  ngOnInit(): void {
    void this.load();
  }

  protected hasFilters(): boolean {
    const { search, categoryId, minStock, maxStock } = this.filters.getRawValue();
    return !!search.trim() || categoryId !== null || minStock !== null || maxStock !== null;
  }

  protected clearFilters(): void {
    this.filters.reset({ search: '', categoryId: null, minStock: null, maxStock: null });
  }

  protected onPage(event: PageEvent): void {
    this.page.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    void this.load();
  }

  protected onSort(sort: Sort): void {
    this.sortBy = sort.direction ? sort.active : 'name';
    this.sortDescending = sort.direction === 'desc';
    this.page.set(1);
    void this.load();
  }

  protected openAddStock(product: Product): void {
    void this.adjustStock(product, 'add');
  }

  protected openRemoveStock(product: Product): void {
    void this.adjustStock(product, 'remove');
  }

  protected confirmDelete(product: Product): void {
    const data: ConfirmDialogData = {
      title: 'Delete product?',
      message: `"${product.name}" will no longer appear in the catalogue.`,
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
          await firstValueFrom(this.api.deleteProduct(product.id));
          this.notifications.success(`Deleted "${product.name}".`);
          await this.load();
        } catch {
          // The error interceptor already showed a message.
        }
      });
  }

  /** Loads the list using whichever endpoint matches the active filters. */
  private async load(): Promise<void> {
    const { search, categoryId, minStock, maxStock } = this.filters.getRawValue();
    const term = search.trim();

    this.loading.set(true);

    try {
      if (term) {
        this.products.set(await firstValueFrom(this.api.searchByName(term)));
        this.totalCount.set(this.products().length);
        this.isPaged.set(false);
      } else if (minStock !== null || maxStock !== null) {
        this.products.set(
          await firstValueFrom(
            this.api.getByStockLevel(minStock ?? undefined, maxStock ?? undefined),
          ),
        );
        this.totalCount.set(this.products().length);
        this.isPaged.set(false);
      } else {
        const result = await firstValueFrom(
          this.api.getProducts({
            page: this.page(),
            pageSize: this.pageSize(),
            sortBy: this.sortBy,
            sortDescending: this.sortDescending,
            categoryId: categoryId ?? undefined,
          }),
        );

        this.products.set([...result.items]);
        this.totalCount.set(result.totalCount);
        this.isPaged.set(true);
      }
    } catch {
      this.products.set([]);
      this.totalCount.set(0);
    } finally {
      this.loading.set(false);
      this.loaded.set(true);
    }
  }

  private async adjustStock(product: Product, mode: StockAdjustMode): Promise<void> {
    const data: StockAdjustDialogData = {
      mode,
      productName: product.name,
      currentStock: product.stockQuantity,
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
          ? this.api.addToStock(product.id, result.quantity, result.reason)
          : this.api.decrementStock(product.id, result.quantity, result.reason);

      const outcome = await firstValueFrom(request);
      this.notifications.success(
        `${product.name}: stock ${outcome.previousStock} to ${outcome.newStock}.`,
      );

      // Reload so the table shows the server's value.
      await this.load();
    } catch {
      // The error interceptor already showed a message.
    }
  }
}
