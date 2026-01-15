## 2025-02-17 - Web Security Headers
**Vulnerability:** Missing HTTP security headers (HSTS, X-Frame-Options, etc.) in the Next.js web application.
**Learning:** Next.js requires manual configuration in `next.config.ts` to add these headers globally.
**Prevention:** Always verify security headers are configured in the framework's config file during project setup.

## 2025-02-17 - Unsecured API Endpoints
**Vulnerability:** The `/api/rfq` endpoint was exposed without authentication, allowing potential abuse.
**Learning:** Next.js API routes are public by default. Authentication must be explicitly implemented, preferably using a shared middleware or utility function.
**Prevention:** Audit all new API endpoints for authentication checks. Consider implementing a middleware to enforce `GREENCHAINZ_API_SECRET` verification globally or on specific paths.
