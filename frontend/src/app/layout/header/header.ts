import { Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink } from '@angular/router';
import { AuthService, ROLE_LABELS } from '../../core/auth/auth-service';
import { Logo } from '../logo/logo';

// Шапка: логотип (ссылка на главный экран), кто вошёл и кнопка «Выйти».
@Component({
  imports: [MatToolbarModule, MatButtonModule, MatIconModule, RouterLink, Logo],
  selector: 'app-header',
  styleUrl: './header.scss',
  templateUrl: './header.html',
})
export class Header {
  protected readonly auth = inject(AuthService);

  protected readonly roleLabel = computed(() => {
    const role = this.auth.role();
    return role ? ROLE_LABELS[role] : '';
  });

  protected logout(): void {
    this.auth.logout();
  }
}
