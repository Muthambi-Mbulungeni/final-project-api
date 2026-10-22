using System;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Strategies.Api.Controllers;
using Strategies.Api.Data;
using Strategies.Api.Data.Models;
using Strategies.Api.DTOs;
using Strategies.Api.Services;
using Xunit;

namespace Strategies.Api.Tests
{
    public class DocumentsControllerTests : IDisposable
    {
        private readonly string _parent;
        private readonly LocalFileStorage _storage;
        private readonly ApplicationDbContext _context;
        private readonly Guid _caseId = Guid.NewGuid();
        private readonly Guid _documentTypeId = Guid.NewGuid();

        public DocumentsControllerTests()
        {
            _parent = Path.Combine(Path.GetTempPath(), "documents-tests-" + Guid.NewGuid());
            Directory.CreateDirectory(_parent);
            File.WriteAllText(Path.Combine(_parent, "appsettings.json"), "{\"Jwt\":{\"Key\":\"secret\"}}");
            _storage = new LocalFileStorage(Path.Combine(_parent, "uploads"));

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _context.Cases.Add(new Case { Id = _caseId, ClientId = Guid.NewGuid(), VisaCategoryId = Guid.NewGuid() });
            _context.DocumentTypes.Add(new DocumentType { Id = _documentTypeId, Name = "Passport" });
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
            Directory.Delete(_parent, recursive: true);
        }

        // Signed in as Admin, which skips the client and consultant ownership checks.
        private DocumentsController CreateController() => new(_context, _storage)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, "Admin")
                    }, "Test"))
                }
            }
        };

        private CreateDocumentRequest Request(string filePath) => new()
        {
            CaseId = _caseId,
            DocumentTypeId = _documentTypeId,
            FileName = "passport.pdf",
            FilePath = filePath
        };

        private static void AssertInvalidFileReference(IActionResult result)
        {
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var error = Assert.IsType<ApiErrorResponse>(badRequest.Value);
            Assert.Equal("Validation failed.", error.Message);
            Assert.Equal(new[] { "Invalid file reference." }, error.Errors);
        }

        [Theory]
        [InlineData("passport.pdf")]
        [InlineData("/uploads/3f2b8c1e-4d5a-4b6c-9e7f-0a1b2c3d4e5f.pdf")]
        public async Task CreateDocument_NonGuidName_Returns400(string filePath)
        {
            var result = await CreateController().CreateDocument(Request(filePath));

            AssertInvalidFileReference(result);
            Assert.Empty(_context.Documents);
        }

        [Fact]
        public async Task CreateDocument_ValidGuidNameForMissingFile_Returns400()
        {
            var result = await CreateController().CreateDocument(Request($"{Guid.NewGuid()}.pdf"));

            AssertInvalidFileReference(result);
            Assert.Empty(_context.Documents);
        }

        [Theory]
        [InlineData("../appsettings.json")]
        [InlineData("..\\appsettings.json")]
        [InlineData("//etc/passwd")]
        public async Task CreateDocument_TraversalString_Returns400(string filePath)
        {
            var result = await CreateController().CreateDocument(Request(filePath));

            AssertInvalidFileReference(result);
            Assert.Empty(_context.Documents);
        }

        [Fact]
        public async Task CreateDocument_UploadedFile_Returns201AndStoresBareName()
        {
            var name = await _storage.SaveAsync(new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.4")), ".pdf");

            var result = await CreateController().CreateDocument(Request(name));

            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(name, Assert.Single(_context.Documents).FilePath);
        }

        [Fact]
        public async Task DownloadDocument_FileInRoot_StreamsIt()
        {
            var bytes = Encoding.ASCII.GetBytes("%PDF-1.4 download test");
            var name = await _storage.SaveAsync(new MemoryStream(bytes), ".pdf");
            var document = new Document { Id = Guid.NewGuid(), CaseId = _caseId, DocumentTypeId = _documentTypeId, FileName = "passport.pdf", FilePath = name };
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            var result = await CreateController().DownloadDocument(document.Id);

            var file = Assert.IsType<FileStreamResult>(result);
            Assert.Equal("application/pdf", file.ContentType);
            Assert.Equal("passport.pdf", file.FileDownloadName);
            await using var stream = file.FileStream;
            using var copy = new MemoryStream();
            await stream.CopyToAsync(copy);
            Assert.Equal(bytes, copy.ToArray());
        }

        [Theory]
        [InlineData("../appsettings.json")]
        [InlineData("..\\appsettings.json")]
        [InlineData("//etc/passwd")]
        public async Task DownloadDocument_StoredTraversalValue_Returns404(string filePath)
        {
            // A legacy row written before the fix, pointing at a real file outside the root.
            var document = new Document { Id = Guid.NewGuid(), CaseId = _caseId, DocumentTypeId = _documentTypeId, FileName = "x.pdf", FilePath = filePath };
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            var result = await CreateController().DownloadDocument(document.Id);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Physical file not found.", Assert.IsType<ApiErrorResponse>(notFound.Value).Message);
        }
    }
}
