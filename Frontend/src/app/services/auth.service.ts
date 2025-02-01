import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root',
})
export class AuthService {
    private tokenKey = 'authToken';

    public isLoggedIn(): boolean {
        return !!this.getToken();
    }

    public setToken(token: string) {
        localStorage.setItem(this.tokenKey, token);
    }

    public getToken(): string | null {
        return localStorage.getItem(this.tokenKey);
    }

    public clearToken(): void {
        localStorage.removeItem(this.tokenKey);
    }
}
