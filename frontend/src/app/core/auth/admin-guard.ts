import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth-service';

// Пускает только администратора, остальных отправляет на страницу «Недостаточно прав».
// Ставится вместе с authGuard: сначала проверяется вход, потом роль.
export const adminGuard: CanActivateFn = () => {
  if (inject(AuthService).isAdmin()) {
    return true;
  }
  return inject(Router).createUrlTree(['/forbidden']);
};
