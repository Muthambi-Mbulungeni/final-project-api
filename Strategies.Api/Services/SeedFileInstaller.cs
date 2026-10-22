using System.IO;
using Microsoft.Extensions.Logging;

namespace Strategies.Api.Services
{
    /// <summary>
    /// Copies the placeholder files behind the seeded Document rows (shipped in SeedFiles/) into the
    /// storage root when they are missing. Runs on every startup, so a fresh or redeployed host
    /// needs no manual copy. Idempotent: an existing file is never overwritten.
    /// </summary>
    public static class SeedFileInstaller
    {
        public static int EnsureSeedFiles(string sourceDirectory, string storageRoot, ILogger logger)
        {
            if (!Directory.Exists(sourceDirectory))
            {
                logger.LogWarning("Seed file folder {SourceDirectory} not found; no seed files installed.", sourceDirectory);
                return 0;
            }

            var copied = 0;
            foreach (var source in Directory.EnumerateFiles(sourceDirectory))
            {
                var target = Path.Combine(storageRoot, Path.GetFileName(source));
                if (File.Exists(target)) continue;

                try
                {
                    File.Copy(source, target, overwrite: false);
                    copied++;
                }
                catch (IOException) when (File.Exists(target))
                {
                    // Another instance copied it first.
                }
            }

            if (copied > 0)
                logger.LogInformation("Installed {Count} seed file(s) into the storage root.", copied);
            return copied;
        }
    }
}
