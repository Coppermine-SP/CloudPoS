<h1 align="center">CloudPOS Q/A Guide</h1>
<p align="center">Last Updated: 2025-08-13</p>

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
python -m pytest -vv tests --customer-base-url="https://dev-ecomm-svc.cloudinteractive.net/Customer" --customer-auth-code="CODE"
```