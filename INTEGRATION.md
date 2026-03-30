# GreenChainz Integration Guide

## Overview
GreenChainz integrates a Revit plugin (C#) with a web platform (Next.js) and Autodesk Platform Services (APS).

## Web Integration (greenchainz.com)

### Connectivity
The Revit plugin connects to `https://greenchainz.com/api` (configurable via `ApiConfig.BASE_URL`).

### Key Endpoints
1.  **RFQ Submission**: `POST /api/rfq`
    *   **Payload**: `RFQRequest` (Project info, materials, supplier IDs).
    *   **Handler**: `web/app/api/rfq/route.ts`
    *   **Function**: Stores RFQ in Supabase (if configured) and returns supplier matches.

2.  **Carbon Audit**: `POST /api/audit`
    *   **Payload**: `AuditResult` (Carbon score, material breakdown).
    *   **Handler**: `web/app/api/audit/route.ts` (Newly created).
    *   **Function**: Stores audit history in Supabase.

### Authentication
*   The plugin supports Bearer token authentication.
*   The token is loaded from the `GREENCHAINZ_AUTH_TOKEN` environment variable or `GreenChainzAuthToken` in `GreenChainz.Revit.dll.config`.

## Autodesk Platform Services (APS) Integration

### Current Status
The plugin includes `AutodeskAuthService` which implements 2-legged OAuth (Client Credentials) to obtain an access token.

### Setup
1.  Create an app in the [APS Developer Portal](https://aps.autodesk.com).
2.  Set environment variables on the machine running Revit:
    *   `AUTODESK_CLIENT_ID`
    *   `AUTODESK_CLIENT_SECRET`

### Integration Opportunities
1.  **Data Exchange (ACC/BIM 360)**:
    *   Use the `data:read` scope to access models stored in Autodesk Construction Cloud.
    *   *Implementation*: Use the Data Management API to list hubs/projects/folders and download models for analysis without opening Revit locally (Cloud Engine).

2.  **Design Automation**:
    *   Offload heavy LEED calculations to the cloud using the Design Automation API for Revit.
    *   *Implementation*: Bundle the GreenChainz core logic as an AppBundle, upload to APS, and trigger workitems from the web app or plugin.

3.  **Viewer Integration**:
    *   Embed the APS Viewer in `greenchainz.com` to show the audited model with heatmaps for carbon intensity.
    *   *Implementation*: Upload model to OSS (Object Storage Service), translate to SVF via Model Derivative API, and display in the Next.js app.

## Next Steps for Developers
1.  **Environment Variables**: Ensure all developer machines have the required API keys.
2.  **Supabase**: Run migrations to ensure `rfqs` and `audits` tables exist.
3.  **Testing**: Use the "Send RFQ" command in Revit to test end-to-end connectivity.
