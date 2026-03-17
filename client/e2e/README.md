# E2E Test Notes

These Playwright tests are built around real frontend journeys and expect a running backend with seed data.

## Run

- `npm run test:e2e`
- `npm run test:e2e:ui`

## Environment

Optional test credentials:

- `E2E_BUYER_CREDENTIALS`
- `E2E_BUYER_PASSWORD`
- `E2E_SELLER_CREDENTIALS`
- `E2E_SELLER_PASSWORD`
- `E2E_ADMIN_CREDENTIALS`
- `E2E_ADMIN_PASSWORD`

If credentials are missing, the corresponding auth-protected tests are skipped.

Useful Playwright vars:

- `PLAYWRIGHT_BASE_URL` (default: `http://127.0.0.1:4173`)
- `PLAYWRIGHT_USE_EXISTING_SERVER=true` to run against an already running frontend
- `VITE_REACT_APP_SERVER_URL` for frontend-to-backend API base URL

The default Playwright web server boot config enables mock checkout UI (`mock` provider) for local E2E reliability while keeping normal app flows provider-agnostic in production config.
