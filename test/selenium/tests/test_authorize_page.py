from selenium.webdriver.common.by import By
from pages.authorize_page import AuthorizePage

def _compose_authorize_url(base_url: str) -> str:
    url = base_url.rstrip('/')
    return f"{url}/Authorize" if '/Authorize' not in url else url

def test_render_authorize_page(driver, customer_base_url):
    driver.get(_compose_authorize_url(customer_base_url))
    page = AuthorizePage(driver)
    assert page.is_rendered_properly()

def test_authorize_redirects_to_menu(driver, customer_base_url, customer_auth_code):
    driver.get(_compose_authorize_url(customer_base_url))
    page = AuthorizePage(driver)
    page.authorize(customer_auth_code)
    assert page.is_redirected_to_menu()

def test_legal_link_redirects_to_legal_page(driver, customer_base_url):
    driver.get(_compose_authorize_url(customer_base_url))
    page = AuthorizePage(driver)
    page.click_legal_link()
    assert page.is_redirected_to_legal_page()