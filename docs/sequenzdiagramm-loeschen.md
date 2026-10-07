# Sequenzdiagramm Konto löschen

Ablauf beim Löschen eines Kontos (Zusatz-Feature 1) vom Button bis zur Datenbank, so wie er im Code umgesetzt ist (`ledger.component.ts` → `ledger.service.ts` / `auth.interceptor.ts` → `LedgersController` → `LedgerRepository.Delete()` → MariaDB).

```mermaid
sequenceDiagram
    actor User
    participant C as LedgerComponent
    participant S as LedgerService / authInterceptor
    participant LC as LedgersController
    participant R as LedgerRepository
    participant DB as MariaDB

    User->>C: „Löschen“ bei Konto X klicken
    C->>User: confirm(„Konto "X" wirklich löschen?“)
    alt Abbrechen
        C-->>User: nichts passiert, kein Request
    else OK
        C->>C: deletingId = X (Button gesperrt), Meldung leeren
        C->>S: deleteLedger(id)
        S->>LC: DELETE /api/v1/ledgers/{id}<br/>Authorization: Bearer JWT
        alt kein/ungültiges Token
            LC-->>S: 401 (ohne Body)
        else Rolle nicht Administrators (z.B. testuser)
            LC-->>S: 403 (ohne Body, [Authorize(Roles = "Administrators")])
        else Rolle Administrators
            LC->>R: Delete(id)
            loop Versuch 1..3 (MaxDeleteAttempts)
                R->>DB: neue Verbindung, BEGIN (Read Committed)
                R->>DB: SELECT balance … WHERE id = X FOR UPDATE
                alt Deadlock 1213 / Lock-Wait-Timeout 1205
                    DB-->>R: MySqlException, Transaktion zurückgerollt
                    R->>R: LogWarning, nächster Versuch
                else Konto nicht gefunden
                    R->>DB: ROLLBACK
                    R-->>LC: LedgerNotFound
                    LC-->>S: 404 { message: „Konto nicht gefunden.“ }
                else Saldo ≠ 0
                    R->>DB: ROLLBACK
                    R-->>LC: HasBalance
                    LC-->>S: 409 { message: „Konto hat noch Guthaben.“ }
                else Saldo = 0
                    R->>DB: SELECT COUNT(*) FROM Bookings WHERE SourceId = X OR DestinationId = X
                    alt Konto hat Buchungen
                        R->>DB: ROLLBACK
                        R-->>LC: HasBookings
                        LC-->>S: 409 { message: „Konto hat Buchungen.“ }
                    else keine Buchungen
                        R->>DB: DELETE FROM Ledgers WHERE id = X
                        R->>DB: COMMIT
                        R-->>LC: Success
                        LC-->>S: 204
                    end
                end
            end
            opt 3 Versuche fehlgeschlagen
                R->>R: LogError
                R-->>LC: RetriesExhausted
                LC-->>S: 409 { message }
            end
        end
        S-->>C: Antwort
        C->>C: finalize: deletingId = null
        alt 204
            C-->>User: „Konto "X" gelöscht.“
            C->>S: loadLedgers(): GET /api/v1/ledgers
        else Fehler
            C-->>User: error.error.message, sonst bei 401/403 „Keine Berechtigung zum Löschen.“
        end
    end
```

## Massnahmen aus der Analyse

Prüfung und `DELETE` laufen in einer Transaktion (wie Massnahme 1), und das Konto wird mit `SELECT … FOR UPDATE` gesperrt, sodass Saldo und Buchungen auf der gesperrten Zeile geprüft werden (wie Massnahme 2). Eine gleichzeitige Buchung auf dasselbe Konto sperrt es ebenfalls und wartet deshalb auf das Löschen oder umgekehrt, so kann kein Geld verschwinden und keine Buchung auf ein gelöschtes Konto entstehen. Deadlocks und Lock-Wait-Timeouts fängt dieselbe Retry-Schleife ab wie beim Buchen (Massnahme 3), nach 3 Versuchen gibt es 409 statt 500.

Hinweise zum Diagramm: Mermaid kann einen `loop` nicht vorzeitig verlassen. Im Code beenden die Zweige 404, 409 und 204 die Schleife sofort, nur 1213/1205 führen zum nächsten Versuch. Andere Datenbankfehler werden nicht abgefangen und enden als 500. Ist das Backend nicht erreichbar (Status 0), zeigt das Frontend „Server nicht erreichbar. Bitte später erneut versuchen.“, bei anderen Fehlern ohne `message` „Löschen fehlgeschlagen.“.
