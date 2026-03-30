import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ApiService } from '../services/api';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, CommonModule, RouterLink],
  template: `
    <div class="container">
      <div class="card">
        <h1>📧 Create Account</h1>
        
        <div class="form-group">
          <label>Email</label>
          <input type="email" [(ngModel)]="email" placeholder="you@example.com">
        </div>
        
        <div class="form-group">
          <label>Password</label>
          <input type="password" [(ngModel)]="password" placeholder="********">
        </div>
        
        <button (click)="register()" [disabled]="loading">
          {{ loading ? 'Creating...' : 'Register' }}
        </button>
        
        <p class="error" *ngIf="errorMessage">{{ errorMessage }}</p>
        <p class="success" *ngIf="successMessage">{{ successMessage }}</p>
        
        <p class="link">Already have an account? <a routerLink="/login">Login</a></p>
      </div>
    </div>
  `,
  styles: [`
    .container {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    }
    .card {
      background: white;
      padding: 40px;
      border-radius: 16px;
      width: 100%;
      max-width: 400px;
      box-shadow: 0 10px 40px rgba(0,0,0,0.2);
    }
    h1 {
      margin: 0 0 30px 0;
      text-align: center;
      color: #333;
    }
    .form-group {
      margin-bottom: 20px;
    }
    label {
      display: block;
      margin-bottom: 8px;
      color: #666;
      font-weight: 500;
    }
    input {
      width: 100%;
      padding: 12px;
      border: 1px solid #ddd;
      border-radius: 8px;
      font-size: 16px;
      box-sizing: border-box;
    }
    button {
      width: 100%;
      padding: 12px;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 16px;
      font-weight: 600;
      cursor: pointer;
    }
    button:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }
    .error {
      color: #e74c3c;
      text-align: center;
      margin-top: 15px;
    }
    .success {
      color: #27ae60;
      text-align: center;
      margin-top: 15px;
    }
    .link {
      text-align: center;
      margin-top: 20px;
      color: #666;
    }
    .link a {
      color: #667eea;
      text-decoration: none;
    }
  `]
})
export class Register {
  email = '';
  password = '';
  loading = false;
  errorMessage = '';
  successMessage = '';

  constructor(private api: ApiService, private router: Router) {}

  register() {
    if (!this.email || !this.password) {
      this.errorMessage = 'Please fill all fields';
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.api.register(this.email, this.password).subscribe({
      next: (response: any) => {
        this.successMessage = response.message;
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 2000);
      },
      error: (err: any) => {
        this.errorMessage = err.error?.error || 'Registration failed';
        this.loading = false;
      }
    });
  }
}