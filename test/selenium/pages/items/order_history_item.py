from selenium.webdriver.common.by import By
import re


class OrderHistoryLineItem:
    """
    주문 내역 카드내 상품
    """

    def __init__(self, driver, element):
        self.driver = driver
        self.element = element

    @property
    def name(self) -> str:
        try:
            return self.element.find_element(By.CSS_SELECTOR, "p.mb-0").text.strip()
        except Exception:
            return ""

    @property
    def summary(self) -> str:
        try:
            return self.element.find_element(By.CSS_SELECTOR, "span.monospace-font").text.strip()
        except Exception:
            return ""


class OrderHistoryItem:
    """
    주문 내역 카드를 래핑하여 필드 접근 및 동작을 제공
    """

    def __init__(self, driver, element):
        self.driver = driver
        self.element = element

    @property
    def order_id(self) -> str:
        try:
            # 예: "#28" 형태 -> 그대로 반환, 필요시 숫자만 추출 가능
            return self.element.find_element(By.CSS_SELECTOR, ".card-header span.font-monospace").text.strip()
        except Exception:
            return ""

    @property
    def ordered_at(self) -> str:
        try:
            return self.element.find_element(By.CSS_SELECTOR, ".card-header > span:not(.font-monospace)").text.strip()
        except Exception:
            return ""

    @property
    def status(self) -> str:
        try:
            return self.element.find_element(By.CSS_SELECTOR, ".card-header .float-end span").text.strip()
        except Exception:
            return ""

    def get_line_items(self) -> list[OrderHistoryLineItem]:
        try:
            elements = self.element.find_elements(By.CSS_SELECTOR, ".card-body ul li")
            return [OrderHistoryLineItem(self.driver, el) for el in elements]
        except Exception:
            return []

    @property
    def total_amount(self) -> str:
        try:
            return self.element.find_element(By.CSS_SELECTOR, ".card-body h6").text.strip()
        except Exception:
            return ""

    @staticmethod
    def collect_from_page(driver) -> list["OrderHistoryItem"]:
        """페이지에서 주문 내역 카드들을 수집"""
        try:
            cards = driver.find_elements(By.CSS_SELECTOR, "div.card.shadow")
            return [OrderHistoryItem(driver, el) for el in cards]
        except Exception:
            return []