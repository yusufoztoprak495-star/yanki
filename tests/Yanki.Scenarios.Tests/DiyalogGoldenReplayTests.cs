using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Yanki.Cli;

namespace Yanki.Scenarios.Tests
{
    /// <summary>
    /// Diyalog golden'ları (8. hafta): <c>tests/golden/diyalog/*.golden.json</c> ayrı biçimdedir (kayıt, şablon, diyalog_hash, sablon_hash); dört ana golden'a alan eklenmez.
    /// Satır metni ya da seçimi bilerek değiştiyse: <c>yanki diyalog-replay &lt;dosya&gt; --guncelle</c> (gerekçe PR açıklamasında).
    /// </summary>
    public class DiyalogGoldenReplayTests
    {
        private static string Klasor()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "Yanki.slnx")))
            {
                d = d.Parent;
            }

            return Path.Combine(d!.FullName, "tests", "golden", "diyalog");
        }

        public static IEnumerable<object[]> Goldenlar() =>
            Directory.GetFiles(Klasor(), "*.golden.json").OrderBy(x => x, StringComparer.Ordinal).Select(f => new object[] { Path.GetFileName(f) });

        [Fact]
        public void En_az_uc_diyalog_golden_vardir()
        {
            Assert.True(Directory.GetFiles(Klasor(), "*.golden.json").Length >= 3);
        }

        [Theory]
        [MemberData(nameof(Goldenlar))]
        public void Diyalog_golden_bastan_oynatilinca_diyalog_ve_sablon_hashi_tutar(string dosya)
        {
            var cikti = new StringWriter();
            var hata = new StringWriter();

            var kod = KomutCalistirici.Calistir(new[] { "diyalog-replay", Path.Combine(Klasor(), dosya) }, cikti, hata);

            Assert.True(kod == KomutCalistirici.Basarili, $"{dosya}: çıkış kodu {kod} (3 diyalog_hash, 7 sablon_hash).\n{cikti}{hata}");
        }
    }
}
