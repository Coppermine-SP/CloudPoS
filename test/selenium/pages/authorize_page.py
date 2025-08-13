from pages import BasePage
from selenium.webdriver.common.by import By

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

    def is_redirected_to_menu(self):
        """
        메뉴 페이지로 리다이렉트되었는지 확인
        """
        return self.driver.current_url.endswith("/Menu")

    def is_redirected_to_legal_page(self):
        """
        도움말 및 개인정보처리방침 페이지로 리다이렉트되었는지 확인
        """
        return self.driver.current_url.endswith("/About")

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
        self.click(self.LEGAL_LINK)