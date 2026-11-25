import { inject } from "@angular/core";
import { Router } from "@angular/router";
import { Auth } from "./auth";

export function authGuard() {
    const auth = inject(Auth);
    const router = inject(Router);

    if (auth.user()) {
        return true;
    }

    return router.parseUrl('/login');
}