namespace CourtSyncPro.Models.AI
{
    public class GeminiResponse
    {
        public List<Candidate> candidates { get; set; } = new();
    }

    public class Candidate
    {
        public Content content { get; set; } = new();
    }
}
