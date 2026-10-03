using System.Collections.Generic;
using System.Linq;
namespace Strategies.Api.DTOs
{
    public class PagedResponse<T>
    {
        public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
    }
}