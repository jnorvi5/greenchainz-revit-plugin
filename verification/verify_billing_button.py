from playwright.sync_api import sync_playwright

def verify_billing_button():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()

        try:
            print("Navigating to billing page...")
            page.goto("http://localhost:3000/billing")

            print("Waiting for button...")
            # Locate the button by its text
            button = page.get_by_role("button", name="Manage your subscription")

            # Check if it's visible
            if button.is_visible():
                print("Button is visible.")
            else:
                print("Button not found!")

            # Take a screenshot of the whole page
            page.screenshot(path="verification/billing_page.png")
            print("Screenshot saved to verification/billing_page.png")

        except Exception as e:
            print(f"Error: {e}")
        finally:
            browser.close()

if __name__ == "__main__":
    verify_billing_button()
