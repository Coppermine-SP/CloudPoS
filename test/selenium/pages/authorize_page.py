from pages import BasePage
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait

class AuthorizePage(BasePage):
    # 환영 문구
    WELCOME_MESSAGE = (By.XPATH, "//h2[contains(text(), '환영합니다!')]")
    # 상점 이름
    SHOP_NAME = (By.XPATH, "//h3[contains(text(), '클라우드인터렉티브입니다.')]")

    # 도움말 및 개인정보처리방침 링크
    LEGAL_LINK = (By.XPATH, "//a[contains(text(), '도움말 및 개인정보처리방침')]")

    # 인증 코드 입력 필드
    AUTH_CODE_INPUT_1 = (By.ID, "code-1")
    AUTH_CODE_INPUT_2 = (By.ID, "code-2")
    AUTH_CODE_INPUT_3 = (By.ID, "code-3")
    AUTH_CODE_INPUT_4 = (By.ID, "code-4")

    # 인증 방법 설명
    AUTH_METHOD_DESCRIPTION = (By.XPATH, "//h6[contains(text(), '주문을 시작하려면, 인증 코드를 입력하세요.')]")

    def is_rendered_properly(self):
        """
        페이지가 정상적으로 렌더링되었는지 확인
        """
        try:
            self.find(self.WELCOME_MESSAGE)
            self.find(self.SHOP_NAME)
            self.find(self.LEGAL_LINK)
            self.find(self.AUTH_METHOD_DESCRIPTION)
            self.find(self.AUTH_CODE_INPUT_1)
            self.find(self.AUTH_CODE_INPUT_2)
            self.find(self.AUTH_CODE_INPUT_3)
            self.find(self.AUTH_CODE_INPUT_4)
            return True
        except:
            return False

    def is_redirected_to_menu(self, timeout: int = 10):
        """
        메뉴 페이지로 리다이렉트되었는지 확인
        """
        try:
            WebDriverWait(self.driver, timeout).until(
                lambda d: "/Menu" in d.current_url or "/menu" in d.current_url
            )
            return True 
        except Exception:
            return False

    def is_redirected_to_legal_page(self, timeout: int = 10):
        """
        도움말 및 개인정보처리방침 페이지로 리다이렉트되었는지 확인
        """
        try:    
            WebDriverWait(self.driver, timeout).until(
                lambda d: "/About" in d.current_url or "/about" in d.current_url
            )
            return True
        except Exception:
            return False

    def authorize(self, auth_code):
        """
        인증 코드 입력
        """
        self.type(self.AUTH_CODE_INPUT_1, auth_code[:1])
        self.type(self.AUTH_CODE_INPUT_2, auth_code[1:2])
        self.type(self.AUTH_CODE_INPUT_3, auth_code[2:3])
        self.type(self.AUTH_CODE_INPUT_4, auth_code[3:4])

    def click_legal_link(self):
        """
        도움말 및 개인정보처리방침 링크 클릭
        """
        el = self.find(self.LEGAL_LINK)
        self.click_element(el)

class AdminAuthorizePage(BasePage):
    ADMIN_AUTH_INPUT_FORM = (By.XPATH, "/html/body/div[3]/div/div[1]/form/div/input")
    ADMIN_AUTH_BUTTON = (By.XPATH, "//*[@id='button-addon2']")

    def authorize(self, auth_code):
        """
        인증 코드 입력
        """
        self.type(self.ADMIN_AUTH_INPUT_FORM, auth_code)
        self.click_element(self.ADMIN_AUTH_BUTTON)

    def is_redirected_to_admin_page(self, timeout: int = 10):
        """
        관리자 페이지로 리다이렉트되었는지 확인
        """
        try:
            WebDriverWait(self.driver, timeout).until(
                lambda d: "/tableview" in d.current_url
            )
            return True
        except Exception:
            return False

    def redirect_to_order_page(self, timeout: int = 10):
        """
        관리자 주문 페이지로 이동
        """
        try:
            import re
            current = self.driver.current_url
            m = re.match(r"^(https?://[^/]+)", current)
            origin = m.group(1) if m else ""
            dest = f"{origin}/administrative/orderview"
            self.driver.get(dest)
            WebDriverWait(self.driver, timeout).until(
                lambda d: "/orderview" in d.current_url.lower()
            )
            return True
        except Exception:
            return False