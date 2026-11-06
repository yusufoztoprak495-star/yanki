using System;
using System.IO;
using Xunit;
using Yanki.Cli;

namespace Yanki.Scenarios.Tests
{
    /// <summary><c>yanki validate</c> CLI davranışı: çıkış kodları ve dosya:satir çıktısı (9. hafta, V kararı).</summary>
    public sealed class DogrulamaCliTests : IDisposable
    {
        private readonly string _dizin;

        public DogrulamaCliTests()
        {
            _dizin = Path.Combine(Path.GetTempPath(), "yanki-validate-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dizin);
        }

        public void Dispose() => Directory.Delete(_dizin, true);

        private static (int Kod, string Cikti, string Hata) Calistir(params string[] args)
        {
            var cikti = new StringWriter();
            var hata = new StringWriter();
            var kod = KomutCalistirici.Calistir(args, cikti, hata);
            return (kod, cikti.ToString(), hata.ToString());
        }

        private static string Repo(params string[] parcalar)
        {
            var yol = AppContext.BaseDirectory;
            while (!File.Exists(Path.Combine(yol, "Yanki.slnx")))
            {
                yol = Path.GetDirectoryName(yol)!;
            }

            return Path.Combine(yol, Path.Combine(parcalar));
        }

        [Fact]
        public void Hatasiz_ama_uyarili_senaryo_sifir_doner_uyari_hata_ile_bir_doner()
        {
            var dizin = Repo("samples", "HanGerilim");

            var normal = Calistir("validate", dizin);
            var siki = Calistir("validate", dizin, "--uyari-hata");

            Assert.Equal(0, normal.Kod);
            Assert.Contains("0 hata", normal.Cikti, StringComparison.Ordinal);
            Assert.Contains("uyarı kisilik", normal.Cikti, StringComparison.Ordinal);
            Assert.Equal(1, siki.Kod);
        }

        [Fact]
        public void Hatali_senaryoda_bir_doner_ve_hatalar_dosya_satir_ile_listelenir()
        {
            File.WriteAllText(Path.Combine(_dizin, "scenario.json"), "{ \"surum\": 1, \"ad\": \"x\" }");

            var r = Calistir("validate", _dizin);

            Assert.Equal(1, r.Kod);
            Assert.Contains("scenario.json:", r.Cikti, StringComparison.Ordinal);
        }

        [Fact]
        public void Olmayan_yol_ve_eksik_arguman_bir_doner()
        {
            Assert.Equal(1, Calistir("validate", Path.Combine(_dizin, "yok")).Kod);
            Assert.Equal(1, Calistir("validate").Kod);
        }

        [Fact]
        public void Coverage_kriter_tutmazsa_iki_doner_markdown_stdout_a_yazilir()
        {
            // HanGerilim'in oynanislar/ dizini yok: yalnızca oyuncusuz koşu var, betikli toplam 0 (kurallar hiç tetiklenmez).
            var r = Calistir("coverage", Repo("samples", "HanGerilim"), "--seeds", "2", "--tick", "6");

            Assert.Equal(2, r.Kod);
            Assert.StartsWith("# Kapsama raporu: HanGerilim", r.Cikti, StringComparison.Ordinal);
            Assert.Contains("hiç tetiklenmedi", r.Cikti, StringComparison.Ordinal);
            Assert.DoesNotContain(((char)13).ToString(), r.Cikti, StringComparison.Ordinal);
        }

        [Fact]
        public void Coverage_kullanim_hatalari_bir_doner()
        {
            Assert.Equal(1, Calistir("coverage").Kod);
            Assert.Equal(1, Calistir("coverage", Repo("samples", "HanGerilim"), "--seeds", "0").Kod);
            Assert.Equal(1, Calistir("coverage", Path.Combine(_dizin, "yok")).Kod);
        }
    }
}
