using System;

namespace Strategies.Api.Data.Models
{
    public class ChecklistTemplate
    {
        public Guid Id { get; set; }
        public Guid VisaCategoryId { get; set; }
        public Guid DocumentTypeId { get; set; }
        public bool IsMandatory { get; set; }
    }
}