using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CourtSyncPro.Data;
using CourtSyncPro.Hubs;
using CourtSyncPro.Models.Entities;

namespace CourtSyncPro.Controllers
{
    public class PlayersController : Controller
    {
        private readonly ApplicationDbContext _db;

        public PlayersController(ApplicationDbContext db) => _db = db;

        // GET: /Players — online players list
        public async Task<IActionResult> Index()
        {
            var myId = HttpContext.Session.GetInt32("UserId");
            if (!myId.HasValue)
                return RedirectToAction("Login", "Account");

            // Get online user IDs from hub
            var onlineIds = ChatHub.GetOnlineUserIds()
                .Where(id => id != myId.Value)
                .ToList();

            // Get their user records
            var onlinePlayers = await _db.Users
                .Where(u => onlineIds.Contains(u.UserId))
                .ToListAsync();

            // Get my pending sent requests
            var sentRequests = await _db.ChatRequests
                .Where(r => r.FromUserId == myId.Value &&
                            r.Status == ChatRequestStatus.Pending)
                .Select(r => r.ToUserId)
                .ToListAsync();

            // Get accepted chats
            var acceptedChats = await _db.ChatRequests
                .Where(r => (r.FromUserId == myId.Value ||
                             r.ToUserId == myId.Value) &&
                             r.Status == ChatRequestStatus.Accepted)
                .Select(r => r.FromUserId == myId.Value
                    ? r.ToUserId : r.FromUserId)
                .ToListAsync();

            // Pending requests FOR me
            var pendingForMe = await _db.ChatRequests
                .Include(r => r.FromUser)
                .Where(r => r.ToUserId == myId.Value &&
                            r.Status == ChatRequestStatus.Pending)
                .ToListAsync();

            ViewBag.OnlinePlayers = onlinePlayers;
            ViewBag.SentRequests = sentRequests;
            ViewBag.AcceptedChats = acceptedChats;
            ViewBag.PendingForMe = pendingForMe;
            ViewBag.MyId = myId.Value;

            return View();
        }

        // GET: /Players/PendingRequests (AJAX)
        [HttpGet]
        public async Task<IActionResult> PendingRequests()
        {
            var myId = HttpContext.Session.GetInt32("UserId");
            if (!myId.HasValue) return Json(new List<object>());

            var pending = await _db.ChatRequests
                .Include(r => r.FromUser)
                .Where(r => r.ToUserId == myId.Value &&
                            r.Status == ChatRequestStatus.Pending)
                .Select(r => new
                {
                    r.RequestId,
                    r.FromUserId,
                    fromName = r.FromUser.Name,
                    r.SentAt
                })
                .ToListAsync();

            return Json(pending);
        }
    }
}