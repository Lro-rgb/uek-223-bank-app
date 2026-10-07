import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BookingHistoryEntry } from '../models/booking-history-entry.interface';

@Injectable({
    providedIn: 'root',
})
export class BookingService {
    private apiUrl = 'http://localhost:5000/api/v1';

    constructor(private http: HttpClient) {}

    getHistory(ledgerId: number): Observable<BookingHistoryEntry[]> {
        return this.http.get<BookingHistoryEntry[]>(`${this.apiUrl}/bookings`, { params: { ledgerId } });
    }
}
