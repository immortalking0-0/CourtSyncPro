namespace CourtSyncPro.Models.AI.Services
{
    using CourtSyncPro.Data;
    using Microsoft.EntityFrameworkCore;

    public class BookingAiService
    {
        private readonly GeminiService _gemini;
        private readonly ApplicationDbContext _db;

        public BookingAiService(GeminiService gemini, ApplicationDbContext db)
        {
            _gemini = gemini;
            _db = db;
        }

        public async Task<string> SuggestBookingAsync(string userInput)
        {
            // ✅ YOUR CODE GOES HERE
            var courts = await _db.Courts.ToListAsync();
            var slots = await _db.TimeSlots
                .Where(s => s.IsAvailable)
                .ToListAsync();

            var prompt = $@"
Available courts:
{string.Join("\n", courts.Select(c => $"{c.CourtName} - {c.PricePerHour}/hr"))}
Available slots:
{string.Join("\n", slots.Select(s => $"{s.StartTime:t} - {s.EndTime:t}"))}
User request: {userInput}
Suggest best booking option.
";
            return await _gemini.AskAsync(prompt);
        }
    }
}