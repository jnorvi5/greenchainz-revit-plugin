# Sentinel Journal

## 2025-02-17 - Web Security Headers
**Vulnerability:** Missing HTTP security headers (HSTS, X-Frame-Options, etc.) in the Next.js web application.
**Learning:** Next.js requires manual configuration in `next.config.ts` to add these headers globally.
**Prevention:** Always verify security headers are configured in the framework's config file during project setup.

## 2024-05-22 - [CRITICAL] PII Leakage in API Client Logging
**Vulnerability:** The `ApiClient.SendRequestAsync` method was logging the full JSON request body to the telemetry file (`%AppData%/GreenChainz/logs.txt`). This included the `RFQRequest` object which contains `ProjectAddress` and `SpecialInstructions`, potentially exposing Personally Identifiable Information (PII) or sensitive project data in plain text logs on the user's machine.
**Learning:** Generic logging wrappers that "log everything for debugging" often inadvertently capture sensitive data. Telemetry services that write to local disk are still a security risk if the machine is compromised or logs are shared for support.
**Prevention:**
1. Never log full request/response bodies in production code.
2. Use specific logging for specific fields if necessary.
3. Implement a redaction policy for logging if body logging is absolutely required for debugging (which should be toggled off by default).
