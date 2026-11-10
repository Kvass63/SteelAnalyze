import { Component, computed, input } from '@angular/core';

@Component({
  imports: [],
  selector: 'app-class-form',
  styleUrl: './class-form.scss',
  templateUrl: './class-form.html',
})
export class ClassForm {
  // На адресе /classes/:id/edit здесь будет id класса, на /classes/new — undefined.
  readonly id = input<string>();
  protected readonly isEdit = computed(() => this.id() !== undefined);
}
