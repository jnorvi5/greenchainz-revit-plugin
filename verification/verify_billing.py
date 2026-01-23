from playwright.sync_api import Page, expect, sync_playwright
import os

def verify_billing_page(page: Page):
    print("Navigating to billing page...")
    page.goto("http://localhost:3000/billing")

    print("Checking for header...")
    expect(page.get_by_text("Simple, Transparent Pricing")).to_be_visible()

    print("Checking for subscription button...")
    button = page.get_by_role("button", name="Already a subscriber? Manage your subscription")
    expect(button).to_be_visible()

    # Take screenshot
    if not os.path.exists("verification"):
        os.makedirs("verification")

    screenshot_path = "verification/billing_page.png"
    page.screenshot(path=screenshot_path)
    print(f"Screenshot saved to {screenshot_path}")

if __name__ == "__main__":
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        try:
            verify_billing_page(page)
        except Exception as e:
            print(f"Error: {e}")
            # Take screenshot on error if possible
            try:
                page.screenshot(path="verification/error.png")
            except:
                pass
            exit(1)
        finally:
            browser.close()
