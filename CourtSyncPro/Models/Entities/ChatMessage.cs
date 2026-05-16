using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CourtSyncPro.Models.Entities
{
    public class ChatMessage
    {
        [Key]
        public int MessageId { get; set; }

        [ForeignKey("FromUser")]
        public int FromUserId { get; set; }

        [ForeignKey("ToUser")]
        public int ToUserId { get; set; }

        [Required, MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        // Navigation
        public User FromUser { get; set; } = null!;
        public User ToUser { get; set; } = null!;
    }
}