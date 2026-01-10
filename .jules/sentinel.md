## 2025-02-17 - Web Security Headers
**Vulnerability:** Missing HTTP security headers (HSTS, X-Frame-Options, etc.) in the Next.js web application.
**Learning:** Next.js requires manual configuration in `next.config.ts` to add these headers globally.
**Prevention:** Always verify security headers are configured in the framework's config file during project setup.

## 2025-05-20 - Unauthenticated API Endpoints
**Vulnerability:** The `/api/audit` endpoint was completely public, allowing any unauthenticated user to submit fake audit data.
**Learning:** Next.js API Routes are public by default. Unlike some frameworks with global middleware defaults, each route must explicitly implement or import authentication checks.
**Prevention:** Implement a middleware or a reusable `requireAuth` helper function that checks `Authorization` headers and use it in every secure API route.
