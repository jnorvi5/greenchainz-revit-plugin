
from playwright.sync_api import sync_playwright

def verify_dashboard_page():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()

        # Navigate to dashboard page
        page.goto("http://localhost:3000/dashboard")

        # Wait for loading skeleton to disappear and content to appear
        # The mock fetch takes 1.5s
        page.wait_for_selector("h1:text('Dashboard')", timeout=5000)

        # Verify content
        expect_plan = page.get_by_text("Current Plan")
        if expect_plan.count() == 0:
            print("Failed to find 'Current Plan'")
            exit(1)

        # Take screenshot
        page.screenshot(path="verification/dashboard_page.png")

        browser.close()
        print("Verification screenshot taken at verification/dashboard_page.png")

if __name__ == "__main__":
    verify_dashboard_page()
