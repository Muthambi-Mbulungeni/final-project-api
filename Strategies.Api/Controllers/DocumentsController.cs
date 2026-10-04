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
            else if (User.IsInRole("Consultant"))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Guid.TryParse(userIdStr, out var currentUserId);
                var owns = await _context.Cases.AnyAsync(c => c.Id == caseId && c.ConsultantId == currentUserId);
                if (!owns) return Forbid();
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

            var relativePath = document.FilePath.StartsWith("/") ? document.FilePath.Substring(1) : document.FilePath;
            var filePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", relativePath);
            if (!System.IO.File.Exists(filePath))
                return NotFound(new ApiErrorResponse { Message = "Physical file not found." });

            var memory = new System.IO.MemoryStream();
            using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            
            var ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            return File(memory, contentType, document.FileName);
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

            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                return BadRequest(new ApiErrorResponse { Message = "Invalid file type. Only PDF, JPG, and PNG are allowed." });

            var uploadsFolder = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!System.IO.Directory.Exists(uploadsFolder))
                System.IO.Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var physicalPath = System.IO.Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new System.IO.FileStream(physicalPath, System.IO.FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new
            {
                Message = "File uploaded successfully.",
                FileName = file.FileName,
                FilePath = $"/uploads/{uniqueFileName}"
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
                FilePath = dto.FilePath.Trim(),
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
            document.ReviewedById = reviewerId;

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