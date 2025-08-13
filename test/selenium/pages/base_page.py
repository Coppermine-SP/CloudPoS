from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.common.action_chains import ActionChains
from selenium.common.exceptions import ElementClickInterceptedException, TimeoutException, WebDriverException

class BasePage:
    def __init__(self, driver):
        self.driver = driver

    def find(self, locator, timeout=10):
        return WebDriverWait(self.driver, timeout).until(
            EC.presence_of_element_located(locator)
        )

    def find_all(self, locator, timeout=10):
        return WebDriverWait(self.driver, timeout).until(
            EC.presence_of_all_elements_located(locator)
        )

    def click(self, locator, timeout=10):
        self.find(locator, timeout).click()

    def type(self, locator, text, timeout=10):
        element = self.find(locator, timeout)
        element.clear()
        element.send_keys(text)

    def scroll_into_view(self, element):
        try:
            self.driver.execute_script(
                "arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});",
                element,
            )
        except Exception:
            pass

    def wait_until_clickable(self, target, timeout=10):
        try:
            return WebDriverWait(self.driver, timeout).until(EC.element_to_be_clickable(target))
        except TimeoutException:
            return None

    def click_element(self, element, timeout=10):
        self.scroll_into_view(element)
        clickable = self.wait_until_clickable(element, timeout)
        try:
            (clickable or element).click()
            return
        except (ElementClickInterceptedException, WebDriverException):
            pass

        try:
            ActionChains(self.driver).move_to_element(element).pause(0.1).click().perform()
            return
        except (ElementClickInterceptedException, WebDriverException):
            pass

        # Fallback to JS click
        self.driver.execute_script("arguments[0].click();", element)