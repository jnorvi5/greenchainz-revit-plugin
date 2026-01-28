from playwright.sync_api import sync_playwright

def verify_billing_page(page):
    print("Navigating to billing page...")
    page.goto("http://localhost:3000/billing")

    print("Waiting for button...")
    # Wait for the button
    button = page.get_by_role("button", name="Already a subscriber? Manage your subscription")
    button.wait_for()

    print("Taking screenshot...")
    # Screenshot
    page.screenshot(path="verification/billing_page.png")
    print("Screenshot saved.")

if __name__ == "__main__":
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        try:
            verify_billing_page(page)
        finally:
            browser.close()
