import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ApiService } from '../services/api';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, CommonModule, RouterLink],
  template: `
    <div class="container">
      <div class="card">
        <h1>💰 Subscription Tracker</h1>
        <p class="subtitle">Track all your subscriptions in one place</p>
        
        <div class="form-group">
          <label>Email</label>
          <input type="email" [(ngModel)]="email" placeholder="you@example.com">
        </div>
        
        <div class="form-group">
          <label>Password</label>
          <input type="password" [(ngModel)]="password" placeholder="********">
        </div>
        
        <button (click)="login()" [disabled]="loading">
          {{ loading ? 'Logging in...' : 'Login' }}
        </button>
        
        <p class="error" *ngIf="errorMessage">{{ errorMessage }}</p>
        <p class="success" *ngIf="successMessage">{{ successMessage }}</p>
        
        <p class="link">Don't have an account? <a routerLink="/register">Register</a></p>
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
      margin: 0 0 10px 0;
      text-align: center;
      color: #333;
    }
    .subtitle {
      text-align: center;
      color: #666;
      margin-bottom: 30px;
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
export class Login {
  email = '';
  password = '';
  loading = false;
  errorMessage = '';
  successMessage = '';

  constructor(private api: ApiService, private router: Router) {}

  login() {
  if (!this.email || !this.password) {
    this.errorMessage = 'Please fill all fields';
    return;
  }

  this.loading = true;
  this.errorMessage = '';
  this.successMessage = '';

  console.log('Attempting login with:', { email: this.email });

  this.api.login(this.email, this.password).subscribe({
    next: (response: any) => {
      console.log('Login response received:', response);
      
      // Save token and user info
      if (response.token) {
       sessionStorage.setItem('token', response.token);
          sessionStorage.setItem('userId', response.userId);
          sessionStorage.setItem('userEmail', response.email);
        // Verify immediately
        console.log('After saving - Token:', localStorage.getItem('token'));
        console.log('After saving - UserId:', localStorage.getItem('userId'));
        console.log('After saving - UserEmail:', localStorage.getItem('userEmail'));
        
        this.successMessage = 'Login successful! Redirecting...';
        
        // Use setTimeout to ensure storage is saved
        setTimeout(() => {
          console.log('Navigating to dashboard...');
          // Use window.location for a hard navigation instead of router
          window.location.href = '/dashboard';
        }, 1000);
      } else {
        console.error('No token in response!');
        this.errorMessage = 'Invalid response from server';
        this.loading = false;
      }
    },
    error: (err: any) => {
      console.error('Login error:', err);
      this.errorMessage = err.error?.error || 'Login failed. Please check your credentials.';
      this.loading = false;
    }
  });

  }
}