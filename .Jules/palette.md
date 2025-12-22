## 2024-05-23 - Skeleton Loading & Build Resilience
**Learning:**
1.  **Skeleton vs Text:** Replacing "Loading..." text with a Skeleton UI significantly improves perceived performance and polish. It maintains layout stability and reduces cognitive load during data fetching.
2.  **Next.js Buildtime Instantiation:** Next.js static builds attempt to evaluate code in API routes. Initializing third-party clients (Stripe, Supabase) with non-nullable environment variables (e.g., `process.env.KEY!`) causes build failures if those keys aren't present in the build environment.

**Action:**
1.  Always prefer Skeleton UIs for dashboard-like views.
2.  Use fallback/placeholder values for environment variables in global scope initializations to ensure builds pass in CI/CD environments without secrets.
