from pages.authorize_page import AuthorizePage
from pages.menu_page import MenuPage
import time

def _compose_authorize_url(base_url: str) -> str:
    url = base_url.rstrip('/')
    return f"{url}/Authorize" if '/Authorize' not in url else url

def test_menu_items_list_present(driver, customer_base_url, customer_auth_code):
    driver.get(_compose_authorize_url(customer_base_url))
    auth = AuthorizePage(driver)
    auth.authorize(customer_auth_code)
    assert auth.is_redirected_to_menu()

    menu = MenuPage(driver)
    items = menu.get_menu_items()
    assert len(items) > 0

    first = items[0]
    assert first.title != ""
    assert first.price_value is not None

def test_add_item_to_cart(driver, customer_base_url, customer_auth_code):
    driver.get(_compose_authorize_url(customer_base_url))
    auth = AuthorizePage(driver)
    auth.authorize(customer_auth_code)
    assert auth.is_redirected_to_menu()

    menu = MenuPage(driver)

    assert menu.add_item_by_title("골빔면", timeout=10)

def test_redirect_to_history(driver, customer_base_url, customer_auth_code):
    driver.get(_compose_authorize_url(customer_base_url))
    auth = AuthorizePage(driver)
    auth.authorize(customer_auth_code)
    assert auth.is_redirected_to_menu()

    menu = MenuPage(driver)
    assert menu.is_redirected_to_history()

def test_change_menu_category(driver, customer_base_url, customer_auth_code):
    driver.get(_compose_authorize_url(customer_base_url))
    auth = AuthorizePage(driver)
    auth.authorize(customer_auth_code)
    assert auth.is_redirected_to_menu()

    menu = MenuPage(driver)
    assert menu.change_menu_category("사이드", timeout=10)