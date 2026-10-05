using System;
using System.ComponentModel.DataAnnotations;

namespace Strategies.Api.DTOs
{
    public class CaseDto
    {
        public Guid Id { get; set; }
        public Guid ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ConsultantName { get; set; } = string.Empty;
        public string VisaCategoryName { get; set; } = string.Empty;
        public string Stage { get; set; } = string.Empty;
        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class UpdateCaseStageRequest
    {
        [Required(ErrorMessage = "Stage is required.")]
        public Strategies.Api.Data.Enums.CaseStage? Stage { get; set; }
    }

    public class AssignConsultantRequest
    {
        public Guid ConsultantId { get; set; }
    }

    public class CreateCaseRequest
    {
        public Guid ClientId { get; set; }
        public Guid VisaCategoryId { get; set; }
        public Guid? LeadId { get; set; }
    }
}