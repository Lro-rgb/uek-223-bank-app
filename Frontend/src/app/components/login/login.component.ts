import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
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
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [CommonModule, FormsModule],
})
export class LoginComponent {
    public username = '';
    public password = '';
    // Signal, damit OnPush die Meldung nach der HTTP-Antwort neu zeichnet. Als normales Feld blieb sie unsichtbar (E2E-03).
    public errorMessage = signal('');

    constructor(
        private http: HttpClient,
        private authService: AuthService,
        private router: Router,
    ) {}

    public onSubmit() {
        const loginData = { username: this.username, password: this.password };

        this.http.post<Token>('http://localhost:5000/api/v1/Login', loginData).subscribe({
            next: (response: Token) => {
                console.log('Login successful', response);
                this.authService.setToken(response.token);
                void this.router.navigate(['/ledgers']);
            },
            error: (err) => {
                console.error('Login failed', err);
                this.errorMessage.set('Benutzername oder Passwort falsch.');
            },
        });
    }
}
