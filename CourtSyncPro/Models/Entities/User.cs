using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.ComponentModel.DataAnnotations;

namespace CourtSyncPro.Models.Entities
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Phone, MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        public MembershipType MembershipType { get; set; } = MembershipType.None;
        public DateTime JoinDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
    public enum MembershipType { None, SixMonth, TwelveMonth }

}
