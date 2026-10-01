using System;
using System.ComponentModel.DataAnnotations;

namespace Strategies.Api.DTOs
{
    public class UpdateDocumentStatusRequest
    {
        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; } = string.Empty;

        public string? ReviewComment { get; set; }
    }
}
