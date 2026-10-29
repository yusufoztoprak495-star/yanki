using System.IO;
using Xunit;

namespace Yanki.Core.Tests
{
    /// <summary>
    /// 1. hafta iskeletinin eksiksiz olduğunu denetler (plan bölüm 5, 12 ve 13).
    /// Bu hafta bilerek OLUŞTURULMAYAN dosyaların yokluğu da burada korunur.
    /// </summary>
    public class RepoStructureTests
    {
        [Theory]
        [InlineData("Events")]
        [InlineData("World")]
        [InlineData("Mind")]
        [InlineData("Perception")]
        [InlineData("Criteria")]
        [InlineData("Rules")]
        [InlineData("Decision")]
        [InlineData("Brakes")]
        [InlineData("Simulation")]
        public void Core_modul_klasorunde_dolu_README_vardir(string module)
        {
            var path = RepoRoot.Combine("src", "Yanki.Core", module, "README.md");

            Assert.True(File.Exists(path), $"{module}/README.md yok.");
            Assert.False(string.IsNullOrWhiteSpace(File.ReadAllText(path)), $"{module}/README.md boş.");
        }

        [Theory]
        [InlineData("CONTRIBUTING.md")]
        [InlineData("docs/criteria-language.md")]
        [InlineData("docs/authoring-guide.md")]
        [InlineData("docs/evaluation.md")]
        [InlineData("samples/Quickstart/README.md")]
        [InlineData("samples/KasabaHani/npcs.md")]
        [InlineData("tests/golden/README.md")]
        [InlineData("tests/Yanki.Benchmarks/Yanki.Benchmarks.csproj")]
        public void Teslim_dosyasi_vardir(string relativePath)
        {
            Assert.True(File.Exists(RepoRoot.Combine(relativePath.Split('/'))), $"{relativePath} yok.");
        }

        [Theory]
        [InlineData("src/Yanki.Demo.Web")]
        [InlineData(".github/workflows/release.yml")]
        [InlineData(".github/workflows/pages.yml")]
        public void Sonraki_haftalarin_dosyasi_henuz_yoktur(string relativePath)
        {
            var path = RepoRoot.Combine(relativePath.Split('/'));

            Assert.False(File.Exists(path) || Directory.Exists(path), $"{relativePath} 1. haftada oluşturulmamalı.");
        }

        [Fact]
        public void Adr_007_vardir_onerildi_durumunda_ve_gerekcesi_bostur()
        {
            var path = RepoRoot.Combine("docs", "adr", "0007-sayisal-temsil-decimal.md");

            Assert.True(File.Exists(path), "ADR-007 yok.");
            var text = File.ReadAllText(path).Replace("\r\n", "\n");
            Assert.Contains("**Durum:** Önerildi", text);
            Assert.Contains("**Kabul eden:** —", text);

            // "Gerekçe (Yusuf)" bölümünü Yusuf yazar; bu bölüm boş kalmalı.
            var start = text.IndexOf("## Gerekçe (Yusuf)", System.StringComparison.Ordinal);
            Assert.True(start >= 0, "Gerekçe (Yusuf) başlığı yok.");
            var rest = text.Substring(start + "## Gerekçe (Yusuf)".Length);
            var next = rest.IndexOf("\n## ", System.StringComparison.Ordinal);
            var body = next >= 0 ? rest.Substring(0, next) : rest;
            Assert.True(string.IsNullOrWhiteSpace(body), "Gerekçe (Yusuf) boş olmalı.");
        }
    }
}
