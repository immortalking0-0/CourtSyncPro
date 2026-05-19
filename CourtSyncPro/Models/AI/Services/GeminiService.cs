using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace CourtSyncPro.Models.AI.Services
{
    public class GeminiService   // keeping same class name so nothing else breaks
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
            var apiKey = _config["Groq:ApiKey"];
            var model = _config["Groq:Model"] ?? "llama-3.3-70b-versatile";

            if (string.IsNullOrWhiteSpace(apiKey))
                return "Error: Groq API key is missing from appsettings.json.";

            var url = "https://api.groq.com/openai/v1/chat/completions";

            var body = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 1024
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            HttpResponseMessage response;
            try
            {
                response = await _http.PostAsync(url, content);
            }
            catch (Exception ex)
            {
                return $"Network Error: {ex.Message}";
            }

            var rawJson = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                return "AI is busy. Please wait a moment and try again.";

            if (!response.IsSuccessStatusCode)
                return $"Groq Error [{response.StatusCode}]: {rawJson}";

            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                var text = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return text ?? "AI returned an empty response.";
            }
            catch
            {
                return $"Error parsing response: {rawJson}";
            }
        }
    }
}