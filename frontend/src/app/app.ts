import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

// Корневой компонент. Сам ничего не рисует: показывает то, что выбрал роутер.
@Component({
  imports: [RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {}
