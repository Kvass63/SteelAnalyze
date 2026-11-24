import { Service, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

export interface LoginResponse {
    token: string;
    username: string;
    role: string;
}

@Service()
export class Auth {
    private http = inject(HttpClient);
    private router = inject(Router);

    user = signal<LoginResponse | null>(this.loadUser());

    login(username: string, password: string) {
        return this.http.post<LoginResponse>('/api/auth/login', {username, password});
    }

    saveUser(user: LoginResponse) {
        localStorage.setItem('user', JSON.stringify(user))
        this.user.set(user);
    }

    logout() {
        localStorage.removeItem('user');
        this.user.set(null);
        this.router.navigateByUrl('/login');
    }

    isAdmin() {
        return this.user()?.role === 'Admin';
    }
    


    private loadUser(): LoginResponse | null {
        const text = localStorage.getItem('user');
        return text ? JSON.parse(text) : null
    }
}
