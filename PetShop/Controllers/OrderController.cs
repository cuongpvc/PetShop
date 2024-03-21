using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Models;
using static PetShop.Controllers.ProductController;

namespace PetShop.Controllers
{
    public class OrderController : Controller
    {
        private readonly ProjectContext _db;
        private readonly IVnPayService _vpnPayService;

        public OrderController(ProjectContext db, IVnPayService vnPayService)
        {
            _vpnPayService = vnPayService;
            _db = db;
        }

        public IActionResult AddToCart(int id, decimal unitPrice)
        {
            List<OrderDetail> orderList = new List<OrderDetail>();

            if (HttpContext.Session.GetString("order") != null)
            {
                string data = HttpContext.Session.GetString("order");
                orderList = JsonSerializer.Deserialize<List<OrderDetail>>(data);
            }
            else
            {
                orderList = new List<OrderDetail>();
            }

            OrderDetail order = orderList.FirstOrDefault(s => s.ProductId == id);
            if (order != null)
            {
                order.Quantity++;
            }
            else
            {
                order = new OrderDetail();
                order.ProductId = id;
                order.Quantity = 1;
                order.UnitPrice = unitPrice;
                orderList.Add(order);
            }

            int totalUniqueProductIds = orderList.Select(item => item.ProductId).Distinct().Count();
            HttpContext.Session.SetInt32("NumOfCartItems", totalUniqueProductIds);
            HttpContext.Session.SetString("order", JsonSerializer.Serialize(orderList));
            return RedirectToAction("ListProduct", "Product");
        }

        public IActionResult ViewOrder()
        {
            List<OrderDetail> list = new List<OrderDetail>();
            List<CategoryProductModel> CategoryProducts = new List<CategoryProductModel>();
            if (HttpContext.Session.GetString("order") != null)
            {
                string data = HttpContext.Session.GetString("order");
                list = JsonSerializer.Deserialize<List<OrderDetail>>(data);
            }
            else
            {
                list = new List<OrderDetail>();
            }

            list.ForEach(x =>
            {
                x.Product = _db.Products
                    .Include(s => s.Pictures)
                    .FirstOrDefault(b => b.ProductId == x.ProductId);
            });
            CategoryProducts.Add(new CategoryProductModel { OrderDetails = list });

            return View(CategoryProducts);
        }

        public IActionResult DeleteToCard(int id)
        {
            List<OrderDetail> list = new List<OrderDetail>();
            if (HttpContext.Session.GetString("order") != null)
            {
                string data = HttpContext.Session.GetString("order");
                list = JsonSerializer.Deserialize<List<OrderDetail>>(data);

                var itemToRemove = list.FirstOrDefault(x => x.ProductId == id);
                if (itemToRemove != null)
                {
                    list.Remove(itemToRemove);
                    int totalUniqueProductIds = list.Select(item => item.ProductId)
                        .Distinct()
                        .Count();
                    HttpContext.Session.SetInt32("NumOfCartItems", totalUniqueProductIds);
                }

                HttpContext.Session.SetString("order", JsonSerializer.Serialize(list));
            }

            return RedirectToAction("ViewOrder");
        }

        public IActionResult DeleteToCart(int id)
        {
            List<OrderDetail> list = new List<OrderDetail>();
            if (HttpContext.Session.GetString("order") != null)
            {
                string data = HttpContext.Session.GetString("order");
                list = JsonSerializer.Deserialize<List<OrderDetail>>(data);

                var itemToRemove = list.FirstOrDefault(x => x.ProductId == id);
                if (itemToRemove != null)
                {
                    list.Remove(itemToRemove);
                    int totalUniqueProductIds = list.Select(item => item.ProductId)
                        .Distinct()
                        .Count();
                    HttpContext.Session.SetInt32("NumOfCartItems", totalUniqueProductIds);
                }

                HttpContext.Session.SetString("order", JsonSerializer.Serialize(list));
            }

            return RedirectToAction("ListProduct", "Product");
        }

        public IActionResult Payment(List<OrderDetail> orderDetails, string payment)
        {
            if (HttpContext.Session.GetString("PetSession") == null)
            {
                return RedirectToAction("SignIn", "Home");
            }
            if (!User.Identity.IsAuthenticated)
            {
                string mess = "You must be logged in to proceed. Please log in!";
                HttpContext.Session.SetString("messFail", mess);
                return RedirectToAction("SignIn", "Home");
            }
            else if (payment.Contains("Payment"))
            {
                string acc = HttpContext.Session.GetString("LoggedInAccount");
                Account account = JsonSerializer.Deserialize<Account>(acc);
                if (account != null)
                {
                    List<OrderDetail> list = new List<OrderDetail>();
                    string data = HttpContext.Session.GetString("order");
                    list = JsonSerializer.Deserialize<List<OrderDetail>>(data);

                    double total = 0;

                    foreach (OrderDetail item in list)
                    {
                        total += (double)(item.Quantity * item.UnitPrice);
                    }

                    var vnpayModel = new VnPaymentRequestModel
                    {
                        Amount = total * 0.0001,
                        CreatedDate = DateTime.Now
                    };

                    return Redirect(_vpnPayService.CreatePaymentUrl(HttpContext, vnpayModel));
                }
            }
            return Ok();
        }

        public IActionResult PaymentFail()
        {
            string mess = "You has must been loginn!!. Please Login!!";
            HttpContext.Session.SetString("messFail", mess);
            return RedirectToAction("ListProduct", "Product");
        }

        public IActionResult PaymentBack()
        {
            var response = _vpnPayService.PaymentExecute(Request.Query);

            if (response == null || response.VnPayResponseCode != "00")
            {
                TempData["Message"] = $"Payment Fail!!: {response.VnPayResponseCode}";

                return RedirectToAction("PaymentFail");
            }

            if (!User.Identity.IsAuthenticated)
            {
                string messT = "You must be logged in to proceed. Please log in!";
                HttpContext.Session.SetString("messFail", messT);
                return RedirectToAction("SignIn", "Home");
            }
            else
            {
                string acc = HttpContext.Session.GetString("LoggedInAccount");
                Account account = JsonSerializer.Deserialize<Account>(acc);
                if (account != null)
                {
                    List<OrderDetail> list = new List<OrderDetail>();
                    string data = HttpContext.Session.GetString("order");
                    list = JsonSerializer.Deserialize<List<OrderDetail>>(data);

                    Order order = new Order()
                    {
                        CustomerId = account.CustomerId,
                        EmployeeId = account.EmployeeId,
                        OrderDate = DateTime.Now,
                        ShippedDate = DateTime.Now,
                        OrderStatus = "Delivered",
                        OrderDetails = list
                    };
                    _db.Orders.Add(order);
                    if (_db.SaveChanges() > 0)
                    {
                        list = new List<OrderDetail>();
                        HttpContext.Session.SetString("order", JsonSerializer.Serialize(list));
                        HttpContext.Session.SetInt32("NumOfCartItems", 0);
                    }

                    string mess = "Payment to success!!";
                    HttpContext.Session.SetString("messSccuess", mess);
                    return RedirectToAction("ListProduct", "Product");
                }
                else
                {
                    string messT = "You must be logged in to proceed. Please log in!";
                    HttpContext.Session.SetString("messFail", messT);
                    return RedirectToAction("SignIn", "Home");
                }
            }
        }
    }
}
