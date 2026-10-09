import { test, expect, Page, APIRequestContext } from '@playwright/test';
import credentials from './credentials';

// End-to-End-Tests im Browser gegen das laufende Frontend (4200), Backend (5000) und die lokale DB.
// Die Tests hängen nicht von festen Saldi ab: Vorher und nachher werden die Konten über die API gelesen
// (siehe docs/e2e-tests.md, E2E-01 bis E2E-10).

const apiUrl = 'http://localhost:5000/api/v1';

interface Ledger {
    id: number;
    name: string | null;
    balance: number;
}

interface HistoryEntry {
    id: number;
}

type Credentials = { username: string; password: string };

async function apiToken(request: APIRequestContext, user: Credentials): Promise<string> {
    const response = await request.post(`${apiUrl}/Login`, { data: user });
    expect(response.ok()).toBeTruthy();
    return ((await response.json()) as { token: string }).token;
}

async function apiLedgers(request: APIRequestContext, token: string): Promise<Ledger[]> {
    const response = await request.get(`${apiUrl}/Ledgers`, { headers: { Authorization: `Bearer ${token}` } });
    expect(response.ok()).toBeTruthy();
    return (await response.json()) as Ledger[];
}

async function apiHistory(request: APIRequestContext, token: string, ledgerId: number): Promise<HistoryEntry[]> {
    const response = await request.get(`${apiUrl}/Bookings?ledgerId=${ledgerId}`, { headers: { Authorization: `Bearer ${token}` } });
    expect(response.ok()).toBeTruthy();
    return (await response.json()) as HistoryEntry[];
}

function total(ledgers: Ledger[]): number {
    return Math.round(ledgers.reduce((sum, ledger) => sum + ledger.balance, 0) * 100) / 100;
}

function balanceOf(ledgers: Ledger[], id: number): number {
    const ledger = ledgers.find((l) => l.id === id);
    expect(ledger).toBeDefined();
    return ledger!.balance;
}

// Konto mit dem grössten Saldo als Quelle, irgendein anderes als Ziel.
function pickSourceAndDestination(ledgers: Ledger[]): { source: Ledger; destination: Ledger } {
    const sorted = [...ledgers].sort((a, b) => b.balance - a.balance);
    expect(sorted.length).toBeGreaterThanOrEqual(2);
    expect(sorted[0].balance).toBeGreaterThan(1);
    return { source: sorted[0], destination: sorted[1] };
}

async function login(page: Page, user: Credentials): Promise<void> {
    await page.goto('/login');
    await page.fill('[name="username"]', user.username);
    await page.fill('[name="password"]', user.password);
    await page.click('[type="submit"]');
    await expect(page).toHaveURL('/ledgers');
    await expect(page.locator('#sourceId option')).not.toHaveCount(1);
}

// Die Optionen stehen in derselben Reihenfolge wie GET /Ledgers, Index 0 ist der Platzhalter „Konto wählen“.
async function book(page: Page, ledgers: Ledger[], sourceId: number, destinationId: number, amount: string): Promise<void> {
    await page.selectOption('#sourceId', { index: ledgers.findIndex((l) => l.id === sourceId) + 1 });
    await page.selectOption('#destinationId', { index: ledgers.findIndex((l) => l.id === destinationId) + 1 });
    await page.fill('#amount', amount);
    await page.getByRole('button', { name: 'Buchen' }).click();
}

test.describe('Anmeldung', () => {
    test('E2E-01 Login als admin führt zur Kontenliste', async ({ page }) => {
        await login(page, credentials.admin);

        await expect(page.getByRole('heading', { name: 'Ledgers' })).toBeVisible();
    });

    test('E2E-02 /ledgers ohne Login leitet auf /login um', async ({ page }) => {
        await page.goto('/ledgers');

        await expect(page).toHaveURL('/login');
    });

    test('E2E-03 falsches Passwort zeigt eine Fehlermeldung', async ({ page }) => {
        await page.goto('/login');
        await page.fill('[name="username"]', credentials.admin.username);
        await page.fill('[name="password"]', 'falsch');
        await page.click('[type="submit"]');

        await expect(page.getByText('Benutzername oder Passwort falsch.')).toBeVisible();
        await expect(page).toHaveURL('/login');
    });
});

