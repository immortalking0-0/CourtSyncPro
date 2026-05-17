using CourtSyncPro.Data;
using CourtSyncPro.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourtSyncPro.Controllers
{
    public class TimeSlotsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public TimeSlotsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /TimeSlots/Create?courtId=5
        public async Task<IActionResult> Create(int courtId)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var ownerId = HttpContext.Session.GetInt32("UserId");

            if (role != "Owner" || !ownerId.HasValue)
                return RedirectToAction("Login", "Account");

            var court = await _db.Courts.FindAsync(courtId);

            if (court == null || court.OwnerId != ownerId.Value)
                return Forbid();

            ViewBag.Court = court;

            return View(new TimeSlot
            {
                CourtId = courtId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TimeSlot slot)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var ownerId = HttpContext.Session.GetInt32("UserId");

            if (role != "Owner" || !ownerId.HasValue)
                return RedirectToAction("Login", "Account");

            ModelState.Remove("Court");
            ModelState.Remove("Bookings");

            var court = await _db.Courts.FindAsync(slot.CourtId);

            if (court == null || court.OwnerId != ownerId.Value)
                return Forbid();

            if (ModelState.IsValid)
            {
                slot.IsAvailable = true;
                slot.IsBlocked = false;

                _db.TimeSlots.Add(slot);

                await _db.SaveChangesAsync();

                TempData["Success"] =
                    $"Time slot added successfully!";

                return RedirectToAction(
                    "ManageSlots",
                    new { courtId = slot.CourtId });
            }

            ViewBag.Court = court;

            return View(slot);
        }

        // GET: /TimeSlots/ManageSlots/5
        public async Task<IActionResult> ManageSlots(int courtId)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var ownerId = HttpContext.Session.GetInt32("UserId");

            if (role != "Owner" || !ownerId.HasValue)
                return RedirectToAction("Login", "Account");

            var court = await _db.Courts.FindAsync(courtId);

            if (court == null || court.OwnerId != ownerId.Value)
                return Forbid();

            var slots = await _db.TimeSlots
                .Where(s => s.CourtId == courtId)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            ViewBag.Court = court;

            return View(slots);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var ownerId = HttpContext.Session.GetInt32("UserId");

            if (role != "Owner" || !ownerId.HasValue)
                return RedirectToAction("Login", "Account");

            var slot = await _db.TimeSlots.FindAsync(id);

            if (slot == null)
                return NotFound();

            var court = await _db.Courts.FindAsync(slot.CourtId);

            if (court == null || court.OwnerId != ownerId.Value)
                return Forbid();

            int courtId = slot.CourtId;

            _db.TimeSlots.Remove(slot);

            await _db.SaveChangesAsync();

            TempData["Success"] = "Slot removed successfully.";

            return RedirectToAction(
                "ManageSlots",
                new { courtId });
        }
    }
}