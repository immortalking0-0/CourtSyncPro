using CourtSyncPro.Data;
using CourtSyncPro.Hubs;                              // ← ADD
using CourtSyncPro.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;                   // ← ADD
using Microsoft.EntityFrameworkCore;

namespace CourtSyncPro.Controllers
{
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IHubContext<BookingHub> _hub; // ← ADD

        public BookingsController(
            ApplicationDbContext db,
            IHubContext<BookingHub> hub)               // ← ADD
        {
            _db = db;
            _hub = hub;                               // ← ADD
        }

        // ── All your existing actions stay exactly the same ──
        // ── Only update Create POST and Cancel POST below   ──

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Booking booking)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (!userId.HasValue || role != "User")
                return RedirectToAction("Login", "Account");

            booking.UserId = userId.Value;

            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("Court");
            ModelState.Remove("TimeSlot");
            ModelState.Remove("Payment");
            ModelState.Remove("QRCode");
            ModelState.Remove("BookingDate");
            ModelState.Remove("Status");
            ModelState.Remove("TotalAmount");
            ModelState.Remove("PromoCode");
            ModelState.Remove("SlotId");
            ModelState.Remove("CourtId");

            if (booking.SlotId == 0)
                ModelState.AddModelError("SlotId", "Please select a time slot.");
            if (booking.CourtId == 0)
                ModelState.AddModelError("CourtId", "Please select a court.");

            if (ModelState.IsValid)
            {
                var court = await _db.Courts.FindAsync(booking.CourtId);
                var slot = await _db.TimeSlots.FindAsync(booking.SlotId);

                if (court == null || slot == null)
                {
                    ModelState.AddModelError("", "Invalid court or time slot selected.");
                    await PopulateCreateViewBag();
                    return View(booking);
                }

                // ── Race condition guard ──────────────────────────
                // Re-check availability right before saving
                bool alreadyBooked = await _db.Bookings.AnyAsync(b =>
                    b.SlotId == slot.SlotId &&
                    b.Status != BookingStatus.Cancelled);

                if (alreadyBooked)
                {
                    ModelState.AddModelError("",
                        "⚡ Sorry! This slot was just booked by someone else. Please choose another.");
                    await PopulateCreateViewBag();
                    return View(booking);
                }

                // ── Price calculation (your existing logic) ───────
                var hours = (decimal)(slot.EndTime.TimeOfDay - slot.StartTime.TimeOfDay).TotalHours;

                if (hours <= 0 || hours > 5)
                {
                    ModelState.AddModelError("", "Invalid time slot duration.");
                    return View(booking);
                }

                booking.TotalAmount = Math.Round(court.PricePerHour * hours, 2);

                var daysAhead = (slot.StartTime.Date - DateTime.Now.Date).TotalDays;
                if (daysAhead >= 2)
                    booking.TotalAmount = Math.Round(booking.TotalAmount * 0.90m, 2);

                slot.IsAvailable = false;

                _db.Bookings.Add(booking);
                await _db.SaveChangesAsync();

                // ── Broadcast to ALL connected browsers ───────────
                string slotLabel = $"{slot.StartTime:dd MMM HH:mm} → {slot.EndTime:HH:mm}";

                await _hub.Clients.All.SendAsync(
                    "SlotTaken",
                    slot.SlotId,
                    booking.CourtId,
                    court.CourtName,
                    slotLabel);

                TempData["Success"] = $"Booking confirmed! Your QR Code: {booking.QRCode}";
                return RedirectToAction(nameof(Details), new { id = booking.BookingId });
            }

            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            TempData["Error"] = "Failed: " + string.Join(" | ", errors);
            await PopulateCreateViewBag();
            return View(booking);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var booking = await _db.Bookings
                .Include(b => b.TimeSlot)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();

            if (booking.TimeSlot != null)
                booking.TimeSlot.IsAvailable = true;

            booking.Status = BookingStatus.Cancelled;
            await _db.SaveChangesAsync();

            // ── Broadcast slot is free again ──────────────────────
            if (booking.TimeSlot != null)
            {
                await _hub.Clients.All.SendAsync(
                    "SlotAvailable",
                    booking.TimeSlot.SlotId,
                    booking.CourtId);
            }

            TempData["Success"] = "Booking cancelled and slot is now available.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Bookings
        public async Task<IActionResult> Index()
        {
            var bookings = await _db.Bookings
                .Include(b => b.User)
                .Include(b => b.Court)
                .Include(b => b.TimeSlot)
                .Include(b => b.Payment)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }

        // GET: /Bookings/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _db.Bookings
                .Include(b => b.User)
                .Include(b => b.Court).ThenInclude(c => c.CourtOwner)
                .Include(b => b.TimeSlot)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();
            return View(booking);
        }

        // GET: /Bookings/Create
        public async Task<IActionResult> Create()
        {
            await PopulateCreateViewBag();
            return View();
        }

        // GET: /Bookings/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var booking = await _db.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            ViewBag.Users = await _db.Users.ToListAsync();
            ViewBag.Courts = await _db.Courts.Where(c => c.IsActive).ToListAsync();
            return View(booking);
        }

        // POST: /Bookings/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Booking booking)
        {
            if (id != booking.BookingId) return BadRequest();
            if (ModelState.IsValid)
            {
                _db.Update(booking);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Booking updated.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Users = await _db.Users.ToListAsync();
            ViewBag.Courts = await _db.Courts.Where(c => c.IsActive).ToListAsync();
            return View(booking);
        }

        // GET: /Bookings/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var booking = await _db.Bookings
                .Include(b => b.User)
                .Include(b => b.Court)
                .FirstOrDefaultAsync(b => b.BookingId == id);
            if (booking == null) return NotFound();
            return View(booking);
        }

        // POST: /Bookings/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _db.Bookings.FindAsync(id);
            if (booking != null)
            {
                _db.Bookings.Remove(booking);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Booking deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ── Helper to repopulate dropdowns ──────────────────────
        private async Task PopulateCreateViewBag()
        {
            ViewBag.Users = await _db.Users.ToListAsync();
            ViewBag.Courts = await _db.Courts.Where(c => c.IsActive).ToListAsync();
            ViewBag.Slots = await _db.TimeSlots
                .Where(ts => !ts.IsBlocked &&
                    !_db.Bookings.Any(b =>
                        b.SlotId == ts.SlotId &&
                        b.Status != BookingStatus.Cancelled
                    )
                )
                .Include(ts => ts.Court)
                .ToListAsync();
        }
    }
}