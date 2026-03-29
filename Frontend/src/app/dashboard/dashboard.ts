import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Api } from '../services/api';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [FormsModule, CommonModule],
  template: `
    <div class="dashboard">
      <div class="header">
        <h1>💰 My Subscriptions</h1>
        <div class="user-info">
          <span>{{ userEmail }}</span>
          <button class="logout" (click)="logout()">Logout</button>
        </div>
      </div>

      <div class="stats-card">
        <h3>Monthly Total</h3>
        <div class="total-amount">\${{ monthlyTotal.toFixed(2) }}</div>
        <p>{{ subscriptions.length }} active subscription(s)</p>
      </div>

      <button class="add-btn" (click)="showAddForm = !showAddForm">
        {{ showAddForm ? 'Cancel' : '+ Add Subscription' }}
      </button>

      <div class="add-form" *ngIf="showAddForm">
        <h3>Add New Subscription</h3>
        <input [(ngModel)]="newSubscription.name" placeholder="Service name (e.g., Netflix)">
        <input [(ngModel)]="newSubscription.amount" type="number" placeholder="Amount (e.g., 15.99)">
        <select [(ngModel)]="newSubscription.frequency">
          <option value="monthly">Monthly</option>
          <option value="yearly">Yearly</option>
          <option value="weekly">Weekly</option>
        </select>
        <input [(ngModel)]="newSubscription.nextChargeDate" type="date">
        <input [(ngModel)]="newSubscription.cancellationLink" placeholder="Cancel link (optional)">
        <button (click)="addSubscription()">Save Subscription</button>
      </div>

      <div class="subscription-list">
        <div class="subscription-card" *ngFor="let sub of subscriptions">
          <div class="service-icon">{{ sub.name.charAt(0) }}</div>
          <div class="details">
            <h3>{{ sub.name }}</h3>
            <div class="info">
              <span class="amount">\${{ sub.amount }}/{{ sub.frequency }}</span>
              <span class="next-date">Next: {{ sub.nextChargeDate | date:'MMM d, yyyy' }}</span>
            </div>
          </div>
          <div class="actions">
            <a *ngIf="sub.cancellationLink" [href]="sub.cancellationLink" target="_blank" class="cancel-link">Cancel</a>
            <button class="delete-btn" (click)="deleteSubscription(sub.id)">Remove</button>
          </div>
        </div>

        <div class="empty-state" *ngIf="subscriptions.length === 0">
          <p>No subscriptions yet.</p>
          <small>Click "Add Subscription" to get started</small>
        </div>
      </div>

      <div class="upcoming-alert" *ngIf="upcomingSubscriptions.length > 0">
        <h3>⚠️ Upcoming Charges (Next 7 Days)</h3>
        <div class="upcoming-item" *ngFor="let sub of upcomingSubscriptions">
          {{ sub.name }} - \${{ sub.amount }} on {{ sub.nextChargeDate | date:'MMM d' }}
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dashboard {
      max-width: 900px;
      margin: 0 auto;
      padding: 20px;
      font-family: system-ui, -apple-system, sans-serif;
    }
    .header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 30px;
      padding-bottom: 20px;
      border-bottom: 2px solid #f0f0f0;
    }
    .user-info {
      display: flex;
      align-items: center;
      gap: 15px;
    }
    .logout {
      background: #ef4444;
      color: white;
      border: none;
      padding: 8px 16px;
      border-radius: 8px;
      cursor: pointer;
    }
    .stats-card {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      padding: 30px;
      border-radius: 16px;
      text-align: center;
      margin-bottom: 30px;
    }
    .stats-card h3 {
      margin: 0 0 10px 0;
      font-size: 18px;
      opacity: 0.9;
    }
    .total-amount {
      font-size: 48px;
      font-weight: bold;
      margin: 10px 0;
    }
    .add-btn {
      width: 100%;
      padding: 14px;
      background: #10b981;
      color: white;
      border: none;
      border-radius: 12px;
      font-size: 16px;
      font-weight: 600;
      cursor: pointer;
      margin-bottom: 20px;
    }
    .add-form {
      background: #f3f4f6;
      padding: 20px;
      border-radius: 12px;
      margin-bottom: 20px;
    }
    .add-form h3 {
      margin: 0 0 15px 0;
    }
    .add-form input, .add-form select {
      width: 100%;
      padding: 10px;
      margin-bottom: 10px;
      border: 1px solid #ddd;
      border-radius: 8px;
      box-sizing: border-box;
    }
    .add-form button {
      width: 100%;
      padding: 10px;
      background: #3b82f6;
      color: white;
      border: none;
      border-radius: 8px;
      cursor: pointer;
    }
    .subscription-card {
      background: white;
      border-radius: 12px;
      padding: 16px;
      margin-bottom: 12px;
      display: flex;
      align-items: center;
      gap: 16px;
      box-shadow: 0 2px 8px rgba(0,0,0,0.1);
    }
    .service-icon {
      width: 50px;
      height: 50px;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      border-radius: 12px;
      display: flex;
      align-items: center;
      justify-content: center;
      color: white;
      font-weight: bold;
      font-size: 20px;
    }
    .details {
      flex: 1;
    }
    .details h3 {
      margin: 0 0 8px 0;
      font-size: 18px;
    }
    .info {
      display: flex;
      gap: 20px;
      font-size: 14px;
      color: #666;
    }
    .actions {
      display: flex;
      gap: 10px;
    }
    .cancel-link {
      background: #3b82f6;
      color: white;
      padding: 6px 12px;
      border-radius: 6px;
      text-decoration: none;
      font-size: 14px;
    }
    .delete-btn {
      background: #ef4444;
      color: white;
      border: none;
      padding: 6px 12px;
      border-radius: 6px;
      cursor: pointer;
    }
    .empty-state {
      text-align: center;
      padding: 60px;
      background: #f9fafb;
      border-radius: 12px;
      color: #666;
    }
    .upcoming-alert {
      margin-top: 30px;
      background: #fef3c7;
      padding: 20px;
      border-radius: 12px;
    }
    .upcoming-alert h3 {
      margin: 0 0 15px 0;
      color: #92400e;
    }
    .upcoming-item {
      padding: 8px 0;
      border-bottom: 1px solid #fde68a;
      color: #92400e;
    }
    @media (max-width: 768px) {
      .subscription-card {
        flex-direction: column;
        text-align: center;
      }
      .actions {
        width: 100%;
        justify-content: center;
      }
    }
  `]
})
export class Dashboard implements OnInit {
  subscriptions: any[] = [];
  monthlyTotal = 0;
  upcomingSubscriptions: any[] = [];
  userEmail = '';
  showAddForm = false;
  
