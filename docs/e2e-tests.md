# End-to-End-Tests (Playwright)

Die E2E-Tests bedienen die App im Browser (Chromium) wie eine Benutzerin: anmelden, buchen, Historie ansehen, Konto löschen. Sie liegen in `Frontend/e2e/bank.spec.ts` und laufen gegen das lokale Frontend (`http://localhost:4200`), das Backend (`http://localhost:5000`) und die lokale DB `bank`.

Die Tests verlassen sich nicht auf feste Saldi. Vor und nach jeder Aktion lesen sie die Konten und die Historie über die API (`GET /api/v1/Ledgers`, `GET /api/v1/Bookings?ledgerId=`) und vergleichen. Sie laufen deshalb auch auf einer DB, auf der schon gebucht wurde. Weil alle Tests dieselbe DB verwenden, laufen sie nacheinander (`workers: 1`) und nur in Chromium.

## Ausführen

DB, Backend und Frontend müssen laufen (README, Starten). Danach im Ordner `Frontend`:

```
cp e2e/credentials.sample.ts e2e/credentials.ts   # Test-User aus dem Seeder, einmalig
npx playwright install chromium                    # Browser für Playwright, einmalig
npm run e2e-ci                                     # alle Tests in der Konsole
npm run e2e                                        # alternativ mit Playwright-Oberfläche
```

Der HTML-Report landet in `Frontend/playwright-report/` (nicht im Repo). Die E2E-Tests laufen nicht in der CI, weil dort Backend und DB zusätzlich gestartet werden müssten.

Die Tests buchen kleine Beträge (1.00 und 2.50) zwischen bestehenden Konten. Das Total bleibt dabei gleich.

## Protokoll

| Datum | Stand | Ergebnis |
|---|---|---|
| 09.10.2026 | Branch `fix/put-saldo-test` | 12/12 bestanden (10 aus `bank.spec.ts`, 2 aus der Vorlage `example.spec.ts`), zweimal hintereinander |

Gefundener Fehler: E2E-03 war zuerst rot. Bei falschem Passwort erschien keine Meldung, weil `LoginComponent` die Meldung in einem normalen Feld hielt und die Komponente mit `OnPush` nach der HTTP-Antwort nicht neu gezeichnet wurde. Behoben mit einem Signal wie in den anderen Komponenten, die Meldung lautet jetzt „Benutzername oder Passwort falsch.“.

## Testfälle

| Nr. | Test | Ablauf | Erwartetes Ergebnis | Anforderung |
|---|---|---|---|---|
| E2E-01 | Login als admin | Benutzername und Passwort eingeben, absenden | Weiterleitung auf `/ledgers`, Kontenliste sichtbar | A1 |
| E2E-02 | Ohne Login | `/ledgers` direkt aufrufen | Weiterleitung auf `/login` (`AuthGuard`, externer Test F4) | A1 |
| E2E-03 | Falsches Passwort | admin mit falschem Passwort | Meldung „Benutzername oder Passwort falsch.“, bleibt auf `/login` | A1 |
| E2E-04 | Buchen als admin | 1.00 vom Konto mit dem grössten Saldo auf ein anderes | „Buchung erfolgreich.“, Quelle −1, Ziel +1, Total gleich (über API geprüft) | A2 |
| E2E-05 | Buchen als testuser | 1.00 als `testuser` | „Buchung erfolgreich.“, Total gleich | A2 |
| E2E-06 | Zu wenig Guthaben | Saldo + 1 buchen | Antwort 409, „Zu wenig Guthaben auf dem Quellkonto.“, Saldi und Historie unverändert | A4, A6 |
| E2E-07 | Betrag 0 | 0 buchen | „Der Betrag muss grösser als 0 sein.“, kein `POST /bookings` | A5 |
| E2E-08 | Historie (Zusatz-Feature 2) | Historie des Zielkontos öffnen, dann 2.50 darauf buchen | Ohne Neuladen genau ein Eintrag mehr, zuoberst mit 2.50, über API ebenfalls genau einer mehr | A6 |
| E2E-09 | Konto mit Guthaben löschen (Zusatz-Feature 1) | „Löschen“ beim Konto mit dem grössten Saldo, Dialog bestätigen | „Konto hat noch Guthaben.“, Konto bleibt | Zusatz-Feature 1 |
| E2E-10 | Löschen als testuser | „Löschen“ als `testuser`, Dialog bestätigen | „Keine Berechtigung zum Löschen.“, alle Konten bleiben | A3 |

Erfolgreiches Löschen ist nicht als E2E-Test umgesetzt, weil es ein Konto mit Saldo 0 und ohne Buchungen braucht und es keine Funktion zum Erstellen von Konten gibt. Es ist über IT-D1 (Repository), UT-C1 (Controller) und ZF-1 im [externen Testprotokoll](testprotokoll-extern.md) abgedeckt.
