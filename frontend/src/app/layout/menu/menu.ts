import { Component } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { RouterLink, RouterLinkActive } from '@angular/router';

// Один пункт меню: адрес, подпись и иконка.
interface MenuItem {
  link: string;
  label: string;
  icon: string;
}

@Component({
  imports: [MatListModule, MatIconModule, RouterLink, RouterLinkActive],
  selector: 'app-menu',
  styleUrl: './menu.scss',
  templateUrl: './menu.html',
})
export class Menu {
  // Все разделы открыты обеим ролям; действия только для Admin
  // (создать, изменить, удалить) прячутся на самих страницах.
  // Названия иконок — из набора Material Symbols (https://fonts.google.com/icons).
  protected readonly items: MenuItem[] = [
    { link: '/inspect', label: 'Проверить снимок', icon: 'image_search' },
    { link: '/inspections', label: 'Журнал', icon: 'history' },
    { link: '/classes', label: 'Классы дефектов', icon: 'category' },
    { link: '/references', label: 'Эталоны', icon: 'photo_library' },
    { link: '/reports', label: 'Отчёты', icon: 'analytics' },
  ];
}
