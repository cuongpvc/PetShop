using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using NuGet.Packaging;
using PetShop.Models;

namespace PetShop.Controllers
{
    /// <summary>
    /// Controller dành cho admin
    /// bao gồm quản lý category, product, account
    /// </summary>
    public class AdminController : Controller
    {

        private readonly ProjectContext _context;

        public AdminController(ProjectContext context)
        {
            _context = context;
        }

        /// GET: /admin
        public IActionResult Index()
        {
            return View();
        }

        /// GET: /admin/category
        public IActionResult Category()
        {
            var categories = _context.Categories.ToList();
            return View(categories);
        }

        /// GET: /admin/product
        public IActionResult Product()
        {
            var products = _context.Products.Include(p => p.Category).Include(p => p.Pictures).ToList();
            return View(products);
        }

        /// GET: /admin/account
        public IActionResult Account()
        {
            var accounts = _context.Accounts.Include(a => a.Customer).Include(a => a.Employee).ToList();
            return View(accounts);
        }

        #region API

        /// GET: /admin/api/product/{id}
        [HttpGet("/admin/api/product/{id}")]
        public IActionResult ApiProduct(int id)
        {
            var product = _context.Products.Include(p => p.Pictures).FirstOrDefault(p => p.ProductId == id);
            // dùng newtonsoft.json để serialize object và trả về dưới dạng json
            var json = JsonConvert.SerializeObject(product, Formatting.Indented, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
            return Content(json, "application/json");
        }

        /// POST: /admin/api/product
        [HttpPost("/admin/api/product")]
        public IActionResult ApiProduct([FromBody] Product product)
        {
            if (_context.Products.Any(p => p.ProductName.ToLower() == product.ProductName.ToLower()))
            {
                return BadRequest("Product name already exists");
            }
            if (product.Pictures.Count > 0)
            {
                _context.PictureProducts1.AddRange(product.Pictures);
            }
            product.CreateDate = System.DateTime.Now;
            _context.Products.Add(product);
            _context.SaveChanges();
            return Ok();
        }

        /// PUT: /admin/api/product/{id}
        [HttpPut("/admin/api/product/{id}")]
        public IActionResult ApiProduct(int id, [FromBody] Product product)
        {
            var oldProduct = _context.Products.Include(x => x.Pictures).FirstOrDefault(p => p.ProductId == id);
            if (oldProduct == null)
            {
                return NotFound();
            }
            if (_context.Products.Any(p => p.ProductName.ToLower() == product.ProductName.ToLower() && p.ProductId != id))
            {
                return BadRequest("Product name already exists");
            }
            if (oldProduct.Pictures.Count > 0)
            {
                foreach (var item in oldProduct.Pictures)
                {
                    if (!product.Pictures.Any(p => p.PictureId == item.PictureId))
                    {
                        oldProduct.Pictures.Remove(item);
                        _context.PictureProducts1.Remove(item);
                    }
                }
            }
            var newPictures = product.Pictures.Where(p => p.PictureId == 0).ToList();
            if (newPictures.Count > 0)
            {
                _context.PictureProducts1.AddRange(newPictures);
                oldProduct.Pictures.AddRange(newPictures);
            }

            product.UpdateDate = System.DateTime.Now;
            _context.Entry(oldProduct).CurrentValues.SetValues(product);
            _context.Entry(oldProduct).State = EntityState.Modified;
            _context.SaveChanges();
            return Ok();
        }

        /// DELETE: /admin/api/product/{id}
        [HttpDelete("/admin/api/product/{id}")]
        public IActionResult ApiProductDelete(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }
            try
            {
                _context.Products.Remove(product);
                _context.SaveChanges();
            }
            catch (DbUpdateException e)
            {
                return BadRequest("This product is being used in other tables");
            }
            return Ok();
        }

        /// GET: /admin/api/category
        [HttpGet("/admin/api/category")]
        public IActionResult ApiCategory()
        {
            var categories = _context.Categories.ToList();
            var json = JsonConvert.SerializeObject(categories, Formatting.Indented);
            return Content(json, "application/json");
        }

        /// GET: /admin/api/category/{id}
        [HttpGet("/admin/api/category/{id}")]
        public IActionResult ApiCategory(int id)
        {
            var category = _context.Categories.FirstOrDefault(c => c.CategoryId == id);
            var json = JsonConvert.SerializeObject(category, Formatting.Indented);
            return Content(json, "application/json");
        }

        /// POST: /admin/api/category
        [HttpPost("/admin/api/category")]
        public IActionResult ApiCategory([FromForm] Category category)
        {
            if (_context.Categories.Any(c => c.CategoryName.ToLower() == category.CategoryName.ToLower()))
            {
                return BadRequest("Category name already exists");
            }
            _context.Categories.Add(category);
            _context.SaveChanges();
            return Ok();
        }

        /// PUT: /admin/api/category/{id}
        [HttpPut("/admin/api/category/{id}")]
        public IActionResult ApiCategory(int id, [FromForm] Category category)
        {
            var oldCategory = _context.Categories.FirstOrDefault(c => c.CategoryId == id);
            if (oldCategory == null)
            {
                return NotFound();
            }
            if (_context.Categories.Any(c => c.CategoryName.ToLower() == category.CategoryName.ToLower() && c.CategoryId != id))
            {
                return BadRequest("Category name already exists");
            }
            _context.Entry(oldCategory).CurrentValues.SetValues(category);
            _context.Entry(oldCategory).State = EntityState.Modified;
            _context.SaveChanges();
            return Ok();
        }

        /// UPDATE ACCOUNT STATUS
        /// PUT: /admin/api/account/{id}
        [HttpPut("/admin/api/account/{id}")]
        public IActionResult ApiAccount(int id, bool IsActive)
        {
            var oldAccount = _context.Accounts.FirstOrDefault(a => a.AccountId == id);
            if (oldAccount == null)
            {
                return NotFound();
            }
            oldAccount.IsActive = IsActive;
            _context.Entry(oldAccount).CurrentValues.SetValues(oldAccount);
            _context.Entry(oldAccount).State = EntityState.Modified;
            _context.SaveChanges();
            return Ok();
        }

        #endregion
    }
}
