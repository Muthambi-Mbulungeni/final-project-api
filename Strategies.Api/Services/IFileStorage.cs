using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Strategies.Api.Services
{
    /// <summary>
    /// Document file storage. Callers only ever handle the stored file name; the folder stays inside the service.
    /// </summary>
    public interface IFileStorage
    {
        /// <summary>Full path of the storage root, without a trailing separator.</summary>
        string RootPath { get; }

        /// <summary>True for a name the upload endpoint could have generated: a GUID plus .pdf, .jpg, .jpeg or .png.</summary>
        bool IsValidStoredName(string name);

        /// <summary>Saves the content under a new "{Guid}{extension}" name and returns that name.</summary>
        Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default);

        /// <summary>
        /// Full path of an existing file inside the root for a stored FilePath value, or null.
        /// Only the last path segment is used, so legacy values like "local-test/x.pdf" still resolve
        /// and traversal values can never leave the root.
        /// </summary>
        string? ResolveExisting(string storedPath);
    }
}
