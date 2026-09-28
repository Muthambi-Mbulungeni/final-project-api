using System;

namespace Strategies.Api.DTOs
{
    public class CaseDto
    {
        public Guid Id { get; set; }
        public Guid ClientId { get; set; }
        public string ClientName { get; set; }
        public string ConsultantName { get; set; }
        public string VisaCategoryName { get; set; }
        public string Stage { get; set; }
        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}