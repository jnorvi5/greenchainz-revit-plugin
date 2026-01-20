from playwright.sync_api import Page, expect, sync_playwright

def verify_billing_page(page: Page):
    # 1. Arrange: Go to the Billing page.
    page.goto("http://localhost:3000/billing")

    # 2. Assert: Verify the page loaded
    expect(page.get_by_role("heading", name="Simple, Transparent Pricing")).to_be_visible()

    # 3. Screenshot full page
    page.screenshot(path="verification/billing_page.png", full_page=True)

    # 4. Verify Manage Subscription button
    manage_btn = page.get_by_role("button", name="Already a subscriber? Manage your subscription")
    expect(manage_btn).to_be_visible()

    # 5. Focus the button to check focus styles
    manage_btn.focus()
    page.screenshot(path="verification/billing_manage_focus.png")

if __name__ == "__main__":
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        try:
            verify_billing_page(page)
            print("Verification script completed successfully.")
        except Exception as e:
            print(f"Verification script failed: {e}")
        finally:
            browser.close()
