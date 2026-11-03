using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Yanki.Cli;

namespace Yanki.Scenarios.Tests
{
    /// <summary>CLI: <c>query</c> fren listesini gösterir, <c>why</c> fren olayından engellenen tetikleyiciye iner. Sahte (fikstür) senaryo, içerik değildir.</summary>
    public sealed class FrenCliTests : IDisposable
    {
        private const string Senaryo = @"{
  ""surum"": 1, ""ad"": ""Fren"",
  ""mekanlar"": [""han"", ""pazar""], ""baglantilar"": [[""han"", ""pazar""]],
  ""oyuncu"": { ""id"": ""oyuncu"", ""konum"": ""han"" },
  ""npcler"": [
    { ""id"": ""Ali"", ""konum"": ""han"", ""kisilik"": { ""acgozluluk"": 0.5, ""durustluk"": 0.5, ""cesaret"": 0.5, ""sosyallik"": 0.5, ""kin_tutma"": 0.5, ""merhamet"": 0.5 } }
  ],
  ""gercekler"": [], ""olay_turleri"": [""yardim_etti""],
  ""kural_dosyalari"": [""kurallar.json""]
}";

        private const string Kurallar = @"{ ""kurallar"": [ {
  ""id"": ""hedefe_minnet"", ""ne_zaman"": ""yardim_etti"",
  ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.hedef"", ""modifier"": ""minnet"", ""deger"": 15, ""sure"": 100 } ]
} ] }";

        private readonly string _dizin;

        public FrenCliTests()
        {
            _dizin = Path.Combine(Path.GetTempPath(), "yanki-fren-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dizin);
            File.WriteAllText(Yol("senaryo.json"), Senaryo);
            File.WriteAllText(Yol("kurallar.json"), Kurallar);
        }

        public void Dispose() => Directory.Delete(_dizin, true);

        private string Yol(string ad) => Path.Combine(_dizin, ad);

        private static (int Kod, string Cikti, string Hata) Calistir(params string[] args)
        {
            var cikti = new StringWriter();
            var hata = new StringWriter();
            var kod = KomutCalistirici.Calistir(args, cikti, hata);
            return (kod, cikti.ToString(), hata.ToString());
        }

        private string Oturum()
        {
            var oturum = Yol("oturum.json");
            var (kod, _, hata) = Calistir("run", Yol("senaryo.json"), "--seed", "1", "--eylem", "yardim_etti:pazar", "--tick", "1", "--kaydet", oturum);
            Assert.True(kod == 0, hata);
            return oturum;
        }

        [Fact]
        public void Query_npcnin_fren_olaylarini_listeler()
        {
            var (kod, cikti, hata) = Calistir("query", Oturum(), "Ali");

            Assert.True(kod == 0, hata);
            Assert.Contains("-- frenler --", cikti);
            Assert.Contains("kisi_olmayan_hedef", cikti);
            Assert.Contains("hedefe_minnet", cikti);
            Assert.Contains("pazar", cikti);
        }

        [Fact]
        public void Why_fren_olayindan_engellenen_tetikleyiciye_iner()
        {
            var oturum = Oturum();
            var (_, run, _) = Calistir("run", Yol("senaryo.json"), "--seed", "1", "--eylem", "yardim_etti:pazar", "--tick", "1");
            var id = Regex.Match(run, @"^#(\d+) fren_uygulandi ", RegexOptions.Multiline).Groups[1].Value;
            Assert.NotEmpty(id);

            var (kod, cikti, hata) = Calistir("why", oturum, id);

            Assert.True(kod == 0, hata);
            Assert.Contains("yardim_etti", cikti);
            Assert.Contains("fren_uygulandi", cikti);
        }

        [Fact]
        public void Fren_yoksa_query_cikti_eskisiyle_ayni_frenler_basligi_yok()
        {
            var oturum = Yol("temiz.json");
            var (kod, _, hata) = Calistir("run", Yol("senaryo.json"), "--seed", "1", "--eylem", "yardim_etti:Ali", "--tick", "1", "--kaydet", oturum);
            Assert.True(kod == 0, hata);

            var (_, cikti, _) = Calistir("query", oturum, "Ali");
            Assert.DoesNotContain("-- frenler --", cikti);
        }
    }
}
