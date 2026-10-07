export interface BookingHistoryEntry {
    id: number;
    sourceId: number;
    sourceName: string;
    destinationId: number;
    destinationName: string;
    amount: number;
    createdAt: string;
}
