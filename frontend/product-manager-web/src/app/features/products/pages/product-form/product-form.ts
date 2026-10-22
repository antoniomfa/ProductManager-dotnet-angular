import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Router, RouterLink } from '@angular/router';
import { catchError, firstValueFrom, of } from 'rxjs';

import { CategoryApiService } from '../../../../core/api/category-api.service';
import { ProductApiService } from '../../../../core/api/product-api.service';
import { CreateProductRequest, UpdateProductRequest } from '../../../../core/models/product.model';
import { NotificationService } from '../../../../core/services/notification.service';

// Same limits as the API validators, so the user sees errors before sending the request.
const NAME_MAX_LENGTH = 150;
const DESCRIPTION_MAX_LENGTH = 1000;
const MAX_PRICE = 9_999_999.99;
const MAX_STOCK = 1_000_000;

/**
 * Creates or edits a product. Which one depends on whether the route has an id.
 * The API validates everything again, so these checks are only for quick feedback.
 */
@Component({
  selector: 'app-product-form',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './product-form.html',
  styleUrl: './product-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductForm {
  private readonly api = inject(ProductApiService);
  private readonly categoriesApi = inject(CategoryApiService);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);

  /** Set on the edit route, empty on the create route. */
  readonly id = input<string | undefined>(undefined);

  protected readonly isEditMode = computed(() => this.id() !== undefined);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly notFound = signal(false);

  protected readonly categories = toSignal(
    this.categoriesApi.getCategories().pipe(catchError(() => of([]))),
    { initialValue: [] },
  );

  protected readonly limits = {
    nameMaxLength: NAME_MAX_LENGTH,
    descriptionMaxLength: DESCRIPTION_MAX_LENGTH,
  };

  protected readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(NAME_MAX_LENGTH)]],
    description: ['', [Validators.maxLength(DESCRIPTION_MAX_LENGTH)]],
    price: [0, [Validators.required, Validators.min(0), Validators.max(MAX_PRICE)]],
    initialStock: [0, [Validators.required, Validators.min(0), Validators.max(MAX_STOCK)]],
    lowStockThreshold: [5, [Validators.required, Validators.min(0), Validators.max(MAX_STOCK)]],
    categoryId: [null as number | null, [Validators.required]],
  });

  constructor() {
    effect(() => {
      const routeId = this.id();

      if (routeId === undefined) {
        this.resetForCreate();
        return;
      }

      void this.loadForEdit(Number(routeId));
    });
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notifications.error('Fix the highlighted fields before saving.');
      return;
    }

    this.saving.set(true);

    try {
      const savedId = this.isEditMode() ? await this.update() : await this.create();

      await this.router.navigate(['/products', savedId]);
    } catch {
      // The error interceptor already showed a message. The form keeps what the user typed.
    } finally {
      this.saving.set(false);
    }
  }

  private async create(): Promise<number> {
    const value = this.form.getRawValue();

    const request: CreateProductRequest = {
      name: value.name.trim(),
      description: this.trimmedOrUndefined(value.description),
      price: Number(value.price),
      initialStock: Number(value.initialStock),
      lowStockThreshold: Number(value.lowStockThreshold),
      categoryId: value.categoryId as number,
    };

    const created = await firstValueFrom(this.api.createProduct(request));
    this.notifications.success(`Created "${created.name}" with ID ${created.id}.`);

    return created.id;
  }

  private async update(): Promise<number> {
    const value = this.form.getRawValue();
    const productId = Number(this.id());

    // Stock is not sent here. It changes through the add and remove stock endpoints instead,
    // so every change is recorded in the product's history.
    const request: UpdateProductRequest = {
      name: value.name.trim(),
      description: this.trimmedOrUndefined(value.description),
      price: Number(value.price),
      lowStockThreshold: Number(value.lowStockThreshold),
      categoryId: value.categoryId as number,
    };

    const updated = await firstValueFrom(this.api.updateProduct(productId, request));
    this.notifications.success(`Saved changes to "${updated.name}".`);

    return updated.id;
  }

  private resetForCreate(): void {
    this.notFound.set(false);
    this.form.reset({
      name: '',
      description: '',
      price: 0,
      initialStock: 0,
      lowStockThreshold: 5,
      categoryId: null,
    });
    this.form.controls.initialStock.enable();
  }

  private async loadForEdit(productId: number): Promise<void> {
    if (!Number.isFinite(productId)) {
      this.notFound.set(true);
      return;
    }

    this.loading.set(true);
    this.notFound.set(false);

    try {
      const product = await firstValueFrom(this.api.getProduct(productId));

      this.form.patchValue({
        name: product.name,
        description: product.description ?? '',
        price: product.price,
        initialStock: product.stockQuantity,
        lowStockThreshold: product.lowStockThreshold,
        categoryId: product.categoryId,
      });

      // Shown for information only. Stock is changed from the product page.
      this.form.controls.initialStock.disable();
    } catch {
      this.notFound.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  private trimmedOrUndefined(value: string): string | undefined {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : undefined;
  }
}
