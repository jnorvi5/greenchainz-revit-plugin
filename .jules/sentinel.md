## 2024-05-23 - Corrupted Files as Security Risks & Secure Logging
**Vulnerability:** The `ApiClient.cs` file was corrupted with merge conflicts, resulting in duplicate constructors and ambiguous code paths. One path contained logging that dumped full request bodies (including potential PII) to debug logs.
**Learning:** Corrupted files are not just build errors; they are security risks because they hide which code is actually executing, potentially bypassing security checks (like auth headers) or enabling insecure features (like verbose logging) that were meant to be removed.
**Prevention:**
1. Always resolve merge conflicts before committing.
2. treat "Duplicate Code" lint errors as potential security flags.
3. Use structured logging that explicitly excludes sensitive fields rather than dumping full JSON objects.
