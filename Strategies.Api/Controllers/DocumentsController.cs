using Microsoft.AspNetCore.Mvc;
using Strategies.Api.Data.Enums;
using Strategies.Api.Data.Models;
using System;
using System.Collections.Generic;

namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        // GET: api/documents/case/{caseId}
        // Unblocks Kundai & Riaad: View uploaded documents for a case
        [HttpGet("case/{caseId}")]
        public IActionResult GetDocumentsForCase(Guid caseId)
        {
            var mockDocs = new List<Document>
            {
                new Document
                {
                    Id = Guid.NewGuid(),
                    CaseId = caseId,
                    FileName = "passport_scan.pdf",
                    Status = DocumentStatus.Approved,
                    UploadedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Document
                {
                    Id = Guid.NewGuid(),
                    CaseId = caseId,
                    FileName = "bank_statement.jpg",
                    Status = DocumentStatus.Pending,
                    UploadedAt = DateTime.UtcNow.AddHours(-2)
                }
            };

            return Ok(mockDocs);
        }

        // POST: api/documents
        // Unblocks Kundai: Kotlin Mobile App Camera Upload
        [HttpPost]
        public IActionResult UploadDocument([FromBody] Document newDoc)
        {
            // Simulate saving to Cloudflare R2 and returning a 201 Created
            return StatusCode(201, new
            {
                Message = "Document uploaded successfully.",
                DocumentId = Guid.NewGuid(),
                Status = DocumentStatus.Pending.ToString()
            });
        }
    }
}