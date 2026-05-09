namespace CourtSyncPro.Models.AI
{
    public class GeminiRequest
    {
        public List<Content> contents { get; set; } = new();
    }

    public class Content
    {
        public List<Part> parts { get; set; } = new();
    }

    public class Part
    {
        public string text { get; set; } = string.Empty;
    }
}
