# Integrationstests

Die Tests liegen in `Backend/Bank.Tests` und laufen gegen die Test-DB `bank_test` (wird von `db-init` angelegt, siehe README Setup). Alle Tests mit DB laufen nacheinander in der Collection `Database`, jeder Test legt eigene Konten an und räumt sie wieder auf.

Ausführen: `dotnet test Backend/Bank.Tests`

## Protokoll

| Datum | Stand | Ergebnis |
|---|---|---|
| 07.10.2026 | `main` 293e8fb, frische DB | 20/20 bestanden |
| 08.10.2026 | `main` 0f7a6ed | 20/20 bestanden |
| 08.10.2026 | Branch `fix/19-externer-test` (Befunde F1–F4 aus dem [externen Testbericht](testbericht-extern.md)) | 24/24 bestanden |

## Konten lesen und ändern

Getestet wird `LedgerRepository.GetAllLedgers()`, `SelectOne()` und `Update()` in `LedgerReadTests.cs`. Anlass war Befund F1 aus dem [externen Testbericht](testbericht-extern.md): Ein Konto mit `name` = NULL liess `GET /api/v1/Ledgers` endlos hängen, weil die Retry-Schleife bei jedem Fehler neu startete. Jetzt wird nur bei 1213/1205 wiederholt (wie beim Buchen), und der Name wird nullable gelesen. Die Lese-Tests brechen nach 10 Sekunden ab, statt zu hängen.

| Nr. | Test | Ziel | Eingabe | Erwartetes Ergebnis |
|---|---|---|---|---|
| IT-L1 | `GetAllLedgers_LedgerWithoutName_ReturnsLedgerWithNullName` | Ein Konto ohne Namen legt die Kontenliste nicht lahm (F1). | Konto mit 100, Name auf NULL | Antwort innerhalb 10 s, Konto mit `Name` = null und Saldo 100 |
| IT-L2 | `SelectOne_LedgerWithoutName_ReturnsLedgerWithNullName` | Dasselbe für ein einzelnes Konto (F1). | Konto mit 50, Name auf NULL | Konto mit `Name` = null und Saldo 50 |
| IT-L3 | `SelectOne_UnknownLedger_ReturnsNull` | Unbekanntes Konto ohne Exception. | ID `int.MaxValue` | `null` |
| IT-L4 | `Update_UnknownLedger_ReturnsFalse` | `PUT` auf ein unbekanntes Konto wird erkannt (F2). | ID `int.MaxValue` | `false` (Controller: 404 „Konto nicht gefunden.“) |

## Konto löschen (Zusatz-Feature 1)

Getestet wird `LedgerRepository.Delete()` in `LedgerDeleteTests.cs`. Der Controller setzt das Ergebnis in den HTTP-Status um (`Success` → 204, `HasBalance`/`HasBookings` → 409, `LedgerNotFound` → 404).

| Nr. | Test | Ziel | Eingabe | Erwartetes Ergebnis |
|---|---|---|---|---|
| IT-D1 | `Delete_ZeroBalanceWithoutBookings_RemovesLedger` | Ein leeres Konto ohne Historie kann gelöscht werden. | Konto mit Saldo 0, keine Buchungen | `Success`, Konto existiert nicht mehr |
| IT-D2 | `Delete_BalanceNotZero_IsRejectedAndLedgerStays` | Kein Geld verschwindet aus dem Total. | Konto mit Saldo 100 | `HasBalance` (409 „Konto hat noch Guthaben.“), Konto bleibt mit Saldo 100 |
| IT-D3 | `Delete_LedgerWithBookings_IsRejectedAndLedgerStays` | Die Historie bleibt erhalten. | Konto mit 100, ganzer Saldo auf ein anderes Konto gebucht (Saldo 0, 1 Buchung) | `HasBookings` (409 „Konto hat Buchungen.“), Konto und Buchung bleiben |
| IT-D4 | `Delete_UnknownLedger_ReturnsNotFound` | Unbekannte oder bereits gelöschte Konten werden sauber abgelehnt. | ID `int.MaxValue` | `LedgerNotFound` (404 „Konto nicht gefunden.“) |

## Buchen (Pflicht-Feature)

Getestet wird `BookingRepository.Book()` in `BookingRepositoryTests.cs` und `BookingConcurrencyTests.cs`. Der `BookingsController` setzt das Ergebnis in den HTTP-Status um (`Success` → 200, `InvalidAmount`/`SameLedger` → 400, `LedgerNotFound` → 404, `InsufficientFunds`/`RetriesExhausted` → 409).

