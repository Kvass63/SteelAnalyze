import { HttpClient } from '@angular/common/http';
import { computed, inject, Service, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';

// Роли — те же строки, что в Auth/Roles.cs на сервере.
export type Role = 'Admin' | 'User';

export const ROLE_LABELS: Record<Role, string> = {
  Admin: 'Администратор',
  User: 'Оператор',
};

// Ответ POST /api/auth/login (LoginResponse на сервере).
export interface LoginResponse {
  token: string;
  username: string;
  role: Role;
}

// То, что храним в браузере: ответ сервера + момент, когда токен перестанет действовать.
interface Session extends LoginResponse {
  expiresAt: number;
}

const STORAGE_KEY = 'steelanalyze.session';

// Вход и выход, хранение токена. @Service() — один экземпляр на всё приложение,
// компоненты получают его через inject(AuthService).
@Service()
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  // Текущий сеанс или null, если вход не выполнен. При запуске читаем его из localStorage,
  // поэтому после перезагрузки страницы входить заново не нужно.
  private readonly session = signal<Session | null>(readStoredSession());

  // computed — значения, которые пересчитываются сами, когда меняется session.
  readonly isLoggedIn = computed(() => this.session() !== null);
  readonly username = computed(() => this.session()?.username ?? '');
  readonly role = computed(() => this.session()?.role ?? null);
  readonly isAdmin = computed(() => this.role() === 'Admin');

  // Токен для заголовка Authorization. Если срок вышел — забываем сеанс и возвращаем null.
  token(): string | null {
    const current = this.session();
    if (current === null) {
      return null;
    }
    if (Date.now() >= current.expiresAt) {
      this.clear();
      return null;
    }
    return current.token;
  }

  isAuthenticated(): boolean {
    return this.token() !== null;
  }

  // Observable — «отложенный» результат запроса: запрос уходит, когда на него подпишутся
  // (subscribe на странице входа). tap сохраняет сеанс, если сервер ответил успехом.
  login(username: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>('/api/auth/login', { username, password })
      .pipe(tap((response) => this.save({ ...response, expiresAt: readExpiry(response.token) })));
  }

  // returnUrl — куда вернуть пользователя после повторного входа.
  logout(returnUrl?: string): void {
    this.clear();
    const queryParams = returnUrl && returnUrl !== '/' ? { returnUrl } : {};
    this.router.navigate(['/login'], { queryParams });
  }

  private save(session: Session): void {
    this.session.set(session);
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    } catch {
      // Хранилище браузера недоступно (например, запрещено настройками) — сеанс живёт до перезагрузки.
    }
  }

  private clear(): void {
    this.session.set(null);
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
      // См. save().
    }
  }
}

function readStoredSession(): Session | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw === null) {
      return null;
    }
    const session = JSON.parse(raw) as Session;
    return Date.now() < session.expiresAt ? session : null;
  } catch {
    return null;
  }
}

// JWT состоит из трёх частей через точку; во второй (payload) лежит JSON с полем exp —
// временем окончания в секундах от 1970 года. Подпись проверяет сервер, нам нужен только срок.
function readExpiry(token: string): number {
  try {
    const payload = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    const { exp } = JSON.parse(atob(payload)) as { exp?: number };
    return typeof exp === 'number' ? exp * 1000 : Number.MAX_SAFE_INTEGER;
  } catch {
    // Срок не прочитался — полагаемся на сервер: он ответит 401, и перехватчик выполнит выход.
    return Number.MAX_SAFE_INTEGER;
  }
}
