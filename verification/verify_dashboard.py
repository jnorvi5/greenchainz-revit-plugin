from playwright.sync_api import sync_playwright

def verify_dashboard():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        try:
            # Navigate to dashboard
            page.goto("http://localhost:3000/dashboard")

            # Wait for content to load (since we added loading state)
            # The title 'Dashboard' should be visible
            page.wait_for_selector("h1:has-text('Dashboard')", timeout=10000)

            # Take a screenshot
            page.screenshot(path="verification/dashboard_verified.png", full_page=True)
            print("Screenshot taken successfully")

        except Exception as e:
            print(f"Error: {e}")
        finally:
            browser.close()

if __name__ == "__main__":
    verify_dashboard()
