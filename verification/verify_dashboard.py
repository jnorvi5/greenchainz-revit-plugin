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
            print("Navigating to dashboard...")
            page.goto("http://localhost:3000/dashboard")

            print("Waiting for Change Plan button...")
            # Using CSS selector as a fallback to ensure we find it
            change_plan_link = page.locator("a[href='/billing']")
            change_plan_link.wait_for(state="visible", timeout=10000)

            print("Taking screenshot...")
            page.screenshot(path="verification/dashboard_fix.png")
            print("Screenshot saved to verification/dashboard_fix.png")

        except Exception as e:
            print(f"Error: {e}")
            page.screenshot(path="verification/error.png")
        finally:
            browser.close()

if __name__ == "__main__":
    verify_dashboard()
