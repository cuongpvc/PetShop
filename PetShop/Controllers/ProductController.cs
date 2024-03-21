using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PetShop.Controllers
{
    public class ProductController : Controller
    {
        private readonly ProjectContext _db;

        public ProductController(ProjectContext db)
        {
            _db = db;
        }
       
        public async Task<IActionResult> ListProduct(bool isAllProducts = false, int? categoryId = null)
        {

            List<CategoryProductModel> CategoryProducts = new List<CategoryProductModel>();
            List<OrderDetail> list = new List<OrderDetail>();

            if (HttpContext.Session.GetString("order") != null)
            {
                string data = HttpContext.Session.GetString("order");
                list = JsonSerializer.Deserialize<List<OrderDetail>>(data);
            }
            else
            {
                list = new List<OrderDetail>();
            }

            list.ForEach(
                x =>
                {
                    x.Product = _db.Products.Include(s => s.Pictures).FirstOrDefault(b => b.ProductId == x.ProductId);
                }

                );

            if (isAllProducts)
            {
                CategoryProducts.Add(new CategoryProductModel
                {
                    Category = new Category { CategoryName = "Tất cả sản phẩm" },
                    Products = await _db.Products.Include(p => p.Pictures).ToListAsync(),
                    OrderDetails = list
                });
            }
            else if (categoryId.HasValue)
            {
                CategoryProducts.Add(new CategoryProductModel
                {
                    Category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == categoryId),
                    Products = await _db.Products.Include(p => p.Pictures).Where(p => p.CategoryId == categoryId).ToListAsync(),
                    OrderDetails = list
                });
            }
            else
            {
                CategoryProducts = await _db.Categories
                    .Include(c => c.Products)
                    .ThenInclude(p => p.Pictures)
                    .Select(c => new CategoryProductModel
                    {
                        Category = c,
                        Products = c.Products.ToList(),
                        OrderDetails = list
                    })
                    .ToListAsync();
            }

            return View(CategoryProducts);
        }

        public class CategoryProductModel
        {
            public Category Category { get; set; }
            public List<Product> Products { get; set; }

            public List<OrderDetail> OrderDetails { get; set; }
        }
        
    }
}

