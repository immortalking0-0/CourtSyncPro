using CourtSyncPro.Data;
using CourtSyncPro.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourtSyncPro.Controllers
{
    public class TimeSlotsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public TimeSlotsController(ApplicationDbContext db) => _db = db;

        // GET: /TimeSlots/Create?courtId=5
        public async Task<IActionResult> Create(int courtId)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var ownerIdStr = HttpContext.Session.GetString("UserId");

            if (role != "Owner" || ownerIdStr == null)
                return RedirectToAction("Login", "Account");

            var court = await _db.Courts.FindAsync(courtId);
            if (court == null || court.OwnerId != int.Parse(ownerIdStr))
                return Forbid();

            ViewBag.Court = court;
            return View(new TimeSlot { CourtId = courtId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TimeSlot slot)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Owner")
                return RedirectToAction("Login", "Account");

            ModelState.Remove("Court");
            ModelState.Remove("Bookings");

            if (ModelState.IsValid)
            {
                slot.IsAvailable = true;
                slot.IsBlocked = false;
                _db.TimeSlots.Add(slot);
                await _db.SaveChangesAsync();

                TempData["Success"] = $"Time slot added: {slot.StartTime:dd MMM HH:mm} – {slot.EndTime:HH:mm}";
                return RedirectToAction("ManageSlots", new { courtId = slot.CourtId });
            }

            var court = await _db.Courts.FindAsync(slot.CourtId);
            ViewBag.Court = court;
            return View(slot);
        }

        // GET: /TimeSlots/ManageSlots/5
        public async Task<IActionResult> ManageSlots(int courtId)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var ownerIdStr = HttpContext.Session.GetString("UserId");

            if (role != "Owner" || ownerIdStr == null)
                return RedirectToAction("Login", "Account");

            var court = await _db.Courts.FindAsync(courtId);
            if (court == null || court.OwnerId != int.Parse(ownerIdStr))
                return Forbid();

            var slots = await _db.TimeSlots
                                 .Where(s => s.CourtId == courtId)
                                 .OrderBy(s => s.StartTime)
                                 .ToListAsync();

            ViewBag.Court = court;
            return View(slots);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var slot = await _db.TimeSlots.FindAsync(id);
            if (slot != null)
            {
                int courtId = slot.CourtId;
                _db.TimeSlots.Remove(slot);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Slot removed.";
                return RedirectToAction("ManageSlots", new { courtId });
            }
            return RedirectToAction("Dashboard", "Courts");
        }
    }
}