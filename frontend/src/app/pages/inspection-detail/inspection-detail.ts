import { Component, input } from '@angular/core';

@Component({
  imports: [],
  selector: 'app-inspection-detail',
  styleUrl: './inspection-detail.scss',
  templateUrl: './inspection-detail.html',
})
export class InspectionDetail {
  // Значение берётся из адреса /inspections/:id (см. withComponentInputBinding в app.config.ts).
  readonly id = input.required<string>();
}
