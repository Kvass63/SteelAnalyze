import { Component, OnInit, inject, signal } from '@angular/core';
import { MatSortModule, Sort } from '@angular/material/sort';
import { Api, DefectClass  } from '../../services/api'

@Component({
  imports: [MatSortModule],
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
    });
  }

  onSort(sort: Sort) {
    const list = [...this.classes()]
    if (sort.active === 'name') {
      list.sort((a, b) => a.name.localeCompare(b.name))
    }

    if (sort.active === 'code') {
      list.sort((a, b) => a.code.localeCompare(b.code));
    }

    if (sort.active === 'severityLevel') {
      list.sort((a, b) => a.severityLevel - b.severityLevel);
    }

    if (sort.direction === 'desc') {
      list.reverse();
    }

    this.classes.set(list);
  }
}
