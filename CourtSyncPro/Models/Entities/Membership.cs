using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CourtSyncPro.Models.Entities
{
    public class Membership
    {
        [Key] public int MembershipId { get; set; }
        [ForeignKey("User")] public int UserId { get; set; }
        public MembershipType PlanType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        [Column(TypeName = "decimal(5,2)")] public decimal DiscountRate { get; set; }
        public bool IsActive { get; set; } = true;
        public User User { get; set; } = null!;
    }

}
