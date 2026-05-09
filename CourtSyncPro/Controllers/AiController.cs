// ✅ Make sure your controller has this
using CourtSyncPro.Models.AI.Services;
using Microsoft.AspNetCore.Mvc;

[Route("ai")]                          // ← matches /ai/...
public class AiController : Controller
{
    private readonly GeminiService _gemini;
    private readonly BookingAiService _bookingAi;

    public AiController(GeminiService gemini, BookingAiService bookingAi)
    {
        _gemini = gemini;
        _bookingAi = bookingAi;
    }

    [HttpGet("")]                      // ← matches GET /ai
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost("ask")]                  // ← matches POST /ai/ask
    public async Task<IActionResult> Ask(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return BadRequest("Prompt is required.");

        var response = await _gemini.AskAsync(prompt);
        return Json(new { reply = response });
    }

    [HttpPost("suggest-booking")]      // ← matches POST /ai/suggest-booking
    public async Task<IActionResult> SuggestBooking(string userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput))
            return BadRequest("User input is required.");

        var suggestion = await _bookingAi.SuggestBookingAsync(userInput);
        return Json(new { reply = suggestion });
    }
}