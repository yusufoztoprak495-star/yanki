using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Xunit;

namespace Yanki.Core.Tests
{
    /// <summary>
    /// ADR-004: netstandard2.1 çıktısı da yalnızca çerçeveye bağlıdır. Test projesi net10.0 hedefini
    /// referans aldığı için Unity'nin kullanacağı DLL'i doğrudan meta verisinden okuyoruz.
    /// </summary>
    public class NetStandardOutputTests
    {
        [Fact]
        public void Core_netstandard21_cikti_yalnizca_netstandard_referans_verir()
        {
            var dll = RepoRoot.Combine("src", "Yanki.Core", "bin", Configuration(), "netstandard2.1", "Yanki.Core.dll");
            Assert.True(File.Exists(dll), $"netstandard2.1 çıktısı bulunamadı: {dll}");

            using var stream = File.OpenRead(dll);
            using var pe = new PEReader(stream);
            var reader = pe.GetMetadataReader();
            var references = reader.AssemblyReferences
                .Select(h => reader.GetString(reader.GetAssemblyReference(h).Name))
                .ToList();

            Assert.NotEmpty(references);
            Assert.All(references, name => Assert.Equal("netstandard", name));
        }

        // Test çıktı yolu: .../tests/Yanki.Core.Tests/bin/<Configuration>/net10.0/
        private static string Configuration() =>
            new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar)).Parent!.Name;
    }
}
