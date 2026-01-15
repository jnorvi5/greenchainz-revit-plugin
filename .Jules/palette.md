## 2024-05-23 - Skeleton Loading & Build Resilience
**Learning:**
1.  **Skeleton vs Text:** Replacing "Loading..." text with a Skeleton UI significantly improves perceived performance and polish. It maintains layout stability and reduces cognitive load during data fetching.
2.  **Next.js Buildtime Instantiation:** Next.js static builds attempt to evaluate code in API routes. Initializing third-party clients (Stripe, Supabase) with non-nullable environment variables (e.g., `process.env.KEY!`) causes build failures if those keys aren't present in the build environment.

**Action:**
1.  Always prefer Skeleton UIs for dashboard-like views.
2.  Use fallback/placeholder values for environment variables in global scope initializations to ensure builds pass in CI/CD environments without secrets.

## 2025-02-23 - Semantic Data Visualization & Transition States
**Learning:**
1.  **Semantic Colors:** Using color (Red/Orange/Green) to indicate status (Critical/Warning/Good) in progress bars adds immediate cognitive value compared to a static brand color.
2.  **Server Component Transitions:** When switching from a `loading.tsx` skeleton to the final Server Component UI, the instant "snap" can be jarring. Adding a simple CSS fade-in animation (`animate-in fade-in`) creates a much smoother, higher-quality feel.

**Action:**
1.  Use semantic colors for data visualization where status is implied.
2.  Add fade-in transitions to page wrappers when using Next.js Suspense/Loading boundaries.
