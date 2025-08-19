from selenium.webdriver.common.by import By
from pages.base_page import BasePage
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC


class AdminOrderItem(BasePage):
    """
    관리자 주문 카드(1건)를 래핑하는 객체
    """

    COMPLETE_ORDER_BUTTON = (By.CSS_SELECTOR, "#offcanvasResponsive .offcanvas-body .btn.btn-success")
    CANCEL_ORDER_BUTTON = (By.CSS_SELECTOR, "#offcanvasResponsive .offcanvas-body .btn.btn-danger")
    CONFIRM_ORDER_BUTTON = (By.CSS_SELECTOR, ".modal.show .btn.btn-primary")

    def __init__(self, driver, element):
        super().__init__(driver)
        self.element = element

    @property
    def order_number(self):
        """
        주문 번호 반환 (예: 5)
        """
        try:
            header = self.element.find_element(By.CLASS_NAME, "card-header")
            import re
            m = re.search(r"#(\d+)", header.text)
            if m:
                return int(m.group(1))
        except Exception:
            pass
        return None

    @property
    def table_number(self):
        """
        카드 우측 상단의 테이블 번호 반환 (예: 1)
        """
        try:
            header = self.element.find_element(By.CLASS_NAME, "card-header")
            span = header.find_element(By.CLASS_NAME, "float-end")
            return int(span.text.strip())
        except Exception:
            return None

    @property
    def items(self):
        """
        주문 내역(아이템 리스트) 반환 (예: ["닭다리살 소금꼬치 2p x 1"])
        """
        try:
            body = self.element.find_element(By.CLASS_NAME, "card-body")
            ul = body.find_element(By.CLASS_NAME, "order-items-list")
            lis = ul.find_elements(By.TAG_NAME, "li")
            return [li.text.strip() for li in lis]
        except Exception:
            return []

    def click_self(self):
        self.driver.execute_script("arguments[0].scrollIntoView({block: 'center'}); arguments[0].click();", self.element)
    
    def complete_order(self):
        self.click_self()
        try:
            WebDriverWait(self.driver, 3).until(
                EC.presence_of_element_located((By.CSS_SELECTOR, "#offcanvasResponsive.show"))
            )
        except Exception:
            pass
        complete_btn = self.find(self.COMPLETE_ORDER_BUTTON, timeout=10)
        self.driver.execute_script("arguments[0].click();", complete_btn)
        confirm_btn = self.find(self.CONFIRM_ORDER_BUTTON, timeout=10)
        self.click_element(confirm_btn)
        try:
            WebDriverWait(self.driver, 10).until(EC.staleness_of(self.element))
        except Exception:
            pass

    def cancel_order(self):
        self.click_self()
        try:
            WebDriverWait(self.driver, 10).until(
                EC.presence_of_element_located((By.CSS_SELECTOR, "#offcanvasResponsive.show"))
            )
        except Exception:
            pass
        cancel_btn = self.find(self.CANCEL_ORDER_BUTTON, timeout=10)
        self.click_element(cancel_btn)
        confirm_btn = self.find(self.CONFIRM_ORDER_BUTTON, timeout=10)
        self.click_element(confirm_btn)
        try:
            WebDriverWait(self.driver, 10).until(EC.staleness_of(self.element))
        except Exception:
            pass