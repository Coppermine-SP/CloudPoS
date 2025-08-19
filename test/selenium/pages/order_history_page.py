from pages import BasePage
from pages.items import OrderHistoryItem
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC

class OrderHistoryPage(BasePage):
    def __init__(self, driver):
        super().__init__(driver)

    def get_order_history(self):
        try:
            WebDriverWait(self.driver, 3).until(
                EC.presence_of_all_elements_located((By.CSS_SELECTOR, "div.card.shadow"))
            )
        except Exception:
            pass
        return OrderHistoryItem.collect_from_page(self.driver)

    def is_redirected_to_history(self):
        try:
            WebDriverWait(self.driver, 3).until(
                lambda d: "/History" in d.current_url or "/history" in d.current_url
            )
            WebDriverWait(self.driver, 3).until(
                EC.presence_of_element_located((By.CSS_SELECTOR, "div.card.shadow .card-header"))
            )
            return True
        except Exception:
            return False

    def click_request_payment_button(self):
        self.driver.find_element(By.CSS_SELECTOR, "button.btn-primary").click()
        return True

    def check_payment_modal_appeared(self):
        try:
            WebDriverWait(self.driver, 3).until(
                EC.presence_of_element_located((By.CSS_SELECTOR, "div.modal-dialog"))
            )
            return True
        except Exception:
            return False

    def check_modal_rendered_properly(self):
        try:
            WebDriverWait(self.driver, 3).until(
                EC.presence_of_element_located((By.CSS_SELECTOR, "div.modal-content"))
            )
            
            title_element = self.driver.find_element(By.CSS_SELECTOR, "h5.fw-semibold.mb-2")
            if title_element.text.strip() != "계산 요청하기":
                return False

            message_element = self.driver.find_element(By.CSS_SELECTOR, "p.fs-6.lh-base.mb-2.fw-light")
            expected_message = "정말 계산 요청을 하시겠습니까?\n계산 요청을 하면 더 이상 주문을 할 수 없습니다."
            if message_element.text.strip() != expected_message:
                return False
            
            return True
        except Exception:
            return False
        