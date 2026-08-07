import { Routes } from '@angular/router';

/**
 * Pages are lazy loaded.
 * 'products/new' has to come before 'products/:id', or ":id" would match the word "new".
 */
export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'products',
  },
  {
    path: 'products',
    title: 'Products',
    loadComponent: () =>
      import('./features/products/pages/product-list/product-list').then((m) => m.ProductList),
  },
  {
    path: 'products/new',
    title: 'New product',
    loadComponent: () =>
      import('./features/products/pages/product-form/product-form').then((m) => m.ProductForm),
  },
  {
    path: 'products/:id/edit',
    title: 'Edit product',
    loadComponent: () =>
      import('./features/products/pages/product-form/product-form').then((m) => m.ProductForm),
  },
  {
    path: 'products/:id',
    title: 'Product',
    loadComponent: () =>
      import('./features/products/pages/product-detail/product-detail').then(
        (m) => m.ProductDetail,
      ),
  },
  {
    path: '**',
    redirectTo: 'products',
  },
];
