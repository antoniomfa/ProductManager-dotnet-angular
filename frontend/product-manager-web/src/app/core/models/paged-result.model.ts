/** One page of results from the API. */
export interface PagedResult<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
  readonly totalPages: number;
  readonly hasPreviousPage: boolean;
  readonly hasNextPage: boolean;
}

/** Optional query options for the product list endpoint. */
export interface ProductQuery {
  readonly page?: number;
  readonly pageSize?: number;
  readonly sortBy?: string;
  readonly sortDescending?: boolean;
  readonly categoryId?: number;
  readonly search?: string;
}
