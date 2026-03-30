import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(private router: Router) {}

  canActivate(): boolean {
    const token = sessionStorage.getItem('token');
    console.log('AuthGuard - Token from sessionStorage:', token);
    
    if (token && token !== 'null' && token !== 'undefined') {
      console.log('AuthGuard - Token valid, allowing access');
      return true;
    }
    
    console.log('AuthGuard - No valid token, redirecting to login');
    this.router.navigate(['/login']);
    return false;
  }
}