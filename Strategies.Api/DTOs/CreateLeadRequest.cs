using System;
using System.ComponentModel.DataAnnotations;
using Strategies.Api.Data.Enums;

namespace Strategies.Api.DTOs
{
    public class CreateLeadRequest
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 200 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "A valid email address is required.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Immigration status is required.")]
        public ImmigrationStatus ImmigrationStatus { get; set; }

        public bool NeedsAssistance { get; set; }

        [Required(ErrorMessage = "Desired start date is required.")]
        public DateTime DesiredStartDate { get; set; }

        public Guid? VisaCategoryId { get; set; }
    }

    public class UpdateLeadStatusRequest
    {
        [Required(ErrorMessage = "Status is required.")]
        public TriageStatus? Status { get; set; }
    }
}
