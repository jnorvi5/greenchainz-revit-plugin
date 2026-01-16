from playwright.sync_api import sync_playwright, expect
import time

def verify_pages():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        context = browser.new_context()
        page = context.new_page()

        print("Navigating to Dashboard...")
        try:
            page.goto("http://localhost:3000/dashboard")
            # Wait for content to load (skipping skeleton)
            expect(page.get_by_role("heading", name="Dashboard")).to_be_visible(timeout=10000)
            expect(page.get_by_text("Credits Balance")).to_be_visible()

            page.screenshot(path="verification/dashboard.png")
            print("Dashboard screenshot saved.")

            print("Navigating to Billing...")
            page.goto("http://localhost:3000/billing")
            expect(page.get_by_role("heading", name="Simple, Transparent Pricing")).to_be_visible()
            expect(page.get_by_role("heading", name="Free")).to_be_visible()
            expect(page.get_by_role("heading", name="Pro")).to_be_visible()

            page.screenshot(path="verification/billing.png")
            print("Billing screenshot saved.")

        except Exception as e:
            print(f"Error: {e}")
            page.screenshot(path="verification/error.png")
            raise e
        finally:
            browser.close()

if __name__ == "__main__":
    verify_pages()
