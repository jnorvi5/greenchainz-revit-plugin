from playwright.sync_api import sync_playwright, expect

def run():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        try:
            print("Navigating to billing page...")
            page.goto("http://localhost:3000/billing")

            # Wait for content
            print("Waiting for heading...")
            expect(page.get_by_role("heading", name="Simple, Transparent Pricing")).to_be_visible(timeout=10000)

            # Verify Manage Subscription button
            manage_btn = page.get_by_role("button", name="Manage your subscription")
            expect(manage_btn).to_be_visible()

            # Focus the button to check focus ring styles
            manage_btn.focus()

            # Take screenshot
            print("Taking screenshot...")
            page.screenshot(path="verification/billing_page.png")
            print("Screenshot saved.")

        except Exception as e:
            print(f"Error: {e}")
            page.screenshot(path="verification/error.png")
        finally:
            browser.close()

if __name__ == "__main__":
    run()
