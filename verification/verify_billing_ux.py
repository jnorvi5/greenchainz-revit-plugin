
from playwright.sync_api import sync_playwright, expect

def run():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        context = browser.new_context()
        page = context.new_page()

        try:
            # Go to the billing page
            print("Navigating to billing page...")
            page.goto("http://localhost:3000/billing")

            # Wait for content to load
            print("Waiting for content...")
            page.wait_for_selector("main#main-content")

            # 1. Verify "Back to Dashboard" link attributes
            print("Verifying 'Back to Dashboard' link...")
            back_link = page.locator("a[href='/dashboard']")
            expect(back_link).to_be_visible()

            # Check title attribute
            title = back_link.get_attribute("title")
            print(f"Back link title: {title}")
            assert title == "Return to your dashboard"

            # Check hover class (tailwind class verification is tricky in computed styles,
            # but we can check if the class string contains it)
            class_attr = back_link.get_attribute("class")
            print(f"Back link classes: {class_attr}")
            assert "hover:underline" in class_attr

            # 2. Verify "Manage Subscription" button attributes
            print("Verifying 'Manage Subscription' button...")
            manage_btn = page.get_by_role("button", name="Already a subscriber? Manage your subscription")
            expect(manage_btn).to_be_visible()

            # Check title attribute
            btn_title = manage_btn.get_attribute("title")
            print(f"Manage button title: {btn_title}")
            assert btn_title == "Manage your existing subscription and billing details"

            # 3. Verify Pricing Cards Accessibility
            print("Verifying Pricing Cards...")

            # Check Pro Tier (index 1)
            # Find the card container
            # We used aria-labelledby="tier-name-{id}"
            pro_card = page.locator("div[aria-labelledby='tier-name-pro']")
            expect(pro_card).to_be_visible()

            # Verify the Subscribe button has aria-describedby
            subscribe_btn = pro_card.get_by_role("button", name="Subscribe")
            described_by = subscribe_btn.get_attribute("aria-describedby")
            print(f"Pro subscribe button described by: {described_by}")
            assert described_by == "tier-desc-pro"

            # Verify the description element exists
            desc_el = page.locator(f"#{described_by}")
            expect(desc_el).to_be_visible()
            print("Description element found.")

            # Take a screenshot
            print("Taking screenshot...")
            page.screenshot(path="verification/billing_page_ux.png")
            print("Screenshot saved to verification/billing_page_ux.png")

        except Exception as e:
            print(f"Error: {e}")
            # Take error screenshot
            page.screenshot(path="verification/error.png")
            raise e
        finally:
            browser.close()

if __name__ == "__main__":
    run()
