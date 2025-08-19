import time
import random
import pytest
from pages.menu_page import MenuPage
from pages.authorize_page import AuthorizePage, AdminAuthorizePage
from pages.admin_page import AdministrativeOrderPage

def _compose_authorize_url(base_url: str) -> str:
    url = base_url.rstrip('/')
    return f"{url}/Authorize" if '/Authorize' not in url else url


# def test_hold_signalr_session(driver, customer_base_url, customer_auth_code, hold_seconds):
#     """
#     단순 로드 테스트: 인증 후 메뉴 페이지에 머무르며 SignalR 세션을 유지한다.
#     병렬 실행(-n N) 시 워커 수 만큼 동시 접속 부하를 발생시킨다.
    
#     옵션:
#     - --hold-seconds: 유지 시간(초)
#     - --auth-codes-file: 워커별 다른 인증코드 분배
#     - --no-browser/--disable-images: 리소스 사용 절감
#     - --grid-url: Selenium Grid 사용 시 원격 실행
#     """
#     driver.get(_compose_authorize_url(customer_base_url))
#     auth = AuthorizePage(driver)
#     auth.authorize(customer_auth_code)
#     assert auth.is_redirected_to_menu()

#     if hold_seconds > 0:
#         time.sleep(hold_seconds)

def test_order_signalr_session(driver, customer_base_url, customer_auth_code, hold_seconds, vu, order_item_name, order_interval_seconds, num_users, round_robin, tick_seconds, order_item_list):
    """
    주문 테스트: 인증 후 주문 함
    병렬 실행(-n N) 시 워커 수 만큼 동시 접속 부하를 발생시킨다.
    
    옵션:
    - --hold-seconds: 유지 시간(초)
    - --auth-codes-file: 워커별 다른 인증코드 분배
    - --no-browser/--disable-images: 리소스 사용 절감
    - --grid-url: Selenium Grid 사용 시 원격 실행
    """

    try:
        print(f"[VU {vu}] AUTH_CODE={customer_auth_code}")
    except Exception:
        pass

    driver.get(_compose_authorize_url(customer_base_url))
    auth = AuthorizePage(driver)
    auth.authorize(customer_auth_code)
    assert auth.is_redirected_to_menu()

    menu = MenuPage(driver)

    try:
        target_item_name = None
        if order_item_list:
            target_item_name = order_item_list[int(vu) % len(order_item_list)]
            if not menu.find_item_by_title(target_item_name, timeout=5):
                items = menu.get_menu_items(timeout=5)
                if not items:
                    print(f"[VU {vu}] no menu items available; skipping loop")
                    return
                target_item_name = items[int(vu) % len(items)].title
        else:
            if not order_item_name:
                items = menu.get_menu_items(timeout=5)
                if not items:
                    print(f"[VU {vu}] no menu items available; skipping loop")
                    return
                target_item_name = items[int(vu) % len(items)].title
            else:
                target_item_name = order_item_name
                if not menu.find_item_by_title(target_item_name, timeout=5):
                    items = menu.get_menu_items(timeout=5)
                    if not items:
                        print(f"[VU {vu}] no menu items available; skipping loop")
                        return
                    target_item_name = items[int(vu) % len(items)].title
    except Exception:
        return

    if hold_seconds > 0:
        end_at = time.time() + hold_seconds
        next_tick_at = time.time()
        while time.time() < end_at:
            now = time.time()
            if round_robin:
                if now < next_tick_at:
                    time.sleep(min(0.01, next_tick_at - now))
                    continue
                current_slot = int(now // max(0.001, tick_seconds)) % max(1, num_users)
                if current_slot != int(vu):
                    next_tick_at = (int(now // max(0.001, tick_seconds)) + 1) * max(0.001, tick_seconds)
                    continue
                next_tick_at = (int(now // max(0.001, tick_seconds)) + 1) * max(0.001, tick_seconds)

            try:
                ok = menu.order_menu_item(target_item_name, timeout=10)
                print(f"[VU {vu}] order attempt -> {ok}")
            except Exception as e:
                print(f"[VU {vu}] order exception: {e}")

            if not round_robin:
                sleep_sec = order_interval_seconds if order_interval_seconds is not None else 0
                if sleep_sec > 0:
                    time.sleep(sleep_sec)
    else:
        menu.order_menu_item(target_item_name, timeout=10)

def test_admin_complete_order(driver, administrative_base_url, admin_auth_code, vu, num_users, round_robin, tick_seconds, max_throughput):
    driver.get(_compose_authorize_url(administrative_base_url))
    auth = AdminAuthorizePage(driver)
    auth.authorize(admin_auth_code)
    assert auth.is_redirected_to_admin_page()
    auth.redirect_to_order_page()

    admin = AdministrativeOrderPage(driver)

    try:
        vu_index = int(vu)
    except Exception:
        vu_index = 0
    try:
        total_users = max(1, int(num_users))
    except Exception:
        total_users = 1

    if not max_throughput:
        time.sleep(0.05 * vu_index)

    deadline = time.time() + 60
    while time.time() < deadline:
        orders = admin.get_order_items()
        my_bucket = [o for o in orders if (o.order_number is not None) and (int(o.order_number) % total_users == vu_index)]

        if not my_bucket:
            break

        target = my_bucket[0]

        time.sleep(random.uniform(0.005, 0.03))

        try:
            per_order_offset = ((int(target.order_number) * 37) % 200) / 1000.0
            time.sleep(per_order_offset)
        except Exception:
            pass

        try:
            target.complete_order()
        except Exception:
            time.sleep(0.15)
            continue

        end_poll = time.time() + 1.0
        while time.time() < end_poll:
            remaining = admin.get_order_items()
            if all((x.order_number != target.order_number) for x in remaining):
                break
            time.sleep(0.1)

    final_deadline = time.time() + 2.0
    ok = False
    while time.time() < final_deadline:
        remaining = admin.get_order_items()
        remaining_numbers = {o.order_number for o in remaining if o.order_number is not None}
        if all((int(n) % total_users) != vu_index for n in remaining_numbers):
            ok = True
            break
        time.sleep(0.1)
    assert ok
