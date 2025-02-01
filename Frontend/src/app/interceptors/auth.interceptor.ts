import { HttpHandlerFn, HttpRequest } from '@angular/common/http';
import { AuthService } from '../services/auth.service';
import { inject } from '@angular/core';

export function authInterceptor(req: HttpRequest<unknown>, next: HttpHandlerFn) {
    // Inject the current `AuthService` and use it to get an authentication token:
    const authToken = inject(AuthService).getToken();

    if (!authToken) {
        return next(req);
    }

    // Clone the request to add the authentication header.
    console.log(`Bearer ${authToken.toString()}`);
    const newReq = req.clone({ setHeaders: { Authorization: `Bearer ${authToken.toString()}` } });
    return next(newReq);
}
