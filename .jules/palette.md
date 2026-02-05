## 2024-05-23 - Duplicate Props in React
**Learning:** React build tools (Next.js build) may not fail on duplicate props in JSX (e.g., multiple `className` attributes), but `eslint-plugin-react` (`react/jsx-no-duplicate-props`) will catch them.
**Action:** Always run `pnpm lint` before `pnpm build` to catch duplicate props which can lead to unexpected styling or behavior.

## 2024-05-23 - Next.js Route Announcer
**Learning:** Next.js injects a route announcer with `role="alert"` (id `__next-route-announcer__`) into the DOM. This causes Playwright's `get_by_role("alert")` to fail with strict mode violation if multiple alerts exist.
**Action:** When testing for alert messages in Next.js, use `get_by_role("alert")` combined with `filter(has_text=...)` or use `get_by_text(...)` to target the specific user-facing alert, avoiding the hidden route announcer.
