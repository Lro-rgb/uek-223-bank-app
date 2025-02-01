import { ChangeDetectionStrategy, Component, DestroyRef, OnInit } from '@angular/core';
import { LedgerService } from '../../services/ledger.service';
import { Ledger } from '../../models/ledger.interface';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BehaviorSubject } from 'rxjs';

@Component({
    selector: 'app-ledger',
    templateUrl: './ledger.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [CommonModule, FormsModule],
})
export class LedgerComponent implements OnInit {
    public ledgers$ = new BehaviorSubject<Ledger[]>([]);
    public fromLedgerId: number | null = null;
    public toLedgerId: number | null = null;
    public amount: number | null = null;
    public transferMessage = '';

    constructor(
        private ledgerService: LedgerService,
        private destroyRef: DestroyRef,
    ) {}

    ngOnInit(): void {
        this.loadLedgers();
    }

    public loadLedgers(): void {
        this.ledgerService
            .getLedgers()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: (data: Ledger[]) => {
                    this.ledgers$.next(data);
                },
                error: (error) => {
                    console.error('Error fetching ledgers', error);
                },
            });
    }

    public makeTransfer(): void {
        if (this.fromLedgerId !== null && this.toLedgerId !== null && this.amount !== null && this.amount > 0) {
            this.ledgerService.transferFunds(this.fromLedgerId, this.toLedgerId, this.amount).subscribe({
                next: () => {
                    this.transferMessage = 'Transfer successful!';
                    this.loadLedgers();
                },
                error: (error) => {
                    this.transferMessage = `Transfer failed: ${error.error.message}`;
                    console.error('Transfer error', error);
                },
            });
        } else {
            this.transferMessage = 'Please fill in all fields with valid data.';
        }
    }
}
