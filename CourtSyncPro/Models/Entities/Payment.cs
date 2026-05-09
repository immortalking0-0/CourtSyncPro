using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CourtSyncPro.Models.Entities
{
    public class Payment
    {
        [Key] public int PaymentId { get; set; }
        [ForeignKey("Booking")] public int BookingId { get; set; }
        [Column(TypeName = "decimal(10,2)")] public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        [MaxLength(100)] public string TransactionId { get; set; }
        public DateTime PaidAt { get; set; }
        public Booking Booking { get; set; } = null!;
    }
    public enum PaymentMethod { CreditCard, DebitCard, EasyPaisa, JazzCash }
    public enum PaymentStatus { Pending, Completed, Failed, Refunded }

}
