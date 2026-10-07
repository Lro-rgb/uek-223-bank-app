import { ChangeDetectionStrategy, Component, DestroyRef, Input, OnChanges, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { BookingService } from '../../services/booking.service';
import { BookingHistoryEntry } from '../../models/booking-history-entry.interface';
import { Ledger } from '../../models/ledger.interface';

@Component({
    selector: 'app-booking-history',
    templateUrl: './booking-history.component.html',
    styleUrl: './booking-history.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [CommonModule, FormsModule],
})
export class BookingHistoryComponent implements OnChanges {
    @Input() public ledgers: Ledger[] = [];

    public selectedId = signal<number | null>(null);
    public entries = signal<BookingHistoryEntry[]>([]);
    public loaded = signal(false);
    public isLoading = signal(false);
    public errorMessage = signal('');

    constructor(
        private bookingService: BookingService,
        private destroyRef: DestroyRef,
    ) {}

    // Die Kontoliste wird nach jeder Buchung und jedem Löschen neu geladen. Dann wird auch die Historie neu geladen.
    ngOnChanges(): void {
        const id = this.selectedId();
        if (id === null) {
            return;
        }
        if (!this.ledgers.some((ledger) => ledger.id === id)) {
            this.reset();
            return;
        }
        this.load(id);
    }

    public select(id: number | null): void {
        this.selectedId.set(id);
        if (id === null) {
            this.reset();
            return;
        }
        this.load(id);
    }

    private load(id: number): void {
        this.isLoading.set(true);
        this.errorMessage.set('');

        this.bookingService
            .getHistory(id)
            .pipe(
                finalize(() => this.isLoading.set(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: (entries) => {
                    this.entries.set(entries);
                    this.loaded.set(true);
                },
                error: (error: HttpErrorResponse) => {
                    console.error('History error', error);
                    this.entries.set([]);
                    this.loaded.set(false);
                    this.errorMessage.set(this.toErrorMessage(error));
                },
            });
    }

    private reset(): void {
        this.selectedId.set(null);
        this.entries.set([]);
        this.loaded.set(false);
        this.errorMessage.set('');
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
            return 'Keine Berechtigung für die Historie.';
        }
        return 'Historie konnte nicht geladen werden.';
    }
}
