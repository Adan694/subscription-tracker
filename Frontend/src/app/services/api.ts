import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class Api {
  private apiUrl = 'http://localhost:5114/api';

  constructor(private http: HttpClient) { }

  register(email: string, password: string) {
    return this.http.post<any>(`${this.apiUrl}/register`, { email, password });
  }

  login(email: string, password: string) {
    return this.http.post<any>(`${this.apiUrl}/login`, { email, password });
  }

  getSubscriptions(userId: string) {
    return this.http.get<any>(`${this.apiUrl}/subscriptions/${userId}`);
  }

  addSubscription(data: any) {
    return this.http.post<any>(`${this.apiUrl}/subscriptions`, data);
  }

  deleteSubscription(subscriptionId: string) {
    return this.http.delete<any>(`${this.apiUrl}/subscriptions/${subscriptionId}`);
  }
}
