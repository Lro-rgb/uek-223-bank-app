# Integrationstests

Die Tests liegen in `Backend/Bank.Tests` und laufen gegen die Test-DB `bank_test` (wird von `db-init` angelegt, siehe README Setup). Alle Tests mit DB laufen nacheinander in der Collection `Database`, jeder Test legt eigene Konten an und räumt sie wieder auf.

Ausführen: `dotnet test Backend/Bank.Tests`

## Konto löschen (Zusatz-Feature 1)

Getestet wird `LedgerRepository.Delete()` in `LedgerDeleteTests.cs`. Der Controller setzt das Ergebnis in den HTTP-Status um (`Success` → 204, `HasBalance`/`HasBookings` → 409, `LedgerNotFound` → 404).

| Nr. | Test | Ziel | Eingabe | Erwartetes Ergebnis |
|---|---|---|---|---|
| IT-D1 | `Delete_ZeroBalanceWithoutBookings_RemovesLedger` | Ein leeres Konto ohne Historie kann gelöscht werden. | Konto mit Saldo 0, keine Buchungen | `Success`, Konto existiert nicht mehr |
| IT-D2 | `Delete_BalanceNotZero_IsRejectedAndLedgerStays` | Kein Geld verschwindet aus dem Total. | Konto mit Saldo 100 | `HasBalance` (409 „Konto hat noch Guthaben.“), Konto bleibt mit Saldo 100 |
| IT-D3 | `Delete_LedgerWithBookings_IsRejectedAndLedgerStays` | Die Historie bleibt erhalten. | Konto mit 100, ganzer Saldo auf ein anderes Konto gebucht (Saldo 0, 1 Buchung) | `HasBookings` (409 „Konto hat Buchungen.“), Konto und Buchung bleiben |
| IT-D4 | `Delete_UnknownLedger_ReturnsNotFound` | Unbekannte oder bereits gelöschte Konten werden sauber abgelehnt. | ID `int.MaxValue` | `LedgerNotFound` (404 „Konto nicht gefunden.“) |
