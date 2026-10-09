# Playwright End-to-End Testing

Copy the file `credentials.sample.ts` to `credentials.ts` (contains the seeded test users from the README) and install the browser once with `npx playwright install chromium`.

Run the tests with `npm run e2e-ci` (console) or `npm run e2e` (UI). The database, backend and frontend must be running. Test cases and results: [docs/e2e-tests.md](../../docs/e2e-tests.md).
