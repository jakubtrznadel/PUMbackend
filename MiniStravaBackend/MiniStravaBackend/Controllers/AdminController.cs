using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniStrava.Data;
using MiniStrava.Models;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace MiniStrava.Controllers
{
    [Authorize] 
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AppDbContext db, ILogger<AdminController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [AllowAnonymous]
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
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        [HttpGet("/admin/login")]
        public IActionResult Login()
        {
            var admin = _db.Admins.FirstOrDefault();
            if (admin != null && string.IsNullOrEmpty(admin.PasswordHash))
            {
                return RedirectToAction("SetPassword");
            }

            ViewBag.Username = admin?.Username ?? "admin";
            ViewBag.Error = TempData["Error"] as string;
            return View();
        }

        [AllowAnonymous]
        [HttpPost("/admin/login")]
        public async Task<IActionResult> Login(string username, string password)
        {
            _logger.LogInformation("Attempting login for username: {Username}", username);
            var admin = _db.Admins.FirstOrDefault(a => a.Username == username);
            if (admin == null)
            {
                _logger.LogWarning("Admin not found for username: {Username}", username);
                TempData["Error"] = "Nie znaleziono konta administratora.";
                return RedirectToAction("Login");
            }

            if (string.IsNullOrEmpty(admin.PasswordHash))
            {
                _logger.LogWarning("PasswordHash is null or empty for admin: {Username}", username);
                TempData["Succes"] = "Hasło zostało ustawione poprawnie.";
                return RedirectToAction("SetPassword");
            }

            if (!BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash))
            {
                _logger.LogWarning("Incorrect password for username: {Username}", username);
                TempData["Error"] = "Nieprawidłowe hasło.";
                return RedirectToAction("Login");
            }

            _logger.LogInformation("Login successful for username: {Username}", username);
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, admin.Username), new Claim(ClaimTypes.Role, "Admin") };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
            };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("/admin")]
        public IActionResult Index()
        {
            ViewBag.UserCount = _db.Users.Count();
            ViewBag.ActivityCount = _db.Activities.Count();
            ViewBag.TotalDistance = _db.Activities.Sum(a => a.Distance);

            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("/admin/activities")]
        public async Task<IActionResult> Activities(string email, string type, string date, double? minDistance, double? maxDistance)
        {
            var query = _db.Activities.AsQueryable();

            if (!string.IsNullOrEmpty(email))
                query = query.Where(a => a.User.Email.Contains(email));

            if (!string.IsNullOrEmpty(type))
                query = query.Where(a => a.Type.Contains(type));

            // Filtr daty
            if (!string.IsNullOrEmpty(date))
            {
                if (DateTime.TryParse(date, out var filterDate))
                {
                    // porównujemy tylko datę (bez godziny)
                    query = query.Where(a => a.CreatedAt.Date == filterDate.Date);
                }
            }

            if (minDistance.HasValue)
                query = query.Where(a => a.Distance >= minDistance.Value);

            if (maxDistance.HasValue)
                query = query.Where(a => a.Distance <= maxDistance.Value);

            var model = await query
                .Select(a => new AdminActivityViewModel
                {
                    ActivityId = a.ActivityId,
                    Name = a.Name,
                    Type = a.Type,
                    UserEmail = a.User.Email,
                    Distance = a.Distance,
                    Duration = a.Duration,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return View(model);
        }


        // Zmieniona nazwa metody usuwającej dla admina aby nie kolidowała z metodą w ActivitiesController
        [Authorize(Roles = "Admin")]
        [HttpPost("/admin/activities/delete/{id}")]
        public IActionResult DeleteActivityAdmin(int id)
        {
            var activity = _db.Activities.FirstOrDefault(a => a.ActivityId == id);
            if (activity == null)
                return NotFound();

            _db.Activities.Remove(activity);
            _db.SaveChanges();

            return RedirectToAction("Activities");
        }

        [AllowAnonymous]
        [HttpGet("/admin/setpassword")]
        public IActionResult SetPassword()
        {
            var admin = _db.Admins.FirstOrDefault();
            if (admin == null) return RedirectToAction("RedirectToProperPage");

            if (admin.IsPasswordSet) return RedirectToAction("Login");

            return View();
        }

        [AllowAnonymous]
        [HttpPost("/admin/setpassword")]
        public IActionResult SetPassword(string password)
        {
            var admin = _db.Admins.FirstOrDefault();
            if (admin == null) return RedirectToAction("RedirectToProperPage");

            if (admin.IsPasswordSet) return RedirectToAction("Login");

            if (string.IsNullOrEmpty(password))
            {
                TempData["Error"] = "Hasło nie może być puste.";
                return View();
            }

            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            admin.IsPasswordSet = true;
            _db.SaveChanges();
            _logger.LogInformation("Password set successfully for admin: {Username}", admin.Username);

            return RedirectToAction("Login");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("/logout")]  
        public async Task<IActionResult> LogoutSimple()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");  
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("/admin/users")]
        public async Task<IActionResult> Users(string? email, string? name, DateTime? fromDate, DateTime? toDate)
        {
            var query = _db.Users.AsQueryable();

            if (!string.IsNullOrEmpty(email))
                query = query.Where(u => u.Email.Contains(email));

            if (!string.IsNullOrEmpty(name))
                query = query.Where(u => u.FirstName.Contains(name)); // Lub u.Name jeśli masz pole Name

            if (fromDate.HasValue)
                query = query.Where(u => u.CreatedAt >= fromDate.Value); // Dostosuj pole daty, np. RegistrationDate

            if (toDate.HasValue)
                query = query.Where(u => u.CreatedAt <= toDate.Value);

            var users = await query
                .OrderByDescending(u => u.CreatedAt) // Lub po ID lub nazwie
                .ToListAsync();

            return View(users);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("/admin/users/delete/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            // Opcjonalnie: Kasuj aktywności użytkownika (cascade delete)
            var userActivities = await _db.Activities.Where(a => a.UserId == id).ToListAsync();
            _db.Activities.RemoveRange(userActivities);

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Użytkownik i jego aktywności zostały usunięte.";
            return RedirectToAction("Users");
        }

    }

    public class AdminActivityViewModel
    {
        public int ActivityId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public double Distance { get; set; }
        public double Duration { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserEmail { get; set; } = string.Empty;
    }
}
