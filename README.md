# RongTuli

A simple cash-on-delivery clothing store. Viewers browse products by category, add items to a cart and place orders after signing in with Google; admins manage categories, products, images and order status.

## Stack

| Layer    | Choice                                                       | Hosting           |
|----------|--------------------------------------------------------------|-------------------|
| Frontend | React + Vite + TypeScript, React Router, TanStack Query, Tailwind CSS | Vercel     |
| API      | ASP.NET Core Web API (.NET 10), EF Core                      | Render (Docker)   |
| Database | PostgreSQL (Npgsql)                                          | Neon              |
| Images   | Signed direct upload, CDN resizing                           | Cloudinary        |
| Auth     | Google sign-in; API issues its own JWT with a role           | Google Cloud OAuth |
| CI/CD    | GitHub Actions                                               | GitHub            |

## Repository layout

```
/api
  /Shop.Api      ASP.NET Core Web API
  /Shop.Tests    API tests
/web             React app (Vite)
/.github/workflows  CI pipelines
```

## Branches

- `main` — stable, deployed
- `develop` — ongoing development

## Status

Project scaffold only. Built in steps per the build plan (accounts → API/web skeletons → auth → catalog → images → storefront → admin → cart → checkout → hardening → polish).
