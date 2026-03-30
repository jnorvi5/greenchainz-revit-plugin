# GreenChainz: Revit Plugin to Main App Feature Mapping

This document maps the existing features in the `green-sourcing-b2b-app` to the corresponding implementations required in the Revit plugin.

## 1. Messaging Service
*   **Main App Implementation:** `server/messaging-service.ts` & `server/messaging-router.ts`. Uses Web PubSub for real-time notifications.
*   **Revit Plugin Integration:**
    *   **New UI:** A "Messages" panel in Revit to view and respond to supplier threads.
    *   **API Connection:** Connect to `getOrCreateThread` and `sendMessage` endpoints.
    *   **Real-time:** Implement a long-polling or WebSocket client (SignalR/WebPubSub) within the plugin to notify architects of new messages without leaving Revit.

## 2. RFQ Marketplace
*   **Main App Implementation:** `server/rfq-service.ts` & `server/rfq-router.ts`. Handles auto-matching, bidding, and analytics.
*   **Revit Plugin Integration:**
    *   **BIM-to-RFQ:** Automatically populate RFQ material lists from Revit schedules.
    *   **Submission:** Direct call to `rfqMarketplaceRouter.submit` from the `CreateRFQDialog.xaml`.
    *   **Tracking:** Add an "RFQ Status" tab to the plugin to track bids and lead times directly on the Revit model elements.

## 3. AI Agents (ChainBot / Otto)
*   **Main App Implementation:** `server/agent.ts`, `server/agent-triage-service.ts`, and `server/agent-response-handler.ts`.
*   **Revit Plugin Integration:**
    *   **Contextual Agent:** A chat interface in Revit that sends the current model context (material selected, project location) to the `material` and `rfq` agents.
    *   **Otto Integration:** Use the triage service to route architect queries from Revit to the right specialist.

## 4. Scorecards (CCPS Engine)
*   **Main App Implementation:** `server/ccps-engine.ts`. Calculates 0-100 scores based on 6 pillars.
*   **Revit Plugin Integration:**
    *   **Real-time Scoring:** Display the CCPS score in the Revit Properties palette when a material is selected.
    *   **Scorecard Window:** A detailed breakdown window in Revit showing the 6 pillars (Carbon, Compliance, Cert, Cost, Supply, Health) as defined in the `CcpsBreakdown` interface.
    *   **Swaps:** Use the `findSwapCandidates` logic to suggest higher-scoring alternatives directly in the Revit material browser.

## 5. Authentication
*   **Main App Implementation:** `auth.ts` using Better Auth / CIAM.
*   **Revit Plugin Integration:**
    *   **OAuth Flow:** Implement a secure browser-based login flow that returns a JWT to the plugin.
    *   **Secure Storage:** Store tokens using Windows DPAPI as outlined in the integration plan.
