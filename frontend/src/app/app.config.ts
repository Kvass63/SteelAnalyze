import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth-interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // withComponentInputBinding: параметры адреса (например, :id или ?returnUrl=...)
    // попадают прямо во входные свойства компонента, объявленные через input().
    provideRouter(routes, withComponentInputBinding()),
    // HttpClient для запросов к API; каждый запрос проходит через authInterceptor.
    provideHttpClient(withInterceptors([authInterceptor])),
  ],
};
