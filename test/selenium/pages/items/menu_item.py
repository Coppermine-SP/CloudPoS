from selenium.webdriver.common.by import By
import re
from selenium.webdriver.support.ui import WebDriverWait


class MenuItem:
    """
    단일 메뉴 카드 요소를 래핑하여 필드 접근 및 동작을 제공
    """

    TITLE = (By.CSS_SELECTOR, ".card-title")
    DESCRIPTION = (By.CSS_SELECTOR, ".card-text")
    PRICE = (By.CSS_SELECTOR, "p.mb-0")
    ADD_BUTTON = (By.CSS_SELECTOR, "button.btn.btn-primary")

    def __init__(self, driver, root_element):
        self.driver = driver
        self.root = root_element

    @property
    def title(self) -> str:
        try:
            return self.root.find_element(*self.TITLE).text.strip()
        except Exception:
            return ""

    @property
    def description(self) -> str:
        try:
            return self.root.find_element(*self.DESCRIPTION).text.strip()
        except Exception:
            return ""

    @property
    def price_text(self) -> str:
        try:
            return self.root.find_element(*self.PRICE).text.strip()
        except Exception:
            return ""

    @property
    def price_value(self):
        text = self.price_text
        if not text:
            return None
        digits = re.sub(r"[^0-9]", "", text)
        return int(digits) if digits else None

    def add_to_cart(self):
        button = self.root.find_element(*self.ADD_BUTTON)
        try:
            self.driver.execute_script(
                """
                const el = arguments[0];
                el.scrollIntoView({block: 'center', inline: 'nearest'});
                const r = el.getBoundingClientRect();
                const desiredBottomGap = 140;
                const bottomGap = window.innerHeight - r.bottom;
                if (bottomGap < desiredBottomGap) {
                  window.scrollBy(0, desiredBottomGap - bottomGap);
                }
                """,
                button,
            )
        except Exception:
            pass

        try:
            WebDriverWait(self.driver, 5).until(
                lambda d: d.execute_script(
                    """
                    const el = arguments[0];
                    const r = el.getBoundingClientRect();
                    const x = Math.floor(r.left + r.width / 2);
                    const y = Math.floor(r.top + r.height / 2);
                    const topEl = document.elementFromPoint(x, y);
                    return topEl === el || el.contains(topEl);
                    """,
                    button,
                )
            )
        except Exception:
            pass

        try:
            button.click()
            return
        except Exception:
            try:
                from selenium.webdriver.common.action_chains import ActionChains
                ActionChains(self.driver).move_to_element(button).pause(0.1).click().perform()
                return
            except Exception:
                self.driver.execute_script("arguments[0].click();", button)

    def open_detail(self):
        try:
            self.root.click()
        except Exception:
            from selenium.webdriver.common.action_chains import ActionChains
            ActionChains(self.driver).move_to_element(self.root).pause(0.1).click().perform()
            return
        except Exception:
            self.driver.execute_script("arguments[0].click();", self.root)