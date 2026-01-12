from playwright.sync_api import sync_playwright

def verify_skip_link():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()

        # Navigate to the dashboard
        page.goto("http://localhost:3000/dashboard")

        # Wait for hydration
        page.wait_for_load_state("networkidle")

        # Press Tab to focus the skip link
        page.keyboard.press("Tab")

        # Verify the skip link is focused and visible
        skip_link = page.locator("a[href='#main-content']")

        # Check if focused
        is_focused = skip_link.evaluate("el => el === document.activeElement")
        print(f"Skip link focused: {is_focused}")

        if is_focused:
            # Take a screenshot of the focused skip link
            page.screenshot(path="verification/skip_link_focused.png")
            print("Screenshot saved to verification/skip_link_focused.png")
        else:
            print("Skip link was not focused after first Tab press")

        browser.close()

if __name__ == "__main__":
    verify_skip_link()
