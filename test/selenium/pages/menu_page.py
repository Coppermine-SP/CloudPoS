from pages import BasePage
from selenium.webdriver.common.by import By
from pages.items import CategoryItem, MenuItem
from selenium.webdriver.support.ui import WebDriverWait


class MenuPage(BasePage):
    # 메뉴 카드 컨테이너들
    MENU_ITEM_CARDS = (By.CSS_SELECTOR, "div.card.item")
    MENU_CATEGORY_BUTTONS = (By.CSS_SELECTOR, "div.category-item")

    def get_menu_items(self, timeout=10):
        """
        메뉴 아이템 목록 반환
        """
        elements = self.find_all(self.MENU_ITEM_CARDS, timeout)
        return [MenuItem(self.driver, el) for el in elements]

    def find_item_by_title(self, title_text: str, timeout=10):
        """
        메뉴 아이템 중 타이틀이 일치하는 아이템 반환
        """
        for item in self.get_menu_items(timeout):
            if item.title == title_text:
                return item
        return None

    def add_item_by_title(self, title_text: str, timeout=10) -> bool:
        """
        메뉴 아이템 중 타이틀이 일치하는 아이템 장바구니에 추가
        """
        item = self.find_item_by_title(title_text, timeout)
        if not item:
            return False
        item.add_to_cart()
        return True

    def is_redirected_to_history(self):
        """
        주문 내역 페이지로 리다이렉트되었는지 확인
        """
        try:
            nav_link = self.driver.find_element(By.LINK_TEXT, "주문 내역")
            self.click_element(nav_link)
        except Exception:
            pass

        try:
            WebDriverWait(self.driver, 10).until(
                lambda d: "/History" in d.current_url or "/history" in d.current_url
            )
            return True
        except Exception:
            return False

    def get_menu_categories(self, timeout=10):
        """
        메뉴 카테고리 목록 반환
        """
        elements = self.find_all(self.MENU_CATEGORY_BUTTONS, timeout)
        return [CategoryItem(self.driver, el) for el in elements]

    def find_category_by_text(self, text: str, timeout=10):
        """
        메뉴 카테고리 중 텍스트가 일치하는 카테고리 반환
        """
        for category in self.get_menu_categories(timeout):
            if category.category_name == text:
                return category
        return None

    def change_menu_category(self, category_text: str, timeout=10):
        """
        메뉴 카테고리 중 텍스트가 일치하는 카테고리 클릭
        """
        category = self.find_category_by_text(category_text, timeout)
        if not category:
            return False
        category.click()
        return True
