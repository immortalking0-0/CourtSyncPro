using CourtSyncPro.Data;
using CourtSyncPro.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourtSyncPro.Controllers
{
    public class CourtsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CourtsController(ApplicationDbContext db) => _db = db;

        // GET: /Courts  ← NO [HttpPost] here!
        public async Task<IActionResult> Index(string? city, SportType? sport, decimal? maxPrice)
        {
            var query = _db.Courts
                .Include(c => c.CourtOwner)
                .Where(c => c.IsActive);

            if (!string.IsNullOrEmpty(city))
                query = query.Where(c => c.City.Contains(city));

            if (sport.HasValue)
                query = query.Where(c => c.SportType == sport);

            if (maxPrice.HasValue)
                query = query.Where(c => c.PricePerHour <= maxPrice);

            ViewBag.Cities = await _db.Courts.Select(c => c.City).Distinct().ToListAsync();
            ViewBag.Sports = Enum.GetValues<SportType>();
            ViewBag.SelCity = city;
            ViewBag.SelSport = sport;
            ViewBag.MaxPrice = maxPrice;

            return View(await query.ToListAsync());
        }

        // GET: /Courts/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var court = await _db.Courts
                .Include(c => c.CourtOwner)
                .Include(c => c.Reviews).ThenInclude(r => r.User)
                .FirstOrDefaultAsync(c => c.CourtId == id);

            if (court == null) return NotFound();
            return View(court);
        }

        // GET: /Courts/Create
        [HttpGet]
        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Owner")
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Court court)
        {
            var ownerIdStr = HttpContext.Session.GetString("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (ownerIdStr == null || role != "Owner")
                return RedirectToAction("Login", "Account");

            court.OwnerId = int.Parse(ownerIdStr);

            // Remove ALL navigation properties and auto-set fields from validation
            ModelState.Remove("OwnerId");
            ModelState.Remove("CourtOwner");
            ModelState.Remove("TimeSlots");
            ModelState.Remove("Bookings");
            ModelState.Remove("Reviews");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("Rating");
            ModelState.Remove("IsActive");

            if (ModelState.IsValid)
            {
                court.CreatedAt = DateTime.UtcNow;
                court.IsActive = true;
                court.Rating = 0;

                _db.Courts.Add(court);
                await _db.SaveChangesAsync();

                TempData["Success"] = $"{court.CourtName} has been registered successfully!";
                return RedirectToAction(nameof(Dashboard));
            }

            // This will now show exactly which field is failing
            var errors = ModelState.Values
                                   .SelectMany(v => v.Errors)
                                   .Select(e => e.ErrorMessage)
                                   .ToList();
            TempData["Error"] = "Validation failed: " + string.Join(" | ", errors);

            return View(court);
        }

        // GET: /Courts/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var court = await _db.Courts.FindAsync(id);
            if (court == null) return NotFound();
            ViewBag.Owners = _db.CourtOwners.Where(o => o.IsVerified).ToList();
            return View(court);
        }

        // POST: /Courts/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Court court)
        {
            if (id != court.CourtId) return BadRequest();
            if (ModelState.IsValid)
            {
                _db.Update(court);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Court updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(court);
        }

        // GET: /Courts/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var court = await _db.Courts
                .Include(c => c.CourtOwner)
                .FirstOrDefaultAsync(c => c.CourtId == id);
            if (court == null) return NotFound();
            return View(court);
        }

        // POST: /Courts/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var court = await _db.Courts.FindAsync(id);
            if (court != null)
            {
                court.IsActive = false;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Court removed from listings.";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Dashboard()
        {
            var ownerId = HttpContext.Session.GetInt32("UserId");

            var role = HttpContext.Session.GetString("UserRole");

            if (!ownerId.HasValue || role != "Owner")
                return RedirectToAction("Login", "Account");

            var myCourts = await _db.Courts
                .Where(c => c.OwnerId == ownerId.Value)
                .ToListAsync();

            return View(myCourts);
        }
    }
}