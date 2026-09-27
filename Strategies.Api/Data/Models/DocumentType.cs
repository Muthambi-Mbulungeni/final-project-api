using System;

namespace Strategies.Api.Data.Models
{
    public class DocumentType
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}