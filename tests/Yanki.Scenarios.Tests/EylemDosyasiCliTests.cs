using System;
using System.IO;
using Xunit;
using Yanki.Cli;

namespace Yanki.Scenarios.Tests
{
    /// <summary>CLI'nın <c>eylem_dosyalari</c> okuması; sahte (fikstür) senaryo geçici klasöre yazılır, Kasaba Hanı içeriği değildir.</summary>
    public sealed class EylemDosyasiCliTests : IDisposable
    {
        private const string SenaryoSablonu = @"{
  ""surum"": 1,
  ""ad"": ""Deneme"",
  ""mekanlar"": [""han"", ""pazar""],
  ""baglantilar"": [[""han"", ""pazar""]],
  ""oyuncu"": { ""id"": ""oyuncu"", ""konum"": ""han"" },
  ""npcler"": [
    { ""id"": ""Ali"", ""konum"": ""han"", ""kisilik"": { ""acgozluluk"": 0.5, ""durustluk"": 0.5, ""cesaret"": 0.5, ""sosyallik"": 0.5, ""kin_tutma"": 0.5, ""merhamet"": 0.5 } }
  ],
  ""gercekler"": [],
  ""olay_turleri"": [""yardim_etti""],
  ""kural_dosyalari"": [""kurallar.json""],
  ""eylem_dosyalari"": [EYLEMLER]
}";

        private const string Eylem = @"{ ""eylemler"": [ {
  ""id"": ""selam_ver"", ""olay_turu"": ""selam_verdi"", ""hedef"": ""yok"", ""taban"": 1,
  ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.5 } ]
} ] }";

        // Kural, NPC eyleminin olay türüne bağlanır: eylem dosyaları kurallardan ÖNCE okunmalı.
        private const string Kural = @"{ ""kurallar"": [ {
  ""id"": ""selam_kurali"", ""ne_zaman"": ""selam_verdi"",
  ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.4 } ]
} ] }";

        private readonly string _dizin;

        public EylemDosyasiCliTests()
        {
            _dizin = Path.Combine(Path.GetTempPath(), "yanki-eylem-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dizin);
            File.WriteAllText(Yol("kurallar.json"), Kural);
        }

        public void Dispose()
        {
            Directory.Delete(_dizin, true);
        }

        private string Yol(string ad) => Path.Combine(_dizin, ad);

        private string Senaryo(string eylemListesi)
        {
            File.WriteAllText(Yol("senaryo.json"), SenaryoSablonu.Replace("EYLEMLER", eylemListesi));
            return Yol("senaryo.json");
        }

        private static (int Kod, string Cikti, string Hata) Calistir(params string[] args)
        {
            var cikti = new StringWriter();
            var hata = new StringWriter();
            var kod = KomutCalistirici.Calistir(args, cikti, hata);
            return (kod, cikti.ToString(), hata.ToString());
        }

        [Fact]
        public void Eylem_dosyasi_okunur_ve_kural_npc_eyleminin_turune_baglanabilir()
        {
            File.WriteAllText(Yol("eylemler.json"), Eylem);

            var (kod, _, hata) = Calistir("run", Senaryo("\"eylemler.json\""), "--seed", "1", "--tick", "2");

            Assert.True(kod == 0, hata);
        }

        [Fact]
        public void Eylem_dosyasi_hatasi_dosya_satir_eylem_id_bicimiyle_yazilir_ve_2_koduyla_cikar()
        {
            File.WriteAllText(Yol("eylemler.json"), Eylem.Replace("\"taban\": 1", "\"taban\": 0"));

            var (kod, _, hata) = Calistir("run", Senaryo("\"eylemler.json\""), "--tick", "1");

            Assert.Equal(2, kod);
            Assert.Contains("eylemler.json:", hata);
            Assert.Contains("eylem \"selam_ver\": ", hata);
            Assert.Contains("taban", hata);
        }

        [Fact]
        public void Olmayan_eylem_dosyasi_acik_mesajla_reddedilir()
        {
            var (kod, _, hata) = Calistir("run", Senaryo("\"yok.json\""), "--tick", "1");

            Assert.Equal(2, kod);
            Assert.Contains("eylem dosyası bulunamadı", hata);
        }

        [Fact]
        public void Dosyalar_arasi_yinelenen_eylem_kimligi_reddedilir()
        {
            File.WriteAllText(Yol("a.json"), Eylem);
            File.WriteAllText(Yol("b.json"), Eylem);

            var (kod, _, hata) = Calistir("run", Senaryo("\"a.json\", \"b.json\""), "--tick", "1");

            Assert.Equal(2, kod);
            Assert.Contains("selam_ver", hata);
            Assert.Contains("tekrar", hata);
        }

        [Fact]
        public void Eylem_dosyasi_olmayan_senaryo_eskisi_gibi_calisir()
        {
            File.WriteAllText(Yol("kurallar.json"), "{ \"kurallar\": [] }");
            var senaryo = System.Text.RegularExpressions.Regex.Replace(
                SenaryoSablonu.Replace("EYLEMLER", string.Empty), @",\s*""eylem_dosyalari"": \[\]", string.Empty);
            Assert.DoesNotContain("eylem_dosyalari", senaryo);
            File.WriteAllText(Yol("senaryo.json"), senaryo);

            var (kod, _, hata) = Calistir("run", Yol("senaryo.json"), "--tick", "1");

            Assert.True(kod == 0, hata);
        }
    }
}
