using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Pages.Customer;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Order = CloudInteractive.CloudPos.Models.Order;

namespace CloudInteractive.CloudPos.Pages.Administrative;

public class AddTestData(ILogger<Authorize> logger, ServerDbContext context) : PageModel
{
    public void OnGet()
    {
        var category1 = new Category()
        {
            Name = "식사"
        };

        var category2 = new Category()
        {
            Name = "음료 및 주류"
        };
        
        var category3 = new Category()
        {
            Name = "전골"
        };
        
        var category4 = new Category()
        {
            Name = "사이드"
        };

        var item1 = new Item()
        {
            Name = "얼큰 순대국밥",
            Category = category1,
            IsAvailable = true,
            Price = 7000
        };

        var item2 = new Item()
        {
            Name = "테라",
            Category = category2,
            IsAvailable = true,
            Price = 3000
        };
        
        var item3 = new Item()
        {
            Name = "사이다",
            Category = category2,
            IsAvailable = true,
            Price = 2000
        };
        
        var item4 = new Item()
        {
            Name = "순대전골",
            Category = category3,
            IsAvailable = true,
            Price = 20000
        };
        
        var item5 = new Item()
        {
            Name = "순대국밥",
            Category = category1,
            IsAvailable = true,
            Price = 6500
        };
        
        var item6 = new Item()
        {
            Name = "돼지고기 편육",
            Category = category4,
            IsAvailable = true,
            Price = 9000
        };
        
        var item7 = new Item()
        {
            Name = "한우스지전골",
            Category = category3,
            IsAvailable = true,
            Price = 25000
        };
        
        var item8 = new Item()
        {
            Name = "한우등심전골",
            Category = category1,
            IsAvailable = false,
            Price = 28000
        };
        
        var item9 = new Item()
        {
            Name = "돼지고기 수육",
            Category = category4,
            IsAvailable = true,
            Price = 11000
        };
        
        var item10 = new Item()
        {
            Name = "참이슬",
            Category = category1,
            IsAvailable = true,
            Price = 2000
        };
        
        var item11 = new Item()
        {
            Name = "세로",
            Category = category1,
            IsAvailable = true,
            Price = 2000
        };
        
        var item12 = new Item()
        {
            Name = "돼지고기 편육",
            Category = category1,
            IsAvailable = true,
            Price = 9000
        };

        var table1 = new Table()
        {
            Name = "1"
        };
        
        var table2 = new Table()
        {
            Name = "2"
        };

        var session1 = new TableSession()
        {
            AuthCode = "A1B2",
            CreatedAt = DateTime.Now,
            Table = table1
        };
        
        var session2 = new TableSession()
        {
            AuthCode = "P2KA",
            CreatedAt = DateTime.Now,
            EndedAt = DateTime.Now,
            Table = table1
        };
        
        var session3 = new TableSession()
        {
            AuthCode = "CCCP",
            CreatedAt = DateTime.Now,
            Table = table2
        };
        
        var order1 = new Order()
        {
            Session = session1,
            CreatedAt = DateTime.Now,
            Status = Order.OrderStatus.Completed
        };

        var orderitem1 = new OrderItem()
        {
            Order = order1,
            Item = item1,
            Quantity = 1
        };

        var orderitem2 = new OrderItem()
        {
            Order = order1,
            Item = item2,
            Quantity = 3
        };
        
        var order2 = new Order()
        {
            Session = session1,
            CreatedAt = DateTime.Now - TimeSpan.FromDays(1),
            Status = Order.OrderStatus.Completed
        };

        var orderitem3 = new OrderItem()
        {
            Order = order2,
            Item = item2,
            Quantity = 4
        };
        
        var order3 = new Order()
        {
            Session = session1,
            CreatedAt = DateTime.Now - TimeSpan.FromDays(4),
            Status = Order.OrderStatus.Cancelled
        };

        var orderitem4 = new OrderItem()
        {
            Order = order3,
            Item = item1,
            Quantity = 1
        };
        
        var order4 = new Order()
        {
            Session = session1,
            CreatedAt = DateTime.Now - TimeSpan.FromDays(2),
            Status = Order.OrderStatus.Completed
        };
        
        var orderitem5 = new OrderItem()
        {
            Order = order4,
            Item = item3,
            Quantity = 2
        };
        
        var orderitem6 = new OrderItem()
        {
            Order = order4,
            Item = item4,
            Quantity = 2
        };
        
        var orderitem7 = new OrderItem()
        {
            Order = order4,
            Item = item5,
            Quantity = 1
        };
        
        var orderitem8 = new OrderItem()
        {
            Order = order4,
            Item = item6,
            Quantity = 2
        };

        
        context.Categories.Add(category1);
        context.Categories.Add(category2);
        context.Categories.Add(category3);
        context.Categories.Add(category4);
        context.Items.Add(item1);
        context.Items.Add(item2);
        context.Items.Add(item3);
        context.Items.Add(item4);
        context.Items.Add(item5);
        context.Items.Add(item6);
        context.Items.Add(item7);
        context.Items.Add(item8);
        context.Items.Add(item9);
        context.Items.Add(item10);
        context.Items.Add(item11);
        context.Items.Add(item12);
        context.Tables.Add(table1);
        context.Sessions.Add(session1);
        context.Sessions.Add(session2);
        context.Sessions.Add(session3);
        context.Orders.Add(order1);
        context.Orders.Add(order2);
        context.Orders.Add(order3);
        context.Orders.Add(order4);
        context.OrderItems.Add(orderitem1);
        context.OrderItems.Add(orderitem2);
        context.OrderItems.Add(orderitem3);
        context.OrderItems.Add(orderitem4);
        context.OrderItems.Add(orderitem5);
        context.OrderItems.Add(orderitem6);
        context.OrderItems.Add(orderitem7);
        context.OrderItems.Add(orderitem8);
        context.SaveChanges();
    }
}