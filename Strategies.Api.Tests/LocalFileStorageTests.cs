using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Strategies.Api.Services;
using Xunit;

namespace Strategies.Api.Tests
{
    /// <summary>
    /// Each test gets a temp folder holding the storage root ("uploads") and, beside it, a real
    /// secret.txt and appsettings.json, so a traversal that escaped the root would find a real file.
    /// </summary>
    public class LocalFileStorageTests : IDisposable
    {
        private readonly string _parent;
        private readonly LocalFileStorage _storage;

        public LocalFileStorageTests()
        {
            _parent = Path.Combine(Path.GetTempPath(), "storage-tests-" + Guid.NewGuid());
            Directory.CreateDirectory(_parent);
            File.WriteAllText(Path.Combine(_parent, "secret.txt"), "secret");
            File.WriteAllText(Path.Combine(_parent, "appsettings.json"), "{\"Jwt\":{\"Key\":\"secret\"}}");
            _storage = new LocalFileStorage(Path.Combine(_parent, "uploads"));
        }

        public void Dispose() => Directory.Delete(_parent, recursive: true);

        public static TheoryData<string> TraversalValues()
        {
            return new TheoryData<string>
            {
                "../appsettings.json",
                "..\\appsettings.json",
                "../secret.txt",
                "..\\secret.txt",
                "//etc/passwd",
                "/etc/passwd",
                "C:\\Windows\\win.ini",
                "C:/Windows/win.ini",
                "..",
                ".",
                "../",
                "uploads/../../secret.txt"
            };
        }

        [Theory]
        [MemberData(nameof(TraversalValues))]
        public void ResolveExisting_TraversalValues_NeverLeaveRoot(string storedPath)
        {
            var resolved = _storage.ResolveExisting(storedPath);

            // None of these names exists inside the root, so nothing resolves at all.
            Assert.Null(resolved);
        }

        [Fact]
        public void ResolveExisting_AbsolutePathToRealFileOutsideRoot_ReturnsNull()
        {
            var outside = Path.Combine(_parent, "secret.txt");
            Assert.True(File.Exists(outside));

            Assert.Null(_storage.ResolveExisting(outside));
        }

        [Theory]
        [InlineData("../appsettings.json")]
        [InlineData("..\\appsettings.json")]
        [InlineData("//etc/appsettings.json")]
        [InlineData("C:\\Windows\\appsettings.json")]
        public void ResolveExisting_TraversalToNameThatExistsInRoot_ResolvesToTheCopyInsideRoot(string storedPath)
        {
            // A file with the same name inside the root: the traversal collapses to it, never to the one outside.
            var inside = Path.Combine(_storage.RootPath, "appsettings.json");
            File.WriteAllText(inside, "inside");

            var resolved = _storage.ResolveExisting(storedPath);

            // On Linux a backslash is a normal character, so "..\appsettings.json" is a (missing) file name there.
            if (resolved == null)
            {
                Assert.False(OperatingSystem.IsWindows() && storedPath.Contains('\\'));
                return;
            }
            Assert.Equal(inside, resolved);
            Assert.StartsWith(_storage.RootPath + Path.DirectorySeparatorChar, resolved);
        }

        [Fact]
        public void ResolveExisting_RealFileInRoot_Resolves()
        {
            var name = $"{Guid.NewGuid()}.pdf";
            File.WriteAllText(Path.Combine(_storage.RootPath, name), "%PDF-1.4");

            Assert.Equal(Path.Combine(_storage.RootPath, name), _storage.ResolveExisting(name));
        }

        [Fact]
        public void ResolveExisting_LegacySeedPath_ResolvesByFileName()
        {
            File.WriteAllText(Path.Combine(_storage.RootPath, "passport-john-doe.pdf"), "%PDF-1.4");

            Assert.Equal(
                Path.Combine(_storage.RootPath, "passport-john-doe.pdf"),
                _storage.ResolveExisting("local-test/passport-john-doe.pdf"));
        }

        [Fact]
        public async Task SaveAsync_WritesGuidNamedFileInsideRoot()
        {
            var bytes = Encoding.ASCII.GetBytes("%PDF-1.4 test");
            var name = await _storage.SaveAsync(new MemoryStream(bytes), ".pdf");

            Assert.True(_storage.IsValidStoredName(name));
            Assert.Equal(bytes, File.ReadAllBytes(Path.Combine(_storage.RootPath, name)));
        }

        [Theory]
        [InlineData("3f2b8c1e-4d5a-4b6c-9e7f-0a1b2c3d4e5f.pdf", true)]
        [InlineData("3f2b8c1e-4d5a-4b6c-9e7f-0a1b2c3d4e5f.jpg", true)]
        [InlineData("3f2b8c1e-4d5a-4b6c-9e7f-0a1b2c3d4e5f.jpeg", true)]
        [InlineData("3f2b8c1e-4d5a-4b6c-9e7f-0a1b2c3d4e5f.png", true)]
        [InlineData("3f2b8c1e-4d5a-4b6c-9e7f-0a1b2c3d4e5f.exe", false)]
        [InlineData("3f2b8c1e-4d5a-4b6c-9e7f-0a1b2c3d4e5f", false)]
        [InlineData("passport.pdf", false)]
        [InlineData("/uploads/3f2b8c1e-4d5a-4b6c-9e7f-0a1b2c3d4e5f.pdf", false)]
        [InlineData("../3f2b8c1e-4d5a-4b6c-9e7f-0a1b2c3d4e5f.pdf", false)]
        [InlineData("{3f2b8c1e-4d5a-4b6c-9e7f-0a1b2c3d4e5f}.pdf", false)]
        [InlineData("", false)]
        public void IsValidStoredName_AcceptsOnlyGuidPlusAllowedExtension(string name, bool expected)
        {
            Assert.Equal(expected, _storage.IsValidStoredName(name));
        }
    }
}
