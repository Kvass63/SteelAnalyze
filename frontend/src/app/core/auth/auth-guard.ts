import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth-service';

// Пускает на страницу только после входа. Иначе — на /login, запомнив, куда хотели попасть.
// Возврат true — «открыть страницу», UrlTree — «перейти на другой адрес вместо неё».
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  if (auth.isAuthenticated()) {
    return true;
  }
  return inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};
