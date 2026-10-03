using System;

namespace Strategies.Api.Data.Models
{
    public class VisaRecord
    {
        public Guid Id { get; set; }
        public Guid CaseId { get; set; }
        public string VisaType { get; set; } = string.Empty;
        public DateTime IssuedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool RenewalFlagged { get; set; } = false;
    }
}