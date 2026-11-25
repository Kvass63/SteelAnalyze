import { Component, OnInit, inject, signal } from '@angular/core';
import { Api, DefectClass  } from '../../services/api'

@Component({
  imports: [],
  selector: 'app-classes',
  styleUrl: './classes.scss',
  templateUrl: './classes.html',
})
export class Classes implements OnInit {
  private api = inject(Api);

  classes = signal<DefectClass[]>([]);
  error = signal('');

  ngOnInit() {
    this.api.getClasses().subscribe({
      next: (list) => this.classes.set(list),
      error: (err) => this.error.set(this.api.handleError(err))
    })
  }
}
