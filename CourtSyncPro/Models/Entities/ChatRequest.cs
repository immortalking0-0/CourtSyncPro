using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CourtSyncPro.Models.Entities
{
    public class ChatRequest
    {
        [Key]
        public int RequestId { get; set; }

        [ForeignKey("FromUser")]
        public int FromUserId { get; set; }

        [ForeignKey("ToUser")]
        public int ToUserId { get; set; }

        public ChatRequestStatus Status { get; set; }
            = ChatRequestStatus.Pending;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public DateTime? RespondedAt { get; set; }

        // Navigation
        public User FromUser { get; set; } = null!;
        public User ToUser { get; set; } = null!;
    }

    public enum ChatRequestStatus { Pending, Accepted, Declined }
}