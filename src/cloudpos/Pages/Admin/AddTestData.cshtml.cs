using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Pages.Customer;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Order = CloudInteractive.CloudPos.Models.Order;

namespace CloudInteractive.CloudPos.Pages.Admin;

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

        var table1 = new Table()
        {
            Name = "1"
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
        
        var order1 = new Order()
        {
            Session = session1,
            CreatedAt = DateTime.Now,
            Status = Order.OrderStatus.Received
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

        
        context.Categories.Add(category1);
        context.Categories.Add(category2);
        context.Items.Add(item1);
        context.Items.Add(item2);
        context.Tables.Add(table1);
        context.Sessions.Add(session1);
        context.Sessions.Add(session2);
        context.Orders.Add(order1);
        context.Orders.Add(order2);
        context.Orders.Add(order3);
        context.OrderItems.Add(orderitem1);
        context.OrderItems.Add(orderitem2);
        context.OrderItems.Add(orderitem3);
        context.OrderItems.Add(orderitem4);
        context.SaveChanges();
    }
}