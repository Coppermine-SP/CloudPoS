from pages import BasePage
from selenium.webdriver.common.by import By
from pages.items import CategoryItem, MenuItem, Cart
from selenium.webdriver.support.ui import WebDriverWait


class MenuPage(BasePage):
    # 메뉴 카드 컨테이너들
    MENU_ITEM_CARDS = (By.CSS_SELECTOR, "div.card.item")
    MENU_CATEGORY_BUTTONS = (By.CSS_SELECTOR, "div.category-item")

    def get_current_theme(self):
        """
        현재 테마 반환
        """
        html_element = self.driver.find_element(By.TAG_NAME, "html")
        return html_element.get_attribute("data-bs-theme")

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

    def order_menu_item(self, item_name: str, timeout=10):
        """
        메뉴 아이템 중 타이틀이 일치하는 아이템 주문
        """
        item = self.find_item_by_title(item_name, timeout)
        if not item:
            return False
        item.add_to_cart()

        cart = Cart(self.driver, self.driver.find_element(By.CSS_SELECTOR, "div.bg-body-tertiary.rounded-5.px-3.py-2.mx-4.mb-2.shadow"))
        cart.click_order_button()
        cart.confirm_order()
        return True

    def open_menu_item_detail(self, item_name: str, timeout=10):
        """
        메뉴 아이템 중 타이틀이 일치하는 아이템 상세보기
        """
        item = self.find_item_by_title(item_name, timeout)
        if not item:
            return False
        item.open_detail()
        return True

    def open_hamburger_menu(self, timeout=10):
        """
        햄버거 메뉴 버튼 클릭
        """
        from selenium.webdriver.support.ui import WebDriverWait
        from selenium.webdriver.support import expected_conditions as EC
        
        wait = WebDriverWait(self.driver, timeout)
        hamburger_button = wait.until(EC.element_to_be_clickable((By.CSS_SELECTOR, "div.btn-dark.dropdown")))
        hamburger_button.click()
        return True

    def click_staff_call_button(self, timeout=10):
        """
        직원 호출 버튼 클릭
        """
        self.driver.find_element(By.CSS_SELECTOR, "a.dropdown-item i.bi-bell-fill").find_element(By.XPATH, "..").click()
        return True

    def click_session_share_button(self, timeout=10):
        """
        세션 공유 버튼 클릭
        """
        self.driver.find_element(By.CSS_SELECTOR, "a.dropdown-item i.bi-qr-code-scan").find_element(By.XPATH, "..").click()
        return True

    def check_modal_open(self, timeout=10):
        """
        모달이 열렸는지 확인
        """
        from selenium.webdriver.support.ui import WebDriverWait
        from selenium.webdriver.support import expected_conditions as EC
        
        wait = WebDriverWait(self.driver, timeout)
        modal = wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, "div.modal-content.border-0.shadow-lg.bg-body-tertiary")))
        return modal.is_displayed()

    def click_theme_button(self, timeout=10):
        """
        테마 변경 버튼 클릭
        """
        from selenium.webdriver.support.ui import WebDriverWait
        from selenium.webdriver.support import expected_conditions as EC
        
        wait = WebDriverWait(self.driver, timeout)
        theme_button = wait.until(EC.element_to_be_clickable((By.XPATH, "/html/body/nav/div/div/div[2]/ul/li[3]/a")))
        theme_button.click()
        return True