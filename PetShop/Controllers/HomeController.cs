using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Models;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net;
using System.Text.RegularExpressions;
using System;

namespace PetShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ProjectContext _db;

        public HomeController(ProjectContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult SignIn()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignIn(Account account)
        {
            if (ModelState.IsValid)
            {
                var acc = await _db.Accounts.FirstOrDefaultAsync(a => a.TaiKhoan == account.TaiKhoan && a.Password == account.Password);

                if (acc != null)
                {
                    if (acc.IsActive == true)
                    {
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, acc.TaiKhoan),
                            new Claim(ClaimTypes.Role, acc.Role.ToString())
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = false,
                            AllowRefresh = true,
                        };

                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                        if (acc.Role == 2)
                        {
                            return RedirectToAction("Index", "Home");
                        }
                        else if (acc.Role == 1)
                        {
                            return RedirectToAction("Index", "Manager");
                        }
                        else if (acc.Role == 3)
                        {
                            return RedirectToAction("Index", "Shipper");
                        }
                    }
                    else
                    {
                        ViewData["msgActiveAccount"] = "Tài khoản hiện tại đang không hoạt động";
                    }
                }
                else
                {
                    ViewData["msgEmailPass"] = "Tài khoản hoặc mật khẩu không chính xác";
                }
            }

            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("SignIn", "Home");
        }

        // Action để xác thực OTP
        [HttpGet]
        public IActionResult VerifyOTP()
        {
            return View();
        }

        [HttpPost]
        public ActionResult VerifyOTP(int otp)
        {
            try
            {
                int storedOTP = Convert.ToInt32(TempData["OTP"]);

                if (otp == storedOTP)
                {
                    ViewData["msgSuccess"] = "Xác thực thành công. Đăng ký tài khoản thành công!";
                    // Xác thực thành công
                    Customer customer = TempData["Customer"] as Customer;
                    customer.Phone = customer.Phone.Trim();
                    customer.TaiKhoan = customer.TaiKhoan.Trim();

                    _db.Customers.Add(customer);
                    _db.SaveChanges();

                    return RedirectToAction("SignIn", "Home");
                }
                else
                {
                    ModelState.AddModelError("otp", "Mã OTP không đúng vui lòng thử lại");
                    return View();
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError("otp", "Mã OTP Không hợp lệ !");
                return View();
            }
        }

        // Phương thức gửi OTP qua email
        private bool SendOTP(string email, int otp)
        {
            try
            {
                // Configure SMTP client
                SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587); // Update with your SMTP server details
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential("your-email@gmail.com", "your-password"); // Update with your email credentials
                smtpClient.EnableSsl = true;

                // Compose email message
                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress("your-email@gmail.com"); // Update with your email address
                mailMessage.To.Add(email);
                mailMessage.Subject = "Your OTP for Registration";
                mailMessage.Body = "Your OTP is: " + otp.ToString();

                // Send email
                smtpClient.Send(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                // Failed to send email
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        // Action để hiển thị form đăng ký
        [HttpGet]
        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> SignUp(Customer customer)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra email không null và không trống
                if (string.IsNullOrEmpty(customer.Email))
                {
                    ViewData["msgEmail"] = "Vui lòng nhập địa chỉ email.";
                    return View(customer);
                }

                // Kiểm tra email không trùng
                var existingEmail = await _db.Customers.FirstOrDefaultAsync(c => c.Email == customer.Email);
                if (existingEmail != null)
                {
                    ViewData["msgEmailSame"] = "Email đã được sử dụng. Vui lòng sử dụng email khác.";
                    return View(customer);
                }

                // Kiểm tra Name không null và không trống
                if (string.IsNullOrEmpty(customer.Name))
                {
                    ViewData["msgName"] = "Vui lòng nhập tên của bạn.";
                    return View(customer);
                }

                // Kiểm tra Name không trùng
                var existingName = await _db.Customers.FirstOrDefaultAsync(c => c.Name == customer.Name);
                if (existingName != null)
                {
                    ViewData["msgNameSame"] = "Tên người dùng đã tồn tại. Vui lòng chọn tên khác.";
                    return View(customer);
                }

                // Kiểm tra tên tài khoản không null và không trống
                if (string.IsNullOrEmpty(customer.TaiKhoan))
                {
                    ViewData["msgTaiKhoan"] = "Vui lòng nhập tên tài khoản.";
                    return View(customer);
                }

                // Kiểm tra tên tài khoản không trùng
                var existingTaikhoan = await _db.Customers.FirstOrDefaultAsync(c => c.TaiKhoan == customer.TaiKhoan);
                if (existingTaikhoan != null)
                {
                    ViewData["msgTaikhoanSame"] = "Tên tài khoản đã tồn tại. Vui lòng chọn tên khác.";
                    return View(customer);
                }

                // Kiểm tra số điện thoại không null và không trống
                if (string.IsNullOrEmpty(customer.Phone))
                {
                    ViewData["msgPhone"] = "Vui lòng nhập số điện thoại.";
                    return View(customer);
                }

                // Kiểm tra số điện thoại không trùng
                var existingPhone = await _db.Customers.FirstOrDefaultAsync(c => c.Phone == customer.Phone);
                if (existingPhone != null)
                {
                    ViewData["msgPhoneSame"] = "Số điện thoại đã được sử dụng. Vui lòng sử dụng số khác.";
                    return View(customer);
                }

                // Kiểm tra số điện thoại có đúng định dạng không
                if (!Regex.IsMatch(customer.Phone, @"^0\d{9}$"))
                {
                    ViewData["msgPhone"] = "Số điện thoại không hợp lệ. Vui lòng nhập số bắt đầu từ 0 và có 10 chữ số!";
                    return View(customer);
                }

                // Tạo và gửi OTP
                Random random = new Random();
                int otp = random.Next(100000, 999999);
                if (SendOTP(customer.Email, otp))
                {
                    TempData["OTP"] = otp;
                    TempData["Customer"] = customer;

                    // Create a new Account for the customer with IsActive set to true
                    var account = new Account
                    {
                        TaiKhoan = customer.TaiKhoan,
                        Password = customer.MatKhau,
                        Role = 2, // Set the default role or retrieve it from somewhere
                        IsActive = true // Set IsActive to true for new accounts
                    };

                    // Save the new Account
                    _db.Accounts.Add(account);
                    await _db.SaveChangesAsync();

                    return RedirectToAction("VerifyOTP", "Home");
                }
            }

            return View(customer);
        }
    }
}
