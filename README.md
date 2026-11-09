# RongTuli

[![CI](https://github.com/shakil1990/RongTuli/actions/workflows/CI.yml/badge.svg?branch=develop)](https://github.com/shakil1990/RongTuli/actions/workflows/CI.yml)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](LICENSE)

RongTuli is a full-stack e-commerce storefront for a clothing shop in Bangladesh that sells with **cash on delivery**. Customers browse the catalog, build a cart and place orders as guests with just a phone number, with no account to create. Admins sign in with Google to manage the catalog, product photos and order fulfilment.

The project runs entirely on free-tier hosting. It has a React single-page app, an ASP.NET Core API and a PostgreSQL database.

**Live:** [rong-tuli-peach.vercel.app](https://rong-tuli-peach.vercel.app) · API [rongtuliapi.onrender.com/health](https://rongtuliapi.onrender.com/health)

> The API runs on Render's free tier and sleeps when idle, so the first request after a quiet period can take up to a minute. The storefront shows a notice while the API wakes up.

---

## Features

### Storefront
- Browse products by category, sort by price or newest arrival, with sorting and paging kept in the URL so links can be shared
- Product pages with an image gallery (up to 5 photos, resized by Cloudinary's CDN) and a size picker
- A cart that survives page reloads and is re-priced by the server before checkout. Items that are unavailable, have changed price or are missing a size are flagged
- Guest checkout with a **Bangladeshi mobile number**, with no account to create. Phone verification by one-time code is built in and can be switched on with one setting once an SMS provider is live. Customers choose a delivery area (Inside Dhaka City or Outside Dhaka) and the fee is added to the order
- An order confirmation page, and order lookup by verifying the same phone number by code
- Light and dark themes

### Admin panel
- Google sign-in, limited to an allowlist of admin email addresses
- Create, edit and publish or unpublish products, and manage categories
- Upload, reorder, delete and choose the cover photo for product images. Photos upload directly from the browser to Cloudinary with a server-signed request
- An order queue filtered by status, with a pending-order count and fixed rules for which status changes are allowed

### Engineering
- Every order is re-priced on the server. If a price changed after the customer saw it, the order is rejected (`409 Conflict`)
- Rate limits on sign-in, OTP requests and order placement, plus a per-phone cooldown and hourly and daily caps on OTP sends
- OTP codes are stored only as HMAC hashes and compared in constant time. The token issued after verification carries no role, so it can never reach an admin endpoint
- Security headers on the API and the web host, and structured JSON logging with Serilog
- A database-backed `/health` endpoint used by the host's health checks
- CI on every pull request: build, a check that EF Core migrations match the model, API and web tests, lint and type-check

## Architecture

```
 Browser ──► React SPA (Vercel) ──► ASP.NET Core API (Render, Docker) ──► PostgreSQL (Neon)
    │                                     │
    └──── signed direct upload ──► Cloudinary ◄── signs uploads, stores image IDs
                                          │
                     Google Identity ◄────┘  verifies admin ID tokens, issues the API's own JWT
```

| Layer    | Technology                                                                | Hosting            |
|----------|---------------------------------------------------------------------------|--------------------|
| Frontend | React 19, TypeScript, Vite, React Router, TanStack Query, Tailwind CSS 4  | Vercel             |
| API      | ASP.NET Core minimal APIs on .NET 10, EF Core, Serilog                    | Render (Docker)    |
| Database | PostgreSQL through Npgsql                                                 | Neon               |
| Images   | Signed direct upload and CDN transforms                                   | Cloudinary         |
| Auth     | Google Identity Services for admins and phone OTP for customers; the API issues its own JWTs | Google Cloud OAuth |
| CI       | GitHub Actions                                                            | GitHub             |

The API is organised by feature (`Catalog`, `Cart`, `Orders`, `Images`, `Phone`, `Auth`). Each folder holds its own endpoints, DTOs, validation and queries, while `Domain` and `Data` hold the entities and EF Core configuration.

## Design decisions

**Phone OTP instead of customer accounts.** Customers in this market expect to order with a phone number and pay the courier, and asking them to create an account loses sales. Fake cash-on-delivery orders are the main business risk, because each one wastes a delivery trip. Verifying the phone by a texted code before accepting an order addresses that risk directly. Until an SMS provider is chosen, that check is switched off with one setting (`Orders:RequirePhoneVerification`), and the admin confirmation call is the safeguard. Reading orders back still needs a verified phone, so a typed number never reveals someone else’s orders or address. Google sign-in is kept for the two admins only.

**The API issues its own JWTs.** The API verifies a Google ID token once, then issues a short-lived token of its own that carries the user's role. The role is re-read from the admin allowlist on every sign-in, so an admin can be removed without editing the database. The phone-verification token is signed with the same key but carries no role, so the authorization policies can never mistake it for an admin token.

**Bearer tokens instead of cookies.** The web app and the API live on different sites (`vercel.app` and `onrender.com`). Cookie auth would need `SameSite=None`, credentialed CORS and CSRF protection. A bearer token with a fixed lifetime and no refresh token keeps that whole class of problems out of the design.

**The server owns the price.** The cart lives in the browser and is treated as untrusted input. The API re-prices every cart and every order using the same rules. If a price changed after the customer saw it, the order is rejected with `409` instead of being charged silently at a different amount. Delivery fees are copied onto each order, so changing a fee never alters past orders.

**Images go straight to Cloudinary.** The API only signs upload requests, so no image bytes pass through the free container host. On delete, the API removes the image from Cloudinary before deleting the database row. If Cloudinary is unreachable, the catalog stays unchanged instead of pointing at a missing image.

**External services sit behind interfaces.** Google token verification, image storage and SMS sending are each behind an interface with a fake used in tests. CI needs no credentials and sends no real messages. In development, the SMS sender writes codes to the log, and outside development an unconfigured provider fails loudly instead of logging codes.

**Abuse limits match how the shop is used.** OTP sends are capped per phone, per IP, per hour and per day, because SMS costs real money and "SMS pumping" is a known attack. With verification on, order placement is limited per *verified phone* rather than per IP, because mobile carriers put many customers behind one IP address.

**Free tiers, chosen deliberately.** Every service runs on a free plan with no card on file. The trade-offs are documented and handled in the app: the API sleeps when idle, so the UI shows a wake-up notice and background jobs run at startup, and photos are capped at five per product to stay inside the image quota. The [build plan](docs/build-plan.md#free-tier-limits-and-risks) lists what changes before taking real sales.

## Repository layout

```
api/
  Shop.Api/          ASP.NET Core Web API (feature folders, EF Core migrations, Dockerfile)
  Shop.Tests/        Integration and unit tests (xUnit, WebApplicationFactory)
web/                 React storefront and admin panel (Vite)
docs/build-plan.md   Living build plan: scope, data model, step status, configuration, change log
knowledge-base/      Source content and configuration for a planned RAG shopping assistant
.github/workflows/   CI pipeline
```

## Getting started

### Prerequisites
- [.NET SDK 10](https://dotnet.microsoft.com/download) (version pinned in `api/global.json`)
- [Node.js 24](https://nodejs.org/) and npm
- A PostgreSQL database, either local or a free [Neon](https://neon.tech) project
- Optional: a Google OAuth web client ID (for admin sign-in) and a Cloudinary account (for image upload)

### API

```bash
cd api/Shop.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=...;Database=...;Username=...;Password=...;SSL Mode=Require"
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 32)"
dotnet user-secrets set "Google:ClientId" "<your-client-id>.apps.googleusercontent.com"
dotnet user-secrets set "Admin:Emails:0" "you@example.com"

cd ..
dotnet tool restore
dotnet ef database update --project Shop.Api --connection "<same connection string>"
dotnet run --project Shop.Api        # http://localhost:5210
```

`Jwt:Key` is the only setting the API needs to start. Google, Cloudinary and SMS settings are checked the first time they are used, so the API boots without them. In Development, OTP codes are written to the console log instead of being sent by SMS.

### Web

```bash
cd web
cp .env.example .env.local           # set VITE_API_URL=http://localhost:5210 and VITE_GOOGLE_CLIENT_ID
npm ci
npm run dev                          # http://localhost:5173
```

The full list of settings, with defaults and which ones are secret, is in [docs/build-plan.md](docs/build-plan.md#credentials-and-configuration). Nothing secret is committed. Use `dotnet user-secrets` and `web/.env.local` locally and host environment variables in production.

## Testing

```bash
cd api && dotnet test Shop.sln       # 230+ integration and unit tests; no database or secrets required
cd web && npm test                   # 120+ Vitest unit tests
cd web && npm run lint && npm run build
```

API tests run the real HTTP pipeline through `WebApplicationFactory` against an in-memory database, with Google, Cloudinary and SMS replaced by fakes. A separate set of tests checks that the cart and order queries translate to SQL.

## Deployment

| Target | Trigger | Notes |
|--------|---------|-------|
| Web → Vercel | Push to the tracked branch | `VITE_API_URL` and `VITE_GOOGLE_CLIENT_ID` are baked in at build time |
| API → Render | Push to the tracked branch | Docker build from `api/Shop.Api/Dockerfile` with context `api`, health check `/health` |
| Database → Neon | Manual | `dotnet ef database update` is run before deploying a branch that adds a migration |

## Branching

- `main`: stable releases
- `develop`: the integration branch and default PR target
- `feature/*`: one branch per piece of work, merged into `develop` by pull request once CI passes

## Project status

The core shopping flow is built: catalog, images, storefront, admin panel, cart, checkout and orders, and security hardening. Phone verification is implemented but switched off until an SMS provider is chosen, so checkout currently takes a valid mobile number without a code. Online payment, reviews, coupons and wishlists are out of scope for v1, and the data model leaves room to add them later.

Step-by-step progress and decisions are tracked in [docs/build-plan.md](docs/build-plan.md).

## Author

**Shahadat Shakil**. I designed and built RongTuli end to end: product scope, data model, API, web app, CI and deployment.

- GitHub: [@shakil1990](https://github.com/shakil1990)
<!-- Add LinkedIn / portfolio links here, e.g. - LinkedIn: [name](https://www.linkedin.com/in/...) -->

## License

Licensed under the [Apache License 2.0](LICENSE).
