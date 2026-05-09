using CourtSyncPro.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourtSyncPro.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        public HomeController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            int v = await _db.Courts.CountAsync(c => c.IsActive);
            ViewBag.TotalCourts = v;
            ViewBag.TotalBookings = await _db.Bookings.CountAsync();
            ViewBag.TotalUsers = await _db.Users.CountAsync();
            ViewBag.TopCourts = await _db.Courts
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.Rating)
                .Take(4)
                .Include(c => c.CourtOwner)
                .ToListAsync();
            return View();
        }
    }

}
