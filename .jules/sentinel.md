## 2024-05-23 - Corrupted Files as Security Risks & Secure Logging
**Vulnerability:** The `ApiClient.cs` file was corrupted with merge conflicts, resulting in duplicate constructors and ambiguous code paths. One path contained logging that dumped full request bodies (including potential PII) to debug logs.
**Learning:** Corrupted files are not just build errors; they are security risks because they hide which code is actually executing, potentially bypassing security checks (like auth headers) or enabling insecure features (like verbose logging) that were meant to be removed.
**Prevention:**
1. Always resolve merge conflicts before committing.
2. treat "Duplicate Code" lint errors as potential security flags.
3. Use structured logging that explicitly excludes sensitive fields rather than dumping full JSON objects.
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
## 2025-05-21 - Recurrent Code Corruption in Security-Critical Routes
**Vulnerability:** Multiple API routes (`audit`, `rfq`) and UI pages contained duplicated code blocks and syntax errors resembling unresolved merge conflicts, but without conflict markers. This rendered security checks ambiguous (duplicate auth headers, cut-off validation blocks).
**Learning:** Code corruption often manifests as "syntax errors" or "lint errors" but can be subtle enough to allow compilation while executing unexpected logic (e.g., duplicated side effects, bypassed checks). In this repo, it seems to stem from poor merge practices.
**Prevention:** Treat "Parsing error" and "Duplicate identifier" lint errors as P0 security issues. Do not just "fix the build" by commenting out code; reconstruct the intended logic to ensure no security checks were deleted during the corruption.

## 2024-05-22 - [Medium] Missing Input Validation in Audit API
**Vulnerability:** The `/api/audit` endpoint accepted any JSON structure for `Materials` and `Recommendations`, relying on implicit database handling. It also lacked type checks for `OverallScore`.
**Learning:** Even internal/machine-to-machine APIs (authenticated via secret) must validate input to prevent data corruption or potential injection/DoS vectors.
**Prevention:** Implement strict schema validation (using Zod or manual checks) for all API inputs, regardless of authentication method.

## 2024-05-22 - [Critical] Code Corruption in Billing Page
**Vulnerability:** The `BillingPage` contained quadruple duplicate `className` props on a button, indicating a bad merge resolution.
**Learning:** `pnpm build` might not catch all JSX issues that `pnpm lint` catches.
**Prevention:** Always run `pnpm lint` before pushing, especially after resolving merge conflicts.
