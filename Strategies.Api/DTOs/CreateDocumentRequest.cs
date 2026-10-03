using System;
using System.ComponentModel.DataAnnotations;

namespace Strategies.Api.DTOs
{
    public class CreateDocumentRequest
    {
        [Required(ErrorMessage = "CaseId is required.")]
        public Guid CaseId { get; set; }

        [Required(ErrorMessage = "DocumentTypeId is required.")]
        public Guid DocumentTypeId { get; set; }

        [Required(ErrorMessage = "File name is required.")]
        [StringLength(500, MinimumLength = 1)]
        public string FileName { get; set; } = string.Empty;

        [Required(ErrorMessage = "File path is required.")]
        [StringLength(2000, MinimumLength = 1)]
        public string FilePath { get; set; } = string.Empty;
    }
}
