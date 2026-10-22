import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { PagedResult } from '../models/paged-result.model';
import { Product } from '../models/product.model';
import { ProductApiService } from './product-api.service';

/** Checks that each method calls the right URL with the right method and parameters. */
describe('ProductApiService', () => {
  let service: ProductApiService;
  let httpMock: HttpTestingController;

  const product: Product = {
    id: 100001,
    name: 'Aurora Over-Ear Headphones',
    price: 249.99,
    stockQuantity: 42,
    lowStockThreshold: 10,
    stockStatus: 'InStock',
    categoryId: 1,
    categoryName: 'Audio',
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  };

  const emptyPage: PagedResult<Product> = {
    items: [],
    page: 1,
    pageSize: 20,
    totalCount: 0,
    totalPages: 0,
    hasPreviousPage: false,
    hasNextPage: false,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(ProductApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('sends paging parameters', () => {
    service.getProducts({ page: 2, pageSize: 25 }).subscribe();

    const request = httpMock.expectOne(
      (candidate) => candidate.url === '/api/products' && candidate.method === 'GET',
    );

    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('25');

    // Options that were not given should not be sent.
    expect(request.request.params.has('search')).toBe(false);

    request.flush(emptyPage);
  });

  it('sends filtering and sorting parameters', () => {
    service
      .getProducts({ sortBy: 'price', sortDescending: true, categoryId: 3, search: 'lumen' })
      .subscribe();

    const request = httpMock.expectOne((candidate) => candidate.url === '/api/products');

    expect(request.request.params.get('sortBy')).toBe('price');
    expect(request.request.params.get('sortDescending')).toBe('true');
    expect(request.request.params.get('categoryId')).toBe('3');
    expect(request.request.params.get('search')).toBe('lumen');

    request.flush(emptyPage);
  });

  it('gets one product by id', () => {
    service.getProduct(100001).subscribe();

    const request = httpMock.expectOne('/api/products/100001');
    expect(request.request.method).toBe('GET');

    request.flush({ ...product, stockMovements: [] });
  });

  it('does not send an id when creating', () => {
    service
      .createProduct({
        name: 'New Product',
        price: 10,
        initialStock: 5,
        lowStockThreshold: 2,
        categoryId: 1,
      })
      .subscribe();

    const request = httpMock.expectOne('/api/products');
    expect(request.request.method).toBe('POST');

    // The database generates the id.
    expect(request.request.body).not.toHaveProperty('id');

    request.flush({ ...product, stockMovements: [] });
  });

  it('updates a product', () => {
    service
      .updateProduct(100001, {
        name: 'Renamed',
        price: 199.99,
        lowStockThreshold: 5,
        categoryId: 1,
      })
      .subscribe();

    const request = httpMock.expectOne('/api/products/100001');
    expect(request.request.method).toBe('PUT');

    request.flush({ ...product, stockMovements: [] });
  });

  it('deletes a product', () => {
    service.deleteProduct(100001).subscribe();

    const request = httpMock.expectOne('/api/products/100001');
    expect(request.request.method).toBe('DELETE');

    request.flush(null);
  });

  it('puts the quantity in the URL and the reason in the body when adding stock', () => {
    service.addToStock(100001, 8, 'Delivery GRN-5512').subscribe();

    const request = httpMock.expectOne('/api/products/100001/add-to-stock/8');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ reason: 'Delivery GRN-5512' });

    request.flush({ ...product, previousStock: 42, newStock: 50, productId: 100001 });
  });

  it('removes stock', () => {
    service.decrementStock(100001, 3).subscribe();

    const request = httpMock.expectOne('/api/products/100001/decrement-stock/3');
    expect(request.request.method).toBe('POST');

    request.flush({ ...product, previousStock: 42, newStock: 39, productId: 100001 });
  });

  it('sends the search term', () => {
    service.searchByName('lumen').subscribe();

    const request = httpMock.expectOne((candidate) => candidate.url === '/api/products/search');

    expect(request.request.params.get('name')).toBe('lumen');
    request.flush([]);
  });

  it('leaves out a missing stock-level bound', () => {
    service.getByStockLevel(10, undefined).subscribe();

    const request = httpMock.expectOne(
      (candidate) => candidate.url === '/api/products/stock-level',
    );

    expect(request.request.params.get('min')).toBe('10');
    expect(request.request.params.has('max')).toBe(false);

    request.flush([]);
  });
});
