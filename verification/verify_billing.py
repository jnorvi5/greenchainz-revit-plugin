
from playwright.sync_api import sync_playwright

def verify_billing_page():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        context = browser.new_context()
        page = context.new_page()

        try:
            # Go to the billing page
            page.goto("http://localhost:3000/billing")

            # Wait for the button to be visible
            button = page.locator("button[title='Manage your existing subscription and billing details']")
            button.wait_for(state="visible")

            # Check if the button has the correct initial text
            if "Already a subscriber? Manage your subscription" not in button.text_content():
                print("FAILED: Button text is incorrect")
                print(f"Actual text: {button.text_content()}")
            else:
                print("SUCCESS: Button text is correct")

            # Check for the correct class names (partial match since Tailwind classes are long)
            # We look for the ghost button classes we added
            class_attr = button.get_attribute("class")
            if "hover:bg-indigo-50" in class_attr and "text-indigo-600" in class_attr:
                print("SUCCESS: Button has ghost button classes")
            else:
                print("FAILED: Button missing ghost button classes")
                print(f"Actual classes: {class_attr}")

            # Take a screenshot
            page.screenshot(path="verification/billing_page.png")
            print("Screenshot saved to verification/billing_page.png")

        except Exception as e:
            print(f"Error during verification: {e}")
        finally:
            browser.close()

if __name__ == "__main__":
    verify_billing_page()
