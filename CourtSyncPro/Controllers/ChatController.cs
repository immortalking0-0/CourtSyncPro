using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CourtSyncPro.Data;
using CourtSyncPro.Models.Entities;

namespace CourtSyncPro.Controllers
{
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ChatController(ApplicationDbContext db) => _db = db;

        // GET: /Chat/With/5
        public async Task<IActionResult> With(int id)
        {
            var myId = HttpContext.Session.GetInt32("UserId");
            if (!myId.HasValue)
                return RedirectToAction("Login", "Account");

            // Verify they have accepted connection
            var request = await _db.ChatRequests
                .FirstOrDefaultAsync(r =>
                    ((r.FromUserId == myId.Value && r.ToUserId == id) ||
                     (r.FromUserId == id && r.ToUserId == myId.Value)) &&
                    r.Status == ChatRequestStatus.Accepted);

            if (request == null)
            {
                TempData["Error"] = "No active chat connection with this player.";
                return RedirectToAction("Index", "Players");
            }

            // Load chat history
            var history = await _db.ChatMessages
                .Where(m => (m.FromUserId == myId.Value && m.ToUserId == id) ||
                            (m.FromUserId == id && m.ToUserId == myId.Value))
                .OrderBy(m => m.SentAt)
                .Include(m => m.FromUser)
                .ToListAsync();

            // Mark messages as read
            var unread = await _db.ChatMessages
                .Where(m => m.FromUserId == id &&
                            m.ToUserId == myId.Value &&
                            !m.IsRead)
                .ToListAsync();

            unread.ForEach(m => m.IsRead = true);
            await _db.SaveChangesAsync();

            var otherPlayer = await _db.Users.FindAsync(id);

            ViewBag.OtherPlayer = otherPlayer;
            ViewBag.OtherUserId = id;
            ViewBag.MyId = myId.Value;
            ViewBag.History = history;

            return View();
        }

        // GET: /Chat/UnreadCount (AJAX for navbar badge)
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var myId = HttpContext.Session.GetInt32("UserId");
            if (!myId.HasValue) return Json(new { count = 0 });

            var count = await _db.ChatMessages
                .CountAsync(m => m.ToUserId == myId.Value && !m.IsRead);

            return Json(new { count });
        }
    }
}

// No changes here - placeholder for search