| Nr. | Test | Ziel | Eingabe | Erwartetes Ergebnis |
|---|---|---|---|---|
| IT-B1 | `Book_ValidAmount_MovesMoneyAndKeepsTotal` | Eine gültige Buchung verschiebt das Geld vollständig (Massnahme 1). | Quelle 500, Ziel 100, Betrag 200 | `Success`, Quelle 300, Ziel 300, Total 600 unverändert |
| IT-B2 | `Book_AmountGreaterThanBalance_IsRejectedAndNothingChanges` | Kein Saldo unter 0 (Massnahme 2). | Quelle 50, Betrag 50.01 | `InsufficientFunds` (409), beide Saldi und Historie unverändert |
| IT-B3 | `Book_ExactBalance_LeavesSourceAtZero` | Grenzfall: genau den ganzen Saldo buchen. | Quelle 975, Betrag 975 | `Success`, Quelle 0, Ziel 1000 |
| IT-B4 | `Book_Success_CreatesExactlyOneBookingEntry` | Jede Buchung erzeugt genau einen Historien-Eintrag (Massnahme 4). | Quelle 100, Betrag 40 | genau 1 Eintrag in `Bookings` |
| IT-B5 | `Book_TwoBookingsAtSameTime_SecondSeesNewBalanceAndIsRejected` | Nachweis der Sperre (Massnahme 2), siehe unten. | Quelle 100, zwei Buchungen über je 80 gleichzeitig | erste `Success`, zweite `InsufficientFunds`, Quelle 20, 1 Eintrag |
| IT-B6 | `Book_ParallelBookings_KeepTotalAndNoNegativeBalance` | Unter Last bleibt alles konsistent (Massnahmen 1–4). | 20 parallele User × 100 Buchungen zwischen 2 Konten mit je 100, Beträge 50–100 | Total unverändert, kein Saldo unter 0, Anzahl `Success` = Anzahl Einträge, mindestens 1 `Success`. `RetriesExhausted` wird nur gezählt und ausgegeben, nicht als Fehler gewertet. |
| IT-B7 | `Book_UnknownLedger_ReturnsNotFoundAndNothingChanges` | Buchung auf ein nicht (mehr) existierendes Konto wird sauber abgelehnt. | Ziel-ID `int.MaxValue` | `LedgerNotFound` (404), Quelle unverändert, kein Eintrag |

## Buchungshistorie (Zusatz-Feature 2)

Getestet wird `BookingRepository.GetHistory()` in `BookingHistoryTests.cs`. Der Controller liefert für `GET /api/v1/Bookings?ledgerId=X` die Liste (200) oder 404, wenn das Konto nicht existiert.

| Nr. | Test | Ziel | Eingabe | Erwartetes Ergebnis |
|---|---|---|---|---|
| IT-H1 | `GetHistory_SuccessfulBooking_AppearsExactlyOnceForBothLedgers` | Eine erfolgreiche Buchung erscheint genau einmal, bei Quelle und Ziel (A6). | Buchung 30 von Quelle (100) auf Ziel | je 1 Eintrag mit derselben ID, Betrag 30 |
| IT-H2 | `GetHistory_RejectedBooking_DoesNotAppear` | Eine abgelehnte Buchung hinterlässt keinen Eintrag (A6, Massnahme 4). | Buchung 51 von Quelle mit 50 | `InsufficientFunds`, Historie beider Konten leer |
| IT-H3 | `GetHistory_UnknownLedger_ReturnsNull` | Unbekanntes Konto wird erkannt. | ID `int.MaxValue` | `null` (Controller: 404 „Konto nicht gefunden.“) |
| IT-H4 | `GetHistory_SeveralBookings_NewestFirst` | Neueste Buchung steht oben. | Buchung 10, danach 20 | Reihenfolge 20, 10 |
| IT-H5 | `GetHistory_LedgerWithoutName_ReturnsEntryWithNullName` | Ein Konto ohne Namen (`name` = NULL) führt nicht zu einem Fehler 500. | Buchung, danach Name der Quelle auf NULL | Eintrag mit `SourceName` = null (Frontend zeigt „(ohne Name)“) |

## Unit-Tests ohne Datenbank

`BookingValidatorTests.cs` testet `BookingValidator.Validate()`, die Eingabeprüfung vor der Transaktion.

| Nr. | Test | Eingabe | Erwartetes Ergebnis |
|---|---|---|---|
| UT-V1 | `Validate_AmountNotPositive_ReturnsInvalidAmount` | Betrag 0 und -50 | `InvalidAmount` (400) |
| UT-V2 | `Validate_SameLedger_ReturnsSameLedger` | Quelle = Ziel | `SameLedger` (400) |
| UT-V3 | `Validate_ValidInput_ReturnsSuccess` | Quelle 1, Ziel 2, Betrag 0.01 | `Success` |

## Nachweis Transaktionssicherheit (IT-B5)

Zufällige Last trifft das kurze Zeitfenster zwischen „Saldo lesen“ und „Saldo schreiben“ nur selten. IT-B5 erzwingt deshalb genau diesen Fall: Über den Test-Haken `BookingRepository.AfterBalanceCheck` wird die erste Buchung nach ihrer Saldo-Prüfung angehalten, und genau dann startet die zweite.

- **Mit `FOR UPDATE`:** Die zweite Buchung wartet in der DB auf die Sperre, bis die erste committet hat. Sie liest danach 20 und wird abgelehnt.
- **Ohne `FOR UPDATE`:** Die zweite Buchung liest noch 100, besteht die Prüfung ebenfalls, und das Konto steht danach bei -60.

Ergebnis vom 06.10.2026, je 5 Durchläufe:

| Variante | Ergebnis |
|---|---|
| `SELECT ... FOR UPDATE` (so wie im Code) | 5 × grün |
| `FOR UPDATE` testweise entfernt | 5 × rot („Expected: InsufficientFunds, Actual: Success“) |

Damit ist belegt, dass die Sperre die Ursache für das korrekte Verhalten ist und nicht der Zufall. Nachstellen: in `BookingRepository.SelectBalanceForUpdate()` ` FOR UPDATE` aus der Abfrage entfernen und `dotnet test Backend/Bank.Tests --filter TwoBookingsAtSameTime` ausführen (danach wieder rückgängig machen).
