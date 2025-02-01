import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root',
})
export class AuthService {
    private tokenKey = 'authToken';
    private isAuthenticated: boolean | null = null;

    isLoggedIn() {
        if (this.isAuthenticated == null) {
            this.isAuthenticated = this.getToken() != null;
        }

        return this.isAuthenticated;
    }

    setToken(token: string) {
        localStorage.setItem(this.tokenKey, token);
        this.isAuthenticated = true;
    }

    getToken(): string | null {
        return localStorage.getItem(this.tokenKey);
    }

    clearToken() {
        localStorage.removeItem(this.tokenKey);
        this.isAuthenticated = false;
    }
}
