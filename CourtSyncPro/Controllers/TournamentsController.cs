using CourtSyncPro.Data;
using CourtSyncPro.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CourtSyncPro.Controllers
{
    public class TournamentsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public TournamentsController(ApplicationDbContext db) => _db = db;

        // ── GET: /Tournaments ─────────────────────────────────────
        // Players view all tournaments with sport filter
        public async Task<IActionResult> Index(SportType? sport, string? status)
        {
            var query = _db.Tournaments
                .Include(t => t.Court)
                .Include(t => t.Registrations)
                .AsQueryable();

            if (sport.HasValue)
                query = query.Where(t => t.SportType == sport);

            if (!string.IsNullOrEmpty(status) &&
                Enum.TryParse<TournamentStatus>(status, out var s))
                query = query.Where(t => t.Status == s);

            ViewBag.Sports = Enum.GetValues<SportType>();
            ViewBag.Statuses = Enum.GetValues<TournamentStatus>();
            ViewBag.SelSport = sport;
            ViewBag.SelStatus = status;

            return View(await query
                .OrderBy(t => t.TournamentDate)
                .ToListAsync());
        }

        // ── GET: /Tournaments/Details/5 ───────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var tournament = await _db.Tournaments
                .Include(t => t.Court).ThenInclude(c => c.CourtOwner)
                .Include(t => t.Registrations).ThenInclude(r => r.User)
                .FirstOrDefaultAsync(t => t.TournamentId == id);

            if (tournament == null) return NotFound();

            // Check if logged-in player already registered
            var userId = HttpContext.Session.GetInt32("UserId");
            ViewBag.AlreadyRegistered = userId.HasValue &&
                tournament.Registrations.Any(r => r.UserId == userId.Value);
            ViewBag.SpotsLeft = tournament.MaxTeams -
                tournament.Registrations.Count(r =>
                    r.Status != RegistrationStatus.Cancelled);

            return View(tournament);
        }

        // ── GET: /Tournaments/Register/5 (Player joins) ───────────
        public async Task<IActionResult> Register(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var tournament = await _db.Tournaments
                .Include(t => t.Court)
                .FirstOrDefaultAsync(t => t.TournamentId == id);

            if (tournament == null) return NotFound();

            // Already registered?
            bool alreadyIn = await _db.TournamentRegistrations
                .AnyAsync(r => r.TournamentId == id && r.UserId == userId.Value);
            if (alreadyIn)
            {
                TempData["Error"] = "You are already registered for this tournament!";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Full?
            int taken = await _db.TournamentRegistrations
                .CountAsync(r => r.TournamentId == id &&
                                 r.Status != RegistrationStatus.Cancelled);
            if (taken >= tournament.MaxTeams)
            {
                TempData["Error"] = "Sorry, this tournament is full!";
                return RedirectToAction(nameof(Details), new { id });
            }

            ViewBag.Tournament = tournament;
            return View(new TournamentRegistration
            {
                TournamentId = id,
                UserId = userId.Value,
                FeePaid = tournament.RegistrationFee
            });
        }

        // ── POST: /Tournaments/Register ───────────────────────────
        // ── POST: /Tournaments/Register ───────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(TournamentRegistration reg, string paymentMethod)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
            {
                TempData["Error"] = "Please login first.";
                return RedirectToAction("Login", "Account");
            }

            var tournament = await _db.Tournaments
                .Include(t => t.Court)
                .FirstOrDefaultAsync(t => t.TournamentId == reg.TournamentId);

            if (tournament == null)
                return NotFound();

            // Remove validation for auto-generated fields
            ModelState.Remove("User");
            ModelState.Remove("Tournament");

            // Check duplicate registration
            bool alreadyRegistered = await _db.TournamentRegistrations
                .AnyAsync(r => r.TournamentId == reg.TournamentId
                            && r.UserId == userId.Value
                            && r.Status != RegistrationStatus.Cancelled);

            if (alreadyRegistered)
            {
                TempData["Error"] = "You are already registered!";
                return RedirectToAction("Details", new { id = reg.TournamentId });
            }

            // Tournament full check
            int registeredCount = await _db.TournamentRegistrations
                .CountAsync(r => r.TournamentId == reg.TournamentId
                              && r.Status != RegistrationStatus.Cancelled);

            if (registeredCount >= tournament.MaxTeams)
            {
                TempData["Error"] = "Tournament is full!";
                return RedirectToAction("Details", new { id = reg.TournamentId });
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Tournament = tournament;
                return View(reg);
            }

            reg.UserId = userId.Value;
            reg.FeePaid = tournament.RegistrationFee;
            reg.TransactionId = "TRN-" + Guid.NewGuid().ToString("N")[..8].ToUpper();
            reg.Status = RegistrationStatus.Confirmed;
            reg.RegisteredAt = DateTime.UtcNow;

            _db.TournamentRegistrations.Add(reg);

            await _db.SaveChangesAsync();

            TempData["Success"] = "Tournament registration successful!";

            return RedirectToAction(nameof(MyRegistrations));
        }

        // ── GET: /Tournaments/MyRegistrations ─────────────────────
        public async Task<IActionResult> MyRegistrations()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var regs = await _db.TournamentRegistrations
                .Where(r => r.UserId == userId.Value)
                .Include(r => r.Tournament)
                    .ThenInclude(t => t.Court)
                .OrderByDescending(r => r.RegisteredAt)
                .ToListAsync();

            return View(regs);
        }

        // ── POST: /Tournaments/CancelRegistration/5 ───────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRegistration(int id)
        {
            var reg = await _db.TournamentRegistrations.FindAsync(id);
            if (reg == null) return NotFound();

            reg.Status = RegistrationStatus.Cancelled;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Registration cancelled successfully.";
            return RedirectToAction(nameof(MyRegistrations));
        }

        // ════════════════════════════════════════════════════════════
        // ADMIN / OWNER ACTIONS
        // ════════════════════════════════════════════════════════════

        // ── GET: /Tournaments/Create (Admin or Owner) ─────────────
        public async Task<IActionResult> Create()
        {
            var role = HttpContext.Session.GetString("UserRole");
            var ownerId = HttpContext.Session.GetInt32("OwnerId");

            if (role != "Admin" && role != "CourtOwner" && role != "Owner")
                return RedirectToAction("Login", "Account");

            var courtsQuery = _db.Courts.Where(c => c.IsActive);

            if (role == "CourtOwner" || role == "Owner")
                courtsQuery = courtsQuery.Where(c => c.OwnerId == ownerId);

            var vm = new TournamentCreateVM
            {
                Courts = await courtsQuery
                    .Select(c => new SelectListItem
                    {
                        Value = c.CourtId.ToString(),
                        Text = c.CourtName
                    })
                    .ToListAsync(),

                Status = TournamentStatus.Upcoming
            };

            return View(vm);
        }
        // ── POST: /Tournaments/Create ─────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TournamentCreateVM vm)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var ownerId = HttpContext.Session.GetInt32("OwnerId");

            if (role != "Admin" && role != "CourtOwner" && role != "Owner")
                return RedirectToAction("Login", "Account");

            // 🔴 validation failure → reload dropdown safely
            if (!ModelState.IsValid)
            {
                var courtsQuery = _db.Courts.Where(c => c.IsActive);

                if (role == "CourtOwner" || role == "Owner")
                    courtsQuery = courtsQuery.Where(c => c.OwnerId == ownerId);

                vm.Courts = await courtsQuery
                    .Select(c => new SelectListItem
                    {
                        Value = c.CourtId.ToString(),
                        Text = c.CourtName
                    })
                    .ToListAsync();

                return View(vm);
            }

            var tournament = new Tournament
            {
                TournamentName = vm.TournamentName,
                Description = vm.Description,
                SportType = vm.SportType,
                Status = vm.Status,
                CourtId = vm.CourtId,
                TournamentDate = vm.TournamentDate,
                RegistrationDeadline = vm.RegistrationDeadline,
                MaxTeams = vm.MaxTeams,
                RegistrationFee = vm.RegistrationFee,
                PrizeInformation = vm.PrizeInformation,
                CreatedAt = DateTime.UtcNow,
                CreatedByAdmin = role == "Admin",
                CreatedByOwnerId = (role == "CourtOwner" || role == "Owner") ? ownerId : null
            };

            _db.Tournaments.Add(tournament);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Tournament created successfully!";
            return RedirectToAction(nameof(Index));
        }        // ── GET: /Tournaments/Edit/5 ──────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Admin" && role != "CourtOwner")
                return RedirectToAction("Login", "Account");

            var t = await _db.Tournaments.FindAsync(id);
            if (t == null) return NotFound();

            ViewBag.Courts = await _db.Courts
                .Where(c => c.IsActive).ToListAsync();
            return View(t);
        }

        // ── POST: /Tournaments/Edit/5 ─────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Tournament tournament)
        {
            if (id != tournament.TournamentId) return BadRequest();

            if (ModelState.IsValid)
            {
                _db.Update(tournament);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Tournament updated!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Courts = await _db.Courts
                .Where(c => c.IsActive).ToListAsync();
            return View(tournament);
        }

        // ── GET: /Tournaments/Participants/5 (Admin/Owner) ────────
        public async Task<IActionResult> Participants(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Admin" && role != "CourtOwner")
                return RedirectToAction("Login", "Account");

            var tournament = await _db.Tournaments
                .Include(t => t.Registrations).ThenInclude(r => r.User)
                .Include(t => t.Court)
                .FirstOrDefaultAsync(t => t.TournamentId == id);

            if (tournament == null) return NotFound();
            return View(tournament);
        }

        // ── POST: /Tournaments/Delete/5 ───────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Admin") return Forbid();

            var t = await _db.Tournaments.FindAsync(id);
            if (t != null)
            {
                t.Status = TournamentStatus.Cancelled;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Tournament cancelled.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}