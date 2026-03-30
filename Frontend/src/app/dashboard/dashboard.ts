import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [FormsModule, CommonModule],
  template: `
    <div class="dashboard">
      <div class="header">
        <h1>💰 My Subscriptions</h1>
        <button class="logout" (click)="logout()">Logout</button>
      </div>

      <div class="stats-card">
        <h3>Monthly Total</h3>
        <div class="total-amount">\${{ totalMonthly.toFixed(2) }}</div>
        <p>{{ subscriptions.length }} active subscription(s)</p>
      </div>

      <button class="add-btn" (click)="showAddForm = !showAddForm">
        {{ showAddForm ? 'Cancel' : '+ Add Subscription' }}
      </button>

      <div class="add-form" *ngIf="showAddForm">
        <h3>Add New Subscription</h3>
        <input [(ngModel)]="newName" placeholder="Service name">
        <input [(ngModel)]="newAmount" type="number" placeholder="Amount">
        <select [(ngModel)]="newFrequency">
          <option value="monthly">Monthly</option>
          <option value="yearly">Yearly</option>
        </select>
        <input [(ngModel)]="newNextDate" type="date">
        <button (click)="addSubscription()">Save</button>
      </div>

      <div class="subscription-list">
        <div class="card" *ngFor="let sub of subscriptions">
          <div><strong>{{ sub.name }}</strong></div>
          <div>\${{ sub.amount }}/{{ sub.frequency }}</div>
          <div>Next: {{ sub.nextChargeDate | date }}</div>
          <button (click)="deleteSubscription(sub.id)">Remove</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dashboard { max-width: 800px; margin: 0 auto; padding: 20px; }
    .header { display: flex; justify-content: space-between; margin-bottom: 20px; }
    .stats-card { background: linear-gradient(135deg, #667eea, #764ba2); color: white; padding: 30px; border-radius: 16px; text-align: center; margin-bottom: 20px; }
    .total-amount { font-size: 48px; font-weight: bold; }
    .add-btn { width: 100%; padding: 12px; background: #10b981; color: white; border: none; border-radius: 8px; margin-bottom: 20px; cursor: pointer; }
    .add-form { background: #f3f4f6; padding: 20px; border-radius: 8px; margin-bottom: 20px; }
    .add-form input, .add-form select { width: 100%; padding: 8px; margin-bottom: 10px; border: 1px solid #ddd; border-radius: 4px; }
    .card { background: white; padding: 15px; border-radius: 8px; margin-bottom: 10px; display: flex; justify-content: space-between; align-items: center; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
    .logout { background: #ef4444; color: white; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer; }
    button { cursor: pointer; }
  `]
})
export class Dashboard implements OnInit {
  subscriptions: any[] = [];
  totalMonthly = 0;
  showAddForm = false;
  
  newName = '';
  newAmount = 0;
  newFrequency = 'monthly';
  newNextDate = '';

  constructor(private http: HttpClient, private router: Router) {}

  ngOnInit() {
    const token = sessionStorage.getItem('token');
    if (!token) {
      this.router.navigate(['/login']);
      return;
    }
    this.loadSubscriptions();
  }

  private getHeaders() {
    const token = sessionStorage.getItem('token');
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    });
  }

  loadSubscriptions() {
    this.http.get('http://localhost:5114/api/subscriptions', {
      headers: this.getHeaders()
    }).subscribe({
      next: (res: any) => {
        this.subscriptions = res.subscriptions || [];
        this.totalMonthly = res.totalMonthlySpend || 0;
      },
      error: (err) => {
        console.error('Load error:', err);
        if (err.status === 401) {
          sessionStorage.clear();
          this.router.navigate(['/login']);
        }
      }
    });
  }

  addSubscription() {
    const data = {
      name: this.newName,
      amount: this.newAmount,
      frequency: this.newFrequency,
      nextChargeDate: this.newNextDate ? new Date(this.newNextDate).toISOString() : null
    };

    this.http.post('http://localhost:5114/api/subscriptions', data, {
      headers: this.getHeaders()
    }).subscribe({
      next: () => {
        this.showAddForm = false;
        this.newName = '';
        this.newAmount = 0;
        this.newNextDate = '';
        this.loadSubscriptions();
      },
      error: (err) => alert('Failed to add: ' + err.message)
    });
  }

  deleteSubscription(id: string) {
    if (confirm('Remove?')) {
      this.http.delete(`http://localhost:5114/api/subscriptions/${id}`, {
        headers: this.getHeaders()
      }).subscribe({
        next: () => this.loadSubscriptions(),
        error: (err) => alert('Delete failed')
      });
    }
  }

  logout() {
    sessionStorage.clear();
    this.router.navigate(['/login']);
  }
}