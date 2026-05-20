using CourtSyncPro.Data;
using CourtSyncPro.Hubs;                              // ← ADD
using CourtSyncPro.Models.Entities;
using CourtSyncPro.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;                   // ← ADD
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace CourtSyncPro.Controllers
{
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly DynamicPricingService _pricing;
        private readonly IHubContext<BookingHub> _hub; // ← ADD

        public BookingsController(
            ApplicationDbContext db, DynamicPricingService pricing,
            IHubContext<BookingHub> hub)               // ← ADD
        {
            _db = db;
            _pricing = pricing;
            _hub = hub;                               // ← ADD
        }

        // ── All your existing actions stay exactly the same ──
        // ── Only update Create POST and Cancel POST below   ──

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Booking booking)
        {
            // ── Step 1: Session check — must be logged in as Player ──
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (!userId.HasValue || role != "User")
                return RedirectToAction("Login", "Account");

            booking.UserId = userId.Value;

            // ── Step 2: Remove navigation property errors ────────────
            // EF Core navigation properties are not submitted in forms
            // so we must remove them from ModelState validation
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

            // ── Step 3: Manual validation for required dropdowns ─────
            if (booking.SlotId == 0)
                ModelState.AddModelError("SlotId", "Please select a time slot.");
            if (booking.CourtId == 0)
                ModelState.AddModelError("CourtId", "Please select a court.");

            if (ModelState.IsValid)
            {
                // ── Step 4: Load court and slot from database ─────────
                var court = await _db.Courts.FindAsync(booking.CourtId);
                var slot = await _db.TimeSlots.FindAsync(booking.SlotId);

                if (court == null || slot == null)
                {
                    ModelState.AddModelError("",
                        "Invalid court or time slot selected.");
                    await PopulateCreateViewBag();
                    return View(booking);
                }

                // ── Step 5: Race condition guard ──────────────────────
                // Check AGAIN right before saving — another user may have
                // booked this slot between loading the form and submitting
                bool alreadyBooked = await _db.Bookings.AnyAsync(b =>
                    b.SlotId == slot.SlotId &&
                    b.Status != BookingStatus.Cancelled);

                if (alreadyBooked)
                {
                    ModelState.AddModelError("",
                        "⚡ Sorry! This slot was just booked by someone else." +
                        " Please choose another slot.");
                    await PopulateCreateViewBag();
                    return View(booking);
                }

                // ── Step 6: Validate slot duration ───────────────────
                var hours = (slot.EndTime - slot.StartTime).TotalHours;

                if (hours <= 0 || hours > 5)
                {
                    ModelState.AddModelError("",
                        "Invalid time slot duration. Must be between 1–5 hours.");
                    await PopulateCreateViewBag();
                    return View(booking);
                }

                // ── Step 7: Count today's bookings for demand factor ──
                int bookingsToday = await _db.Bookings
                    .CountAsync(b =>
                        b.CourtId == booking.CourtId &&
                        b.BookingDate.Date == DateTime.Today &&
                        b.Status != BookingStatus.Cancelled);

                // ── Step 8: Dynamic pricing calculation ───────────────
                // Factors: Season + Time of day + Day of week +
                //          Court rating + Live demand
                var breakdown = _pricing.Calculate(
                    court.PricePerHour,
                    slot.StartTime,
                    court.Rating,
                    bookingsToday
                );

                // Total = dynamic price per hour × hours booked
                booking.TotalAmount = Math.Round(
                    breakdown.FinalPrice * (decimal)hours, 2);

                // ── Step 9: Early-bird 10% discount ───────────────────
                // Applied on top of dynamic price if 48+ hours in advance
                var hoursUntilSlot = (slot.StartTime - DateTime.UtcNow).TotalHours;
                if (hoursUntilSlot >= 48)
                    booking.TotalAmount = Math.Round(
                        booking.TotalAmount * 0.90m, 2);

                // ── Step 10: Set booking metadata ────────────────────
                booking.BookingDate = DateTime.UtcNow;
                booking.Status = BookingStatus.Pending;

                // Generate unique 12-character QR code
                booking.QRCode = Guid.NewGuid().ToString("N")[..12].ToUpper();

                // ── Step 11: Lock the slot ────────────────────────────
                // Set IsAvailable = false so no one else can book it
                slot.IsAvailable = false;

                // ── Step 12: Save everything to database ──────────────
                _db.Bookings.Add(booking);
                await _db.SaveChangesAsync();

                // ── Step 13: Generate QR code image (Base64) ─────────
                var qrBase64 = GenerateQRCodeBase64(booking.QRCode);
                TempData["QRCodeImage"] = qrBase64;

                // ── Step 14: Broadcast slot taken via SignalR ─────────
                // Tells ALL connected browsers this slot is now gone
                // (Must be BEFORE the redirect — not after)
                string slotLabel =
                    $"{slot.StartTime:dd MMM HH:mm} → {slot.EndTime:HH:mm}";

                await _hub.Clients.All.SendAsync(
                    "SlotTaken",
                    slot.SlotId,
                    booking.CourtId,
                    court.CourtName,
                    slotLabel
                );

                // ── Step 15: Success and redirect ────────────────────
                TempData["Success"] =
                    $"🎉 Booking #{booking.BookingId} confirmed! " +
                    $"Total: Rs. {booking.TotalAmount} | QR: {booking.QRCode}";

                return RedirectToAction(nameof(Details),
                    new { id = booking.BookingId });
            }

            // ── Validation failed — collect all errors and show form ──
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            TempData["Error"] = "Booking failed: " + string.Join(" | ", errors);

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
            await ExpireOldSlots();
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            IQueryable<Booking> query = _db.Bookings
                .Include(b => b.User)
                .Include(b => b.Court)
                .Include(b => b.TimeSlot)
                .Include(b => b.Payment);

            // Admin sees all, players only see their own
            if (role != "Admin")
                query = query.Where(b => b.UserId == userId.Value);

            var bookings = await query
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

            // ✅ encode full booking info into QR code
            var qrText = $@"CourtSync Pro Booking
====================
Booking ID  : {booking.BookingId}
Player      : {booking.User.Name}
Court       : {booking.Court.CourtName}
Sport       : {booking.Court.SportType}
Date        : {booking.TimeSlot.StartTime:dd MMM yyyy}
Time        : {booking.TimeSlot.StartTime:hh:mm tt} - {booking.TimeSlot.EndTime:hh:mm tt}
Amount      : Rs.{booking.TotalAmount}
Status      : {booking.Status}
QR Code     : {booking.QRCode}
====================
Show this at the venue for check-in.";

            ViewBag.QRCodeImage = GenerateQRCodeBase64(qrText);

            return View(booking);
        }

        // GET: /Bookings/Create
        public async Task<IActionResult> Create()
        {
            await ExpireOldSlots();
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
            ViewBag.Courts = await _db.Courts.Where(c => c.IsActive).ToListAsync();
            ViewBag.Slots = await _db.TimeSlots
                .Where(ts => !ts.IsBlocked &&
                    ts.StartTime > DateTime.Now &&
                    !_db.Bookings.Any(b =>
                        b.SlotId == ts.SlotId &&
                        b.Status != BookingStatus.Cancelled))
                .Include(ts => ts.Court)   // ← must include Court
                .ToListAsync();
        }

        [HttpPost]
        public async Task<IActionResult> CreateFromAI([FromBody] AiBookingRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Json(new { message = "Please log in to book a court." });

            var slot = await _db.TimeSlots
                .Include(s => s.Court)
                .FirstOrDefaultAsync(s => s.SlotId == request.SlotId
                                       && s.IsAvailable
                                       && !s.IsBlocked);

            if (slot == null)
                return Json(new { message = "Sorry, that slot is no longer available." });

            var hours = (decimal)(slot.EndTime - slot.StartTime).TotalHours;
            var total = slot.Court.PricePerHour * hours;

            var booking = new Booking
            {
                UserId = userId.Value,
                CourtId = slot.CourtId,
                SlotId = slot.SlotId,
                TotalAmount = total,
                Status = BookingStatus.Pending,
                BookingDate = DateTime.UtcNow,
                QRCode = Guid.NewGuid().ToString("N")[..12].ToUpper()
            };

            slot.IsAvailable = false;

            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync();

            return Json(new
            {
                message = $"✅ Booking confirmed for {slot.Court.CourtName} at {slot.StartTime:hh:mm tt} on {slot.StartTime:dd MMM yyyy}! Your QR code is {booking.QRCode}. Please complete payment to confirm your slot.",
                bookingId = booking.BookingId
            });
        }

        private async Task ExpireOldSlots()
        {
            var expiredSlots = await _db.TimeSlots
                .Where(s => s.StartTime < DateTime.Now && s.IsAvailable)
                .ToListAsync();

            foreach (var slot in expiredSlots)
                slot.IsAvailable = false;

            if (expiredSlots.Any())
                await _db.SaveChangesAsync();
        }

        private string GenerateQRCodeBase64(string text)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrData);
            var bytes = qrCode.GetGraphic(10);
            return Convert.ToBase64String(bytes);
        }

        public class AiBookingRequest
        {
            public int SlotId { get; set; }
        }
    }
}