import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Footer } from '../footer/footer';
import { Header } from '../header/header';
import { Menu } from '../menu/menu';

// Оболочка всех страниц после входа: шапка сверху, меню слева,
// текущая страница справа, подвал снизу.
@Component({
  imports: [RouterOutlet, Header, Menu, Footer],
  selector: 'app-shell',
  styleUrl: './shell.scss',
  templateUrl: './shell.html',
})
export class Shell {}
