using System;
using System.IO;
using Xunit;

namespace Yanki.Scenarios.Tests
{
    public class SamplesTests
    {
        [Fact]
        public void KasabaHani_ornek_klasoru_mevcuttur()
        {
            var dir = FindRepoRoot();
            Assert.True(Directory.Exists(Path.Combine(dir, "samples", "KasabaHani")),
                "samples/KasabaHani bulunamadı.");
        }

        private static string FindRepoRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null && !File.Exists(Path.Combine(current.FullName, "Yanki.slnx")))
            {
                current = current.Parent;
            }

            return current?.FullName ?? throw new InvalidOperationException("Repo kökü (Yanki.slnx) bulunamadı.");
        }
    }
}
