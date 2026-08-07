import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { Category } from '../models/product.model';
import { API_BASE_URL } from './api-base-url';

/** Reads the category list from the API. */
@Injectable({ providedIn: 'root' })
export class CategoryApiService {
  private readonly http = inject(HttpClient);

  /** GET /api/categories */
  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${API_BASE_URL}/categories`);
  }
}
