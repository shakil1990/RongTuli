# RongTuli — Build Plan

Living version of *Simple E-commerce Build Plan.pdf* (Oct 11, 2026). The PDF is the original; **this file is the source of truth** and is updated whenever a step is completed or the plan changes (see [Change log](#change-log)).

A cash-on-delivery clothing store with a React storefront, an ASP.NET Core API and Google sign-in, built in 12 steps on free tiers. Steps in the same wave can run in parallel, and every step ends with something deployed and testable on the live site.

Status legend: ✅ done · 🟡 partly done · ⬜ not started · 🔒 needs your action (account / credentials)

## Progress at a glance

| Step | Title | Status | Notes |
|---|---|---|---|
| 0 | Accounts and repo setup | 🟡 | Repo, README, CI workflow done. Accounts and Google OAuth are 🔒 (see below). `ci.yml` is not yet in git (see Step 0). |
| 1 | Backend skeleton (.NET API) | 🟡 | Code done: EF Core + Npgsql, entities, `InitialCreate` migration, seed categories, DB-checking `/health`. Left: env-var config for Jwt/Google/Cloudinary/Admin (added by Steps 3 and 5), deploy to host 🔒 |
| 2 | Frontend skeleton (React) | 🟡 | Vite + TS + Tailwind, router with all routes, layout, API client, TanStack Query, `vercel.json` done; lint and build verified. Left: category menu (needs Step 4 API), live Vercel deploy 🔒 |
| 3 | Google SSO and roles | ⬜ | Needs Google OAuth client 🔒 |
| 4 | Catalog API | ⬜ | Entities and indexes already exist from Step 1 |
| 5 | Product image upload | ⬜ | Needs Cloudinary account 🔒 |
| 6 | Storefront UI | ⬜ | |
| 7 | Admin panel UI | ⬜ | |
| 8 | Cart | ⬜ | |
| 9 | Checkout and orders (COD) | ⬜ | |
| 10 | Hardening and CI/CD | ⬜ | CI for build/test/lint already runs on PRs |
| 11 | Polish and portfolio | ⬜ | |

## Scope

| Feature | Viewer | Admin |
|---|---|---|
| Browse by category, sort by price or newest arrival | Yes | Yes |
| Product page with 1 to 5 images | View | Add, reorder, delete |
| Cart | Yes | Yes |
| Place order with phone and address, cash on delivery | Yes, signed in | No |
| Order history and status | Own orders | All orders, change status |
| Manage categories and products | No | Yes |

Out of scope for v1: online payment, reviews, coupons, wishlists and email marketing. The design keeps each of these addable later without schema rewrites.

## Architecture and stack

The React app lives on Vercel and talks to an ASP.NET Core API on a free container host; the API owns the database and signs image uploads, while browsers upload images straight to Cloudinary. Free-tier quotas change often, so confirm current limits when opening each account.

| Layer | Pick | Free hosting | Fallback |
|---|---|---|---|
| Frontend | React + Vite + TypeScript, React Router, TanStack Query, Tailwind CSS | Vercel Hobby, auto-deploy from GitHub | Netlify or Cloudflare Pages |
| Backend | ASP.NET Core Web API (.NET 10 LTS), EF Core | Render free web service (Docker) or Azure App Service F1 | Koyeb free; or Vercel serverless functions if .NET is dropped |
| Database | PostgreSQL via Npgsql | Neon free tier | Supabase Postgres |
| Images | Signed direct upload, CDN delivery with resizing | Cloudinary free plan | Supabase Storage |
| Auth | Google Identity Services button; API verifies the Google ID token and issues its own JWT with a role | Google Cloud OAuth client (free) | None needed |
| CI/CD | GitHub Actions for build and test; hosts deploy on push to main | GitHub (free for public repos) | None needed |

Repo layout (one public monorepo): `/api` (Shop.Api, Shop.Tests), `/web`, `/.github/workflows`, `/docs`, `/knowledge-base`, `README.md`.

## Core data model

Implemented in `api/Shop.Api/Domain` and `api/Shop.Api/Data` (Step 1). Tables are singular PascalCase per the SQL standards; enums are stored as strings.

| Entity | Key fields | Rules |
|---|---|---|
| User | Id, GoogleSub, Email, Name, Role, CreatedAt | Role is Admin when the email is on a configured allowlist, otherwise Viewer. GoogleSub and Email unique. |
| Category | Id, Name, Slug, SortOrder | Name and slug unique |
| Product | Id, CategoryId, Name, Slug, Description, Price, Sizes, IsActive, CreatedAt | CreatedAt drives newest-arrival sort; only active products are public. Slug unique. Indexed on CategoryId, Price, CreatedAt. |
| ProductImage | Id, ProductId, Url, PublicId, SortOrder | 1 to 5 per active product; SortOrder 0 is the cover |
| Order | Id, UserId, Phone, Address, Note, Status, Total, CreatedAt | Status: Pending, Confirmed, Shipped, Delivered, Cancelled |
| OrderItem | Id, OrderId, ProductId, ProductName, Size, UnitPrice, Quantity | Name and price copied at order time so later edits don't change old orders |

Implementation choices made in Step 1: ids are `int`; `Price`/`Total`/`UnitPrice` are `numeric(12,2)` (so fractional prices need no migration, even though the default is whole taka); `Sizes` is a Postgres `text[]`; timestamps are UTC; Order→User and OrderItem→Product use `Restrict` deletes (deactivate products, never hard-delete ordered ones); ProductImage→Product and OrderItem→Order cascade.

## Step order and parallel work

12 steps in 7 waves. A wave starts once the steps it needs are done; the steps inside a wave don't depend on each other. Step 9 is where the store can take real orders.

| Wave | Steps |
|---|---|
| 1 | 0 |
| 2 | 1, 2 (parallel) |
| 3 | 3, 4 (parallel; add the admin policy to Step 4 write endpoints once Step 3 lands) |
| 4 | 5, 6 (parallel) |
| 5 | 7, 8 (parallel) |
| 6 | 9 |
| 7 | 10, 11 (parallel) |

## Steps in detail

### Step 0 — Accounts and repo setup
Needs: nothing.
- [x] Create the public GitHub monorepo with `api/` and `web/` folders and a README stub.
- [ ] 🔒 Open accounts: Vercel (linked to GitHub), Neon, Cloudinary, and the backend host.
- [ ] 🔒 Google Cloud: OAuth consent screen and a Web OAuth client ID; add `http://localhost:5173` and the Vercel domain as authorized JavaScript origins.
- [ ] 🔒 Keep every key and connection string in a password manager, never in the repo.
- [x] CI workflow (`.github/workflows/ci.yml`) builds and tests the API and lints and builds the web app. *(Held out of the Step 1 PR: the git login lacked the GitHub `workflow` scope, so the file is still untracked locally. Add it in its own PR once the scope is granted or via the web UI.)*

Decide: backend host (Render vs Azure F1); whether to keep a .NET API at all. **Done when:** all accounts exist and the repo builds an empty CI run.

### Step 1 — Backend skeleton (.NET API)
Needs: Step 0. Parallel with Step 2.
- [x] `dotnet new webapi`, EF Core with Npgsql, entities above, first migration (`Data/Migrations/*_InitialCreate`).
- [x] `ConnectionStrings__Default` read from configuration (fails fast with a clear message if missing).
- [ ] Remaining settings read from environment variables: `Jwt__Key`, `Google__ClientId`, `Cloudinary__*`, `Admin__Emails` — bound by Steps 3 and 5 when they are first used.
- [x] CORS, ProblemDetails, OpenAPI in development.
- [x] `/health` checks the database: 200 `{"status":"healthy"}` or 503 `{"status":"unhealthy"}`.
- [x] Seed categories (Men, Women, Kids, Accessories) via migration data.
- [x] Dockerfile.
- [ ] 🔒 Deploy to the host and apply the migration to Neon.

Decide: minimal APIs or controllers (open; minimal APIs used so far); run migrations at startup or as a CI step (**open — must be settled before first deploy**). **Done when:** `/health` returns 200 from the public URL with the database connected.

Local setup: `dotnet user-secrets set "ConnectionStrings:Default" "<neon string>"` in `api/Shop.Api`, then `dotnet ef database update --project Shop.Api` from `api/` (local tool restored with `dotnet tool restore`).

### Step 2 — Frontend skeleton (React)
Needs: Step 0. Parallel with Step 1.
- [x] Vite + React + TypeScript, Tailwind, React Router with routes `/`, `/c/:slug`, `/p/:slug`, `/cart`, `/checkout`, `/orders`, `/admin/*`.
- [x] Layout: header with logo, cart link, sign-in button; footer. *(Category menu and real cart count wait for Steps 4 and 8.)*
- [x] API client reading `VITE_API_URL`; TanStack Query.
- [x] `vercel.json` rewrite for deep links.
- [ ] 🔒 Deploy to Vercel.

Decide: plain Tailwind or a component kit such as shadcn/ui (plain Tailwind so far); cart state with Zustand or React context (open until Step 8). **Done when:** the Vercel URL shows the layout and displays the API health status.

### Step 3 — Google SSO and roles
Needs: Steps 1 and 2.
- Frontend: `@react-oauth/google` sign-in button returns a Google ID token.
- API: `POST /api/auth/google` validates the token with `GoogleJsonWebSignature.ValidateAsync` (audience = your client ID), upserts the user, sets Admin role from the email allowlist, returns a short-lived JWT.
- Admin authorization policy, `GET /api/me`, auth context in React, protected routes, sign-out.

Decide: bearer token in memory/localStorage (simplest across vercel.app and the API domain) or an httpOnly cookie (needs SameSite=None and Secure); refresh tokens or re-login on expiry. **Done when:** you sign in on the live site, the admin email sees the Admin menu, and a viewer gets 403 from admin endpoints.

### Step 4 — Catalog API
Needs: Step 1. Can run alongside Step 3.
- Public: `GET /api/categories`, `GET /api/products?category=&sort=newest|price_asc|price_desc&page=&pageSize=`, `GET /api/products/{slug}`; only active products.
- Admin: create, update, deactivate for categories and products; slug generation; validation.
- Indexes on CategoryId, Price and CreatedAt *(done in Step 1)*; response DTOs instead of entities.
- Integration tests for filter, sort and paging.

Decide: soft delete or IsActive only; numbered pages or load-more; how sizes are stored *(text[] chosen)*. **Done when:** seed data sorts and filters correctly through Swagger and the tests pass.

### Step 5 — Product image upload
Needs: Step 4. Parallel with Step 6.
- Admin-only endpoint returns Cloudinary signed upload parameters; the browser uploads directly to Cloudinary.
- `POST /api/products/{id}/images` saves URL and public ID and rejects a sixth image; delete also removes the Cloudinary asset; reorder sets SortOrder.
- A product can be a draft with no images but cannot be activated until it has at least one.
- Thumbnails via Cloudinary URL transforms (width, `f_auto`, `q_auto`); check file type and size in the browser before upload.

Decide: direct-to-Cloudinary (default) or uploading through the API. **Done when:** an admin can add up to five images, the sixth is blocked, and deleted images disappear from Cloudinary.

### Step 6 — Storefront UI
Needs: Steps 2 and 4. Parallel with Step 5.
- Product grid with cover image, name and price; category menu; sort dropdown (Newest, Price low to high, Price high to low).
- Category, sort and page in the URL query string so links are shareable.
- Product page with image gallery and thumbnails, description, size picker and Add to cart.
- Mobile-first layout, lazy-loaded images, loading skeletons and empty states.

**Done when:** browsing, filtering and sorting work at phone width on the live site.

### Step 7 — Admin panel UI
Needs: Steps 3 and 5.
- Guarded `/admin` area: categories table with add, edit, delete; products table with search and an active toggle.
- Product form (name, category, price, description, sizes) with an image manager: upload with progress, drag to reorder, delete, image count against the limit of 5.

Decide: react-hook-form + zod (default) or an admin framework such as react-admin. **Done when:** an admin can create and publish a product with images entirely from the live site.

### Step 8 — Cart
Needs: Step 6.
- Cart store saved to localStorage (product, size, quantity); header badge; cart page with quantity change, remove and subtotal.
- `POST /api/cart/quote` re-checks prices and availability so changed prices and removed products are flagged before checkout.

Decide: client-only cart (default) or a server cart per user. **Done when:** the cart survives a reload and a deactivated product is flagged in it.

### Step 9 — Checkout and orders (cash on delivery)
Needs: Steps 3 and 8.
- Checkout requires sign-in and keeps the cart through the login redirect.
- Form: name (prefilled from Google), phone with format validation, address, optional note; cash on delivery is the only payment method.
- `POST /api/orders` recalculates every price from the database, copies item names and prices into OrderItems, and saves in one transaction; the button is disabled while submitting to stop duplicates.
- Order confirmation page and My orders page; admin orders list filtered by status with a status change action.

Decide: delivery charge rules; whether the admin gets a notification for new orders. **Done when:** a viewer places an order on the live site, the admin moves it to Confirmed, and the viewer sees the new status.

### Step 10 — Hardening and CI/CD
Needs: Steps 7 and 9.
- GitHub Actions on every pull request: `dotnet build`, `dotnet test`, web lint and build *(already in place)*. Hosts deploy main automatically; Vercel adds preview builds per PR.
- Rate limiting on sign-in and order endpoints, validation on every input, security headers, Serilog logging to the host console.
- Database backup routine (scheduled `pg_dump` from a GitHub Action, or the provider's restore window) and one test restore.
- A friendly waking-up message in the UI for the free host's cold start *(basic version in `HealthStatus`)*; an uptime monitor on `/health`.

**Done when:** CI is green, no secret lives in the repo, and a restore has been tested once.

### Step 11 — Polish and portfolio
Needs: Step 9. Parallel with Step 10.
- Realistic seed products and photos you have rights to use; favicon, page titles and share previews.
- README with architecture, screenshots, the live link and a short demo video of the admin flow (never public admin credentials).
- Lighthouse pass for performance and accessibility; unit tests for order totals and status rules.

**Done when:** someone new can understand the project from the README in two minutes and place a test order.

## Credentials and configuration

Nothing is committed. Locally use `dotnet user-secrets` (API) and `web/.env.local` (web); in production set host environment variables.

| Setting | Source | Used by | Secret? |
|---|---|---|---|
| `ConnectionStrings__Default` | Neon pooled connection string, `sslmode=require` | API (Step 1) | Yes |
| `Jwt__Key` | Self-generated, 32+ random bytes | API (Step 3) | Yes |
| `Google__ClientId` / `VITE_GOOGLE_CLIENT_ID` | Google Cloud OAuth Web client | API and web (Step 3) | No |
| `Cloudinary__CloudName`, `__ApiKey`, `__ApiSecret` | Cloudinary dashboard | API (Step 5) | `ApiSecret` yes |
| `Admin__Emails` | Your admin Gmail address(es) | API (Step 3) | Keep out of git |
| `Cors__Origins__0` | Vercel URL | API | No |
| `VITE_API_URL` | Render URL | Web | No |
| `DATABASE_URL` (Actions secret) | Neon, ideally a read-only role | Backup job (Step 10) | Yes |

## Free-tier limits and risks

| Risk | Effect | Mitigation |
|---|---|---|
| Free API host sleeps when idle | Slow first request | Waking-up message in the UI; uptime ping only if the host's terms allow it; Azure F1 as an alternative |
| Vercel Hobby is for non-commercial use | Fine for a portfolio demo, not for a shop taking real orders | Before real sales, move the frontend to a host whose free plan allows commercial use, or upgrade |
| Free quotas or terms change | Service stops or starts billing | Re-check limits in Step 0 and every few months; avoid adding a card where you can |
| Database pauses or hits its storage cap | Site errors | Neon scales back up on request; keep images out of the database; small backups via GitHub Actions |
| Image storage and bandwidth quota | Images stop loading | Max 5 images, browser-side size limit, Cloudinary `f_auto` and `q_auto` |
| Fake cash-on-delivery orders | Wasted deliveries | Sign-in required, rate limits, a confirmation call before shipping, admin can cancel |
| Google consent screen left in Testing | Only listed test users can sign in | Publish the consent screen; check whether Google asks for verification |

## Open decisions

| Decision | Settle in | Default assumed | Current state |
|---|---|---|---|
| Keep a separate .NET API, or all-JavaScript on Vercel functions | Step 0 | Keep .NET | Keeping .NET |
| Backend host | Step 0 | Render free web service with Docker | Open 🔒 |
| Can viewers browse and fill the cart without signing in | Step 3 | Yes; sign-in only at checkout | Open |
| Token storage | Step 3 | Bearer JWT, short expiry, re-login on expiry | Open |
| Currency and price format | Step 4 | Bangladeshi taka, whole numbers | Columns are `numeric(12,2)`; display format open |
| Clothing sizes | Step 4 | A size list per product, no per-size stock | `text[]` on Product |
| Stock tracking | Step 4 | In-stock flag only | Open |
| Delivery charge | Step 9 | A flat fee, editable in settings | Open |
| New-order alerts for the admin | Step 9 | Admin orders page with a Pending count; email later | Open |
| Custom domain | Step 11 | Free vercel.app subdomain | Open |
| Run migrations at startup or as a CI step | Step 1 | — | **Open — settle before first deploy** |

## Maintaining this plan

- When a step (or part of one) is finished, tick its checklist items, update the **Progress at a glance** row, and add a **Change log** entry in the same pull request as the work.
- When the plan itself changes (scope, stack, order, a decision settled), edit the relevant section and log the reason.

## Change log

| Date | Change |
|---|---|
| 2026-10-11 | Created this Markdown version from the PDF. Recorded progress: scaffold (Steps 0–2) already present. |
| 2026-10-11 | Step 1 code completed on `feature/backend-data-model`: EF Core + Npgsql, six entities, `InitialCreate` migration with seed categories, DB-checking `/health` (200/503), in-memory test factory and 12 passing tests. Nullable warnings treated as errors. Added the credentials table and open decision "migrations at startup vs CI step". |
| 2026-10-11 | Pushing `ci.yml` was rejected (token lacks `workflow` scope), so CI was left out of the Step 1 PR; tracked under Step 0. |
