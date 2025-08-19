from pages.authorize_page import AuthorizePage
from pages.menu_page import MenuPage
from pages.items import Cart

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

#!TODO: 메뉴 하드코딩 수정
def test_add_item_to_cart(driver, customer_base_url, customer_auth_code):
    driver.get(_compose_authorize_url(customer_base_url))
    auth = AuthorizePage(driver)
    auth.authorize(customer_auth_code)
    assert auth.is_redirected_to_menu()

    menu = MenuPage(driver)

    assert menu.add_item_by_title("야키소바", timeout=10)

def test_redirect_to_history(driver, customer_base_url, customer_auth_code):
    driver.get(_compose_authorize_url(customer_base_url))
    auth = AuthorizePage(driver)
    auth.authorize(customer_auth_code)
    assert auth.is_redirected_to_menu()

    menu = MenuPage(driver)
    assert menu.is_redirected_to_history()

#!TODO: 메뉴 하드코딩 수정
def test_change_menu_category(driver, customer_base_url, customer_auth_code):
    driver.get(_compose_authorize_url(customer_base_url))
    auth = AuthorizePage(driver)
    auth.authorize(customer_auth_code)
    assert auth.is_redirected_to_menu()

    menu = MenuPage(driver)
    assert menu.change_menu_category("맥주", timeout=10)

#!TODO: 메뉴 하드코딩 수정
def test_order_menu_item(driver, customer_base_url, customer_auth_code):
    driver.get(_compose_authorize_url(customer_base_url))
    auth = AuthorizePage(driver)
    auth.authorize(customer_auth_code)
    assert auth.is_redirected_to_menu()

    menu = MenuPage(driver)
    assert menu.order_menu_item("야키소바", timeout=10)

#!TODO: 메뉴 하드코딩 수정
def test_open_menu_item_detail(driver, customer_base_url, customer_auth_code):
    driver.get(_compose_authorize_url(customer_base_url))
    auth = AuthorizePage(driver)
    auth.authorize(customer_auth_code)
    assert auth.is_redirected_to_menu()

    menu = MenuPage(driver)
    assert menu.open_menu_item_detail("야키소바", timeout=10)

def test_open_hamburger_menu(driver, customer_base_url, customer_auth_code):
    driver.get(_compose_authorize_url(customer_base_url))
    auth = AuthorizePage(driver)
    auth.authorize(customer_auth_code)
    assert auth.is_redirected_to_menu()

    menu = MenuPage(driver)
    assert menu.open_hamburger_menu(timeout=10)

def test_staff_call_button(driver, customer_base_url, customer_auth_code):
    driver.get(_compose_authorize_url(customer_base_url))
    auth = AuthorizePage(driver)
    auth.authorize(customer_auth_code)
    assert auth.is_redirected_to_menu()

    menu = MenuPage(driver)
    assert menu.open_hamburger_menu(timeout=10)
    assert menu.click_staff_call_button(timeout=10)

    assert menu.check_modal_open(timeout=10)

def test_session_share_button(driver, customer_base_url, customer_auth_code):
    driver.get(_compose_authorize_url(customer_base_url))
    auth = AuthorizePage(driver)
    auth.authorize(customer_auth_code)
    assert auth.is_redirected_to_menu()

    menu = MenuPage(driver)
    assert menu.open_hamburger_menu(timeout=10)
    assert menu.click_session_share_button(timeout=10)

    assert menu.check_modal_open(timeout=10)

def test_change_theme(driver, customer_base_url, customer_auth_code):
    driver.get(_compose_authorize_url(customer_base_url))
    auth = AuthorizePage(driver)
    auth.authorize(customer_auth_code)
    assert auth.is_redirected_to_menu()

    menu = MenuPage(driver)

    def toggle_theme():
        assert menu.open_hamburger_menu(timeout=10)
        assert menu.click_theme_button(timeout=10)
    
    initial_theme = menu.get_current_theme()
    
    for _ in range(3):
        toggle_theme()
        new_theme = menu.get_current_theme()
        if initial_theme != new_theme:
            break
    
    assert initial_theme != new_theme
