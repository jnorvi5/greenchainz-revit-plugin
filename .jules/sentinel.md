# Sentinel Journal

## 2025-02-17 - Web Security Headers
**Vulnerability:** Missing HTTP security headers (HSTS, X-Frame-Options, etc.) in the Next.js web application.
**Learning:** Next.js requires manual configuration in `next.config.ts` to add these headers globally.
**Prevention:** Always verify security headers are configured in the framework's config file during project setup.

## 2025-05-20 - Unauthenticated API Endpoints
**Vulnerability:** The `/api/audit` endpoint was completely public, allowing any unauthenticated user to submit fake audit data.
**Learning:** Next.js API Routes are public by default. Unlike some frameworks with global middleware defaults, each route must explicitly implement or import authentication checks.
**Prevention:** Implement a middleware or a reusable `requireAuth` helper function that checks `Authorization` headers and use it in every secure API route.
## 2024-05-22 - [CRITICAL] PII Leakage in API Client Logging
**Vulnerability:** The `ApiClient.SendRequestAsync` method was logging the full JSON request body to the telemetry file (`%AppData%/GreenChainz/logs.txt`). This included the `RFQRequest` object which contains `ProjectAddress` and `SpecialInstructions`, potentially exposing Personally Identifiable Information (PII) or sensitive project data in plain text logs on the user's machine.
**Learning:** Generic logging wrappers that "log everything for debugging" often inadvertently capture sensitive data. Telemetry services that write to local disk are still a security risk if the machine is compromised or logs are shared for support.
**Prevention:**
1. Never log full request/response bodies in production code.
2. Use specific logging for specific fields if necessary.
3. Implement a redaction policy for logging if body logging is absolutely required for debugging (which should be toggled off by default).
## 2025-02-17 - Unsecured API Endpoints
**Vulnerability:** The `/api/rfq` endpoint was exposed without authentication, allowing potential abuse.
**Learning:** Next.js API routes are public by default. Authentication must be explicitly implemented, preferably using a shared middleware or utility function.
**Prevention:** Audit all new API endpoints for authentication checks. Consider implementing a middleware to enforce `GREENCHAINZ_API_SECRET` verification globally or on specific paths.
