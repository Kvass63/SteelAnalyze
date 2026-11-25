import { Routes } from '@angular/router';
import { Login } from './pages/login/login';
import { Inspect } from './pages/inspect/inspect';
import { Inspections } from './pages/inspections/inspections';
import { Classes } from './pages/classes/classes';
import { ClassForm } from './pages/class-form/class-form';
import { NotFound } from './pages/not-found/not-found';
import { authGuard } from './services/auth-guard';

export const routes: Routes = [
    { path: '', redirectTo: 'inspect', pathMatch: 'full' },
    { path: 'login', component: Login },
    { path: 'inspect', component: Inspect, canActivate: [authGuard] },
    { path: 'inspections', component: Inspections, canActivate: [authGuard] },
    { path: 'classes', component: Classes, canActivate: [authGuard] },
    { path: 'classes/new', component: ClassForm, canActivate: [authGuard] },
    { path: 'classes/:id/edit', component: ClassForm, canActivate: [authGuard] },
    { path: '**', component: NotFound },
];
