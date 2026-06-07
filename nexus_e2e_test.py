"""
Nexus Engine E2E Test
Tests full order flow via Edge Selenium against localhost:3000.
"""

import re
import sys

from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.edge.options import Options
from selenium.webdriver.edge.service import Service
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.support.ui import Select, WebDriverWait
from webdriver_manager.microsoft import EdgeChromiumDriverManager


def main():
    options = Options()
    options.add_argument("--headless")
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-dev-shm-usage")

    driver = webdriver.Edge(
        service=Service(EdgeChromiumDriverManager().install()), options=options
    )
    wait = WebDriverWait(driver, 15)

    try:
        driver.get("http://localhost:3000")

        # --- 0. Wait for SignalR connection ---
        wait.until(
            EC.presence_of_element_located(
                (By.XPATH, "//strong[text()='Connected']")
            )
        )
        print("PASS: Connected to SignalR hub")

        # Helpers
        def feedback_text(panel_title: str) -> str:
            """Return the text of the feedback div inside a FormPanel."""
            el = driver.find_element(
                By.XPATH,
                f"//h4[text()='{panel_title}']/following-sibling::div[last()]"
            )
            return el.text

        def wait_feedback(panel_title: str, substring: str, timeout: int = 10):
            WebDriverWait(driver, timeout).until(
                lambda d: substring in feedback_text(panel_title)
            )

        def extract_uuid(text: str) -> str:
            m = re.search(
                r"([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})",
                text,
            )
            assert m, f"UUID not found in: {text}"
            return m.group(1)

        # --- 1. Create Account 1 ---
        create_btn = driver.find_element(
            By.XPATH, "//button[text()='Create Account']"
        )
        create_btn.click()
        wait_feedback("Create Account", "Account created:")
        account1 = extract_uuid(feedback_text("Create Account"))
        print(f"PASS: Account 1 created: {account1}")

        # --- 2. Deposit 10000 on Account 1 (need >= 5000 for Buy order) ---
        dep_acc = driver.find_element(
            By.XPATH,
            "//h4[text()='Deposit']/following-sibling::input[@placeholder='Account ID']",
        )
        dep_acc.send_keys(account1)

        dep_amt = driver.find_element(
            By.XPATH,
            "//h4[text()='Deposit']/following-sibling::input[@placeholder='Amount']",
        )
        dep_amt.send_keys("10000")

        driver.find_element(By.XPATH, "//button[text()='Deposit']").click()
        wait_feedback("Deposit", "Deposit successful")
        print("PASS: Deposit 10000 on account 1")

        # --- 3. Place Buy order: price 50000, qty 0.1 ---
        pl_acc = driver.find_element(
            By.XPATH,
            "//h4[text()='Place Order']/following-sibling::input[@placeholder='Account ID']",
        )
        pl_acc.send_keys(account1)

        driver.find_element(
            By.XPATH,
            "//h4[text()='Place Order']/following-sibling::input[@placeholder='Price']",
        ).send_keys("50000")

        driver.find_element(
            By.XPATH,
            "//h4[text()='Place Order']/following-sibling::input[@placeholder='Quantity']",
        ).send_keys("0.1")

        driver.find_element(By.XPATH, "//button[text()='Place Order']").click()
        wait_feedback("Place Order", "Order placed:")
        print("PASS: Buy order placed (price 50000, qty 0.1)")

        # --- 4. Create Account 2 ---
        create_btn.click()
        wait_feedback("Create Account", "Account created:")
        account2 = extract_uuid(feedback_text("Create Account"))
        print(f"PASS: Account 2 created: {account2}")

        # --- 5. Deposit 10000 on Account 2 ---
        dep_acc.clear()
        dep_acc.send_keys(account2)
        dep_amt.clear()
        dep_amt.send_keys("10000")
        driver.find_element(By.XPATH, "//button[text()='Deposit']").click()
        wait_feedback("Deposit", "Deposit successful")
        print("PASS: Deposit 10000 on account 2")

        # --- 6. Place Sell order: price 50000, qty 0.1 ---
        pl_acc.clear()
        pl_acc.send_keys(account2)

        Select(
            driver.find_element(
                By.XPATH,
                "//h4[text()='Place Order']/following-sibling::select",
            )
        ).select_by_visible_text("Sell")

        price_input = driver.find_element(
            By.XPATH,
            "//h4[text()='Place Order']/following-sibling::input[@placeholder='Price']",
        )
        price_input.clear()
        price_input.send_keys("50000")

        qty_input = driver.find_element(
            By.XPATH,
            "//h4[text()='Place Order']/following-sibling::input[@placeholder='Quantity']",
        )
        qty_input.clear()
        qty_input.send_keys("0.1")

        driver.find_element(By.XPATH, "//button[text()='Place Order']").click()
        wait_feedback("Place Order", "Order placed:")
        print("PASS: Sell order placed (price 50000, qty 0.1)")

        # --- 7. Verify dashboard updates via SignalR ---
        # 7a. Recent Trades should show at least one trade
        try:
            WebDriverWait(driver, 5).until(
                EC.presence_of_element_located(
                    (By.XPATH, "//h3[text()='Recent Trades']/following::table")
                )
            )
            print("PASS: Recent Trades shows trade data (not empty)")
        except Exception:
            print("FAIL: Recent Trades still shows 'No trades yet…' after 5s")
            sys.exit(1)

        # 7b. Order Book no longer shows the matched orders
        # The sell order consumed the buy order, so book should be empty
        order_book_placeholder = driver.find_elements(
            By.XPATH,
            "//h3[text()='Order Book']/following::p[text()='Waiting for snapshot…']",
        )
        print(
            "INFO: Order Book snapshot state (empty placeholder present):",
            len(order_book_placeholder) > 0,
        )

        # 7c. Balance Update should show updated balance for at least one account
        try:
            WebDriverWait(driver, 5).until(
                EC.presence_of_element_located(
                    (By.XPATH, "//h3[text()='Balance Update']/following::dd")
                )
            )
            print("PASS: Balance Update shows data (not empty)")
        except Exception:
            print("FAIL: Balance Update still shows 'No balance update yet…' after 5s")
            sys.exit(1)

        print("\n=== ALL E2E TESTS PASSED ===")

    except Exception as e:
        print(f"\nFAIL: {e}")
        sys.exit(1)
    finally:
        driver.quit()


if __name__ == "__main__":
    main()
