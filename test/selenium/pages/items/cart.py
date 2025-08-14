from selenium.webdriver.common.by import By
class Cart:
    """
    주문서(장바구니) 요소를 래핑하여 필드 접근 및 동작을 제공
    """

    CART_TOGGLE_BUTTON = (By.CSS_SELECTOR, "button.cart-toggle")
    CART_PANEL = (By.CSS_SELECTOR, "div.cart-panel")
    ORDER_BUTTON = (By.CSS_SELECTOR, "button.btn.btn-success.rounded-5")
    CART_ITEMS = (By.CSS_SELECTOR, "div.border-bottom.py-2.d-flex.justify-content-between")
    CART_ITEM_NAME = (By.CSS_SELECTOR, "span.me-1")
    CART_ITEM_PRICE = (By.CSS_SELECTOR, "span.text-secondary.me-3")
    CART_ITEM_QUANTITY = (By.CSS_SELECTOR, "div.form-control.px-2.py-0.text-center.mx-1")
    CART_ITEM_REMOVE_BUTTON = (By.CSS_SELECTOR, "button.btn.p-0.border-0.bg-transparent.me-2")
    CART_ITEM_DECREASE_BUTTON = (By.CSS_SELECTOR, "button.cart-toggle.btn.p-0.border-0.bg-transparent i.bi-dash")
    CART_ITEM_INCREASE_BUTTON = (By.CSS_SELECTOR, "button.cart-toggle.btn.p-0.border-0.bg-transparent i.bi-plus")

    def __init__(self, driver, root_element):
        self.driver = driver
        self.root = root_element

    def click_cart_toggle(self):
        self.root.find_element(*self.CART_TOGGLE_BUTTON).click()

    def is_cart_open(self):
        try:
            cart_panel = self.root.find_element(*self.CART_PANEL)
            class_name = cart_panel.get_attribute("class")
            return "open" in class_name if class_name else False
        except Exception:
            return False

    def is_cart_closed(self):
        return not self.is_cart_open()

    def click_order_button(self):
        from selenium.webdriver.support.ui import WebDriverWait
        from selenium.webdriver.support import expected_conditions as EC
        from selenium.webdriver.common.action_chains import ActionChains

        def _enabled_button(driver):
            try:
                btn = self.root.find_element(*self.ORDER_BUTTON)
                return btn if btn.is_enabled() and not btn.get_attribute("disabled") else False
            except Exception:
                return False

        order_button = WebDriverWait(self.driver, 10).until(_enabled_button)
        try:
            order_button.click()
        except Exception:
            try:
                ActionChains(self.driver).move_to_element(order_button).pause(0.1).click().perform()
            except Exception:
                self.driver.execute_script("arguments[0].click();", order_button)

    def confirm_order(self):
        """
        주문서 확인 모달창에서 확인 버튼을 클릭
        """
        from selenium.webdriver.support.ui import WebDriverWait
        from selenium.webdriver.support import expected_conditions as EC
        from selenium.webdriver.common.action_chains import ActionChains

        locator = (By.XPATH, "//button[contains(@class,'btn-primary') and normalize-space()='확인']")
        confirm_button = WebDriverWait(self.driver, 10).until(EC.element_to_be_clickable(locator))
        try:
            confirm_button.click()
        except Exception:
            try:
                ActionChains(self.driver).move_to_element(confirm_button).pause(0.1).click().perform()
            except Exception:
                self.driver.execute_script("arguments[0].click();", confirm_button)

    def check_cart_item(self, item_name: str):
        try:
            cart_items = self.root.find_elements(*self.CART_ITEMS)
            for cart_item in cart_items:
                if cart_item.find_element(*self.CART_ITEM_NAME).text == item_name:
                    return True
        except Exception:
            return False

    def click_cart_item_remove_button(self, item_name: str):
        try:
            cart_items = self.root.find_elements(*self.CART_ITEMS)
            for cart_item in cart_items:
                if cart_item.find_element(*self.CART_ITEM_NAME).text == item_name:
                    cart_item.find_element(*self.CART_ITEM_REMOVE_BUTTON).click()
                    return True
        except Exception:
            return False

    def click_cart_item_decrease_button(self, item_name: str):
        try:
            cart_items = self.root.find_elements(*self.CART_ITEMS)
            for cart_item in cart_items:
                if cart_item.find_element(*self.CART_ITEM_NAME).text == item_name:
                    cart_item.find_element(*self.CART_ITEM_DECREASE_BUTTON).click()
                    return True
        except Exception:
            return False

    def click_cart_item_increase_button(self, item_name: str):
        try:
            cart_items = self.root.find_elements(*self.CART_ITEMS)
            for cart_item in cart_items:
                if cart_item.find_element(*self.CART_ITEM_NAME).text == item_name:
                    cart_item.find_element(*self.CART_ITEM_INCREASE_BUTTON).click()
                    return True
        except Exception:
            return False