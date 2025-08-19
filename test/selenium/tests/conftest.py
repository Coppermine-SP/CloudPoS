import os
import sys
import pytest
from selenium import webdriver
from selenium.webdriver.chrome.options import Options

_TEST_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
if _TEST_ROOT not in sys.path:
    sys.path.insert(0, _TEST_ROOT)

def pytest_addoption(parser):
    # 고객 사이트의 기본 URL을 설정하는 옵션 추가
    parser.addoption("--customer-base-url", action="store", default="https://dev-ecomm-svc.cloudinteractive.net/Customer",
                     help="Root URL for the customer site")
    # 고객 인증 코드를 설정하는 옵션 추가
    parser.addoption("--customer-auth-code", action="store", default=None,
                     help="Customer authorization code (4 chars)")
    # 브라우저 UI 비활성화 옵션 추가
    parser.addoption("--no-browser", action="store_true", default=False,
                     help="Run tests in headless mode (no browser UI)")
    # Selenium Grid 사용 옵션 추가
    parser.addoption("--grid-url", action="store", default=None,
                     help="Selenium Grid URL (e.g., http://localhost:4444/wd/hub)")
    # 이미지 로딩 비활성화 옵션 (로드 테스트 시 리소스 사용량 절감)
    parser.addoption("--disable-images", action="store_true", default=False,
                     help="Disable image loading in the browser to reduce resource usage")
    # 로드 테스트용: 접속 유지 시간(초)
    parser.addoption("--hold-seconds", action="store", type=int, default=0,
                     help="After reaching target page, hold the session for N seconds (for load tests)")
    # 로드 테스트용: 인증코드 목록 파일 (워커별 분산)
    parser.addoption("--auth-codes-file", action="store", default=None,
                     help="Path to a file that contains one auth code per line. Codes are distributed per worker.")
    # 로드 테스트용: 가상 사용자 수(동일 테스트를 N회 수집)
    parser.addoption("--users", action="store", type=int, default=1,
                     help="Number of virtual users (parametrizes the load test N times)")
    # 로드 테스트용: 주문 아이템 이름(주기적으로 주문)
    parser.addoption("--order-item-name", action="store", default=None,
                     help="If set, order this menu item name periodically during the session")
    # 로드 테스트용: VU별 메뉴 분산을 위한 아이템 이름 목록(쉼표 구분)
    parser.addoption("--order-item-list", action="store", default=None,
                     help="Comma-separated menu item names to distribute across VUs (e.g., 'A,B,C')")
    # 로드 테스트용: 주문 간격(초)
    parser.addoption("--order-interval-seconds", action="store", type=float, default=0,
                     help="If >= 0 and order-item-name is set, place an order every N seconds (0 for as fast as possible)")
    # 라운드로빈 스케줄링: 글로벌 틱마다 VU가 순번대로 주문
    parser.addoption("--round-robin", action="store_true", default=False,
                     help="Enable round-robin ordering across VUs per global tick")
    parser.addoption("--tick-seconds", action="store", type=float, default=1.0,
                     help="Global tick duration for round-robin scheduling (seconds)")
    parser.addoption("--administrative-base-url", action="store", default="https://dev-ecomm-svc.cloudinteractive.net/administrative",
                     help="Root URL for the administrative site")
    parser.addoption("--admin-auth-code", action="store", default=None,
                     help="Administrative authorization code")
    # 최대 처리량 모드: 지터/스태거링/게이팅 비활성화
    parser.addoption("--max-throughput", action="store_true", default=False,
                     help="Disable jitters/staggering/gating for maximum throughput")

def pytest_generate_tests(metafunc):
    """
    --users 값에 따라 테스트 케이스를 vu 파라미터로 N회 수집한다.
    """
    users = int(metafunc.config.getoption("--users"))
    if "vu" in metafunc.fixturenames and users > 1:
        metafunc.parametrize("vu", list(range(users)), ids=[f"vu{v:03d}" for v in range(users)])

@pytest.fixture(scope="session")
def customer_base_url(pytestconfig):
    return str(pytestconfig.getoption("--customer-base-url"))

