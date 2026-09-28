using Microsoft.AspNetCore.Mvc;
using Strategies.Api.DTOs;
using System;
using System.Collections.Generic;

namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CasesController : ControllerBase
    {
        private static readonly Guid StaticCaseId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        [HttpGet]
        public IActionResult GetCases([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string search = null)
        {
            var data = new List<object>
            {
                new { Id = StaticCaseId, ClientName = "John Doe", VisaCategoryName = "Critical Skills Work Visa", Stage = "UnderReview", IsArchived = false, CreatedAt = DateTime.UtcNow.AddDays(-30), LastUpdated = DateTime.UtcNow.AddHours(-2) }
            };
            return Ok(new PagedResponse<object> { Data = data, TotalRecords = 45, TotalPages = 5, CurrentPage = page });
        }

        [HttpGet("{id}")]
        public IActionResult GetCaseById(Guid id)
        {
            return Ok(new { Id = id, ClientName = "John Doe", ConsultantName = "Riaad", VisaCategoryName = "Critical Skills Work Visa", Stage = "UnderReview", IsArchived = false, CreatedAt = DateTime.UtcNow.AddDays(-30), LastUpdated = DateTime.UtcNow.AddHours(-2) });
        }

        // Restored for Kundai
        [HttpGet("client/{clientId}")]
        public IActionResult GetClientCases(Guid clientId)
        {
            var data = new List<object>
            {
                new { Id = StaticCaseId, ClientId = clientId, ClientName = "John Doe", Stage = "UnderReview", CreatedAt = DateTime.UtcNow.AddDays(-30) }
            };
            return Ok(new PagedResponse<object> { Data = data, TotalRecords = 1, TotalPages = 1, CurrentPage = 1 });
        }

        [HttpGet("export")]
        public IActionResult ExportCases([FromQuery] string format = "csv")
        {
            if (format.ToLower() == "csv")
                return File(System.Text.Encoding.UTF8.GetBytes("Id,ClientName,Stage\n11111111-1111-1111-1111-111111111111,John Doe,UnderReview\n"), "text/csv", "cases.csv");

            return BadRequest(new ApiErrorResponse { Message = "Format not supported", Errors = new List<string> { "Only CSV is currently supported." } });
        }
    }
}