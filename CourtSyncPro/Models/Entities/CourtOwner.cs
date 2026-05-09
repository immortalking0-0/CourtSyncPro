using System.ComponentModel.DataAnnotations;

namespace CourtSyncPro.Models.Entities
{
    public class CourtOwner
    {
        [Key] public int OwnerId { get; set; }
        [Required, MaxLength(150)] public string BusinessName { get; set; }
        [Required, EmailAddress] public string Email { get; set; }
        [Required] public string PasswordHash { get; set; }
        [Phone] public string Phone { get; set; }
        public string NationalID { get; set; }
        public bool IsVerified { get; set; } = false;
        public string City { get; set; }
        public DateTime RegisterDate { get; set; } = DateTime.UtcNow;
        public ICollection<Court> Courts { get; set; } = new List<Court>();

    }
}
