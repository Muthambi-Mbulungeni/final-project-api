using System;
using Strategies.Api.Data.Enums;

namespace Strategies.Api.Data.Models
{
    public class Document
    {
        public Guid Id { get; set; }
        public Guid CaseId { get; set; }
        public Guid DocumentTypeId { get; set; }
        public Guid? ReviewedById { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DocumentStatus Status { get; set; }
        public string? ReviewComment { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public bool IsArchived { get; set; } = false;
    }
}