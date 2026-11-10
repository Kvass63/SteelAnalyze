import { Routes } from '@angular/router';
import { adminGuard } from './core/auth/admin-guard';
import { authGuard } from './core/auth/auth-guard';
import { Shell } from './layout/shell/shell';

// Таблица «адрес в браузере → какой компонент показать».
// Маршруты проверяются сверху вниз, побеждает первый подходящий.
//
// loadComponent — ленивая загрузка: код страницы лежит в отдельном файле сборки
// и скачивается браузером только при первом переходе на эту страницу.
// Так первая загрузка сайта остаётся быстрой, сколько бы страниц ни было.
export const routes: Routes = [
  // Страница входа — без шапки и меню.
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login').then((m) => m.Login),
    title: 'Вход · SteelAnalyze',
  },

  // Все остальные страницы показываются внутри оболочки (шапка, меню, подвал).
  // Shell рисует каркас, а дочерний маршрут подставляется в его <router-outlet>.
  // canActivateChild проверяет вход перед открытием каждой страницы внутри оболочки.
  {
    path: '',
    component: Shell,
    canActivateChild: [authGuard],
    children: [
      // Пустой адрес «/» сразу переводим на главный экран оператора.
      { path: '', pathMatch: 'full', redirectTo: 'inspect' },

      {
        path: 'inspect',
        loadComponent: () => import('./pages/inspect/inspect').then((m) => m.Inspect),
        title: 'Проверить снимок · SteelAnalyze',
      },
      {
        path: 'inspections',
        loadComponent: () => import('./pages/inspections/inspections').then((m) => m.Inspections),
        title: 'Журнал проверок · SteelAnalyze',
      },
      // :id — параметр маршрута: /inspections/15 → id = '15'.
      {
        path: 'inspections/:id',
        loadComponent: () =>
          import('./pages/inspection-detail/inspection-detail').then((m) => m.InspectionDetail),
        title: 'Карточка проверки · SteelAnalyze',
      },

      {
        path: 'classes',
        loadComponent: () => import('./pages/classes/classes').then((m) => m.Classes),
        title: 'Классы дефектов · SteelAnalyze',
      },
      // Одна форма на два адреса: без id — создание, с id — редактирование.
      // canActivate: [adminGuard] — страница только для администратора.
      {
        path: 'classes/new',
        loadComponent: () => import('./pages/class-form/class-form').then((m) => m.ClassForm),
        canActivate: [adminGuard],
        title: 'Новый класс · SteelAnalyze',
      },
      {
        path: 'classes/:id/edit',
        loadComponent: () => import('./pages/class-form/class-form').then((m) => m.ClassForm),
        canActivate: [adminGuard],
        title: 'Редактирование класса · SteelAnalyze',
      },

      {
        path: 'references',
        loadComponent: () => import('./pages/references/references').then((m) => m.References),
        title: 'База эталонов · SteelAnalyze',
      },
      {
        path: 'references/new',
        loadComponent: () =>
          import('./pages/reference-new/reference-new').then((m) => m.ReferenceNew),
        canActivate: [adminGuard],
        title: 'Добавить эталон · SteelAnalyze',
      },

      {
        path: 'reports',
        loadComponent: () => import('./pages/reports/reports').then((m) => m.Reports),
        title: 'Отчёты · SteelAnalyze',
      },
      {
        path: 'forbidden',
        loadComponent: () => import('./pages/forbidden/forbidden').then((m) => m.Forbidden),
        title: 'Нет доступа · SteelAnalyze',
      },

      // ** — любой адрес, который не подошёл ни под один маршрут выше. Должен быть последним.
      {
        path: '**',
        loadComponent: () => import('./pages/not-found/not-found').then((m) => m.NotFound),
        title: 'Страница не найдена · SteelAnalyze',
      },
    ],
  },
];
