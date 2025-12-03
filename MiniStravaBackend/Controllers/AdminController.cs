using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniStrava.Data;
using MiniStrava.Models;
using System.Security.Claims;

namespace MiniStrava.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;

        public AdminController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("/")]
        public IActionResult RedirectToProperPage()
        {
            var admin = _db.Admins.FirstOrDefault();

            if (admin == null)
            {
                admin = new Admin { Username = "admin" };
                _db.Admins.Add(admin);
                _db.SaveChanges();
                return RedirectToAction("SetPassword");
            }

            if (string.IsNullOrEmpty(admin.PasswordHash))
                return RedirectToAction("SetPassword");

            return RedirectToAction("Login");
        }

        [HttpGet("/admin/login")]
        public IActionResult Login()
        {
            var admin = _db.Admins.FirstOrDefault();
            ViewBag.Username = admin?.Username ?? "admin"; 
            return View();
        }

        [HttpPost("/admin/login")]
        public IActionResult Login(string username, string password)
        {
            var admin = _db.Admins.FirstOrDefault(a => a.Username == username);
            if (admin == null)
            {
                ViewBag.Error = "Nie znaleziono konta administratora.";
                return View();
            }

            if (string.IsNullOrEmpty(admin.PasswordHash))
                return RedirectToAction("SetPassword");

            if (!BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash))
            {
                ViewBag.Error = "Nieprawidłowe hasło.";
                return View();
            }

            var claims = new List<Claim> { new Claim(ClaimTypes.Name, admin.Username) };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index");
        }

        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("/admin/setpassword")]
        public IActionResult SetPassword()
        {
            var admin = _db.Admins.FirstOrDefault();
            if (admin == null) return RedirectToAction("RedirectToProperPage");

            if (admin.IsPasswordSet)
                return RedirectToAction("Login");

            return View();
        }

        [HttpPost("/admin/setpassword")]
        public IActionResult SetPassword(string password)
        {
            var admin = _db.Admins.FirstOrDefault();
            if (admin == null) return RedirectToAction("RedirectToProperPage");

            if (admin.IsPasswordSet)
                return RedirectToAction("Login");

            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            admin.IsPasswordSet = true;
            _db.SaveChanges();

            return RedirectToAction("Login");
        }

        [Authorize]
        [HttpPost("/admin/logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
