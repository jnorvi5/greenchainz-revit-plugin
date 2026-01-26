from playwright.sync_api import sync_playwright

def verify_billing_button():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()

        try:
            print("Navigating to billing page...")
            page.goto("http://localhost:3000/billing")

            # Wait for the button to be visible
            print("Looking for Manage Subscription button...")
            button = page.get_by_text("Manage your subscription")
            button.wait_for()

            print("Taking screenshot...")
            page.screenshot(path="verification/billing_button.png")
            print("Screenshot saved to verification/billing_button.png")

        except Exception as e:
            print(f"Error: {e}")
            # Take screenshot on error to debug
            page.screenshot(path="verification/error.png")
        finally:
            browser.close()

if __name__ == "__main__":
    verify_billing_button()
