## 2025-02-18 - Duplicate Props in React Components
**Learning:** Duplicate props (like `className` or `disabled`) in React components lead to unpredictable UI behavior where only the last definition "wins", often masking critical accessibility states or styling.
**Action:** Always run `pnpm lint` to catch `react/jsx-no-duplicate-props` errors before committing, especially when refactoring or copying code blocks.
