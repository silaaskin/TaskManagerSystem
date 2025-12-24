using TaskManagerSystem.Data;
using TaskManagerSystem.Models;
using Microsoft.AspNetCore.Mvc;


namespace TaskManagerSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(string name, string email, string password, string confirmPassword, string role = "User")
        {
            // Validasyonlar
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ViewBag.Message = "Lütfen tüm alanları doldurun!";
                return View();
            }


            if (password != confirmPassword)
            {
                TempData["Message"] = "Şifreler eşleşmiyor!";
                return View();
            }

            if (_context.Users.Any(u => u.Email == email))
            {
                TempData["Message"] = "Bu email zaten kayıtlı!";
                return View();
            }

            var passwordHash = PasswordHelper.HashPassword(password);

            // Role bilgisini kaydediyoruz
            var user = new User
            {
                Name = name,
                Email = email,
                Password = passwordHash,
                Role = role // Gelen rolü ata (Admin veya User)
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            TempData["Message"] = "Kayıt başarılı! Giriş yapabilirsiniz.";
            TempData["MessageType"] = "success";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                TempData["Message"] = "Hatalı email veya şifre!";
                return View();
            }

            bool passwordIsTrue = PasswordHelper.VerifyPassword(password, user.Password);

            if (passwordIsTrue)
            {
                // Role bilgisini Session'a ekliyoruz
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("UserName", user.Name);
                HttpContext.Session.SetString("UserRole", user.Role); 

                return RedirectToAction("Index", "Tasks");
            }
            else
            {
                TempData["Message"] = "Hatalı email veya şifre!";
                return View();
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}