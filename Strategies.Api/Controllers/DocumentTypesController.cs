using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Strategies.Api.Data;

namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentTypesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DocumentTypesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET /api/DocumentTypes (routes ignore case, so /api/documenttypes also works)
        /// Returns every document type, sorted by name.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDocumentTypes()
        {
            var docTypes = await _context.DocumentTypes
                .OrderBy(dt => dt.Name)
                .Select(dt => new { dt.Id, dt.Name, dt.Description })
                .ToListAsync();

            return Ok(docTypes);
        }
    }
}
