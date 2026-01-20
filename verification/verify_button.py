from playwright.sync_api import sync_playwright
import time

def verify_billing_button():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        try:
            print("Navigating to billing page...")
            page.goto("http://localhost:3000/billing")

            # Wait for the button to be visible
            # It has text "Already a subscriber? Manage your subscription"
            print("Waiting for button...")
            button = page.get_by_role("button", name="Already a subscriber? Manage your subscription")
            button.wait_for()

            # Hover to trigger the hover state (bg-indigo-50)
            print("Hovering over button...")
            button.hover()

            # Small delay to ensure styles apply
            time.sleep(0.5)

            # Take a screenshot
            print("Taking screenshot...")
            page.screenshot(path="verification/billing_button.png")
            print("Screenshot saved to verification/billing_button.png")

        except Exception as e:
            print(f"Error: {e}")
        finally:
            browser.close()

if __name__ == "__main__":
    verify_billing_button()
