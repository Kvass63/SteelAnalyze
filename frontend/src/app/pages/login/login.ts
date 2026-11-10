import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, input, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth-service';
import { Logo } from '../../layout/logo/logo';

@Component({
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    Logo,
  ],
  selector: 'app-login',
  styleUrl: './login.scss',
  templateUrl: './login.html',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  // Адрес из ?returnUrl=... — куда вернуться после входа (его подставляет authGuard).
  readonly returnUrl = input<string>();

  // Форма: два поля, оба обязательные. nonNullable — значение всегда строка, а не null.
  protected readonly form = new FormGroup({
    username: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly hidePassword = signal(true);

  protected submit(): void {
    if (this.form.invalid || this.loading()) {
      // Подсвечиваем незаполненные поля, если форму отправили клавишей Enter.
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    const { username, password } = this.form.getRawValue();

    // subscribe запускает запрос; next — успех, error — ответ с ошибкой или нет связи.
    this.auth.login(username.trim(), password).subscribe({
      next: () => this.router.navigateByUrl(this.targetUrl()),
      error: (response: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(describeLoginError(response));
      },
    });
  }

  // Возвращаем только на адреса внутри приложения («/…»), иначе — на главный экран.
  private targetUrl(): string {
    const url = this.returnUrl();
    return url && url.startsWith('/') && !url.startsWith('//') ? url : '/inspect';
  }
}

function describeLoginError(response: HttpErrorResponse): string {
  if (response.status === 401) {
    return 'Неверный логин или пароль';
  }
  // 0 — браузер не достучался до сервера; 5xx — сервер упал или прокси не нашёл API.
  if (response.status === 0 || response.status >= 500) {
    return 'Сервер недоступен. Проверьте, что API запущен.';
  }
  return `Не удалось войти (код ${response.status})`;
}
