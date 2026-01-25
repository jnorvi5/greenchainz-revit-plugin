from playwright.sync_api import sync_playwright, expect
import time

def verify_billing_button():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()

        print("Navigating to billing page...")
        # Retry logic for server startup
        for i in range(30):
            try:
                page.goto("http://localhost:3000/billing")
                break
            except Exception as e:
                print(f"Waiting for server... ({i+1}/30)")
                time.sleep(2)
        else:
            print("Failed to connect to server.")
            browser.close()
            return

        print("Page loaded. Looking for button...")
        # Check if the button is visible and has correct text
        button = page.get_by_role("button", name="Already a subscriber? Manage your subscription")
        expect(button).to_be_visible()

        print("Button found. Taking screenshot...")
        # Take a screenshot
        page.screenshot(path="verification/billing_button.png")
        print("Screenshot saved to verification/billing_button.png")

        browser.close()

if __name__ == "__main__":
    verify_billing_button()