  newSubscription = {
    name: '',
    amount: 0,
    frequency: 'monthly',
    nextChargeDate: '',
    cancellationLink: ''
  };

  constructor(private api: Api, private router: Router) {}

  ngOnInit() {
    const userId = localStorage.getItem('userId');
    if (!userId) {
      this.router.navigate(['/login']);
      return;
    }
    
    this.userEmail = localStorage.getItem('userEmail') || '';
    this.loadSubscriptions();
  }

  loadSubscriptions() {
    const userId = localStorage.getItem('userId');
    if (!userId) return;

    this.api.getSubscriptions(userId).subscribe({
      next: (res) => {
        this.subscriptions = res.subscriptions;
        this.monthlyTotal = res.monthlyTotal;
        this.findUpcomingCharges();
      },
      error: (err) => {
        console.error('Failed to load subscriptions', err);
        alert('Failed to load subscriptions. Make sure backend is running.');
      }
    });
  }

  findUpcomingCharges() {
    const today = new Date();
    const nextWeek = new Date();
    nextWeek.setDate(today.getDate() + 7);

    this.upcomingSubscriptions = this.subscriptions.filter(sub => {
      const chargeDate = new Date(sub.nextChargeDate);
      return chargeDate >= today && chargeDate <= nextWeek;
    });
  }

  addSubscription() {
    const userId = localStorage.getItem('userId');
    if (!userId) {
      alert('Please login again');
      return;
    }

    if (!this.newSubscription.name || this.newSubscription.amount <= 0) {
      alert('Please fill service name and amount');
      return;
    }

    // Prepare data exactly as backend expects
    const data = {
      userId: userId,
      name: this.newSubscription.name,
      amount: this.newSubscription.amount,
      frequency: this.newSubscription.frequency,
      nextChargeDate: this.newSubscription.nextChargeDate ? new Date(this.newSubscription.nextChargeDate).toISOString() : null,
      cancellationLink: this.newSubscription.cancellationLink || null
    };

    console.log('Sending data:', data); // For debugging

    this.api.addSubscription(data).subscribe({
      next: (response) => {
        console.log('Success:', response);
        this.showAddForm = false;
        this.newSubscription = {
          name: '',
          amount: 0,
          frequency: 'monthly',
          nextChargeDate: '',
          cancellationLink: ''
        };
        this.loadSubscriptions();
      },
      error: (err) => {
        console.error('Error details:', err);
        let errorMsg = 'Failed to add subscription. ';
        if (err.error?.error) {
          errorMsg += err.error.error;
        } else if (err.message) {
          errorMsg += err.message;
        }
        alert(errorMsg);
      }
    });
  }

  deleteSubscription(id: string) {
    if (confirm('Remove this subscription?')) {
      this.api.deleteSubscription(id).subscribe({
        next: () => {
          this.loadSubscriptions();
        },
        error: (err) => {
          console.error('Delete error:', err);
          alert('Failed to delete subscription');
        }
      });
    }
  }

  logout() {
    localStorage.clear();
    this.router.navigate(['/login']);
  }
}