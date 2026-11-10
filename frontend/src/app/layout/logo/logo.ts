import { Component, computed, input } from '@angular/core';

// Логотип SteelAnalyze. SVG вставлен прямо в разметку (а не через <img>),
// чтобы название брало цвет текста от родителя (fill="currentColor").
@Component({
  imports: [],
  selector: 'app-logo',
  styleUrl: './logo.scss',
  templateUrl: './logo.html',
})
export class Logo {
  // Высота в пикселях; ширина считается по пропорциям рисунка 248 × 64.
  readonly height = input(40);
  protected readonly width = computed(() => (this.height() * 248) / 64);
}
