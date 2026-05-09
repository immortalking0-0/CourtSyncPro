using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CourtSyncPro.Models.Entities
{
    public class Review
    {
        [Key] public int ReviewId { get; set; }
        [ForeignKey("User")] public int UserId { get; set; }
        [ForeignKey("Court")] public int CourtId { get; set; }
        [Range(1, 5)] public int Rating { get; set; }
        [MaxLength(1000)] public string Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public User User { get; set; } = null!;
        public Court Court { get; set; } = null!;
    }

}
