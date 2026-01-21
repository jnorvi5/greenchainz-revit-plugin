
from playwright.sync_api import sync_playwright

def verify_button(page):
    # Navigate to the billing page
    page.goto("http://localhost:3000/billing")

    # Wait for the button to be visible
    # The button text is "Already a subscriber? Manage your subscription"
    # We use get_by_role to find the button
    button = page.get_by_role("button", name="Already a subscriber? Manage your subscription")

    # Wait for it to be visible
    button.wait_for(state="visible")

    # Hover over the button to trigger hover styles (though screenshot might not capture hover state perfectly in headless, it's good practice)
    button.hover()

    # Take a screenshot
    page.screenshot(path="verification/billing_button.png")

    print("Screenshot taken successfully")

if __name__ == "__main__":
    with sync_playwright() as p:
        # Launch browser
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()

        try:
            verify_button(page)
        except Exception as e:
            print(f"Error: {e}")
        finally:
            browser.close()
