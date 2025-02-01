import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login.component';
import { LedgerComponent } from './components/ledger/ledger.component';
import { AuthGuard } from './guards/auth.guard';

export const routes: Routes = [
    {
        path: 'ledgers',
        component: LedgerComponent,
        canActivate: [AuthGuard],
    },
    {
        path: 'login',
        component: LoginComponent,
    },
    {
        path: '',
        redirectTo: '/login',
        pathMatch: 'full',
    },
];
