using CourtSyncPro.Data;
using CourtSyncPro.Models.Entities;
using CourtSyncPro.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourtSyncPro.Controllers
{
    public class CourtsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly DynamicPricingService _pricing;

        public CourtsController(ApplicationDbContext db,
                                DynamicPricingService pricing)
        {
            _db = db;
            _pricing = pricing;
        }   
        
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
            .Include(c => c.TimeSlots
                .Where(ts => ts.IsAvailable
                          && !ts.IsBlocked
                          && ts.StartTime > DateTime.UtcNow))
            .Include(c => c.Reviews)
            .FirstOrDefaultAsync(c => c.CourtId == id);

            if (court == null) return NotFound();

            // Count today's bookings for demand pricing
            int bookingsToday = await _db.Bookings
                .CountAsync(b => b.CourtId == id
                              && b.BookingDate.Date == DateTime.Today
                              && b.Status != BookingStatus.Cancelled);

            // Calculate dynamic price for each slot
            var slotPrices = new Dictionary<int, PriceBreakdown>();

            foreach (var slot in court.TimeSlots)
            {
                var breakdown = _pricing.Calculate(
                    court.PricePerHour,
                    slot.StartTime,
                    court.Rating,
                    bookingsToday
                );
                slotPrices[slot.SlotId] = breakdown;
            }

            ViewBag.SlotPrices = slotPrices;
            ViewBag.BasePrice = court.PricePerHour;
            ViewBag.BookingsToday = bookingsToday;

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Court court)
        {
            var ownerId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            // Check session
            if (!ownerId.HasValue || role != "Owner")
            {
                return RedirectToAction("Login", "Account");
            }

            // Assign owner
            court.OwnerId = ownerId.Value;

            // Remove validation for navigation properties
            ModelState.Remove("CourtOwner");
            ModelState.Remove("TimeSlots");
            ModelState.Remove("Bookings");
            ModelState.Remove("Reviews");

            if (ModelState.IsValid)
            {
                court.CreatedAt = DateTime.UtcNow;
                court.IsActive = true;
                court.Rating = 0;

                _db.Courts.Add(court);

                await _db.SaveChangesAsync();

                TempData["Success"] = "Court created successfully!";

                return RedirectToAction(nameof(Dashboard));
            }

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