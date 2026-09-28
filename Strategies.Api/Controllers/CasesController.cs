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
        // Static GUIDs so React Router click-throughs work
        private static readonly Guid StaticCaseId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        [HttpGet]
        public IActionResult GetCases([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string search = null)
        {
            var data = new List<object>
            {
                new { Id = StaticCaseId, ClientName = "John Doe", Stage = "UnderReview" }
            };
            return Ok(new PagedResponse<object> { Data = data, TotalRecords = 45, TotalPages = 5, CurrentPage = page });
        }

        [HttpGet("{id}")]
        public IActionResult GetCaseById(Guid id)
        {
            return Ok(new { Id = id, ClientName = "John Doe", Stage = "UnderReview" });
        }

        [HttpGet("export")]
        public IActionResult ExportCases([FromQuery] string format = "csv")
        {
            if (format.ToLower() == "csv")
                return File(System.Text.Encoding.UTF8.GetBytes("Id,ClientName\n1,John Doe\n"), "text/csv", "cases.csv");

            return BadRequest(new ApiErrorResponse { Message = "Format not supported" });
        }
    }
}