# GreenChainz Revit Plugin & Backend Integration Plan

## 1. Executive Summary
This plan outlines the technical roadmap for integrating the GreenChainz Revit Plugin with the centralized web backend. The primary goal is to transition from local/mock data to a real-time, cloud-synchronized ecosystem supporting Carbon Audits and RFQ management.

## 2. Integration Architecture
The integration follows a **Client-Server** model utilizing a RESTful API layer.

### 2.1 Key Components
*   **Revit Plugin (C#/.NET 8.0):** Extracts BIM data, performs local carbon calculations, and initiates cloud sync.
*   **Web Backend (Next.js/Node.js):** Hosts API endpoints for data ingestion, supplier matching, and project management.
*   **Data Layer (Supabase/PostgreSQL):** Stores audit history, RFQ records, and user profiles.
*   **External Integrations:** Placeholders for BuildingTransparency (EC3) and Autodesk Construction Cloud (ACC).

### 2.2 Data Flow
1.  **Audit Flow:** Revit -> Extract Materials -> Local GWP Calculation -> `POST /api/audit` -> Web Dashboard.
2.  **RFQ Flow:** Revit -> Select Materials -> `POST /api/rfq` -> Supplier Notification -> Web Messaging.

## 3. Implementation Roadmap

### Phase 1: Authentication & Connectivity (Week 1)
*   **Task 1.1:** Implement secure token storage in `ApiConfig.cs` using Windows Data Protection API (DPAPI).
*   **Task 1.2:** Transition `ApiClient.cs` from hardcoded URLs to dynamic configuration based on environment.
*   **Task 1.3:** Implement a "Connection Test" feature in the Revit UI to verify backend reachability.

### Phase 2: Live Carbon Audit Sync (Week 2)
*   **Task 2.1:** Update `AuditService.cs` to send real project data instead of fallback baselines.
*   **Task 2.2:** Map Revit Material IDs to EC3 categories on the backend for more accurate matching.
*   **Task 2.3:** Implement "View in Dashboard" button in `AuditResultsWindow.xaml` to open the web project page directly.

### Phase 3: Enhanced RFQ & Supplier Integration (Week 3)
*   **Task 3.1:** Connect `CreateRFQDialog.xaml` to the live `GET /api/suppliers` endpoint.
*   **Task 3.2:** Implement real-time status updates for RFQs using WebSockets (Azure Web PubSub).
*   **Task 3.3:** Add support for file attachments (specifications, drawings) via Azure Blob Storage.

## 4. Security & Compliance
*   **Managed Identities:** Utilize Azure Managed Identities for service-to-service communication where possible.
*   **RBAC:** Enforce Role-Based Access Control on all API endpoints.
*   **Encryption:** All data in transit must use TLS 1.3.

## 5. Testing & Verification Strategy
| Test Type | Description | Success Criteria |
| :--- | :--- | :--- |
| **Unit Testing** | Test `ApiClient` with mocked HTTP responses. | 100% pass on core API methods. |
| **Integration Testing** | End-to-end flow from Revit to Supabase. | Audit record appears in DB within 2s. |
| **User Acceptance** | Verify RFQ submission with a test supplier account. | Supplier receives email/notification. |

## 6. Maintenance & Monitoring
*   **Telemetry:** Integrate Azure Application Insights in both the plugin and backend.
*   **Auto-Update:** Implement a version check on plugin startup to ensure users are on the latest integration schema.

---
**Next Steps:**
1. Approve the Phase 1 implementation.
2. Provide Azure/Supabase credentials for the staging environment.
3. Schedule a review of the RFQ messaging UI.
