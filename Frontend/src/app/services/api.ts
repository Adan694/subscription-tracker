import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  private getHeaders(): HttpHeaders {
    const token = sessionStorage.getItem('token');
    console.log('API Service - Getting token from sessionStorage:', token);
    
    const headers = new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': token ? `Bearer ${token}` : ''
    });
    
    return headers;
  }

  register(email: string, password: string, phoneNumber?: string) {
    return this.http.post<any>(`${this.apiUrl}/Auth/register`, { 
      email, 
      password, 
      phoneNumber 
    });
  }

  login(email: string, password: string) {
    return this.http.post<any>(`${this.apiUrl}/Auth/login`, { email, password });
  }

  getDashboard() {
    console.log('API Service - Fetching dashboard');
    return this.http.get<any>(`${this.apiUrl}/subscriptions`, {
      headers: this.getHeaders()
    });
  }

  addSubscription(data: any) {
    return this.http.post<any>(`${this.apiUrl}/Subscriptions`, data, {
      headers: this.getHeaders()
    });
  }

  deleteSubscription(id: string) {
    return this.http.delete<any>(`${this.apiUrl}/Subscriptions/${id}`, {
      headers: this.getHeaders()
    });
  }
}