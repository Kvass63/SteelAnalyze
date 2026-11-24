import { Routes } from '@angular/router';
import { Login } from './pages/login/login';
import { Inspect } from './pages/inspect/inspect';
import { Inspections } from './pages/inspections/inspections';
import { Classes } from './pages/classes/classes';
import { ClassForm } from './pages/class-form/class-form';
import { NotFound } from './pages/not-found/not-found';

export const routes: Routes = [
    { path: '', redirectTo: 'inspect', pathMatch: 'full' },
    { path: 'login', component: Login },
    { path: 'inspect', component: Inspect },
    { path: 'inspections', component: Inspections },
    { path: 'classes', component: Classes },
    { path: 'classes/new', component: ClassForm },
    { path: 'classes/:id/edit', component: ClassForm },
    { path: '**', component: NotFound },
];
