namespace CourtSyncPro.Models.AI.Services
{
    using System.Net.Http.Json;

    public class GeminiService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public GeminiService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public async Task<string> AskAsync(string prompt)
        {
            var apiKey = _config["Gemini:ApiKey"];
            var model = _config["Gemini:Model"];

            var request = new GeminiRequest
            {
                contents = new List<Content>
        {
            new Content
            {
                parts = new List<Part>
                {
                    new Part { text = prompt }
                }
            }
        }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var response = await _http.PostAsJsonAsync(url, request);

            // ✅ Handle quota exceeded gracefully
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                return "AI is currently busy. Please wait a moment and try again.";
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API Error: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>();

            return result?.candidates?.FirstOrDefault()?.content?.parts?.FirstOrDefault()?.text
                   ?? "No response from AI.";
        }
    }
}
