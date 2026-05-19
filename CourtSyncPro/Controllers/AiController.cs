using CourtSyncPro.Models.AI.Services;
using Microsoft.AspNetCore.Mvc;

[Route("ai")]
public class AiController : Controller
{
    // ✅ removed _gemini — controller no longer calls GeminiService directly
    private readonly BookingAiService _bookingAi;

    public AiController(BookingAiService bookingAi)
    {
        _bookingAi = bookingAi;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        var role = HttpContext.Session.GetString("UserRole");
        if (role != "User")
            return RedirectToAction("Index", "Home");

        return View();
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return BadRequest("Prompt is required.");

        try
        {
            var response = await _bookingAi.SuggestBookingAsync(prompt);
            return Json(new { reply = response });
        }
        catch (Exception ex)
        {
            return Json(new { reply = $"Error: {ex.Message}" });
        }
    }

    [HttpPost("suggest-booking")]
    public async Task<IActionResult> SuggestBooking(string userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput))
            return BadRequest("User input is required.");

        try
        {
            var suggestion = await _bookingAi.SuggestBookingAsync(userInput);
            return Json(new { reply = suggestion });
        }
        catch (Exception ex)
        {
            return Json(new { reply = $"Error: {ex.Message}" });
        }
    }
}