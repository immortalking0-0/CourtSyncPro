using CourtSyncPro.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    public AdminController(ApplicationDbContext db) => _db = db;

    // Guard — only admin can access
    private bool IsAdmin() => HttpContext.Session.GetString("UserRole") == "Admin";

    public async Task<IActionResult> Dashboard()
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Account");

        ViewBag.TotalUsers = await _db.Users.CountAsync();
        ViewBag.TotalOwners = await _db.CourtOwners.CountAsync();
        ViewBag.TotalCourts = await _db.Courts.CountAsync();
        ViewBag.TotalBookings = await _db.Bookings.CountAsync();
        ViewBag.PendingOwners = await _db.CourtOwners.CountAsync(o => !o.IsVerified);

        return View();
    }

    public async Task<IActionResult> PendingOwners()
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Account");

        var pending = await _db.CourtOwners
                               .Where(o => !o.IsVerified)
                               .ToListAsync();
        return View(pending);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveOwner(int id)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Account");

        var owner = await _db.CourtOwners.FindAsync(id);
        if (owner != null)
        {
            owner.IsVerified = true;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"{owner.BusinessName} has been approved.";
        }
        return RedirectToAction(nameof(PendingOwners));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectOwner(int id)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Account");

        var owner = await _db.CourtOwners.FindAsync(id);
        if (owner != null)
        {
            _db.CourtOwners.Remove(owner);
            await _db.SaveChangesAsync();
            TempData["Error"] = $"Owner rejected and removed.";
        }
        return RedirectToAction(nameof(PendingOwners));
    }
}