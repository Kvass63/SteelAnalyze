import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';

@Component({
  imports: [MatButtonModule, MatIconModule, RouterLink],
  selector: 'app-forbidden',
  styleUrl: './forbidden.scss',
  templateUrl: './forbidden.html',
})
export class Forbidden {}
