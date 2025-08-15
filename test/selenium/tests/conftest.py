import pytest
from selenium import webdriver
from selenium.webdriver.chrome.options import Options

def pytest_addoption(parser):
    # 고객 사이트의 기본 URL을 설정하는 옵션 추가
    parser.addoption("--customer-base-url", action="store", default="https://dev-ecomm-svc.cloudinteractive.net/Customer",
                     help="Root URL for the customer site")
    # 고객 인증 코드를 설정하는 옵션 추가
    parser.addoption("--customer-auth-code", action="store", default="ABCD",
                     help="Customer authorization code (4 chars)")
    # 브라우저 UI 비활성화 옵션 추가
    parser.addoption("--no-browser", action="store_true", default=False,
                     help="Run tests in headless mode (no browser UI)")

@pytest.fixture(scope="session")
def customer_base_url(pytestconfig):
    return str(pytestconfig.getoption("--customer-base-url"))

@pytest.fixture(scope="session")
def customer_auth_code(pytestconfig):
    return str(pytestconfig.getoption("--customer-auth-code"))

@pytest.fixture
def driver(pytestconfig):
    options = Options()
    if pytestconfig.getoption("--no-browser"):
        options.add_argument("--headless=new")
    options.add_argument("--window-size=1280,900")
    driver = webdriver.Chrome(options=options)
    try:
        yield driver
    finally:
        driver.quit()