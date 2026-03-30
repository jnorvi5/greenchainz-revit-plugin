
from playwright.sync_api import sync_playwright

def verify_billing_page():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()

        # Navigate to billing page
        page.goto("http://localhost:3000/billing")

        # Verify initial state
        page.wait_for_selector("text=Simple, Transparent Pricing")
        page.screenshot(path="verification/billing_initial.png")
        print("Initial state screenshot taken")

        # Click a subscribe button to trigger loading state
        # Using the "Pro" plan subscribe button
        buttons = page.get_by_role("button", name="Subscribe").all()
        if buttons:
            button = buttons[0]
            # Verify focus ring appears on focus
            button.focus()
            page.screenshot(path="verification/billing_focus.png")
            print("Focus state screenshot taken")

            # Click to trigger loading
            button.click()

            # Wait a brief moment for state update and take screenshot of loading state
            # We look for "Processing..." text which indicates the spinner is also present
            page.wait_for_selector("text=Processing...")
            page.screenshot(path="verification/billing_loading.png")
            print("Loading state screenshot taken")

            # Verify aria-busy is true
            is_busy = button.get_attribute("aria-busy")
            if is_busy == "true":
                print("aria-busy is correctly set to true")
            else:
                print(f"aria-busy is {is_busy}")
        else:
            print("Subscribe button not found")

        browser.close()

if __name__ == "__main__":
    verify_billing_page()
