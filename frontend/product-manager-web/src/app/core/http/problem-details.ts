import { HttpErrorResponse } from '@angular/common/http';

/** The error shape the API returns for every failure (RFC 9457 problem details). */
export interface ProblemDetails {
  readonly title?: string;
  readonly status?: number;
  readonly detail?: string;
  /** Field name to error messages, present on validation failures. */
  readonly errors?: Record<string, string[]>;
}

/**
 * Builds a message to show the user from a failed request.
 * The API returns errors in the same shape every time, so there is one place to read them.
 */
export function describeHttpError(error: HttpErrorResponse): string {
  // Status 0 means the request never reached the server.
  if (error.status === 0) {
    return 'Could not reach the API. Check that it is running on http://localhost:5186.';
  }

  // error.error can be anything: the problem document, an HTML page, or a ProgressEvent.
  const problem: ProblemDetails =
    typeof error.error === 'object' && error.error !== null ? error.error : {};

  // Field errors are the most useful, so use them first.
  const fieldErrors = problem.errors ? Object.values(problem.errors).flat() : [];
  if (fieldErrors.length > 0) {
    return fieldErrors.join(' ');
  }

  return problem.detail ?? problem.title ?? describeByStatus(error.status);
}

/** Fallback text when the response had no useful body. */
function describeByStatus(status: number): string {
  switch (status) {
    case 400:
      return 'The request was invalid.';
    case 404:
      return 'That item no longer exists.';
    case 409:
      return 'That change conflicts with the current data. Reload and try again.';
    default:
      return `Something went wrong (HTTP ${status}). Please try again.`;
  }
}
