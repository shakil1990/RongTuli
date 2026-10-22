# RongTuli — Build Plan

Living version of *Simple E-commerce Build Plan.pdf* (Oct 11, 2026). The PDF is the original; **this file is the source of truth** and is updated whenever a step is completed or the plan changes (see [Change log](#change-log)).

A cash-on-delivery clothing store with a React storefront, an ASP.NET Core API, Google sign-in for the two admins and phone-OTP checkout for customers, built in 13 steps on free tiers. Steps in the same wave can run in parallel, and every step ends with something deployed and testable on the live site.

Status legend: ✅ done · 🟡 partly done · ⬜ not started · 🔒 needs your action (account / credentials)

## Progress at a glance

| Step | Title | Status | Notes |
|---|---|---|---|
| 0 | Accounts and repo setup | 🟡 | Repo, README, CI workflow done. All accounts open (Vercel, Neon, Render, Cloudinary). Google OAuth client created (project `rongtuli`, consent screen in Testing with both admins as test users). No billing account needed. |
| 1 | Backend skeleton (.NET API) | ✅ | Code done: EF Core + Npgsql, entities, `InitialCreate` migration, seed categories, DB-checking `/health`. Left: env-var config for Jwt/Google/Cloudinary/Admin (added by Steps 3 and 5). Deployed to Render (https://rongtuliapi.onrender.com), `/health` healthy against Neon. |
| 2 | Frontend skeleton (React) | ✅ | Vite + TS + Tailwind, router with all routes, layout, API client, TanStack Query, `vercel.json` done; lint and build verified. Deployed to Vercel at https://rong-tuli-peach.vercel.app. Live site shows the API health status (`VITE_API_URL` set to the Render URL). Category menu and cart count added in Steps 6 and 8. |
| 3 | Google SSO (admin sign-in) | ✅ | Merged (PR #10): `POST /api/auth/google`, JWT bearer, `GET /api/me`, React auth context, `RequireAdmin` on `/admin/*`. 18 new tests (95 API, 63 web). Verified on the live site 2026-10-22: an admin signed in. |
| 4 | Catalog API | ✅ | Public and admin endpoints with 24 new integration tests (36 total passing); public endpoints verified against Neon. Admin endpoints answer 401 until Step 3 adds JWT sign-in. |
| 5 | Product image upload | 🟡 | Code done: signed direct upload behind `IImageStore`, add/delete/reorder endpoints, the 5-image cap, the no-image-no-activation rule, and Cloudinary URL transforms on the storefront. Left: drive it from the live site, which needs Step 7. |
| 6 | Storefront UI | ✅ | Design tokens with dark mode, category menu, grid, sort and paging in the URL, product page with gallery and size picker. Images now served through Cloudinary transforms (Step 5); the initials placeholder covers a product with no photograph. |
| 7 | Admin panel UI | 🟡 | Merged to `develop` (PR #12): `/admin/products` list with search, paging and a publish switch; create/edit form; image manager (upload, reorder, make cover, delete, count against 5); `/admin/categories` add/edit/delete. Admin product list now returns `isActive`. 15 new web tests (78 web, 95 API). Left: create and publish a product with images on the live site. |
| 8 | Cart | ✅ | localStorage cart behind `useCart`, header badge, cart page, and `POST /api/cart/quote` re-pricing with unavailable/price-changed/size flags. 34 web unit tests, 17 new API tests. |
| 9 | Checkout and orders (COD) | 🟡 | Code on `feature/checkout-orders`: `POST /api/orders` behind a verified-phone policy, repriced through the cart quote's own rules and refused (409) if a price moved since it was shown; delivery area chosen at checkout — Inside Dhaka City ৳80 (default) or Outside ৳150 (`Orders__InsideDhakaFee`, `Orders__OutsideDhakaFee`); customer `GET /api/orders` and `/api/orders/{id}`; admin list by status, counts, detail and status changes with fixed transition rules. Web: full checkout, confirmation page, `/orders` lookup by phone, admin Orders tab with a Pending count. Migration `AddOrderDelivery`. 63 new API tests (224), 22 new web tests (122). Left: apply the migration to Neon, merge, then place an order on the live site. |
| 10 | Hardening and CI/CD | ⬜ | CI for build/test/lint already runs on PRs |
| 11 | Polish and portfolio | ⬜ | |
| 12 | Phone verification (OTP) | 🟡 | Code on `feature/phone-otp`: `POST /api/phone/otp` and `/api/phone/verify`, hashed codes, per-phone cooldown and hourly/daily caps, per-IP limits, a 30-minute verified-phone token with no role, a pruner, the dev SMS sender, and the verify panel on `/checkout`. Migration `AddPhoneVerificationAndGuestOrders`. 66 new API tests (161), 22 new web tests (100). Migration applied to Neon; merged (PR #13). Left: an SMS provider 🔒 for the live test. |

## Scope

| Feature | Viewer | Admin |
|---|---|---|
| Browse by category, sort by price or newest arrival | Yes | Yes |
| Product page with 1 to 5 images | View | Add, reorder, delete |
| Cart | Yes | Yes |
| Place order with phone and address, cash on delivery | Yes, after verifying the phone by OTP — no account | No |
| Order history and status | Confirmation page, then lookup by re-verifying the same phone | All orders, change status |
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
| Admin auth | Google Identity Services button; API verifies the Google ID token and issues its own JWT with a role | Google Cloud OAuth client (free) | None needed |
| Customer verification | Phone OTP: the API texts a code and issues a short-lived verified-phone token. No customer accounts | SMS gateway — provider not chosen yet (Step 12); a dev sender logs the code locally and in CI | Firebase Phone Auth; or a manual confirmation call while a provider is being chosen |
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
| Order | Id, UserId?, CustomerName, Phone, Address, Note, Status, DeliveryFee, Total, CreatedAt | Status: Pending, Confirmed, Shipped, Delivered, Cancelled. UserId is null for customer orders — customers have no account; the Phone must be OTP-verified before the order is accepted (Step 12). Total is the items plus DeliveryFee, the fee in force when the order was placed. Indexed on (Phone, CreatedAt) for the customer's lookup. |
| OrderItem | Id, OrderId, ProductId, ProductName, Size?, UnitPrice, Quantity | Name and price copied at order time so later edits don't change old orders. Size is null for a one-size product. |
| PhoneVerification | Id, Phone, CodeHash, ExpiresAt, AttemptCount, ConsumedAt, CreatedAt | Added in Step 12. One row per code sent, so the send caps count rows rather than keeping a `SendCount`; only the newest row for a phone can be verified. The code is stored only as an HMAC (key derived from `Jwt__Key`) and never returned or logged, except by the dev SMS sender. `AttemptCount` is the concurrency token. Indexed on (Phone, CreatedAt) and CreatedAt; rows are pruned after a day. |

Implementation choices made in Step 1: ids are `int`; `Price`/`Total`/`UnitPrice` are `numeric(12,2)` (so fractional prices need no migration, even though the default is whole taka); `Sizes` is a Postgres `text[]`; timestamps are UTC; Order→User and OrderItem→Product use `Restrict` deletes (deactivate products, never hard-delete ordered ones); ProductImage→Product and OrderItem→Order cascade.

Step 12 adds a migration (`AddPhoneVerificationAndGuestOrders`): the `PhoneVerification` table, `Order.UserId` becomes nullable (the Order→User foreign key stays `Restrict` but optional) and `Order.CustomerName` is added, since a guest order has no account to take the name from. The `User` table stays in use for admins only until a customer-account feature is ever wanted.

Step 9 adds `AddOrderDelivery`: `Order.DeliveryZone` (`InsideDhaka` or `OutsideDhaka`, stored as text; earlier rows get `InsideDhaka`), `Order.DeliveryFee` (`numeric(12,2)`, earlier rows get 0), `OrderItem.Size` becomes nullable, and an index on `Order (Phone, CreatedAt)`.

## Step order and parallel work

13 steps in 7 waves. A wave starts once the steps it needs are done; the steps inside a wave don't depend on each other. Step 9 is where the store can take real orders. Step 12 was added on 2026-10-21 and keeps its own number instead of renumbering the rest; it runs in wave 5 because Step 9 depends on it.

| Wave | Steps |
|---|---|
| 1 | 0 |
| 2 | 1, 2 (parallel) |
| 3 | 3, 4 (parallel; add the admin policy to Step 4 write endpoints once Step 3 lands) |
| 4 | 5, 6 (parallel) |
| 5 | 7, 8, 12 (parallel) |
| 6 | 9 |
| 7 | 10, 11 (parallel) |

## Steps in detail

### Step 0 — Accounts and repo setup
Needs: nothing.
- [x] Create the public GitHub monorepo with `api/` and `web/` folders and a README stub.
- [x] 🔒 Open accounts: Vercel (linked to GitHub), Neon, Render and Cloudinary. Cloudinary cloud name `mxtqngum`; the Root API key and secret are set as `Cloudinary__CloudName/__ApiKey/__ApiSecret` on Render. Local user-secrets added when Step 5 started, with `Cloudinary:Folder` pointed at `rongtuli/dev/products` so development uploads stay out of the production folder.
- [x] 🔒 Google Cloud: project `rongtuli`, consent screen (External, Testing, both admins as test users) and a Web OAuth client ID with `http://localhost:5173` and `https://rong-tuli-peach.vercel.app` as authorized JavaScript origins and no redirect URIs. No billing account or card is needed for any of it. Vercel preview URLs cannot be authorized (changing hostnames, no wildcards), so sign-in works on localhost and the production domain only.
- [ ] 🔒 Keep every key and connection string in a password manager, never in the repo.
- [x] CI workflow (`.github/workflows/CI.yml`) builds and tests the API and lints and builds the web app. *(Merged in PR #5 after being held out of the Step 1 PR.)*

Decide: backend host (Render vs Azure F1); whether to keep a .NET API at all. **Done when:** all accounts exist and the repo builds an empty CI run.

### Step 1 — Backend skeleton (.NET API)
Needs: Step 0. Parallel with Step 2.
- [x] `dotnet new webapi`, EF Core with Npgsql, entities above, first migration (`Data/Migrations/*_InitialCreate`).
- [x] `ConnectionStrings__Default` read from configuration (fails fast with a clear message if missing).
- [x] Remaining settings read from environment variables: `Jwt__Key`, `Google__ClientId`, `Admin__Emails` — bound in Step 3. `Jwt__Key` is validated at startup and stops the API if missing, because the bearer scheme needs it for the first request; `Google__ClientId` is checked when sign-in is first used. `Cloudinary__*` is bound (Step 5), and is checked when the image store is first used rather than at startup, so the API still boots without image credentials.
- [x] CORS, ProblemDetails, OpenAPI in development.
- [x] `/health` checks the database: 200 `{"status":"healthy"}` or 503 `{"status":"unhealthy"}`.
- [x] Seed categories (Men, Women, Kids, Accessories) via migration data.
- [x] Dockerfile.
- [x] Apply the migration to Neon (`InitialCreate` applied; local `/health` returned 200 against Neon).
- [x] Deploy to Render: https://rongtuliapi.onrender.com. Settings: Docker, branch `develop`, Root Directory `api`, Dockerfile Path `api/Shop.Api/Dockerfile` (relative to repo root, must include the filename), build context `api`, health check `/health`, free instance. Env vars: `ConnectionStrings__Default` (Npgsql key-value string, value only, no key name or quotes, not the `postgresql://` URI) and `Cors__Origins__0`.

Decide: minimal APIs or controllers (open; minimal APIs used so far); run migrations at startup or as a CI step (**open — must be settled before first deploy**). **Done when:** `/health` returns 200 from the public URL with the database connected.

Local setup: `dotnet user-secrets set "ConnectionStrings:Default" "<neon string>"` in `api/Shop.Api`, then run `dotnet tool restore` and `dotnet ef database update --project Shop.Api --connection "<neon string>"` from `api/`. The `--connection` flag is required: the design-time factory (`ShopDbContextFactory`) uses a placeholder localhost database and does not read user-secrets.

### Step 2 — Frontend skeleton (React)
Needs: Step 0. Parallel with Step 1.
- [x] Vite + React + TypeScript, Tailwind, React Router with routes `/`, `/c/:slug`, `/p/:slug`, `/cart`, `/checkout`, `/orders`, `/admin/*`.
- [x] Layout: header with logo, cart link, sign-in button; footer. *(Category menu and live cart count added in Steps 6 and 8; the sign-in button stays disabled until Step 3.)*
- [x] API client reading `VITE_API_URL` (set in Vercel to the Render URL); TanStack Query. The live site shows the API health status.
- [x] `vercel.json` rewrite for deep links.
- [x] Deploy to Vercel: https://rong-tuli-peach.vercel.app (Hobby plan, root directory `web`, Vite preset).

Decided: plain Tailwind, no component kit; cart state in a React context (Step 8). **Done when:** the Vercel URL shows the layout and displays the API health status.

### Step 3 — Google SSO (admin sign-in)
Needs: Steps 1 and 2. **Admin-only.** Customers never sign in — they verify a phone number instead (Step 12), so nothing in the shopping flow depends on this step.
- [x] Frontend: `@react-oauth/google` sign-in button on `/admin` (`components/AdminSignIn.tsx`), not advertised on the storefront — the header's disabled Sign in button is gone, replaced by `AdminBadge`, which shows the signed-in admin and a Sign out only once there is one.
- [x] API: `POST /api/auth/google` validates the token with `GoogleJsonWebSignature.ValidateAsync` (audience = the client ID), upserts the user, sets the Admin role from the `Admin__Emails` allowlist, returns the JWT. The role is re-read from configuration on every sign-in, so removing an address takes effect without a database edit.
- [x] A Google account that is not on the allowlist is recorded as a Viewer but gets **403 from the sign-in itself** and no token, so it never reaches an admin endpoint (which answer 401 without one). This replaces the original "403 from admin endpoints" wording, which could not happen once sign-in is refused.
- [x] Admin authorization policy switched from the placeholder scheme to JWT bearer in `Security/AdminAuthorization.cs` (the `Admin` policy itself unchanged), `GET /api/me`, auth context in React (`web/src/auth/`), `RequireAdmin` on `/admin/*`, sign-out.
- [x] 18 new tests (95 API, 63 web). `ShopApiFactory(useRealAuthentication: true)` runs the sign-in tests against the real bearer scheme, so a test signs in and then uses the token it was issued.
- [x] Verify on the live site once `Jwt__Key`, `Google__ClientId` and `Admin__Emails__0/1` are set on Render and `VITE_GOOGLE_CLIENT_ID` on Vercel. *(An admin signed in on the live site on 2026-10-22.)*

**Settled:** bearer JWT in `localStorage`, 8-hour expiry, no refresh tokens; re-login afterwards. **Done when:** you sign in on the live site, both admin emails reach the admin panel, and a non-allowlisted Google account is refused at sign-in.

⚠️ `Jwt__Key` is the one setting the API cannot start without — the bearer scheme needs it to validate the first request, so unlike `Cloudinary__*` and `Google__ClientId` it is not checked lazily. Set it on Render **before** this branch reaches `develop`, or the service fails its health check on deploy.

### Step 4 — Catalog API
Needs: Step 1. Can run alongside Step 3.
- [x] Public: `GET /api/categories`, `GET /api/products?category=&sort=newest|price_asc|price_desc&page=&pageSize=`, `GET /api/products/{slug}`; only active products.
- [x] Admin (`/api/admin/...`, behind the `Admin` policy that Step 3 binds to JWT): create, update, deactivate for categories and products; slug generation; validation.
- [x] Indexes on CategoryId, Price and CreatedAt *(done in Step 1)*; response DTOs instead of entities.
- [x] Integration tests for filter, sort and paging.

Decided: IsActive only (DELETE deactivates a product; a category with products returns 409); numbered pages; how sizes are stored *(text[] chosen)*. **Done when:** seed data sorts and filters correctly through Swagger and the tests pass.

### Step 5 — Product image upload
Needs: Step 4. Parallel with Step 6.
- [x] Admin-only endpoint returns Cloudinary signed upload parameters; the browser uploads directly to Cloudinary. `POST /api/admin/products/{productId}/images/upload-ticket` signs `folder` and `timestamp` only, so a ticket cannot be reused to upload anywhere else in the account, and it answers 409 when the product is already full.
- [x] `POST /api/admin/products/{productId}/images` saves URL and public ID and rejects a sixth image (409); `DELETE .../images/{imageId}` also removes the Cloudinary asset; `PUT .../images/order` sets SortOrder.
- [x] A product can be a draft with no images but cannot be activated until it has at least one — so a new product is always created as a draft, and the last image of an active product cannot be deleted (409).
- [x] Thumbnails via Cloudinary URL transforms (`c_limit,f_auto,q_auto,w_…` in `web/src/lib/cloudinary.ts`, wired into `ProductThumb` as `src` plus a `srcSet` ladder); file type and size are checked in the browser against limits the upload ticket carries, so the rules live on the server only.
- [x] Upload into `rongtuli/products` (the folder exists in the Media Library). Keep the folder prefix configurable: the free plan has one product environment, so local development and production share the `mxtqngum` account and dev uploads should land under a separate prefix (`Cloudinary__Folder`, local user-secrets point at `rongtuli/dev/products`). Signing sits behind `IImageStore`, which the tests fake, so CI needs no Cloudinary credentials.
- [x] The browser reports back where it uploaded, so the API trusts the pair only after checking the public ID's characters and that both URL and public ID sit in this account's configured folder.
- [ ] Verify on the live site — needs the admin UI from Step 7 to drive it.

Decided: direct-to-Cloudinary — no image bytes pass through the API, which keeps the free container host out of the upload path. Deletes call Cloudinary **before** removing the row, so an unreachable host leaves the catalog unchanged and answers 502 rather than leaving a row pointing at a deleted image. Sort orders are kept as a gapless sequence from 0, and 0 is the cover.

**Done when:** an admin can add up to five images, the sixth is blocked, and deleted images disappear from Cloudinary. *(API and web helpers complete, covered by 24 new API tests and 15 new web tests; the end-to-end check waits on Step 7.)*

### Step 6 — Storefront UI
Needs: Steps 2 and 4. Parallel with Step 5.
- [x] Product grid with cover image, name and price; category menu; sort dropdown (Newest, Price low to high, Price high to low).
- [x] Category in the path (`/c/:slug`), sort and page in the query string so links are shareable.
- [x] Product page with image gallery and thumbnails, description, size picker and Add to cart.
- [x] Mobile-first layout, lazy-loaded images, loading skeletons and empty states.
- [x] Design tokens in `web/src/index.css`: semantic colours (surface, ink, accent) re-pointed under `prefers-color-scheme`, so dark mode needs no per-component classes. Contrast pairs checked against WCAG 2.1 AA.
- [x] Accessibility: skip link, breadcrumbs, labelled sort control, a radio group for sizes, visible focus rings, `aria-label` on icon-only controls.

Deviation from the house styling standard (styled-components): this app settled on Tailwind in Step 2, so shared class recipes live in `web/src/components/common/styles.ts` instead.

**Done when:** browsing, filtering and sorting work at phone width on the live site. *(Code complete and verified by build, lint and unit tests; the live check needs products in the database — see Step 7.)*

### Step 7 — Admin panel UI
Needs: Steps 3 and 5.
- [x] Guarded `/admin` area (`AdminLayout` under `RequireAdmin`, `/admin` redirects to `/admin/products`): categories list with add, edit, delete; products table with search, paging and a publish switch. `GET /api/admin/products` now returns `isActive` on each row (`ProductSummaryDto.IsActive`; always true on the storefront).
- [x] Product form (name, category, price, description, sizes) at `/admin/products/new` and `/admin/products/:id`. A new product is saved as a draft and the page moves to its editor, where images can be added and the product published.
- [x] Image manager: upload one file at a time with an "Uploading 2 of 3" status, ← / → and Make cover to reorder, delete with a confirm, and the count against the limit of 5. Deviations: per-file progress rather than a byte progress bar (`fetch` cannot report upload progress), and buttons rather than drag to reorder (keyboard and touch work with no drag library).
- [x] A 401 on any admin call drops the session (`useAdminCall`), so an expired token brings back the sign-in card.
- [ ] Create and publish a product with images on the live site.

Decided: neither react-hook-form + zod nor an admin framework — plain controlled forms. The browser only checks what it needs to build a request (name, category, a numeric price); the API's validation messages come back beside each field (`ApiError.fieldErrors`), so every limit is written down once, on the server. **Done when:** an admin can create and publish a product with images entirely from the live site.

### Step 8 — Cart
Needs: Step 6.
- [x] Cart store saved to localStorage (product, size, quantity); header badge; cart page with quantity change, remove and subtotal.
- [x] `POST /api/cart/quote` re-checks prices and availability so changed prices and removed products are flagged before checkout. Issue codes: `unavailable`, `price_changed`, `size_unavailable`.
- [x] A stored cart is treated as untrusted input: every line is validated and clamped on load, and storage failures (private mode, cleared site data) fall back to an empty cart rather than throwing.
- [x] Checkout is blocked while any line is unorderable; a changed price is shown but does not block.

Decided: client-only cart, re-priced by the server at the cart page and again at checkout. The cart lives in a React context (`web/src/cart/`) reached only through `useCart`, so it has no third-party dependency and one file to swap if it outgrows that. **Done when:** the cart survives a reload and a deactivated product is flagged in it. *(Both covered by unit and integration tests.)*

### Step 9 — Checkout and orders (cash on delivery)
Needs: Steps 8 and 12. (Step 3 only for the admin orders screen.)
- [x] No account and no login redirect. `/checkout` form: name, address, optional note; the phone is the one verified in step 1 of the page (Bangladeshi format, Step 12). Cash on delivery is the only payment method. A summary beside the form shows each line, the subtotal, the delivery fee and the total from `GET /api/checkout/options`.
- [x] The phone is verified inline by OTP (Step 12) before the order can be placed, so the cart survives — verification never leaves the page.
- [x] `POST /api/orders` sits behind the `VerifiedPhone` policy (a bearer token with a `phone` claim; an admin token gets 403) and answers a field error on `phone` if the form's number is not the token's. It reprices every line through `CartPricing` — the code the cart quote uses, extracted from `CartEndpoints` — copies names and prices into OrderItems, and saves the order and its lines in one `SaveChanges`. The button is disabled while submitting.
- [x] Each line carries the price the customer was shown: the web app sends the **quote's** prices, not the ones the cart stored, so a price change the customer has seen is accepted. If a price moves after that, or a line stops being orderable, the order is refused with 409 and the fresh quote, and the page re-prices so they can confirm the new total.
- [x] Order confirmation at `/orders/:id`, opened straight from the order the API returned, with the order number and status. `/orders` lists a phone's orders (newest 50) after re-verifying it; another phone's order answers 404. A header link "Orders" leads there.
- [x] Admin: an Orders tab (with the Pending count, refreshed every minute) listing orders by status with counts per status, paging, and an order page with the customer's details, a call link and buttons for the allowed next statuses. Transitions are fixed in `OrderStatusRules`: Pending → Confirmed or Cancelled, Confirmed → Shipped or Cancelled, Shipped → Delivered or Cancelled; Delivered and Cancelled are final (409 otherwise). Cancelling asks for confirmation.
- [x] The verified-phone token now lives in `sessionStorage` (`web/src/phone/`), shared by checkout, the confirmation page and the lookup, and dropped on expiry or any 401.
- [ ] Apply `AddOrderDelivery` to Neon **before** this reaches `develop` — every order endpoint reads or writes the new columns, though nothing that already runs touches them.
- [ ] Verify on the live site (needs `Sms__Provider=Log` on Render until a gateway is chosen, reading the code from the Render log).

Settled (2026-10-22): two delivery areas, chosen with a radio group on the checkout page — **Inside Dhaka City, ৳80, selected by default**, and **Outside Dhaka City, ৳150**. One must be chosen (the API answers 400 on `deliveryZone` otherwise); the zone and its fee are copied onto the order, and the admin reviews every order regardless (the order page shows the area beside the address to check on the confirmation call). The fees are configuration (`Orders__InsideDhakaFee`, `Orders__OutsideDhakaFee`), not an admin-editable setting yet; new-order alerts are the Pending count on the admin tab, email later; the same 30-minute verified-phone token is reused for order lookup and survives a reload in the same tab. Not done here and left to Step 10: rate limits on `POST /api/orders` (it already needs a verified phone, which the OTP caps protect). **Done when:** a customer places an order on the live site without an account, the admin moves it to Confirmed, and the customer sees the new status after verifying the phone. *(The whole sequence is covered end to end by `OrderApiTests.Customer_VerifiesByText_PlacesAnOrder_AndSeesTheStatusTheAdminSet` against the in-memory database.)*

### Step 10 — Hardening and CI/CD
Needs: Steps 7 and 9.
- GitHub Actions on every pull request: `dotnet build`, `dotnet test`, web lint and build *(already in place)*. Hosts deploy main automatically; Vercel adds preview builds per PR.
- Rate limiting on sign-in, OTP and order endpoints, validation on every input, security headers, Serilog logging to the host console. *(The OTP caps themselves ship with Step 12; this step is where they get reviewed end to end.)*
- Database backup routine (scheduled `pg_dump` from a GitHub Action, or the provider's restore window) and one test restore.
- A friendly waking-up message in the UI for the free host's cold start *(basic version in `HealthStatus`)*; an uptime monitor on `/health`.

**Done when:** CI is green, no secret lives in the repo, and a restore has been tested once.

### Step 11 — Polish and portfolio
Needs: Step 9. Parallel with Step 10.
- Realistic seed products and photos you have rights to use; favicon, page titles and share previews.
- README with architecture, screenshots, the live link and a short demo video of the admin flow (never public admin credentials).
- Lighthouse pass for performance and accessibility; unit tests for order totals and status rules.

**Done when:** someone new can understand the project from the README in two minutes and place a test order.

### Step 12 — Phone verification (OTP)
Needs: Step 1. Runs in wave 5 alongside Steps 7 and 8; Step 9 depends on it. Added 2026-10-21 — this replaces customer sign-in, which is why Step 3 is now admin-only.
- [x] `POST /api/phone/otp` takes a phone number, normalises it to E.164 (`+8801XXXXXXXXX`, operator prefixes 013–019, however it was typed), stores a **hashed** 6-digit code with a short TTL (5 minutes by default) and sends it by SMS. Sending a new code invalidates any live one for that phone (only the newest row can be verified).
- [x] `POST /api/phone/verify` takes the phone and the code, checks the hash in constant time, marks the row consumed and returns a short-lived verified-phone JWT — signed with the same `Jwt__Key`, carrying a `phone` claim and **no role**, so it can never reach an admin endpoint (tested: 403 against the real bearer scheme).
- [x] Abuse limits: max 5 attempts per code (counted under a concurrency token, so racing guesses cannot share one); per phone a 60-second resend cooldown, 3 sends an hour and 10 a day; per client IP 20 sends and 60 verifies an hour. A wrong, expired, used or exhausted code all get the same answer; a refused send answers 429 with `retryAfterSeconds`. The client IP is the last `X-Forwarded-For` entry (the one Render's proxy appends), so a caller cannot forge their way into another bucket. The code never appears in a response or an error message.
- [x] The send sits behind an `ISmsSender` interface. The development sender (`Sms__Provider=Log`, the default only in Development) writes the code to the log instead of sending it, so local work and CI need no provider account and burn no credit. Outside Development, an unset provider fails the send rather than logging codes. A provider that fails answers 502, and the failed send still counts against the caps.
- [x] Frontend: `/checkout` now holds a phone field that formats as typed (`01712-345678`), an inline Send code / Enter code panel, a resend countdown that also counts down a 429, an expiry countdown, and wrong/expired-code messages beside the field. The delivery form is a placeholder until Step 9. The verified token lives in page state only; Step 9 decides whether it survives a reload.
- [x] Rows are pruned once a day old (by then outside the daily cap window) by a background job that runs at startup and hourly — at startup because the free host sleeps after 15 idle minutes.
- [x] Applied `AddPhoneVerificationAndGuestOrders` to Neon (2026-10-22), before merging; the live API stayed healthy.
- [ ] Choose an SMS provider and implement its `ISmsSender`; until then the live site can only verify with `Sms__Provider=Log` on Render (codes read from the Render log).

Settled for now (2026-10-22), all configurable through `Otp__*`: TTL 5 minutes, the caps above, verified-phone token 30 minutes. Still to decide: SMS provider. (Step 9 settled that the same token is reused for order lookup.) **Done when:** a real phone receives a code on the live site, a wrong or expired code is rejected, the caps hold under repeated sends, and a verified phone can place an order.

## Credentials and configuration

Nothing is committed. Locally use `dotnet user-secrets` (API) and `web/.env.local` (web); in production set host environment variables.

| Setting | Source | Used by | Secret? |
|---|---|---|---|
| `ConnectionStrings__Default` | Neon pooled connection string, `sslmode=require` | API (Step 1) | Yes |
| `Jwt__Key` | Self-generated, 32+ random bytes, base64 (`openssl rand -base64 32`) | API (Step 3) — **required at startup** | Yes |
| `Google__ClientId` / `VITE_GOOGLE_CLIENT_ID` | Google Cloud OAuth Web client | API and web (Step 3) | No |
| `Cloudinary__CloudName`, `__ApiKey`, `__ApiSecret` | Cloudinary dashboard; cloud name `mxtqngum` | API (Step 5) | `ApiSecret` yes |
| `Cloudinary__Folder` | Self-chosen upload folder; defaults to `rongtuli/products`, locally `rongtuli/dev/products` | API (Step 5) | No |
| `Admin__Emails` | The two admin Gmail addresses | API (Step 3) | Keep out of git |
| `Sms__Provider` | `Log` (dev sender: codes go to the log, nothing is sent) until a gateway is chosen; defaults to `Log` only in Development | API (Step 12) | No |
| `Sms__ApiKey`, `Sms__SenderId` | SMS gateway account — provider not chosen yet, not read yet | API (Step 12) | `ApiKey` yes |
| `Otp__TtlMinutes` (5), `Otp__MaxAttempts` (5), `Otp__ResendCooldownSeconds` (60), `Otp__MaxSendsPerHour` (3), `Otp__MaxSendsPerDay` (10), `Otp__VerifiedTokenMinutes` (30), `Otp__SendsPerIpPerHour` (20), `Otp__VerifiesPerIpPerHour` (60) | Self-chosen tuning, non-secret; the defaults in brackets apply when unset | API (Step 12) | No |
| `Orders__InsideDhakaFee` (80), `Orders__OutsideDhakaFee` (150) | Self-chosen delivery fees in taka per area; copied onto each order, so a change affects only later orders | API (Step 9) | No |
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
| Fake cash-on-delivery orders | Wasted deliveries | Phone verified by OTP before the order is accepted (Step 12), rate limits, a confirmation call before shipping, admin can cancel |
| Google consent screen left in Testing | Only the listed test users can sign in — the two admins, which is all this project needs | Add both admin emails as test users; publishing is optional now that customers don't sign in |
| OTP abuse / SMS pumping | A burst of sends drains SMS credit or runs up a bill | Caps per phone, per IP and per day; one live code per phone; hashed codes; nothing sensitive in responses or logs; watch the send rate |
| SMS is not free in Bangladesh | Every order costs real money per message, and free tiers are thin or absent | Prepaid credit with no auto-billing; the dev sender covers local and CI; a manual confirmation call is the fallback if credit runs out |

## Open decisions

| Decision | Settle in | Default assumed | Current state |
|---|---|---|---|
| Keep a separate .NET API, or all-JavaScript on Vercel functions | Step 0 | Keep .NET | Keeping .NET |
| Backend host | Step 0 | Render free web service with Docker | **Settled: Render** (2026-10-18), account open and deployed. Sleeps after 15 min idle; move the backend to a paid instance before real sales |
| Can viewers browse and fill the cart without signing in | Step 3 | Yes; sign-in only at checkout | **Settled: yes** (2026-10-18, revised 2026-10-21). Browsing, the cart and `/api/cart/quote` are anonymous, and checkout is now gated on a phone OTP rather than a sign-in |
| How customers identify themselves | Step 3 | — | **Settled: phone OTP, no accounts** (2026-10-21). Google SSO is for the two admins only |
| SMS provider for OTP | Step 12 | A prepaid Bangladeshi gateway with an HTTP API | **Open — blocks the live test of Step 12, not the code** (the dev sender logs the code) |
| Verified-phone token lifetime | Step 12 | 30 minutes, re-verify afterwards | **Settled for now: the default** (2026-10-22), `Otp__VerifiedTokenMinutes`. OTP TTL 5 minutes and the send caps set the same way |
| How a customer sees past orders without an account | Step 9 | Lookup by re-verifying the same phone | **Settled: the default** (2026-10-22). `/orders` after verifying; the 30-minute verified-phone token is reused and kept in `sessionStorage` for the tab |
| Token storage | Step 3 | Bearer JWT, short expiry, re-login on expiry | **Settled: the default** (2026-10-22). Bearer JWT in `localStorage`, 8-hour lifetime, no refresh tokens; the web client drops the token and re-shows Sign in on any 401. A cookie was rejected because the web app and API sit on different sites (`vercel.app` / `onrender.com`), needing `SameSite=None; Secure` and credentialed CORS |
| Currency and price format | Step 4 | Bangladeshi taka, whole numbers | **Settled** (2026-10-18). Columns stay `numeric(12,2)`; `formatTaka` shows `৳` with grouped thousands and decimals only when a price has them |
| Clothing sizes | Step 4 | A size list per product, no per-size stock | `text[]` on Product |
| Stock tracking | Step 4 | In-stock flag only | Open |
| Delivery charge | Step 9 | A flat fee, editable in settings | **Settled** (2026-10-22): two areas, Inside Dhaka City ৳80 (default) and Outside Dhaka City ৳150, from `Orders__InsideDhakaFee` and `Orders__OutsideDhakaFee`; the zone and fee are stored on each order. An admin-editable setting can come later without a schema change |
| New-order alerts for the admin | Step 9 | Admin orders page with a Pending count; email later | **Settled: the default** (2026-10-22). Pending count on the admin Orders tab, refreshed every minute; email later |
| Custom domain | Step 11 | Free vercel.app subdomain | Open |
| Run migrations at startup or as a CI step | Step 1 | — | **Settled for now: manual** (2026-10-22). Applied by hand with `dotnet ef database update --connection "<neon string>"` from `api/` before the code that needs them reaches `develop`; nothing calls `Migrate()` at startup. Revisit in Step 10 alongside the backup routine |

## Maintaining this plan

- When a step (or part of one) is finished, tick its checklist items, update the **Progress at a glance** row, and add a **Change log** entry in the same pull request as the work.
- When the plan itself changes (scope, stack, order, a decision settled), edit the relevant section and log the reason.

## Change log

| Date | Change |
|---|---|
| 2026-10-11 | Created this Markdown version from the PDF. Recorded progress: scaffold (Steps 0–2) already present. |
| 2026-10-11 | Step 1 code completed on `feature/backend-data-model`: EF Core + Npgsql, six entities, `InitialCreate` migration with seed categories, DB-checking `/health` (200/503), in-memory test factory and 12 passing tests. Nullable warnings treated as errors. Added the credentials table and open decision "migrations at startup vs CI step". |
| 2026-10-11 | Pushing `ci.yml` was rejected (token lacks `workflow` scope), so CI was left out of the Step 1 PR; tracked under Step 0. |
| 2026-10-18 | Step 2: frontend deployed to Vercel (https://rong-tuli-peach.vercel.app). Vercel account created. Production branch still to be switched to `main`. |
| 2026-10-18 | Step 0: CI workflow is in git (PR #5, `CI.yml`); removed the stale "untracked" notes. |
| 2026-10-18 | Step 0/1: Neon account created; `InitialCreate` applied to Neon and local `/health` returns 200 against it. Documented that `dotnet ef database update` needs `--connection`. Seed rows not yet verified in the Neon console. |
| 2026-10-18 | Settled backend host: Render free web service (Docker). Must leave the free tier before real sales because of the 15-minute idle sleep. |
| 2026-10-18 | Step 1: API deployed to Render and `/health` is healthy against Neon (local check also healthy). Recorded the working Render settings. Remaining for Step 2: set `VITE_API_URL` in Vercel. |
| 2026-10-18 | Step 2 done: `VITE_API_URL` set in Vercel; the live site shows the API healthy. Steps 1 and 2 marked done (Step 1 env vars for Jwt/Google/Cloudinary/Admin are bound in Steps 3 and 5; Step 2 category menu waits on Step 4). |
| 2026-10-18 | Step 4 done on `feature/catalog-api`: categories and products public API, admin CRUD, slug generation, validation, DTOs. Settled IsActive-only and numbered pages. Seed categories confirmed in Neon via `GET /api/categories`. Activating a product without images is not yet blocked (Step 5). |
| 2026-10-18 | Steps 6 and 8 done on `feature/storefront-ui-and-cart`. Storefront: design tokens with automatic dark mode, category menu, product grid, sort and paging in the URL, product page with gallery and size picker, skeletons and empty states. Cart: localStorage store behind `useCart`, header badge, cart page, and a new `POST /api/cart/quote`. Settled four open decisions (price format, cart state, client-only cart, anonymous browsing). Added vitest to the web app (34 tests) and 17 API tests, 53 in total. |
| 2026-10-18 | Added `CartQueries.ProductsForQuote` with tests that compile it against the Npgsql provider via `ToQueryString`. The endpoint tests use the in-memory provider, which accepts projections PostgreSQL would reject — the `text[]` `Sizes` column made that a real risk. |
| 2026-10-18 | Removed the unused scaffold sprite `web/public/icons.svg` and replaced the scaffold favicon with a RongTuli mark in the new palette. |
| 2026-10-21 | Step 0: Cloudinary account created (cloud name `mxtqngum`, Root API key) and `Cloudinary__CloudName/__ApiKey/__ApiSecret` set on Render; `rongtuli/products` created in the Media Library. Local user-secrets deferred to the day Step 5 starts. Step 5 is unblocked; the Google OAuth client is now the only remaining 🔒 item. |
| 2026-10-21 | Plan change: customers no longer sign in. Google SSO (Step 3) is now admin-only for the two admin emails, and checkout is gated on a phone number verified by OTP — new **Step 12 — Phone verification (OTP)**, which runs in wave 5 and blocks Step 9. Added the `PhoneVerification` entity, a nullable `Order.UserId` with `CustomerName`, `Sms__*` and `Otp__*` settings, OTP-abuse and SMS-cost risks, and five open decisions (SMS provider, token lifetime, order lookup, customer identity, consent-screen publishing). Existing step numbers were not shifted. |
| 2026-10-21 | Step 5 code done on `feature/product-image-upload`: signed direct-to-Cloudinary upload behind `IImageStore` (faked in tests, so CI needs no account), add / delete / reorder endpoints under `/api/admin/products/{id}/images`, the five-image cap enforced before signing and on save, a duplicate-public-ID guard, and the rule that a product needs an image before it can be activated (which also makes every new product a draft, so three catalog tests were updated). Deletes call Cloudinary first and answer 502 if it is unreachable, leaving the catalog untouched. Web: `lib/cloudinary.ts` URL transforms wired into `ProductThumb` (`src` plus a `srcSet` ladder, `c_limit,f_auto,q_auto`) and `services/images.ts` for the browser-side type/size check and the direct upload. 24 new API tests (77 total) and 15 new web tests (49 total). `Cloudinary__Folder` added so development uploads land under their own prefix. Added `Microsoft.Extensions.Http.Resilience` for the typed Cloudinary client's timeout and retries. Still open: driving it from the live site, which needs the Step 7 admin UI. |
| 2026-10-22 | Step 3 decision settled: token storage stays the plan default — bearer JWT in `localStorage`, 8-hour expiry, re-login afterwards. Confirmed the Google OAuth client needs no billing account or card: the consent screen, the Web client ID and Google Identity Services sign-in are all free and unmetered, so the only real money in the project remains the Step 12 SMS credit. Noted that Vercel preview deployments cannot be authorized JavaScript origins (changing hostnames, no wildcard support), so admin sign-in works on localhost and the production Vercel domain only. |
| 2026-10-22 | Step 3 code done on `feature/google-sso`. API: `POST /api/auth/google` verifies the Google ID token against the configured client, upserts the user, reads the role from `Admin__Emails` on every sign-in, and issues an 8-hour JWT; `GET /api/me`; `Security/AdminAuthorization.cs` swapped from the placeholder scheme to JWT bearer with inbound claim mapping off. A non-allowlisted account is recorded as a Viewer and refused at sign-in with 403 rather than being given a token — the original "403 from admin endpoints" could not happen, since such an account never gets one. Web: `web/src/auth/` (context, provider, localStorage session), the Google button on `/admin` behind `RequireAdmin`, `AdminBadge` replacing the header's disabled Sign in button, and `apiGetAsAdmin`/`apiPostAsAdmin` for Step 7. 18 new tests (95 API, 63 web), including a real-bearer-scheme factory that signs in and reuses the issued token. A smoke test against the real startup path caught the verifier answering 500 for a forged token — `GoogleJsonWebSignature.ValidateAsync` fails base64 decoding before it reports an invalid JWT — which the faked-out API tests could not see; now covered by `GoogleTokenVerifierTests`. |
| 2026-10-22 | Step 3 verified: an admin signed in on the live site. Step 7 code written on `feature/admin-panel` (uncommitted): admin products list, product editor with image manager and publish, categories page; admin product list returns `isActive`. Plain controlled forms chosen over react-hook-form + zod, with server field errors shown beside each field. Also removed the stray vertical scrollbar beside the category menu (the links' focus ring overflowed the scrolling list). Recorded the migrations decision, left open since Step 1 although the API has been deployed since 2026-10-18: migrations are applied by hand, to be revisited in Step 10. |
| 2026-10-22 | Step 7 merged to `develop` (PR #12); its row and the entry above, which still said "uncommitted", corrected. |
| 2026-10-22 | Step 12 code done on `feature/phone-otp`. API: `PhoneVerification` entity and the `AddPhoneVerificationAndGuestOrders` migration (also makes `Order.UserId` nullable and adds `Order.CustomerName`), `POST /api/phone/otp` and `POST /api/phone/verify`, HMAC-hashed codes, per-phone cooldown and hourly/daily caps, per-IP rate limits through the built-in rate limiter, a verified-phone JWT with a `phone` claim and no role, an hourly pruner, and `ISmsSender` with the logging dev sender. `SendCount` dropped from the data model: each send is a row, so the caps count rows. Forwarded headers are now read (last `X-Forwarded-For` entry only) so the per-IP limits see the caller rather than Render's proxy. Web: `/checkout` gets the phone verify panel with formatting, resend and expiry countdowns; `ApiError` now carries `retryAfterSeconds`. 66 new API tests (161) and 22 new web tests (100). |
| 2026-10-22 | Step 12: `AddPhoneVerificationAndGuestOrders` applied to Neon by hand before merging PR #13; `/health` stayed healthy on the live API. |
| 2026-10-22 | Step 9 code done on `feature/checkout-orders`, kept uncommitted for review. API: `Orders/` with `POST /api/orders` (behind a new `VerifiedPhone` policy pinned to the bearer scheme), customer `GET /api/orders` and `/api/orders/{id}`, public `GET /api/checkout/options`, and admin list/counts/detail/status under `/api/admin/orders`; `OrderStatusRules` for the allowed moves; the cart's pricing extracted into `CartPricing` so the quote and the order share one rule; an order is refused with 409 and the fresh quote if a shown price moved. Migration `AddOrderDeliveryFee` (`Order.DeliveryFee`, nullable `OrderItem.Size`, index on `Order (Phone, CreatedAt)`) — **not yet applied to Neon**. Web: real checkout form and summary, `/orders/:id` confirmation, `/orders` lookup, header Orders link, admin Orders tab with a Pending count and an order page with status buttons; the verified-phone token moved from page state to `sessionStorage` via `web/src/phone/`; ProductForm's `Field` helper moved to `components/common` for reuse. Settled delivery fee (flat, `Orders__DeliveryFee` ৳60), new-order alerts (Pending count) and order lookup (reuse the token). 58 new API tests (219) and 20 new web tests (120). Also corrected the Step 3 and Step 12 rows, which still listed work already done. |
| 2026-10-22 | Step 9, before commit: the single ৳60 fee became two delivery areas chosen at checkout — Inside Dhaka City ৳80 (selected by default) and Outside Dhaka City ৳150. `Order.DeliveryZone` stores the choice beside `DeliveryFee`; `POST /api/orders` requires `deliveryZone` (400 otherwise); `GET /api/checkout/options` now lists each zone with its fee; settings `Orders__InsideDhakaFee` and `Orders__OutsideDhakaFee` replace `Orders__DeliveryFee`. The unapplied `AddOrderDeliveryFee` migration was regenerated as `AddOrderDelivery`. Enum parsing by name moved to `Orders/EnumNames.cs`, shared by status and zone. The admin order page and list show the area so it can be checked against the address on the confirmation call. 224 API and 122 web tests. |
