using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CourtSyncPro.Models.Entities
{
    public class TournamentRegistration
    {
        [Key]
        public int RegistrationId { get; set; }

        [ForeignKey("Tournament")]
        public int TournamentId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required, MaxLength(150)]
        public string TeamName { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string ContactNumber { get; set; } = string.Empty;

        public int NumberOfPlayers { get; set; }

        public RegistrationStatus Status { get; set; }
            = RegistrationStatus.Pending;

        [Column(TypeName = "decimal(10,2)")]
        public decimal FeePaid { get; set; }

        public string TransactionId { get; set; } = string.Empty;

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Tournament Tournament { get; set; } = null!;
        public User User { get; set; } = null!;
    }

    public enum RegistrationStatus { Pending, Confirmed, Cancelled }
}