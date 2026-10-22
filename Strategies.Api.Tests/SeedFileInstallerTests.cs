using System;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Strategies.Api.Services;
using Xunit;

namespace Strategies.Api.Tests
{
    public class SeedFileInstallerTests : IDisposable
    {
        private readonly string _parent;
        private readonly string _source;
        private readonly string _root;

        public SeedFileInstallerTests()
        {
            _parent = Path.Combine(Path.GetTempPath(), "seed-installer-tests-" + Guid.NewGuid());
            _source = Path.Combine(_parent, "SeedFiles");
            _root = Path.Combine(_parent, "uploads");
            Directory.CreateDirectory(_source);
            Directory.CreateDirectory(_root);
            File.WriteAllText(Path.Combine(_source, "passport-john-doe.pdf"), "%PDF-1.4 passport");
            File.WriteAllText(Path.Combine(_source, "medical-report-john-doe.pdf"), "%PDF-1.4 medical");
        }

        public void Dispose() => Directory.Delete(_parent, recursive: true);

        private int Run() => SeedFileInstaller.EnsureSeedFiles(_source, _root, NullLogger.Instance);

        [Fact]
        public void EnsureSeedFiles_EmptyRoot_CopiesEveryFile()
        {
            Assert.Equal(2, Run());
            Assert.Equal("%PDF-1.4 passport", File.ReadAllText(Path.Combine(_root, "passport-john-doe.pdf")));
            Assert.Equal("%PDF-1.4 medical", File.ReadAllText(Path.Combine(_root, "medical-report-john-doe.pdf")));
        }

        [Fact]
        public void EnsureSeedFiles_SecondRun_DoesNothing()
        {
            Run();
            var writtenAt = File.GetLastWriteTimeUtc(Path.Combine(_root, "passport-john-doe.pdf"));

            Assert.Equal(0, Run());
            Assert.Equal(writtenAt, File.GetLastWriteTimeUtc(Path.Combine(_root, "passport-john-doe.pdf")));
        }

        [Fact]
        public void EnsureSeedFiles_ExistingFile_IsNeverOverwritten()
        {
            File.WriteAllText(Path.Combine(_root, "passport-john-doe.pdf"), "replaced on the server");

            Assert.Equal(1, Run());
            Assert.Equal("replaced on the server", File.ReadAllText(Path.Combine(_root, "passport-john-doe.pdf")));
        }

        [Fact]
        public void EnsureSeedFiles_OneFileDeleted_RestoresOnlyThatFile()
        {
            Run();
            File.Delete(Path.Combine(_root, "medical-report-john-doe.pdf"));

            Assert.Equal(1, Run());
            Assert.True(File.Exists(Path.Combine(_root, "medical-report-john-doe.pdf")));
        }

        [Fact]
        public void EnsureSeedFiles_MissingSourceFolder_ReturnsZeroWithoutThrowing()
        {
            Assert.Equal(0, SeedFileInstaller.EnsureSeedFiles(Path.Combine(_parent, "nope"), _root, NullLogger.Instance));
        }
    }
}
