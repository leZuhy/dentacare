import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject, Injector } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { catchError, throwError } from 'rxjs';
import { ToasterService } from '../services/toaster.service';

/**
 * HTTP interceptor that logs errors.
 *
 * IMPORTANT: This interceptor shows a toaster ONLY for 403 (no permission) – that message is
 * the same everywhere, so components do not need to handle it. All other toaster/form
 * messages should be handled in individual components where you can provide
 * context-specific error messages (see getErrorMessage below and core/forms/server-errors.ts).
 *
 * Features:
 * - Logs HTTP errors to console (except 401 – see below)
 * - 401 is handled by authInterceptor (token refresh, or logout + redirect to /auth/login);
 *   the login form shows its own 401 message ("Pogrešan email ili lozinka.")
 * - 403 → translated toaster "You do not have permission for this action."
 *   (checked by status – a 403 produced by the ASP.NET policy may have an empty body)
 * - Can be extended to send errors to logging service (Sentry, etc.)
 * - Re-throws errors so components can handle them
 */
export const errorLoggingInterceptor: HttpInterceptorFn = (req, next) => {
  // ToasterService/TranslateService uzimamo tek kad zatrebaju, ne odmah:
  // TranslateService i sam koristi HttpClient (učitava /i18n/*.json), pa bi ga rani
  // inject() u interceptoru mogao uvesti u kružnu zavisnost (NG0200).
  const injector = inject(Injector);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }

      if (error.status !== 401) {
        // Log error to console
        // In production, send to logging service like Sentry
        console.error('HTTP Error:', {
          url: req.url,
          method: req.method,
          status: error.status,
          statusText: error.statusText,
          message: error.message,
          error: error.error,
          timestamp: new Date().toISOString()
        });
      }

      if (error.status === 403) {
        const translate = injector.get(TranslateService);
        injector.get(ToasterService).error(translate.instant('ERRORS.FORBIDDEN'));
      }

      // Optional: Send to external logging service
      // logToSentry(error, req);

      // Re-throw error so components can handle it
      // Components should show appropriate toaster messages with context
      return throwError(() => error);
    })
  );
};

/**
 * Optional: Send errors to external logging service
 * Uncomment and implement when using Sentry, LogRocket, etc.
 */
// function logToSentry(error: HttpErrorResponse, req: HttpRequest<any>): void {
//   if (environment.production) {
//     Sentry.captureException(error, {
//       extra: {
//         url: req.url,
//         method: req.method,
//         status: error.status,
//         body: req.body
//       }
//     });
//   }
// }

/**
 * Helper: Get user-friendly error message
 * Can be used by components to extract error messages
 */
export function getErrorMessage(error: HttpErrorResponse): string {
  // Check if error has a custom message from backend (ErrorDto.message)
  if (typeof error.error?.message === 'string' && error.error.message) {
    return error.error.message;
  }

  // Check for validation errors
  const errors = error.error?.errors;
  if (Array.isArray(errors)) {
    // ErrorDto.errors: [{ field, message }]
    const messages = errors
      .map((e: { message?: string }) => e?.message)
      .filter((m): m is string => !!m);
    if (messages.length > 0) {
      return messages.join(' ');
    }
  } else if (errors && typeof errors === 'object') {
    // ProblemDetails: { field: string[] }
    const messages = Object.values(errors).flat();
    if (messages.length > 0) {
      return messages.join(', ');
    }
  }

  // Check for title (ProblemDetails format)
  if (error.error?.title) {
    return error.error.title;
  }

  // Fallback to generic messages based on status code
  switch (error.status) {
    case 0:
      return 'Unable to connect to server. Please check your internet connection.';
    case 400:
      return 'Invalid request. Please check your input.';
    case 401:
      return 'Unauthorized. Please log in again.';
    case 403:
      return 'You do not have permission to perform this action.';
    case 404:
      return 'The requested resource was not found.';
    case 409:
      return 'Conflict. The operation cannot be completed.';
    case 500:
      return 'Server error. Please try again later.';
    case 503:
      return 'Service temporarily unavailable. Please try again later.';
    default:
      return `An error occurred: ${error.statusText || 'Unknown error'}`;
  }
}
