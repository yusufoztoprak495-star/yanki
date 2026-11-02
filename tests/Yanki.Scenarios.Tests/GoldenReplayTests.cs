using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Yanki.Cli;

namespace Yanki.Scenarios.Tests
{
    /// <summary>
    /// Golden replay (plan bölüm 9): <c>tests/golden/*.json</c> oturumları baştan oynatılır ve son durum hash'i kayıttakiyle
    /// birebir aynı olmalıdır. Hash tutmuyorsa determinizm bozulmuştur ya da davranış bilerek değişmiştir; ikinci durumda
    /// <c>yanki replay &lt;dosya&gt; --guncelle</c> çalıştırılır ve gerekçe PR açıklamasına yazılır (tests/golden/README.md).
    /// </summary>
    public class GoldenReplayTests
    {
        public static IEnumerable<object[]> Oturumlar()
        {
            foreach (var dosya in Directory.GetFiles(GoldenKlasoru(), "*.json"))
            {
                yield return new object[] { Path.GetFileName(dosya) };
            }
        }

        [Fact]
        public void En_az_bir_golden_oturum_vardir()
        {
            Assert.NotEmpty(Directory.GetFiles(GoldenKlasoru(), "*.json"));
        }

        [Theory]
        [MemberData(nameof(Oturumlar))]
        public void Golden_oturum_bastan_oynatilinca_beklenen_hashi_verir(string dosyaAdi)
        {
            var yol = Path.Combine(GoldenKlasoru(), dosyaAdi);
            var cikti = new StringWriter();
            var hata = new StringWriter();

            var kod = KomutCalistirici.Calistir(new[] { "replay", yol }, cikti, hata);

            Assert.True(
                kod == KomutCalistirici.Basarili,
                $"{dosyaAdi}: golden hash tutmuyor (çıkış kodu {kod}).\n{cikti}{hata}\n" +
                "Davranış bilerek değiştiyse: dotnet run --project src/Yanki.Cli -- replay tests/golden/" + dosyaAdi + " --guncelle");
        }

        private static string GoldenKlasoru()
        {
            var dizin = new DirectoryInfo(AppContext.BaseDirectory);
            while (dizin != null && !File.Exists(Path.Combine(dizin.FullName, "Yanki.slnx")))
            {
                dizin = dizin.Parent;
            }

            return Path.Combine(dizin?.FullName ?? throw new InvalidOperationException("Repo kökü (Yanki.slnx) bulunamadı."), "tests", "golden");
        }
    }
}
