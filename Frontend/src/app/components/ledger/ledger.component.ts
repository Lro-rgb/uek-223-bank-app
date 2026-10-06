import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { LedgerService } from '../../services/ledger.service';
import { Ledger } from '../../models/ledger.interface';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BehaviorSubject, finalize } from 'rxjs';

@Component({
    selector: 'app-ledger',
    templateUrl: './ledger.component.html',
    styleUrl: './ledger.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [CommonModule, FormsModule],
})
export class LedgerComponent implements OnInit {
    public ledgers$ = new BehaviorSubject<Ledger[]>([]);
    public sourceId: number | null = null;
    public destinationId: number | null = null;
    public amount: number | null = null;

    // Signals, damit OnPush die Meldung und den Button-Zustand nach dem HTTP-Aufruf neu zeichnet
    public message = signal('');
    public isError = signal(false);
    public isSubmitting = signal(false);

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

    public book(): void {
        if (this.sourceId === null || this.destinationId === null || this.amount === null) {
            this.showMessage('Bitte Von, Nach und Betrag ausfüllen.', true);
            return;
        }
        if (this.amount <= 0) {
            this.showMessage('Der Betrag muss grösser als 0 sein.', true);
            return;
        }
        if (this.sourceId === this.destinationId) {
            this.showMessage('Von und Nach müssen verschiedene Konten sein.', true);
            return;
        }

        this.isSubmitting.set(true);
        this.message.set('');

        this.ledgerService
            .book(this.sourceId, this.destinationId, this.amount)
            .pipe(
                finalize(() => this.isSubmitting.set(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: () => {
                    this.showMessage('Buchung erfolgreich.', false);
                    this.amount = null;
                    this.loadLedgers();
                },
                error: (error: HttpErrorResponse) => {
                    console.error('Booking error', error);
                    this.showMessage(this.toErrorMessage(error), true);
                },
            });
    }

    private showMessage(text: string, isError: boolean): void {
        this.message.set(text);
        this.isError.set(isError);
    }

    private toErrorMessage(error: HttpErrorResponse): string {
        const message = (error.error as { message?: string } | null)?.message;
        if (message) {
            return message;
        }
        if (error.status === 0) {
            return 'Server nicht erreichbar. Bitte später erneut versuchen.';
        }
        if (error.status === 401 || error.status === 403) {
            return 'Keine Berechtigung für diese Buchung.';
        }
        return 'Buchung fehlgeschlagen.';
    }
}
