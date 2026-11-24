import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { Auth } from '../../services/auth';

@Component({
  imports: [FormsModule, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  selector: 'app-login',
  styleUrl: './login.scss',
  templateUrl: './login.html',
})
export class Login {
  private auth = inject(Auth);
  private router = inject(Router);
  
  username = '';
  password = '';
  error = signal('');
  loading = signal(false);

  submit() {
    this.error.set('');
    this.loading.set(true);

    this.auth.login(this.username, this.password).subscribe({
      next: (user) => {
        this.auth.saveUser(user);
        this.router.navigateByUrl('/inspect');
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.status === 401 ? 'Неверный логин или пароль' : 'Сервер недоступен');
      }
    })
  }
}
