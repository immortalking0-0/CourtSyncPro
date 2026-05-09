using CourtSyncPro.Data;
using CourtSyncPro.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourtSyncPro.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public PaymentsController(ApplicationDbContext db) => _db = db;

        // GET: /Payments/Pay/5  (5 = BookingId)
        public async Task<IActionResult> Pay(int bookingId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            var role = HttpContext.Session.GetString("UserRole");
            if (userIdStr == null || role != "User")
                return RedirectToAction("Login", "Account");

            var booking = await _db.Bookings
                .Include(b => b.Court)
                .Include(b => b.TimeSlot)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return NotFound();

            // Already paid
            if (booking.Payment != null && booking.Payment.Status == PaymentStatus.Completed)
            {
                TempData["Error"] = "This booking is already paid.";
                return RedirectToAction("Details", "Bookings", new { id = bookingId });
            }

            // Cancelled booking can't be paid
            if (booking.Status == BookingStatus.Cancelled)
            {
                TempData["Error"] = "Cannot pay for a cancelled booking.";
                return RedirectToAction("Details", "Bookings", new { id = bookingId });
            }

            var payment = new Payment
            {
                BookingId = bookingId,
                Amount = booking.TotalAmount
            };

            ViewBag.Booking = booking;
            return View(payment);
        }

        // POST: /Payments/Pay
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(Payment payment)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            var role = HttpContext.Session.GetString("UserRole");
            if (userIdStr == null || role != "User")
                return RedirectToAction("Login", "Account");

            ModelState.Remove("Booking");
            ModelState.Remove("TransactionId");
            ModelState.Remove("PaidAt");
            ModelState.Remove("Status");

            if (!ModelState.IsValid)
            {
                var booking = await _db.Bookings
                    .Include(b => b.Court)
                    .Include(b => b.TimeSlot)
                    .FirstOrDefaultAsync(b => b.BookingId == payment.BookingId);
                ViewBag.Booking = booking;
                return View(payment);
            }

            // Check booking exists and isn't already paid
            var existingPayment = await _db.Payments
                .FirstOrDefaultAsync(p => p.BookingId == payment.BookingId
                                       && p.Status == PaymentStatus.Completed);
            if (existingPayment != null)
            {
                TempData["Error"] = "This booking is already paid.";
                return RedirectToAction("Details", "Bookings", new { id = payment.BookingId });
            }

            // Simulate payment processing
            // In production: integrate EasyPaisa/JazzCash API here
            bool paymentSuccess = true; // ← replace with real gateway result

            payment.Status = paymentSuccess ? PaymentStatus.Completed : PaymentStatus.Failed;
            payment.PaidAt = DateTime.Now;
            payment.TransactionId = "TXN-" + Guid.NewGuid().ToString("N")[..10].ToUpper();

            _db.Payments.Add(payment);

            // ✅ If payment succeeded, confirm the booking
            if (paymentSuccess)
            {
                var booking = await _db.Bookings.FindAsync(payment.BookingId);
                if (booking != null)
                    booking.Status = BookingStatus.Confirmed;
            }

            await _db.SaveChangesAsync();

            if (paymentSuccess)
            {
                TempData["Success"] = $"Payment successful! Transaction ID: {payment.TransactionId}";
                return RedirectToAction("Details", "Bookings", new { id = payment.BookingId });
            }
            else
            {
                TempData["Error"] = "Payment failed. Please try again.";
                return RedirectToAction("Pay", new { bookingId = payment.BookingId });
            }
        }

        // GET: /Payments/Receipt/5  (5 = PaymentId)
        public async Task<IActionResult> Receipt(int id)
        {
            var payment = await _db.Payments
                .Include(p => p.Booking).ThenInclude(b => b.Court)
                .Include(p => p.Booking).ThenInclude(b => b.TimeSlot)
                .Include(p => p.Booking).ThenInclude(b => b.User)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null) return NotFound();
            return View(payment);
        }

        // POST: /Payments/Refund/5  (admin use)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Refund(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Admin")
                return RedirectToAction("Login", "Account");

            var payment = await _db.Payments
                .Include(p => p.Booking).ThenInclude(b => b.TimeSlot)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null) return NotFound();

            payment.Status = PaymentStatus.Refunded;
            payment.Booking.Status = BookingStatus.Cancelled;

            // Free the slot back up
            if (payment.Booking.TimeSlot != null)
            {
                var slot = await _db.TimeSlots.FindAsync(payment.Booking.SlotId);
                if (slot != null)
                {
                    slot.IsAvailable = true;
                    _db.TimeSlots.Update(slot);
                }
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Payment refunded and booking cancelled.";
            return RedirectToAction("Details", "Bookings", new { id = payment.Booking.BookingId });
        }
    }
}