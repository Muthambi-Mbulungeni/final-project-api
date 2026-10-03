using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Strategies.Api.Data;
using Strategies.Api.Data.Enums;
using Strategies.Api.Data.Models;
using Strategies.Api.DTOs;

namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DocumentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET /api/documents?caseId={id}
        /// Returns documents for a given case. Client users can only query their own cases.
        /// Includes document type name for display.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDocuments(
            [FromQuery] Guid? caseId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (caseId == null)
                return BadRequest(new ApiErrorResponse { Message = "caseId query parameter is required." });

            // Enforce client-scoped access
            if (User.IsInRole("Client"))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdStr, out var currentUserId))
                    return Unauthorized();

                var owns = await _context.Cases
                    .AnyAsync(c => c.Id == caseId && c.ClientId == currentUserId);
                if (!owns)
                    return Forbid();
            }

            var query = from d in _context.Documents
                        join dt in _context.DocumentTypes on d.DocumentTypeId equals dt.Id into dtGroup
                        from docType in dtGroup.DefaultIfEmpty()
                        where d.CaseId == caseId
                        select new
                        {
                            d.Id,
                            d.CaseId,
                            d.DocumentTypeId,
                            DocumentTypeName = docType != null ? docType.Name : "Unknown",
                            d.FileName,
                            d.Status,
                            d.ReviewComment,
                            d.IsArchived,
                            d.UploadedAt
                        };

            var totalRecords = await query.CountAsync();
            var documents = await query
                .OrderByDescending(d => d.UploadedAt)
                .Skip((page - 1) * page)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                TotalRecords = totalRecords,
                TotalPages   = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage  = page,
                Data         = documents
            });
        }

        /// <summary>
        /// GET /api/documents/{id}
        /// Single document with document type name. Client users can only fetch their own case documents.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocument(Guid id)
        {
            var doc = await (from d in _context.Documents
                             join dt in _context.DocumentTypes on d.DocumentTypeId equals dt.Id into dtGroup
                             from docType in dtGroup.DefaultIfEmpty()
                             where d.Id == id
                             select new
                             {
                                 d.Id,
                                 d.CaseId,
                                 d.DocumentTypeId,
                                 DocumentTypeName = docType != null ? docType.Name : "Unknown",
                                 d.FileName,
                                 d.Status,
                                 d.ReviewComment,
                                 d.IsArchived,
                                 d.UploadedAt
                             }).FirstOrDefaultAsync();

            if (doc == null)
                return NotFound(new ApiErrorResponse { Message = "Document not found." });

            if (User.IsInRole("Client"))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdStr, out var currentUserId))
                    return Unauthorized();

                var owns = await _context.Cases
                    .AnyAsync(c => c.Id == doc.CaseId && c.ClientId == currentUserId);
                if (!owns)
                    return Forbid();
            }

            return Ok(doc);
        }

        /// <summary>
        /// POST /api/documents
        /// Register a new document record for a case.
        /// File upload is handled separately by the storage layer — this records the metadata.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Consultant,Client")]
        public async Task<IActionResult> CreateDocument([FromBody] CreateDocumentRequest dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new ApiErrorResponse { Message = "Validation failed.", Errors = errors });
            }

            // Client users can only add documents to their own cases
            if (User.IsInRole("Client"))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdStr, out var currentUserId))
                    return Unauthorized();

                var owns = await _context.Cases
                    .AnyAsync(c => c.Id == dto.CaseId && c.ClientId == currentUserId);
                if (!owns)
                    return Forbid();
            }

            var caseExists = await _context.Cases.AnyAsync(c => c.Id == dto.CaseId);
            if (!caseExists)
                return NotFound(new ApiErrorResponse { Message = "Case not found." });

            var docTypeExists = await _context.DocumentTypes.AnyAsync(dt => dt.Id == dto.DocumentTypeId);
            if (!docTypeExists)
                return NotFound(new ApiErrorResponse { Message = "Document type not found." });

            var document = new Document
            {
                Id             = Guid.NewGuid(),
                CaseId         = dto.CaseId,
                DocumentTypeId = dto.DocumentTypeId,
                FileName       = dto.FileName.Trim(),
                FilePath       = dto.FilePath.Trim(),
                Status         = DocumentStatus.Pending,
                UploadedAt     = DateTime.UtcNow,
                IsArchived     = false
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, new
            {
                document.Id,
                document.CaseId,
                document.DocumentTypeId,
                document.FileName,
                document.Status,
                document.UploadedAt
            });
        }

        /// <summary>
        /// PATCH /api/documents/{id}/status
        /// Allows Admin or Consultant to review and set document status.
        /// Clients cannot change their own document status.
        /// </summary>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin,Consultant")]
        public async Task<IActionResult> UpdateDocumentStatus(Guid id, [FromBody] UpdateDocumentStatusRequest dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!Enum.TryParse<DocumentStatus>(dto.Status, ignoreCase: true, out var parsedStatus))
            {
                var validValues = string.Join(", ", Enum.GetNames<DocumentStatus>());
                return BadRequest(new ApiErrorResponse
                {
                    Message = $"Invalid status. Valid values: {validValues}."
                });
            }

            var document = await _context.Documents.FindAsync(id);
            if (document == null)
                return NotFound(new ApiErrorResponse { Message = "Document not found." });

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out var reviewerId);

            document.Status        = parsedStatus;
            document.ReviewComment = dto.ReviewComment?.Trim();
            document.ReviewedById  = reviewerId;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Document status updated.", document.Id, Status = document.Status.ToString() });
        }

        /// <summary>
        /// DELETE /api/documents/{id}
        /// Archives a document (soft delete). Permanent deletion not permitted.
        /// Admin and Consultant only.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Consultant")]
        public async Task<IActionResult> ArchiveDocument(Guid id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
                return NotFound(new ApiErrorResponse { Message = "Document not found." });

            if (document.IsArchived)
                return Conflict(new ApiErrorResponse { Message = "Document is already archived." });

            document.IsArchived = true;
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Document archived." });
        }
    }
}
