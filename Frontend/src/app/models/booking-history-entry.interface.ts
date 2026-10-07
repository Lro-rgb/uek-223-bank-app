export interface BookingHistoryEntry {
    id: number;
    sourceId: number;
    sourceName: string | null;
    destinationId: number;
    destinationName: string | null;
    amount: number;
    createdAt: string;
}
