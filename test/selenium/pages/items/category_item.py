from selenium.webdriver.common.by import By
import re
from selenium.webdriver.support.ui import WebDriverWait


class CategoryItem:
    """
    카테고리를 래핑하여 필드 접근 및 동작을 제공
    """

    def __init__(self, driver, root_element):
        self.driver = driver
        self.root = root_element

    @property
    def category_name(self) -> str:
        try:
            raw = self.root.text.strip()
            # 카테고리 이름름 뒤쪽에 붙는 " (숫자)" 꼬리표 제거
            return re.sub(r"\s*\(\d+\)\s*$", "", raw)
        except Exception:
            return ""

    @property
    def is_active(self) -> bool:
        try:
            class_name = self.root.get_attribute("class")
            return "active" in class_name if class_name else False
        except Exception:
            return False

    def click(self):
        self.root.click()