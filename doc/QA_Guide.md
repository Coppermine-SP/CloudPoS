<h1 align="center">CloudPOS Q/A Guide</h1>
<p align="center">Last Updated: 2025-08-14</p>

## Overview
This document serves as an automated testing guide for quality assurance of the CloudPOS system.

CloudPOS is an e-commerce platform that allows customers to place orders through web browsers. To ensure the stability and quality of this system, we provide automated test scripts using Selenium WebDriver and the pytest framework.

This test suite validates the following key functionalities:
- Customer authentication process
- User interface rendering
- Page-to-page navigation
- Core business logic

## Quickstart
**Before you proceed**: You need to install Python 3.11 & Selenium, pytest.
> Tested on Python 3.11.4, pytest-8.4.1, Selenium-4.22.0
>

``` 
pip install pytest selenium
```
```
cd /path/to/CloudPoS/test/selenium
python -m pytest -vv tests --customer-base-url="https://dev-ecomm-svc.cloudinteractive.net/customer" --customer-auth-code="CODE"
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
