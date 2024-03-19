using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Models;
using System.Text.Json;

namespace PetShop.Controllers
{
    public class OrderController : Controller
    {
        private readonly ProjectContext _db;

        public OrderController(ProjectContext db)
        {
            _db = db;
        }
        public IActionResult ViewOrder()
        {
            List<OrderDetail> list = new List<OrderDetail>();
            if(HttpContext.Session.GetString("order") != null)
            {
                string data = HttpContext.Session.GetString("order");
                list = JsonSerializer.Deserialize<List<OrderDetail>>(data);
            }else
            {
                list = new List<OrderDetail>();
            }

            list.ForEach(
                x =>
                {
                    x.Product = _db.Products.Include(s => s.Pictures).FirstOrDefault(b => b.ProductId == x.ProductId);
                }

                );
            
            return View(list);
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
                }

                HttpContext.Session.SetString("order", JsonSerializer.Serialize(list));
            }

            return RedirectToAction("ViewOrder");
        }


    }
}
