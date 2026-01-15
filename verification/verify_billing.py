
from playwright.sync_api import sync_playwright, expect

def run():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        context = browser.new_context()
        page = context.new_page()

        print("Navigating to billing page...")
        page.goto("http://localhost:3000/billing")

        print("Waiting for pricing cards...")
        # Use exact text match for "Subscribe" button to avoid matching "Already a subscriber?"
        subscribe_btn = page.get_by_role("button", name="Subscribe", exact=True)
        expect(subscribe_btn).to_be_visible()

        print("Clicking Subscribe...")
        # Click the button
        subscribe_btn.click()

        print("Verifying loading state...")
        # Expect the button to have loading text "Processing..."
        # And to be disabled

        # We need to wait for the state change to happen.
        # The click triggers async handleSubscribe which sets loading=true immediately.
        # But React might take a tick to update.

        # Check for the loading text first, as that confirms the state update
        expect(subscribe_btn).to_contain_text("Processing...")
        expect(subscribe_btn).to_be_disabled()

        # Verify aria-busy attribute
        expect(subscribe_btn).to_have_attribute("aria-busy", "true")

        print("Taking screenshot...")
        page.screenshot(path="verification/billing_loading.png")

        browser.close()

if __name__ == "__main__":
    run()
