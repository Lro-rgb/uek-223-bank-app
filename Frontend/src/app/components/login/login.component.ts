import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';
import { Token } from '../../models/token.interface';

@Component({
    selector: 'app-login',
    templateUrl: './login.component.html',
    styleUrl: './login.component.css',
    imports: [CommonModule, FormsModule],
    providers: [HttpClient],
})
export class LoginComponent {
    username = '';
    password = '';
    errorMessage = '';

    constructor(
        private http: HttpClient,
        private authService: AuthService,
        private router: Router,
    ) {}

    onSubmit() {
        const loginData = { username: this.username, password: this.password };

        this.http.post<Token>('http://localhost:5000/api/v1/Login', loginData).subscribe({
            next: (response: Token) => {
                console.log('Login successful', response);
                this.authService.setToken(response.token);
                void this.router.navigate(['/ledgers']);
            },
            error: (err) => {
                console.error('Login failed', err);
                this.errorMessage = 'Invalid username or password.';
            },
        });
    }
}
