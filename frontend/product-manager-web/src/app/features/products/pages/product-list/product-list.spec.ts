import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';

import { PagedResult } from '../../../../core/models/paged-result.model';
import { Product } from '../../../../core/models/product.model';
import { ProductList } from './product-list';

/**
 * Checks that each filter calls the right endpoint.
 * The app is zoneless, so these use Vitest fake timers instead of fakeAsync.
 */
describe('ProductList', () => {
  let fixture: ComponentFixture<ProductList>;
  let httpMock: HttpTestingController;

  const emptyPage: PagedResult<Product> = {
    items: [],
    page: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
    hasPreviousPage: false,
    hasNextPage: false,
  };

  beforeEach(async () => {
    vi.useFakeTimers();

    await TestBed.configureTestingModule({
      imports: [ProductList],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(ProductList);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  /** Answers the category dropdown request and the first product load. */
  async function startUp(): Promise<void> {
    fixture.detectChanges();

    httpMock.expectOne('/api/categories').flush([]);
    httpMock.expectOne((request) => request.url === '/api/products').flush(emptyPage);

    await vi.advanceTimersByTimeAsync(0);
  }

  /** Types into the filters and waits for the debounce to pass. */
  async function setFilter(patch: Record<string, unknown>): Promise<void> {
    fixture.componentInstance['filters'].patchValue(patch);
    await vi.advanceTimersByTimeAsync(500);
  }

  it('loads the product list on startup', async () => {
    await startUp();
    httpMock.verify();
  });

  it('calls the search endpoint when a name is typed', async () => {
    await startUp();
    await setFilter({ search: 'lumen' });

    const request = httpMock.expectOne((candidate) => candidate.url === '/api/products/search');
    expect(request.request.params.get('name')).toBe('lumen');

    request.flush([]);
    httpMock.verify();
  });

  it('calls the stock-level endpoint when a stock range is entered', async () => {
    await startUp();
    await setFilter({ minStock: 0, maxStock: 5 });

    const request = httpMock.expectOne(
      (candidate) => candidate.url === '/api/products/stock-level',
    );
    expect(request.request.params.get('min')).toBe('0');
    expect(request.request.params.get('max')).toBe('5');

    request.flush([]);
    httpMock.verify();
  });

  it('calls the plain product list when only a category is chosen', async () => {
    await startUp();
    await setFilter({ categoryId: 2 });

    const request = httpMock.expectOne((candidate) => candidate.url === '/api/products');
    expect(request.request.params.get('categoryId')).toBe('2');

    request.flush(emptyPage);
    httpMock.verify();
  });

  it('goes back to the plain list when the search box is cleared', async () => {
    await startUp();

    await setFilter({ search: 'lumen' });
    httpMock.expectOne((candidate) => candidate.url === '/api/products/search').flush([]);

    await setFilter({ search: '' });
    httpMock.expectOne((candidate) => candidate.url === '/api/products').flush(emptyPage);

    httpMock.verify();
  });
});
