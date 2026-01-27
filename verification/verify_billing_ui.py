from playwright.sync_api import Page, expect, sync_playwright
import time

def verify_billing_ui(page: Page):
    # 1. Go to billing page
    page.goto("http://localhost:3000/billing")

    # 2. Wait for content to load
    page.wait_for_selector("text=Simple, Transparent Pricing")

    # 3. Locate the button we fixed
    button = page.get_by_text("Already a subscriber? Manage your subscription")

    # 4. Assert it is visible and enabled
    expect(button).to_be_visible()
    expect(button).to_be_enabled()

    # 5. Screenshot
    page.screenshot(path="verification/billing_page.png")
    print("Screenshot saved to verification/billing_page.png")

if __name__ == "__main__":
    with sync_playwright() as p:
        # Launch browser
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        try:
            verify_billing_ui(page)
        except Exception as e:
            print(f"Error: {e}")
            # Take screenshot anyway if possible
            try:
                page.screenshot(path="verification/error_screenshot.png")
            except:
                pass
        finally:
            browser.close()
