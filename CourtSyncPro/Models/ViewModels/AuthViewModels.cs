using System.ComponentModel.DataAnnotations;

namespace CourtSyncPro.Models.ViewModels
{
    // ── Customer Register ──────────────────────────────────────
    public class UserRegisterViewModel
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, Phone, MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required, MinLength(8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // ── Court Owner Register ───────────────────────────────────
    public class OwnerRegisterViewModel
    {
        [Required, MaxLength(150)]
        public string BusinessName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, Phone]
        public string Phone { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string NationalID { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required, MinLength(8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // ── Login (shared for all roles) ───────────────────────────
    public class LoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        // "User", "Owner", "Admin"
        [Required]
        public string Role { get; set; } = "User";

        public bool RememberMe { get; set; }
    }
}