
from playwright.sync_api import sync_playwright

def verify_billing_page():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()

        # Navigate to billing page
        page.goto("http://localhost:3000/billing")

        # Verify title or header
        page.wait_for_selector("h2:text('Simple, Transparent Pricing')")

        # Verify the "Back to Dashboard" link
        back_link = page.get_by_role("link", name="Back to Dashboard")

        # Check accessibility - it should have focus styles when focused
        back_link.focus()

        # Take screenshot
        page.screenshot(path="verification/billing_page_back_link.png")

        browser.close()
        print("Verification screenshot taken at verification/billing_page_back_link.png")

if __name__ == "__main__":
    verify_billing_page()
