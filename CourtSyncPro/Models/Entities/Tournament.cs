using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CourtSyncPro.Models.Entities
{
    public class Tournament
    {
        [Key]
        public int TournamentId { get; set; }

        [Required, MaxLength(150)]
        public string TournamentName { get; set; } = string.Empty;

        public SportType SportType { get; set; }

        [Required]
        [ForeignKey("Court")]
        public int? CourtId { get; set; }

        [Required]
        public DateTime TournamentDate { get; set; }

        [Required]
        public DateTime RegistrationDeadline { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal RegistrationFee { get; set; }

        public int MaxTeams { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(500)]
        public string PrizeInformation { get; set; } = string.Empty;

        public TournamentStatus Status { get; set; } = TournamentStatus.Upcoming;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Who created it (owner or admin)
        public int? CreatedByOwnerId { get; set; }
        public bool CreatedByAdmin { get; set; } = false;

        // Navigation
        public Court Court { get; set; } = null!;
        public ICollection<TournamentRegistration> Registrations { get; set; }
            = new List<TournamentRegistration>();
    }

    public enum TournamentStatus { Upcoming, Ongoing, Completed, Cancelled }
}