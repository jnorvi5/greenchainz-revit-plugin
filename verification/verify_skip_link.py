from playwright.sync_api import sync_playwright, expect

def verify_skip_link(page):
    print("Navigating to home page...")
    page.goto("http://localhost:3000")

    # Press Tab to focus the first element (should be skip link)
    print("Pressing Tab...")
    page.keyboard.press("Tab")

    # Get the active element
    element = page.evaluate("document.activeElement")
    print(f"Active element: {element}")

    # Verify the skip link is visible
    skip_link = page.locator("a[href='#main-content']")
    expect(skip_link).to_be_visible()

    print("Taking screenshot of focused skip link...")
    page.screenshot(path="verification/skip_link_focused.png")

    # Click the link (by pressing Enter, since it's focused)
    print("Pressing Enter...")
    page.keyboard.press("Enter")

    # Verify URL hash
    print(f"Current URL: {page.url}")
    assert "#main-content" in page.url

    print("Verification successful!")

if __name__ == "__main__":
    with sync_playwright() as p:
        print("Launching browser...")
        browser = p.chromium.launch(headless=True)
        context = browser.new_context()
        page = context.new_page()
        try:
            verify_skip_link(page)
        except Exception as e:
            print(f"Error: {e}")
            page.screenshot(path="verification/error.png")
        finally:
            browser.close()
