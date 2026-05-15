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

        public AccountController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ── LOGIN PAGE ───────────────────────────────────────────
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // FIXED: Use GetInt32 instead of GetString
            if (HttpContext.Session.GetInt32("UserId") != null)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;

            return View(new LoginViewModel());
        }

        // ── PROCESS LOGIN ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            // ── USER LOGIN ───────────────────────────────────────
            if (model.Role == "User")
            {
                var user = await _db.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null ||
                    !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                    return View(model);
                }

                // FIXED: Store as INT
                HttpContext.Session.SetInt32("UserId", user.UserId);

                HttpContext.Session.SetString("UserName", user.Name);

                HttpContext.Session.SetString("UserRole", "User");
            }

            // ── OWNER LOGIN ──────────────────────────────────────
            else if (model.Role == "Owner")
            {
                var owner = await _db.CourtOwners
                    .FirstOrDefaultAsync(o => o.Email == model.Email);

                if (owner == null ||
                    !BCrypt.Net.BCrypt.Verify(model.Password, owner.PasswordHash))
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                    return View(model);
                }

                if (!owner.IsVerified)
                {
                    ModelState.AddModelError("", "Your account is pending admin verification.");
                    return View(model);
                }

                // FIXED: Store as INT
                HttpContext.Session.SetInt32("UserId", owner.OwnerId);

                // Important for tournaments/courts
                HttpContext.Session.SetInt32("OwnerId", owner.OwnerId);

                HttpContext.Session.SetString("UserName", owner.BusinessName);

                HttpContext.Session.SetString("UserRole", "Owner");
            }

            // ── ADMIN LOGIN ──────────────────────────────────────
            else if (model.Role == "Admin")
            {
                var admin = await _db.Admins
                    .FirstOrDefaultAsync(a => a.Email == model.Email);

                if (admin == null ||
                    !BCrypt.Net.BCrypt.Verify(model.Password, admin.PasswordHash))
                {
                    ModelState.AddModelError("", "Invalid admin credentials.");
                    return View(model);
                }

                // FIXED: Store as INT
                HttpContext.Session.SetInt32("UserId", admin.AdminId);

                HttpContext.Session.SetString("UserName", admin.Name);

                HttpContext.Session.SetString("UserRole", "Admin");
            }

            // ── RETURN URL ───────────────────────────────────────
            if (!string.IsNullOrEmpty(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // ── ROLE-BASED REDIRECT ──────────────────────────────
            return HttpContext.Session.GetString("UserRole") switch
            {
                "Owner" => RedirectToAction("Dashboard", "Courts"),

                "Admin" => RedirectToAction("Dashboard", "Admin"),

                _ => RedirectToAction("Index", "Home")
            };
        }

        // ── USER REGISTRATION PAGE ──────────────────────────────
        [HttpGet]
        public IActionResult RegisterUser()
        {
            return View(new UserRegisterViewModel());
        }

        // ── PROCESS USER REGISTRATION ───────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterUser(UserRegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            bool emailExists = await _db.Users
                .AnyAsync(u => u.Email == model.Email);

            if (emailExists)
            {
                ModelState.AddModelError("Email",
                    "This email is already registered.");

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

            // AUTO LOGIN AFTER REGISTRATION
            HttpContext.Session.SetInt32("UserId", user.UserId);

            HttpContext.Session.SetString("UserName", user.Name);

            HttpContext.Session.SetString("UserRole", "User");

            TempData["Success"] =
                "Welcome to CourtSync Pro! Your account has been created.";

            return RedirectToAction("Index", "Home");
        }

        // ── OWNER REGISTRATION PAGE ─────────────────────────────
        [HttpGet]
        public IActionResult RegisterOwner()
        {
            return View(new OwnerRegisterViewModel());
        }

        // ── PROCESS OWNER REGISTRATION ──────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterOwner(OwnerRegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            bool emailExists = await _db.CourtOwners
                .AnyAsync(o => o.Email == model.Email);

            if (emailExists)
            {
                ModelState.AddModelError("Email",
                    "This email is already registered.");

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
                IsVerified = false,
                RegisterDate = DateTime.UtcNow
            };

            _db.CourtOwners.Add(owner);

            await _db.SaveChangesAsync();

            TempData["Info"] =
                "Registration submitted! An admin will verify your account shortly.";

            return RedirectToAction("Login");
        }

        // ── LOGOUT ───────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }
    }
}