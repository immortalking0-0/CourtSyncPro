using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CourtSyncPro.Models.Entities
{
    public class TimeSlot
    {
        [Key]
        public int SlotId { get; set; }

        [ForeignKey("Court")]
        public int CourtId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public bool IsAvailable { get; set; } = true;

        public bool IsBlocked { get; set; } = false;

        // Navigation properties
        public Court Court { get; set; } = null!;
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>(); // ← add this
    }
}