import { HttpErrorResponse } from '@angular/common/http';

import { describeHttpError } from './problem-details';

describe('describeHttpError', () => {
  it('uses the field errors when there are any', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        title: 'One or more validation errors occurred',
        status: 400,
        errors: {
          Name: ['Name is required.'],
          Price: ['Price cannot be negative.'],
        },
      },
    });

    const message = describeHttpError(error);

    expect(message).toContain('Name is required.');
    expect(message).toContain('Price cannot be negative.');
  });

  it('uses the detail when there are no field errors', () => {
    const error = new HttpErrorResponse({
      status: 409,
      error: {
        title: 'Insufficient stock',
        status: 409,
        detail: 'Product 100000 has 3 unit(s) in stock; cannot remove 10.',
      },
    });

    expect(describeHttpError(error)).toBe(
      'Product 100000 has 3 unit(s) in stock; cannot remove 10.',
    );
  });

  it('uses the title when there is no detail', () => {
    const error = new HttpErrorResponse({
      status: 404,
      error: { title: 'Resource not found', status: 404 },
    });

    expect(describeHttpError(error)).toBe('Resource not found');
  });

  it('explains a network failure', () => {
    const error = new HttpErrorResponse({ status: 0, error: new ProgressEvent('error') });

    expect(describeHttpError(error)).toContain('Could not reach the API');
  });

  it('falls back to a message based on the status code', () => {
    const error = new HttpErrorResponse({ status: 404, error: 'not json' });

    expect(describeHttpError(error)).toContain('no longer exists');
  });
});
