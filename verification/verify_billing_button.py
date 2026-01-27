from playwright.sync_api import sync_playwright, expect
import time

def run(playwright):
    browser = playwright.chromium.launch(headless=True)
    context = browser.new_context()
    page = context.new_page()

    # Retry logic for server startup
    print("Attempting to connect to http://localhost:3000/billing...")
    connected = False
    for i in range(15):
        try:
            page.goto("http://localhost:3000/billing")
            connected = True
            print("Connected!")
            break
        except Exception as e:
            print(f"Attempt {i+1} failed. Retrying in 2s...")
            time.sleep(2)

    if not connected:
        print("Failed to connect to server after multiple attempts.")
        browser.close()
        return

    try:
        # Check for the button
        # The button text is "Already a subscriber? Manage your subscription"
        # We search for a part of it to be safe
        print("Looking for 'Manage your subscription' button...")
        button = page.get_by_role("button", name="Manage your subscription")

        expect(button).to_be_visible(timeout=10000)
        print("Button found!")

        # Take screenshot
        output_path = "verification/billing_page.png"
        page.screenshot(path=output_path, full_page=True)
        print(f"Screenshot saved to {output_path}")

    except Exception as e:
        print(f"Verification failed: {e}")
        page.screenshot(path="verification/error_state.png")

    finally:
        browser.close()

if __name__ == "__main__":
    with sync_playwright() as playwright:
        run(playwright)
