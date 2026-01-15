
from playwright.sync_api import sync_playwright

def verify_billing_page():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        try:
            page.goto('http://localhost:3000/billing')

            # Wait for content to load
            page.wait_for_selector('text=Simple, Transparent Pricing')

            # Take screenshot of initial state
            page.screenshot(path='verification/billing_initial.png')

            # Find the manage subscription button
            manage_btn = page.locator('button:has-text("Manage your subscription")')

            # Click it to trigger loading state
            manage_btn.click()

            # Wait a tiny bit for the state update (react hook)
            page.wait_for_timeout(100)

            # Take screenshot of loading state
            page.screenshot(path='verification/billing_loading.png')

            print('Verification screenshots taken.')

        except Exception as e:
            print(f'Error: {e}')
        finally:
            browser.close()

if __name__ == '__main__':
    verify_billing_page()

