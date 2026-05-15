using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CourtSyncPro.Models.Entities
{
    public class TournamentCreateVM
    {
        [Required]
        public string TournamentName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        public SportType SportType { get; set; }

        [Required(ErrorMessage = "Court is required")]
        public int? CourtId { get; set; }

        [Required]
        public DateTime TournamentDate { get; set; }

        [Required]
        public DateTime RegistrationDeadline { get; set; }

        public decimal RegistrationFee { get; set; }

        public int MaxTeams { get; set; }

        public string PrizeInformation { get; set; } = string.Empty;

        public TournamentStatus Status { get; set; }

        // dropdown list
        public List<SelectListItem> Courts { get; set; } = new();
    }
}