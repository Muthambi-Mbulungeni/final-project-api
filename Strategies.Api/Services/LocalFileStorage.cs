using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Strategies.Api.Services
{
    public class LocalFileStorage : IFileStorage
    {
        public static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };

        private readonly string _rootWithSeparator;

        // rootPath is resolved once at startup (Program.cs) from Storage:UploadsRoot.
        public LocalFileStorage(string rootPath)
        {
            RootPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));
            _rootWithSeparator = RootPath + Path.DirectorySeparatorChar;
            Directory.CreateDirectory(RootPath);
        }

        public string RootPath { get; }

        public bool IsValidStoredName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            // Parsed from the whole string, not Path helpers, which would silently drop a folder prefix.
            var dot = name.LastIndexOf('.');
            if (dot <= 0) return false;

            if (!AllowedExtensions.Contains(name[dot..], StringComparer.Ordinal)) return false;

            return Guid.TryParseExact(name[..dot], "D", out _);
        }

        public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default)
        {
            var storedName = $"{Guid.NewGuid()}{extension}";
            var physicalPath = Path.Combine(RootPath, storedName);

            // CreateNew: never overwrite an existing file.
            await using var stream = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write);
            await content.CopyToAsync(stream, cancellationToken);
            return storedName;
        }

        public string? ResolveExisting(string storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath)) return null;

            var name = Path.GetFileName(storedPath.Trim());
            if (string.IsNullOrEmpty(name)) return null;

            // ".." or a rooted remainder could still escape; the canonical path must sit strictly inside the root.
            var fullPath = Path.GetFullPath(Path.Combine(RootPath, name));
            if (!fullPath.StartsWith(_rootWithSeparator, StringComparison.Ordinal)) return null;

            return File.Exists(fullPath) ? fullPath : null;
        }
    }
}
