from selenium.webdriver.common.by import By
from .base_page import BasePage
from .items.admin_order_item import AdminOrderItem

class AdministrativeOrderPage(BasePage):
    def get_order_items(self):
        elements = self.find_all((By.CSS_SELECTOR, "div.order-card"), timeout=1)
        return [AdminOrderItem(self.driver, el) for el in elements]

    def get_order_item(self, order_number):
        order_items = self.get_order_items()
        for item in order_items:
            if item.order_number == order_number:
                return item
        return None

    def complete_order(self, order_number):
        order_item = self.get_order_item(order_number)
        if order_item:
            order_item.complete_order()
        else:
            raise ValueError(f"Order item with number {order_number} not found")
        
    def cancel_order(self, order_number):
        order_item = self.get_order_item(order_number)
        if order_item:
            order_item.cancel_order()
        else:
            raise ValueError(f"Order item with number {order_number} not found")
