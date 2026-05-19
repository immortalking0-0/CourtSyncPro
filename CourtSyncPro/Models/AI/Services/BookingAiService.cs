using CourtSyncPro.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CourtSyncPro.Models.AI.Services
{
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
            var courts = await _db.Courts
                .Where(c => c.IsActive)
                .ToListAsync();

            var slots = await _db.TimeSlots
                .Include(s => s.Court)
                .Where(s => s.IsAvailable && !s.IsBlocked && s.StartTime >= DateTime.Now)
                .OrderBy(s => s.StartTime)
                .Take(20)
                .ToListAsync();

            var courtInfo = string.Join("\n", courts.Select(c =>
                $"- {c.CourtName} | Sport: {c.SportType} | City: {c.City} | Price: Rs.{c.PricePerHour}/hr"));

            // ✅ slotId is now included so AI can return the real slot ID
            var slotInfo = string.Join("\n", slots.Select(s =>
                $"- SlotId: {s.SlotId} | Court: {s.Court.CourtName} | Sport: {s.Court.SportType} | Date: {s.StartTime:ddd dd MMM yyyy} | Time: {s.StartTime:hh:mm tt} - {s.EndTime:hh:mm tt} | Price: Rs.{s.Court.PricePerHour}/hr"));

            var prompt = $@"
You are CourtSync Pro's booking assistant for indoor sports courts in Pakistan.

=== AVAILABLE COURTS ===
{courtInfo}

=== AVAILABLE TIME SLOTS ===
{slotInfo}

=== USER REQUEST ===
{userInput}

If the user wants to book a court, find the best matching slot from the list above and respond ONLY with this exact JSON:
{{
  ""intent"": ""book"",
  ""courtName"": ""exact court name from the list"",
  ""slotId"": 0,
  ""message"": ""I found [court name] available at [time] on [date]. Shall I confirm your booking?""
}}

Replace slotId with the actual SlotId number from the list above.

If the user is just asking a question, respond ONLY with this exact JSON:
{{
  ""intent"": ""info"",
  ""message"": ""your answer here""
}}

Always respond with valid JSON only. No extra text, no markdown, no code blocks.
";

            var aiResponse = await _gemini.AskAsync(prompt);

            // ✅ Parse AI response and handle booking intent
            try
            {
                // clean response in case AI adds markdown backticks
                var cleaned = aiResponse
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                using var doc = JsonDocument.Parse(cleaned);
                var intent = doc.RootElement.GetProperty("intent").GetString();
                var message = doc.RootElement.GetProperty("message").GetString() ?? aiResponse;

                if (intent == "book")
                {
                    var slotId = doc.RootElement.GetProperty("slotId").GetInt32();

                    // return special format frontend will detect
                    return $"BOOKING_INTENT:{slotId}:{message}";
                }

                // just an info response
                return message;
            }
            catch
            {
                // AI didn't return valid JSON — return raw response as plain text
                return aiResponse;
            }
        }
    }
}