# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

GreenChainz is a two-part system: a **C# Autodesk Revit 2026 plugin** that analyzes building models for embodied carbon, LEED credits, and sustainable procurement — and a **Next.js web platform** that handles RFQ supplier matching, billing, and audit history via Supabase.

---

## Commands

### Revit Plugin (C#)

```bash
# Build
cd src/GreenChainz.Revit
dotnet build -c Release
dotnet build -c Debug

# Deploy to Revit (copies DLLs to %APPDATA%\Autodesk\Revit\Addins\2026\)
./deploy.bat

# Run C# tests
cd tests/GreenChainz.Revit.Tests
dotnet test
```

### Web Platform (Next.js)

```bash
cd web
npm install          # or pnpm install
npm run dev          # development server
npm run build        # production build
npm run lint         # ESLint
npm run test         # Vitest unit tests
```

---

## Architecture

### Two independent subsystems

**C# Revit Plugin** (`src/GreenChainz.Revit/`) talks to the web API over HTTPS with a Bearer token. The plugin runs inside the Revit process (IExternalApplication lifecycle) and cannot use async I/O at top level — all HTTP calls go through `ApiClient.cs` which wraps `HttpClient` with retry logic.

**Web Platform** (`web/`) is a Next.js 16 app with API routes that serve as the backend. It connects to Supabase (PostgreSQL) and Stripe. The plugin is the only client of these API routes; there is no browser frontend that calls these APIs directly.

### Service layer (C# plugin)

Services are instantiated once in `App.cs` and held as static properties. Key dependencies:

- `AutodeskAuthService` → obtains APS OAuth 2-legged token, required by `SdaConnectorService`
- `MaterialService` → aggregates data from `SdaConnectorService` (APS), `Ec3ApiService` (Building Transparency EC3), and hardcoded fallback materials when credentials are absent
- `AuditService` → scans the open Revit model, extracts element volumes, applies EC3 carbon factors from `MaterialService`
- `RfqService` → POSTs to `/api/rfq` with matched suppliers; falls back through production → Vercel dev → localhost if the primary URL fails; saves locally to `%MyDocuments%/GreenChainz/RFQs/` if all APIs fail
- `LeedCalculatorService` / `LeedV5Calculator` / `LeedMRpc132Calculator` → three separate calculators for LEED v4.1, v5, and MRpc132 pilot credits — they are not interchangeable
- `TelemetryService` → logs to `%AppData%/GreenChainz/logs.txt`; **avoid logging request/response bodies** (PII leakage risk documented in `.jules/sentinel.md`)

### API routes (web)

| Route | Auth | Notes |
|---|---|---|
| `POST /api/rfq` | Bearer token (timing-safe compare against `GREENCHAINZ_API_SECRET`) | Matches materials to 15+ hardcoded suppliers; saves to Supabase |
| `GET /api/rfq?id=` | None | Retrieves RFQ status |
| `GET /api/suppliers` | None | Filter by `?category=&region=` |
| `POST /api/audit` | None | Stores audit results — **authentication not yet implemented** |
| `POST /api/stripe/create-checkout` | Stripe sig | Creates checkout session |
| `POST /api/stripe/webhook` | Stripe sig | Handles subscription events |

The supplier database is **hardcoded** in both `web/app/api/suppliers/route.ts` (TypeScript) and `src/GreenChainz.Revit/Services/RfqService.cs` (C#). If suppliers change, both files must be updated.

### Authentication flow

```
Plugin → Web API:   Bearer token from GREENCHAINZ_AUTH_TOKEN env var or GreenChainz.Revit.dll.config
Plugin → APS:       OAuth 2.0 client_credentials (AUTODESK_CLIENT_ID / AUTODESK_CLIENT_SECRET)
Web → Supabase:     SUPABASE_SERVICE_ROLE_KEY (falls back to anon key)
Web → Stripe:       STRIPE_SECRET_KEY + webhook secret
```

### Data models

The core transfer objects between plugin and web are:

- **RFQRequest**: `{ projectName, projectAddress, materials: RFQItem[], deliveryDate, specialInstructions, selectedSupplierIds }`
- **AuditResult**: `{ projectName, date, overallScore (kgCO2e), materials: MaterialBreakdown[], recommendations[] }` — each `MaterialBreakdown` carries `IfcGuid`, `RevitElementId`, `volumeM3`, `massKg` for openBIM traceability
- **LeedResult / LeedV5Result** — separate types; do not conflate them

### IFC export convention

IFC GUIDs are base64-encoded 22-char strings using `/` → `_` and `+` → `$` substitutions. This is the IFC standard encoding, not a custom scheme.

---

## Environment Variables

**C# plugin** (set in system env or `GreenChainz.Revit.dll.config`):
- `EC3_API_KEY` — Building Transparency EC3 API
- `AUTODESK_CLIENT_ID` / `AUTODESK_CLIENT_SECRET` — APS credentials
- `GREENCHAINZ_AUTH_TOKEN` — Bearer token for web API

**Web** (`.env.local`):
- `GREENCHAINZ_API_SECRET` — validates plugin Bearer tokens
- `NEXT_PUBLIC_SUPABASE_URL` / `NEXT_PUBLIC_SUPABASE_ANON_KEY` / `SUPABASE_SERVICE_ROLE_KEY`
- `STRIPE_SECRET_KEY` / `STRIPE_WEBHOOK_SECRET` / `NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY`

---

## Known Issues / Active Concerns

From `.jules/sentinel.md`:
- `/api/audit` lacks authentication — treat as a pending security fix before any production data sensitivity increases
- `TelemetryService` previously logged full request bodies including `ProjectAddress`; verify no PII flows through logging paths when adding new features
- Past merge conflicts introduced duplicate constructors — treat compiler errors and ambiguous symbol warnings as P0