test.describe('Buchen', () => {
    test('E2E-04 admin bucht 1.00, Saldi ändern sich, Total bleibt gleich', async ({ page, request }) => {
        const token = await apiToken(request, credentials.admin);
        const before = await apiLedgers(request, token);
        const { source, destination } = pickSourceAndDestination(before);
        await login(page, credentials.admin);

        await book(page, before, source.id, destination.id, '1');

        await expect(page.getByText('Buchung erfolgreich.')).toBeVisible();
        const after = await apiLedgers(request, token);
        expect(balanceOf(after, source.id)).toBeCloseTo(source.balance - 1, 2);
        expect(balanceOf(after, destination.id)).toBeCloseTo(destination.balance + 1, 2);
        expect(total(after)).toBeCloseTo(total(before), 2);
    });

    test('E2E-05 testuser darf buchen (Rolle Users)', async ({ page, request }) => {
        const token = await apiToken(request, credentials.admin);
        const before = await apiLedgers(request, token);
        const { source, destination } = pickSourceAndDestination(before);
        await login(page, credentials.user);

        await book(page, before, source.id, destination.id, '1');

        await expect(page.getByText('Buchung erfolgreich.')).toBeVisible();
        expect(total(await apiLedgers(request, token))).toBeCloseTo(total(before), 2);
    });

    test('E2E-06 Betrag grösser als Saldo wird abgelehnt, nichts ändert sich', async ({ page, request }) => {
        const token = await apiToken(request, credentials.admin);
        const before = await apiLedgers(request, token);
        const { source, destination } = pickSourceAndDestination(before);
        const historyBefore = await apiHistory(request, token, source.id);
        await login(page, credentials.admin);

        const bookingResponse = page.waitForResponse((r) => r.url().endsWith('/bookings') && r.request().method() === 'POST');
        await book(page, before, source.id, destination.id, String(source.balance + 1));

        expect((await bookingResponse).status()).toBe(409);
        await expect(page.getByText('Zu wenig Guthaben auf dem Quellkonto.')).toBeVisible();
        const after = await apiLedgers(request, token);
        expect(balanceOf(after, source.id)).toBeCloseTo(source.balance, 2);
        expect(balanceOf(after, destination.id)).toBeCloseTo(destination.balance, 2);
        expect(await apiHistory(request, token, source.id)).toHaveLength(historyBefore.length);
    });

    test('E2E-07 Betrag 0 wird schon im Frontend abgelehnt, kein Request', async ({ page, request }) => {
        const token = await apiToken(request, credentials.admin);
        const ledgers = await apiLedgers(request, token);
        const { source, destination } = pickSourceAndDestination(ledgers);
        await login(page, credentials.admin);
        let posted = false;
        page.on('request', (r) => {
            if (r.url().endsWith('/bookings') && r.method() === 'POST') {
                posted = true;
            }
        });

        await book(page, ledgers, source.id, destination.id, '0');

        await expect(page.getByText('Der Betrag muss grösser als 0 sein.')).toBeVisible();
        expect(posted).toBe(false);
    });
});

test.describe('Buchungshistorie (Zusatz-Feature 2)', () => {
    test('E2E-08 neue Buchung erscheint sofort zuoberst, genau einmal', async ({ page, request }) => {
        const token = await apiToken(request, credentials.admin);
        const ledgers = await apiLedgers(request, token);
        const { source, destination } = pickSourceAndDestination(ledgers);
        const historyBefore = await apiHistory(request, token, destination.id);
        await login(page, credentials.admin);
        await page.selectOption('#historyLedgerId', { index: ledgers.findIndex((l) => l.id === destination.id) + 1 });
        const rows = page.locator('.history-container tbody tr');
        await expect(rows).toHaveCount(historyBefore.length);

        await book(page, ledgers, source.id, destination.id, '2.5');

        await expect(rows).toHaveCount(historyBefore.length + 1);
        await expect(rows.first()).toContainText('2.50');
        const historyAfter = await apiHistory(request, token, destination.id);
        expect(historyAfter).toHaveLength(historyBefore.length + 1);
    });
});

test.describe('Konto löschen (Zusatz-Feature 1)', () => {
    test('E2E-09 Konto mit Guthaben wird mit Meldung abgelehnt und bleibt', async ({ page, request }) => {
        const token = await apiToken(request, credentials.admin);
        const ledgers = await apiLedgers(request, token);
        const { source } = pickSourceAndDestination(ledgers);
        await login(page, credentials.admin);
        page.on('dialog', (dialog) => void dialog.accept());

        await page
            .locator('li', { hasText: source.name ?? '' })
            .getByRole('button', { name: 'Löschen' })
            .click();

        await expect(page.getByText('Konto hat noch Guthaben.')).toBeVisible();
        expect((await apiLedgers(request, token)).some((l) => l.id === source.id)).toBe(true);
    });

    test('E2E-10 testuser darf nicht löschen', async ({ page, request }) => {
        const token = await apiToken(request, credentials.admin);
        const ledgers = await apiLedgers(request, token);
        await login(page, credentials.user);
        page.on('dialog', (dialog) => void dialog.accept());

        await page.getByRole('button', { name: 'Löschen' }).first().click();

        await expect(page.getByText('Keine Berechtigung zum Löschen.')).toBeVisible();
        expect(await apiLedgers(request, token)).toHaveLength(ledgers.length);
    });
});
