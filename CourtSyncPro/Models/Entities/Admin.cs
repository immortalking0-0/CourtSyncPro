using System.ComponentModel.DataAnnotations;

namespace CourtSyncPro.Models.Entities
{
    public class Admin
    {
        [Key] public int AdminId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;
    }
}