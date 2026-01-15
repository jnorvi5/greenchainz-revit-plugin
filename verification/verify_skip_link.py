
from playwright.sync_api import sync_playwright

def verify_skip_link():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()

        # Navigate to the home page
        page.goto("http://localhost:3000")

        # Press Tab to focus the first element
        page.keyboard.press("Tab")

        # The first focused element should be the "Skip to main content" link
        focused_element = page.evaluate("document.activeElement.textContent")
        focused_href = page.evaluate("document.activeElement.getAttribute('href')")

        print(f"Focused element text: {focused_element}")
        print(f"Focused element href: {focused_href}")

        if "Skip to main content" in focused_element and focused_href == "#main-content":
            print("SUCCESS: Skip link is the first focusable element.")

            # Take a screenshot while focused to show it's visible
            page.screenshot(path="verification/skip_link_focused.png")

            # Click the link (simulate Enter key)
            page.keyboard.press("Enter")

            # Verify the hash in URL or scroll position (hard to verify scroll in headless without complex logic,
            # but we can check if the active element or target is correct, though mostly browsers just scroll).
            # The URL should have the hash.
            current_url = page.url
            print(f"Current URL after click: {current_url}")

            if "#main-content" in current_url:
                 print("SUCCESS: URL updated with hash.")
            else:
                 print("WARNING: URL did not update with hash (might be handled by router or native behavior).")

        else:
            print("FAILURE: First focused element is NOT the skip link.")

        browser.close()

if __name__ == "__main__":
    verify_skip_link()
