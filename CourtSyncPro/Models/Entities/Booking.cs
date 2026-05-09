using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CourtSyncPro.Models.Entities
{
    public class Booking
    {
        [Key] public int BookingId { get; set; }
        [ForeignKey("User")] public int UserId { get; set; }
        [ForeignKey("Court")] public int CourtId { get; set; }
        [ForeignKey("TimeSlot")] public int SlotId { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.Pending;
        [Column(TypeName = "decimal(10,2)")] public decimal TotalAmount { get; set; }
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;
        [MaxLength(2048)] public string QRCode { get; set; } = string.Empty;
        [MaxLength(50)] public string? PromoCode { get; set; }
        // Navigation
        public User User { get; set; } = null!;
        public Court Court { get; set; } = null!;
        public TimeSlot TimeSlot { get; set; } = null!;
        public Payment? Payment { get; set; }
    }
    public enum BookingStatus { Pending, Confirmed, Cancelled, Completed }

}