@pytest.fixture
def customer_auth_code(pytestconfig, request):
    codes_file = pytestconfig.getoption("--auth-codes-file")
    single_code = pytestconfig.getoption("--customer-auth-code")
    if isinstance(single_code, str):
        single_code = single_code.strip() or None

    if codes_file:
        if not os.path.exists(codes_file):
            raise pytest.UsageError(f"Auth codes file not found: {codes_file}. Use an absolute path.")
        with open(codes_file, "r", encoding="utf-8") as f:
            codes = [line.strip() for line in f if line.strip()]
        try:
            from collections import OrderedDict
            codes = list(OrderedDict.fromkeys(codes))
        except Exception:
            codes = list(dict.fromkeys(codes))
        if not codes:
            raise pytest.UsageError("Auth codes file is empty.")

        users = int(pytestconfig.getoption("--users") or 1)
        if len(codes) < users:
            raise pytest.UsageError(
                f"Not enough auth codes: users={users}, codes_in_file={len(codes)}. Provide at least as many unique codes as --users.")

        try:
            vu_index = int(getattr(getattr(request, "node", None), "callspec", {}).params.get("vu", 0))
        except Exception:
            vu_index = 0
        return codes[vu_index]

    if single_code:
        return single_code
    raise pytest.UsageError("Provide either --auth-codes-file or --customer-auth-code.")

@pytest.fixture
def driver(pytestconfig, request):
    options = Options()
    if pytestconfig.getoption("--no-browser"):
        options.add_argument("--headless=new")
    options.add_argument("--window-size=1280,900")
    options.add_argument("--disable-gpu")
    options.add_argument("--disable-extensions")
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-dev-shm-usage")
    options.add_argument("--disable-background-networking")
    options.add_argument("--disable-background-timer-throttling")
    options.add_argument("--disable-renderer-backgrounding")
    options.add_argument("--disable-notifications")
    options.add_argument("--mute-audio")
    options.add_argument("--log-level=3")
    options.add_argument("--blink-settings=imagesEnabled=false")
    options.page_load_strategy = 'eager'

    if pytestconfig.getoption("--disable-images"):
        prefs = {
            "profile.managed_default_content_settings.images": 2,
            "profile.default_content_setting_values.notifications": 2,
            "credentials_enable_service": False,
            "profile.password_manager_enabled": False,
        }
        options.add_experimental_option("prefs", prefs)

    worker_id = os.environ.get("PYTEST_XDIST_WORKER", "gw0")
    try:
        vu_index = int(getattr(getattr(request, "node", None), "callspec", {}).params.get("vu", 0))
    except Exception:
        vu_index = 0

    try:
        options.set_capability("se:name", f"vu{vu_index:03d}-{worker_id}")
    except Exception:
        pass

    try:
        import uuid
        options.add_argument(f"--user-data-dir=/tmp/chrome-prof-{worker_id}-vu{vu_index}-{uuid.uuid4().hex[:8]}")
    except Exception:
        pass

    grid_url = pytestconfig.getoption("--grid-url")

    if grid_url:
        driver = webdriver.Remote(command_executor=grid_url, options=options)
    else:
        driver = webdriver.Chrome(options=options)
    try:
        driver.set_page_load_timeout(10)
        driver.set_script_timeout(10)
        driver.implicitly_wait(0)
    except Exception:
        pass
    try:
        yield driver
    finally:
        driver.quit()

@pytest.fixture
def hold_seconds(pytestconfig):
    return int(pytestconfig.getoption("--hold-seconds"))

@pytest.fixture(scope="session")
def order_item_name(pytestconfig):
    return pytestconfig.getoption("--order-item-name") or None

@pytest.fixture(scope="session")
def order_interval_seconds(pytestconfig):
    return float(pytestconfig.getoption("--order-interval-seconds"))

@pytest.fixture(scope="session")
def num_users(pytestconfig):
    return int(pytestconfig.getoption("--users") or 1)

@pytest.fixture(scope="session")
def round_robin(pytestconfig):
    return bool(pytestconfig.getoption("--round-robin"))

@pytest.fixture(scope="session")
def tick_seconds(pytestconfig):
    return float(pytestconfig.getoption("--tick-seconds"))

@pytest.fixture(scope="session")
def order_item_list(pytestconfig):
    raw = pytestconfig.getoption("--order-item-list")
    if not raw:
        return []
    try:
        return [x.strip() for x in str(raw).split(",") if x.strip()]
    except Exception:
        return []

@pytest.fixture(scope="session")
def administrative_base_url(pytestconfig):
    return str(pytestconfig.getoption("--administrative-base-url"))

@pytest.fixture(scope="session")
def admin_auth_code(pytestconfig):
    return pytestconfig.getoption("--admin-auth-code")

@pytest.fixture(scope="session")
def max_throughput(pytestconfig):
    return bool(pytestconfig.getoption("--max-throughput"))

@pytest.fixture
def vu(pytestconfig, request):
    try:
        return int(getattr(getattr(request, "node", None), "callspec", {}).params.get("vu", 0))
    except Exception:
        return 0