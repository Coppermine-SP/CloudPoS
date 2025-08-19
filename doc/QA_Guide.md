<h1 align="center">CloudPOS Q/A Guide</h1>
<p align="center">Last Updated: 2025-08-19</p>

## Overview
This document serves as an automated testing guide for quality assurance of the CloudPOS system.

CloudPOS is an e-commerce platform that allows customers to place orders through web browsers. To ensure the stability and quality of this system, we provide automated test scripts using Selenium WebDriver and the pytest framework.

This test suite validates the following key functionalities:
- Customer authentication process
- User interface rendering
- Page-to-page navigation
- Core business logic
- Some load testing(spike)

## Quickstart
**Before you proceed**: You need to install Python 3.11 & Selenium, pytest.
> Tested on Python 3.11.4, pytest-8.4.1, Selenium-4.22.0

``` 
pip install pytest selenium
```
```
cd /path/to/CloudPoS/test/selenium
python -m pytest -vv tests --customer-base-url="https://dev-ecomm-svc.cloudinteractive.net/customer" --customer-auth-code="CODE"
```
If you want to run tests without browser UI, add the following parameter: **--no-browser**
```
python -m pytest -vv tests --customer-base-url="https://dev-ecomm-svc.cloudinteractive.net/customer" --customer-auth-code="CODE" --no-browser
```
Parallel execution of load tests requires Selenium Grid.
```
docker run -d --name selenium-grid -p 4444:4444 -p 7900:7900 --shm-size="8g" `
    -e SE_NODE_MAX_SESSIONS=40 -e SE_NODE_OVERRIDE_MAX_SESSIONS=true `
    selenium/standalone-chrome:latest
```
Order Load Test
```
cd /path/to/CloudPoS/test/selenium
python -m pytest tests/test_load_signalr.py -k test_order_signalr_session `
  -n auto --dist load --users 10 `
  --grid-url "http://localhost:4444/wd/hub" `
  --auth-codes-file "ABSOULTE\PATH\TO\codes.txt" `
  --order-interval-seconds 0 `
  --hold-seconds 60 `
  --no-browser --disable-images -q -s
```
Admin Page, complete order load test
```
cd /path/to/CloudPoS/test/selenium
pytest -q tests/test_load_signalr.py::test_admin_complete_order -n auto --users 20 `
--grid-url "http://localhost:4444/wd/hub" `
--administrative-base-url "https://dev-ecomm-svc.cloudinteractive.net/administrative" `
--admin-auth-code "YOUR_ADMIN_PASSWORD" `
--disable-images --max-throughput --no-browser -s
```

## Test Cases
### Auth page
- Are the welcome message, shop name, and authentication method rendered successfully?
- Does it redirect properly when the correct code is entered?
- Does the help and privacy policy link redirect correctly when clicked?
### Menu page
- Are menu items successfully rendered?
<br>→ Are items successfully added to cart when the add button is clicked?
<br>→ Does the detail modal appear properly when an item is clicked?
- Does it redirect to the order history page correctly?
- Does menu category switching work properly?
- Does ordering work after selecting menu items?
- Does the hamburger menu button work properly?
<br>→ Does the staff call button work properly?
<br>→ Does the session share button work properly?
<br>→ Does the theme change work properly?
### History page
- Does the order history display correctly?
- Does the payment request modal appear properly when clicked?
### Load test
- Order random menu
- Complete order
