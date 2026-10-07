# Sequenzdiagramm Buchen

Ablauf einer Buchung vom Formular bis zur Datenbank, so wie er im Code umgesetzt ist (`ledger.component.ts` → `ledger.service.ts` / `auth.interceptor.ts` → `BookingsController` → `BookingRepository.Book()` → MariaDB).

```mermaid
sequenceDiagram
    actor User
    participant C as LedgerComponent
    participant S as LedgerService / authInterceptor
    participant BC as BookingsController
    participant R as BookingRepository
    participant DB as MariaDB

    User->>C: Formular „Buchen“ absenden
    C->>C: book(): Felder gesetzt? Betrag > 0? Von ≠ Nach?
    alt clientseitig ungültig
        C-->>User: Fehlermeldung, kein Request
    else gültig
        C->>C: isSubmitting = true
        C->>S: book(sourceId, destinationId, amount)
        S->>BC: POST /api/v1/Bookings { sourceId, destinationId, amount }<br/>Authorization: Bearer JWT
        alt kein/ungültiges Token oder falsche Rolle
            BC-->>S: 401 / 403 (ohne Body, [Authorize])
        else Rolle Administrators oder Users
            BC->>R: Book(sourceId, destinationId, amount)
            R->>R: BookingValidator.Validate()
            alt Betrag ≤ 0 oder Quelle = Ziel
                R-->>BC: InvalidAmount / SameLedger
                BC-->>S: 400 { message }
            else Eingaben gültig
                loop Versuch 1..3 (MaxAttempts)
                    R->>DB: neue Verbindung, BEGIN (Read Committed)
                    R->>DB: SELECT balance … FOR UPDATE (kleinere ID)
                    R->>DB: SELECT balance … FOR UPDATE (grössere ID)
                    alt Deadlock 1213 / Lock-Wait-Timeout 1205
                        DB-->>R: MySqlException, Transaktion zurückgerollt
                        R->>R: LogWarning, nächster Versuch
                    else Konto nicht gefunden
                        R->>DB: ROLLBACK
                        R-->>BC: LedgerNotFound
                        BC-->>S: 404 { message }
                    else Saldo Quelle < Betrag
                        R->>DB: ROLLBACK
                        R-->>BC: InsufficientFunds
                        BC-->>S: 409 { message }
                    else Saldo reicht
                        R->>DB: UPDATE Ledgers balance - Betrag (Quelle)
                        R->>DB: UPDATE Ledgers balance + Betrag (Ziel)
                        R->>DB: INSERT Bookings (SourceId, DestinationId, Amount, CreatedAt)
                        R->>DB: COMMIT
                        R-->>BC: Success
                        BC-->>S: 200
                    end
                end
                opt 3 Versuche fehlgeschlagen
                    R->>R: LogError
                    R-->>BC: RetriesExhausted
                    BC-->>S: 409 { message }
                end
            end
        end
        S-->>C: Antwort
        C->>C: finalize: isSubmitting = false
        alt 200
            C-->>User: „Buchung erfolgreich.“, Betrag leeren
            C->>S: loadLedgers(): GET /api/v1/ledgers
        else 4xx
            C-->>User: error.error.message, sonst Fallback-Text
        end
    end
```

## Massnahmen aus der Analyse

- **Massnahme 1 (Transaktion):** Alles zwischen `BEGIN (Read Committed)` und `COMMIT` bzw. `ROLLBACK` läuft auf einer Verbindung in einer Transaktion (`BookingRepository.TryBook()`), eine halbe Buchung kann es also nicht geben.
- **Massnahme 2 (Saldo-Prüfung):** Beide Konten werden mit `SELECT … FOR UPDATE` gesperrt, und der Saldo wird auf der gesperrten Zeile geprüft. Bis zum Commit kann niemand den Saldo ändern, das verhindert Lost Update und Saldo unter 0.
- **Massnahme 3 (Retry, Sperr-Reihenfolge):** Die Konten werden immer in der Reihenfolge kleinere ID vor grösserer ID gesperrt, was Deadlocks selten macht. Tritt trotzdem 1213 oder 1205 auf, startet der `loop` die ganze Buchung mit neuer Verbindung neu, nach 3 Versuchen gibt es 409 statt 500.
- **Massnahme 4 (Historie):** `INSERT Bookings` läuft in derselben Transaktion wie die beiden `UPDATE`. Ein Rollback oder ein abgebrochener Versuch hinterlässt deshalb keinen Eintrag.

Hinweise zum Diagramm: Mermaid kann einen `loop` nicht vorzeitig verlassen. Im Code beenden die Zweige 404, 409 und 200 die Schleife sofort, nur 1213/1205 führen zum nächsten Versuch. Andere Datenbankfehler werden nicht abgefangen und enden als 500.
