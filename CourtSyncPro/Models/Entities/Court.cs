using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CourtSyncPro.Models.Entities
{
    public class Court
    {
        [Key] public int CourtId { get; set; }
        [ForeignKey("CourtOwner")] public int OwnerId { get; set; }
        [Required, MaxLength(150)] public string CourtName { get; set; }
        public SportType SportType { get; set; }
        [Required, MaxLength(100)] public string City { get; set; }
        [Required, MaxLength(300)] public string Address { get; set; }
        [Column(TypeName = "decimal(10,2)")] public decimal PricePerHour { get; set; }
        public float Rating { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // Navigation
        public CourtOwner CourtOwner { get; set; } = null!;
        public ICollection<TimeSlot> TimeSlots { get; set; }
        public ICollection<Booking> Bookings { get; set; }
        public ICollection<Review> Reviews { get; set; }
    }
    public enum SportType { Futsal, Padel, CricketNet, Badminton }

}
