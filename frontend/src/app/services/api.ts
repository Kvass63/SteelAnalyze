import { Service, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Auth } from './auth';

export interface DefectClass {
    id: number;
    code: string;
    name: string;
    description: string;
    severityLevel: number;
}

@Service()
export class Api {
    private http = inject(HttpClient);
    private auth = inject(Auth);

    getClasses() {
        return this.http.get<DefectClass[]>('/api/defect-classes', this.options());
    }

    handleError(err: HttpErrorResponse): string {
        if (err.status === 401) {
            this.auth.logout();
            return 'Сеанс истёк, войдите заново';
        }

        if (err.status === 403) {
            return 'Недостаточно прав';
        }

        return err.error?.message ?? 'Не удалось выполнить запрос (код ' + err.status + ')';
    }

    private options() {
        return { headers: { Authorization: 'Bearer ' + this.auth.user()?.token } }
    }
}
