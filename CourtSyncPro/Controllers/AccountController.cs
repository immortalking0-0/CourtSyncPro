using BCrypt.Net;
using CourtSyncPro.Data;
using CourtSyncPro.Models.Entities;
using CourtSyncPro.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourtSyncPro.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AccountController(ApplicationDbContext db) => _db = db;

        // ── Show unified login page ──────────────────────────────
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (HttpContext.Session.GetString("UserId") != null)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        // ── Process login ────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            if (model.Role == "User")
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                    return View(model);
                }
                HttpContext.Session.SetString("UserId", user.UserId.ToString());
                HttpContext.Session.SetString("UserName", user.Name);
                HttpContext.Session.SetString("UserRole", "User");
            }
            else if (model.Role == "Owner")
            {
                var owner = await _db.CourtOwners.FirstOrDefaultAsync(o => o.Email == model.Email);
                if (owner == null || !BCrypt.Net.BCrypt.Verify(model.Password, owner.PasswordHash))
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                    return View(model);
                }
                if (!owner.IsVerified)
                {
                    ModelState.AddModelError("", "Your account is pending admin verification.");
                    return View(model);
                }
                HttpContext.Session.SetString("UserId", owner.OwnerId.ToString());
                HttpContext.Session.SetString("UserName", owner.BusinessName);
                HttpContext.Session.SetString("UserRole", "Owner");
            }
            else if (model.Role == "Admin")
            {
                var admin = await _db.Admins.FirstOrDefaultAsync(a => a.Email == model.Email);
                if (admin == null || !BCrypt.Net.BCrypt.Verify(model.Password, admin.PasswordHash))
                {
                    ModelState.AddModelError("", "Invalid admin credentials.");
                    return View(model);
                }
                HttpContext.Session.SetString("UserId", admin.AdminId.ToString());
                HttpContext.Session.SetString("UserName", admin.Name);
                HttpContext.Session.SetString("UserRole", "Admin");
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return HttpContext.Session.GetString("UserRole") switch
            {
                "Owner" => RedirectToAction("Dashboard", "Courts"),
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        // ── Customer signup ──────────────────────────────────────
        [HttpGet] public IActionResult RegisterUser() => View(new UserRegisterViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterUser(UserRegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _db.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            var user = new User
            {
                Name = model.Name,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                JoinDate = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Auto-login after registration
            HttpContext.Session.SetString("UserId", user.UserId.ToString());
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetString("UserRole", "User");

            TempData["Success"] = "Welcome to CourtSync Pro! Your account has been created.";
            return RedirectToAction("Index", "Home");
        }

        // ── Court Owner signup ───────────────────────────────────
        [HttpGet] public IActionResult RegisterOwner() => View(new OwnerRegisterViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterOwner(OwnerRegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _db.CourtOwners.AnyAsync(o => o.Email == model.Email))
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            var owner = new CourtOwner
            {
                BusinessName = model.BusinessName,
                Email = model.Email,
                Phone = model.Phone,
                NationalID = model.NationalID,
                City = model.City,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                IsVerified = false,  // Admin must approve
                RegisterDate = DateTime.UtcNow
            };

            _db.CourtOwners.Add(owner);
            await _db.SaveChangesAsync();

            TempData["Info"] = "Registration submitted! An admin will verify your account shortly.";
            return RedirectToAction("Login");
        }

        // ── Logout ───────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}