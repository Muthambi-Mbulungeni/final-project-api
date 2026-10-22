using System;
using System.Collections.Generic;
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
using Strategies.Api.Services;

namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorage _fileStorage;

        public DocumentsController(ApplicationDbContext context, IFileStorage fileStorage)
        {
            _context = context;
            _fileStorage = fileStorage;
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
            else if (User.IsInRole("Consultant"))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Guid.TryParse(userIdStr, out var currentUserId);
                var owns = await _context.Cases.AnyAsync(c => c.Id == caseId && c.ConsultantId == currentUserId);
                if (!owns) return Forbid();
            }

            // The reviewer is joined without a deleted filter: a decision is history, so the name stays.
            var query = from d in _context.Documents
                        join dt in _context.DocumentTypes on d.DocumentTypeId equals dt.Id into dtGroup
                        from docType in dtGroup.DefaultIfEmpty()
                        join rv in _context.Users on d.ReviewedById equals (Guid?)rv.Id into reviewerGroup
                        from reviewer in reviewerGroup.DefaultIfEmpty()
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
                            ReviewedByName = reviewer != null ? reviewer.FullName : null,
                            d.ReviewedAt,
                            d.IsArchived,
                            d.UploadedAt
                        };

            var totalRecords = await query.CountAsync();
            var documents = await query
                .OrderByDescending(d => d.UploadedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = page,
                Data = documents
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
                             join rv in _context.Users on d.ReviewedById equals (Guid?)rv.Id into reviewerGroup
                             from reviewer in reviewerGroup.DefaultIfEmpty()
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
                                 ReviewedByName = reviewer != null ? reviewer.FullName : null,
                                 d.ReviewedAt,
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
            else if (User.IsInRole("Consultant"))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Guid.TryParse(userIdStr, out var currentUserId);
                var owns = await _context.Cases.AnyAsync(c => c.Id == doc.CaseId && c.ConsultantId == currentUserId);
                if (!owns) return Forbid();
            }

            return Ok(doc);
        }

        /// <summary>
        /// GET /api/documents/{id}/file
        /// Downloads the physical file for a given document. Enforces ownership checks.
        /// </summary>
        [HttpGet("{id}/file")]
        public async Task<IActionResult> DownloadDocument(Guid id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
                return NotFound(new ApiErrorResponse { Message = "Document not found." });

            if (User.IsInRole("Client"))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdStr, out var currentUserId))
                    return Unauthorized();

                var owns = await _context.Cases
                    .AnyAsync(c => c.Id == document.CaseId && c.ClientId == currentUserId);
                if (!owns)
                    return Forbid();
            }
            else if (User.IsInRole("Consultant"))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Guid.TryParse(userIdStr, out var currentUserId);
                var owns = await _context.Cases.AnyAsync(c => c.Id == document.CaseId && c.ConsultantId == currentUserId);
                if (!owns) return Forbid();
            }

            // Resolved inside the storage root only; any refusal looks the same as a missing file.
            var filePath = _fileStorage.ResolveExisting(document.FilePath);
            if (filePath == null)
                return NotFound(new ApiErrorResponse { Message = "Physical file not found." });

            var ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            // Streamed from disk; FileStreamResult disposes the stream after the response.
            var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read,
                System.IO.FileShare.Read, bufferSize: 64 * 1024, useAsync: true);
            return File(stream, contentType, document.FileName);
        }

        /// <summary>
        /// POST /api/documents/upload
        /// Uploads a physical file to local storage. Enforces 5MB size limit and PDF/JPG/PNG types.
        /// </summary>
        [HttpPost("upload")]
        [Authorize(Roles = "Admin,Consultant,Client")]
        public async Task<IActionResult> UploadDocument(Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new ApiErrorResponse { Message = "No file uploaded." });

            const long maxFileSize = 5 * 1024 * 1024; // 5MB limit
            if (file.Length > maxFileSize)
                return BadRequest(new ApiErrorResponse { Message = "File size exceeds the 5MB limit." });

            var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!LocalFileStorage.AllowedExtensions.Contains(extension))
                return BadRequest(new ApiErrorResponse { Message = "Invalid file type. Only PDF, JPG, and PNG are allowed." });

            string storedName;
            await using (var upload = file.OpenReadStream())
            {
                storedName = await _fileStorage.SaveAsync(upload, extension);
            }

            // FilePath is the bare stored name; the web passes it straight back to POST /api/documents.
            return Ok(new
            {
                Message = "File uploaded successfully.",
                FileName = file.FileName,
                FilePath = storedName
            });
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
            else if (User.IsInRole("Consultant"))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Guid.TryParse(userIdStr, out var currentUserId);
                var owns = await _context.Cases.AnyAsync(c => c.Id == dto.CaseId && c.ConsultantId == currentUserId);
                if (!owns) return Forbid();
            }

            // Only a name the upload endpoint generated, for a file that exists. Checked after ownership
            // so a caller without access to the case cannot probe which file names exist.
            var storedName = dto.FilePath.Trim();
            if (!_fileStorage.IsValidStoredName(storedName) || _fileStorage.ResolveExisting(storedName) == null)
                return BadRequest(new ApiErrorResponse
                {
                    Message = "Validation failed.",
                    Errors = new List<string> { "Invalid file reference." }
                });

            var caseExists = await _context.Cases.AnyAsync(c => c.Id == dto.CaseId);
            if (!caseExists)
                return NotFound(new ApiErrorResponse { Message = "Case not found." });

            var docTypeExists = await _context.DocumentTypes.AnyAsync(dt => dt.Id == dto.DocumentTypeId);
            if (!docTypeExists)
                return NotFound(new ApiErrorResponse { Message = "Document type not found." });

            var document = new Document
            {
                Id = Guid.NewGuid(),
                CaseId = dto.CaseId,
                DocumentTypeId = dto.DocumentTypeId,
                FileName = dto.FileName.Trim(),
                FilePath = storedName,
                Status = DocumentStatus.Pending,
                UploadedAt = DateTime.UtcNow,
                IsArchived = false
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
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new ApiErrorResponse { Message = "Validation failed.", Errors = errors });
            }

            if (!Enum.TryParse<DocumentStatus>(dto.Status, true, out var parsedStatus))
            {
                return BadRequest(new ApiErrorResponse { Message = "Invalid status." });
            }

            if (parsedStatus == DocumentStatus.Rejected && string.IsNullOrWhiteSpace(dto.ReviewComment))
            {
                return BadRequest(new ApiErrorResponse { Message = "A review comment is required when rejecting a document." });
            }

            var document = await _context.Documents.FindAsync(id);
            if (document == null)
                return NotFound(new ApiErrorResponse { Message = "Document not found." });

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out var reviewerId);

            if (User.IsInRole("Consultant"))
            {
                var caseOwns = await _context.Cases.AnyAsync(c => c.Id == document.CaseId && c.ConsultantId == reviewerId);
                if (!caseOwns) return Forbid();
            }

            document.Status = parsedStatus;
            document.ReviewComment = dto.ReviewComment?.Trim();
            // Who decided and when, shown to the client and staff. Back to Pending means no decision.
            var decided = parsedStatus != DocumentStatus.Pending;
            document.ReviewedById = decided ? reviewerId : null;
            document.ReviewedAt = decided ? DateTime.UtcNow : null;

            _context.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = reviewerId,
                Action = $"{parsedStatus} document",
                EntityType = "Document",
                EntityId = document.Id,
                CreatedAt = DateTime.UtcNow
            });

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

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out var currentUserId);

            if (User.IsInRole("Consultant"))
            {
                var caseOwns = await _context.Cases.AnyAsync(c => c.Id == document.CaseId && c.ConsultantId == currentUserId);
                if (!caseOwns) return Forbid();
            }

            document.IsArchived = true;

            _context.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = currentUserId,
                Action = "ArchiveDocument",
                EntityType = "Document",
                EntityId = document.Id,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Document archived." });
        }
    }
}