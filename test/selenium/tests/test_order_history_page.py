import time
from pages.authorize_page import AuthorizePage
from pages.order_history_page import OrderHistoryPage
from pages.menu_page import MenuPage

def _compose_authorize_url(base_url: str) -> str:
    url = base_url.rstrip('/')
    return f"{url}/Authorize" if '/Authorize' not in url else url

def test_order_history_page(driver, customer_base_url, customer_auth_code):
    driver.get(_compose_authorize_url(customer_base_url))
    auth = AuthorizePage(driver)
    auth.authorize(customer_auth_code)
    assert auth.is_redirected_to_menu()

    menu = MenuPage(driver)
    menu.is_redirected_to_history()

    history = OrderHistoryPage(driver)
    assert history.is_redirected_to_history()

    order_history = history.get_order_history()
    assert len(order_history) > 0

    for order in order_history:
        assert order.order_id
        assert order.ordered_at
        assert order.status
        assert order.total_amount
        assert order.get_line_items()

def test_request_payment(driver, customer_base_url, customer_auth_code):
    driver.get(_compose_authorize_url(customer_base_url))
    auth = AuthorizePage(driver)
    auth.authorize(customer_auth_code)
    assert auth.is_redirected_to_menu()

    menu = MenuPage(driver)
    menu.is_redirected_to_history()

    history = OrderHistoryPage(driver)
    assert history.is_redirected_to_history()

    order_history = history.get_order_history()
    assert len(order_history) > 0

    assert history.click_request_payment_button()
    assert history.check_payment_modal_appeared()
    assert history.check_modal_rendered_properly()