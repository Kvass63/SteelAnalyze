import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth-service';

const LOGIN_URL = '/api/auth/login';

// Через перехватчик проходит каждый HTTP-запрос приложения (подключён в app.config.ts).
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const snackBar = inject(MatSnackBar);

  const isApi = req.url.startsWith('/api/');
  const isLogin = req.url === LOGIN_URL;
  const token = isApi && !isLogin ? auth.token() : null;

  // Запросы неизменяемы, поэтому для добавления заголовка делаем копию через clone.
  const request = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(request).pipe(
    catchError((error: unknown) => {
      // Ответ 401 на входе означает «неверный пароль» — его разбирает сама страница входа.
      if (error instanceof HttpErrorResponse && isApi && !isLogin) {
        if (error.status === 401) {
          // Токен истёк или недействителен: выходим и запоминаем, где был пользователь.
          auth.logout(router.url);
          snackBar.open('Сеанс завершён, войдите снова', 'OK', { duration: 5000 });
        } else if (error.status === 403) {
          snackBar.open('Недостаточно прав', 'OK', { duration: 5000 });
        }
      }
      // Ошибку передаём дальше, чтобы страница тоже могла на неё отреагировать.
      return throwError(() => error);
    }),
  );
};
