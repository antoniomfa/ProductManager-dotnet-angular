import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { PagedResult, ProductQuery } from '../models/paged-result.model';
import {
  CreateProductRequest,
  Product,
  ProductDetail,
  StockOperationResult,
  UpdateProductRequest,
} from '../models/product.model';
import { API_BASE_URL } from './api-base-url';

/** Calls the product endpoints. One method per endpoint. */
@Injectable({ providedIn: 'root' })
export class ProductApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/products`;

  /** GET /api/products */
  getProducts(query: ProductQuery = {}): Observable<PagedResult<Product>> {
    let params = new HttpParams();

    // Only send the options that were given, so the API applies its own defaults.
    if (query.page !== undefined)
      params = params.set('page', query.page);

    if (query.pageSize !== undefined)
      params = params.set('pageSize', query.pageSize);

    if (query.sortBy)
      params = params.set('sortBy', query.sortBy);

    if (query.sortDescending !== undefined) {
      params = params.set('sortDescending', query.sortDescending);
    }

    if (query.categoryId !== undefined)
      params = params.set('categoryId', query.categoryId);

    if (query.search)
      params = params.set('search', query.search);

    return this.http.get<PagedResult<Product>>(this.baseUrl, { params });
  }

  /** GET /api/products/{id} */
  getProduct(id: number): Observable<ProductDetail> {
    return this.http.get<ProductDetail>(`${this.baseUrl}/${id}`);
  }

  /** POST /api/products */
  createProduct(request: CreateProductRequest): Observable<ProductDetail> {
    return this.http.post<ProductDetail>(this.baseUrl, request);
  }

  /** PUT /api/products/{id} */
  updateProduct(id: number, request: UpdateProductRequest): Observable<ProductDetail> {
    return this.http.put<ProductDetail>(`${this.baseUrl}/${id}`, request);
  }

  /** DELETE /api/products/{id} */
  deleteProduct(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /** POST /api/products/{id}/add-to-stock/{quantity} */
  addToStock(id: number, quantity: number, reason?: string): Observable<StockOperationResult> {
    return this.http.post<StockOperationResult>(
      `${this.baseUrl}/${id}/add-to-stock/${quantity}`,
      { reason }
    );
  }

  /** POST /api/products/{id}/decrement-stock/{quantity} */
  decrementStock(id: number, quantity: number, reason?: string): Observable<StockOperationResult> {
    return this.http.post<StockOperationResult>(
      `${this.baseUrl}/${id}/decrement-stock/${quantity}`,
      { reason }
    );
  }

  /** GET /api/products/search?name= */
  searchByName(name: string): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.baseUrl}/search`, {
      params: new HttpParams().set('name', name)
    });
  }

  /** GET /api/products/stock-level?min=&max= */
  getByStockLevel(min?: number, max?: number): Observable<Product[]> {
    let params = new HttpParams();

    if (min !== undefined) 
      params = params.set('min', min);

    if (max !== undefined) 
      params = params.set('max', max);

    return this.http.get<Product[]>(`${this.baseUrl}/stock-level`, { params });
  }
}
