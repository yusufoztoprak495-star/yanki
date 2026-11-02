using System;
using System.IO;
using Xunit;
using Yanki.Cli;

namespace Yanki.Scenarios.Tests
{
    /// <summary>CLI komutlarının uçtan uca testleri; sahte (fikstür) senaryo geçici klasöre yazılır, Kasaba Hanı içeriği değildir.</summary>
    public sealed class CliTests : IDisposable
    {
        private const string Senaryo = @"{
  ""surum"": 1,
  ""ad"": ""Deneme"",
  ""mekanlar"": [""han"", ""pazar""],
  ""baglantilar"": [[""han"", ""pazar""]],
  ""oyuncu"": { ""id"": ""oyuncu"", ""konum"": ""han"" },
  ""npcler"": [
    { ""id"": ""Ali"", ""konum"": ""han"", ""kisilik"": { ""acgozluluk"": 0.5, ""durustluk"": 0.5, ""cesaret"": 0.5, ""sosyallik"": 0.5, ""kin_tutma"": 0.5, ""merhamet"": 0.5 } },
    { ""id"": ""Can"", ""konum"": ""pazar"", ""kisilik"": { ""acgozluluk"": 0.5, ""durustluk"": 0.5, ""cesaret"": 0.5, ""sosyallik"": 0.5, ""kin_tutma"": 0.5, ""merhamet"": 0.5 } }
  ],
  ""gercekler"": [],
  ""olay_turleri"": [""yardim_etti""],
  ""kural_dosyalari"": [""kurallar.json""]
}";

        private const string Kurallar = @"{ ""kurallar"": [ {
  ""id"": ""yardim_minneti"", ""ne_zaman"": ""yardim_etti"",
  ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""minnet"", ""deger"": 15, ""sure"": 100 } ]
} ] }";

        private readonly string _dizin;

        public CliTests()
        {
            _dizin = Path.Combine(Path.GetTempPath(), "yanki-cli-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dizin);
            File.WriteAllText(Yol("senaryo.json"), Senaryo);
            File.WriteAllText(Yol("kurallar.json"), Kurallar);
        }

        public void Dispose()
        {
            Directory.Delete(_dizin, true);
        }

        private string Yol(string ad) => Path.Combine(_dizin, ad);

        private static (int Kod, string Cikti, string Hata) Calistir(params string[] args)
        {
            var cikti = new StringWriter();
            var hata = new StringWriter();
            var kod = KomutCalistirici.Calistir(args, cikti, hata);
            return (kod, cikti.ToString(), hata.ToString());
        }

        private string OturumKaydet()
        {
            var oturum = Yol("oturum.json");
            var (kod, _, hata) = Calistir("run", Yol("senaryo.json"), "--seed", "42", "--eylem", "yardim_etti:Ali", "--tick", "3", "--kaydet", oturum);
            Assert.True(kod == 0, hata);
            return oturum;
        }

        [Fact]
        public void Run_senaryoyu_calistirir_olaylari_ve_hash_i_yazar()
        {
            var (kod, cikti, hata) = Calistir("run", Yol("senaryo.json"), "--seed", "42", "--eylem", "yardim_etti:Ali", "--tick", "3");

            Assert.True(kod == 0, hata);
            Assert.Contains("yardim_etti", cikti);
            Assert.Contains("modifier_eklendi", cikti);
            Assert.Contains("yardim_minneti", cikti);
            Assert.Contains("tick: 3", cikti);
            Assert.Matches("durum hash: [0-9a-f]{16}", cikti);
        }

        [Fact]
        public void Run_ayni_seed_ve_girdiyle_ayni_hash_yazar()
        {
            var a = Calistir("run", Yol("senaryo.json"), "--seed", "7", "--eylem", "yardim_etti:Ali", "--tick", "2");
            var b = Calistir("run", Yol("senaryo.json"), "--seed", "7", "--eylem", "yardim_etti:Ali", "--tick", "2");
            Assert.Equal(a.Cikti, b.Cikti);
        }

        [Fact]
        public void Run_kaydet_oturum_dosyasi_yazar_ve_replay_dogrular()
        {
            var oturum = OturumKaydet();

            Assert.Contains("\"seed\": 42", File.ReadAllText(oturum));
            var (kod, cikti, hata) = Calistir("replay", oturum);
            Assert.True(kod == 0, hata);
            Assert.Contains("hash tutuyor", cikti);
        }

        [Fact]
        public void Replay_hash_tutmazsa_3_koduyla_cikar_ve_guncelle_duzeltir()
        {
            var oturum = OturumKaydet();
            var icerik = File.ReadAllText(oturum);
            var bozuk = System.Text.RegularExpressions.Regex.Replace(icerik, "\"son_hash\": \"[0-9a-f]{16}\"", "\"son_hash\": \"0000000000000000\"");
            File.WriteAllText(oturum, bozuk);

            var (kod, cikti, _) = Calistir("replay", oturum);
            Assert.Equal(3, kod);
            Assert.Contains("0000000000000000", cikti);

            var (kod2, cikti2, hata2) = Calistir("replay", oturum, "--guncelle");
            Assert.True(kod2 == 0, hata2);
            Assert.Contains("güncellendi", cikti2);
            Assert.Equal(icerik, File.ReadAllText(oturum));
            Assert.Equal(0, Calistir("replay", oturum).Kod);
        }

        [Fact]
        public void Replay_guncelle_hash_zaten_dogruysa_dosyayi_degistirmez()
        {
            var oturum = OturumKaydet();
            var once = File.ReadAllText(oturum);

            var (kod, cikti, _) = Calistir("replay", oturum, "--guncelle");

            Assert.Equal(0, kod);
            Assert.Contains("değişmedi", cikti);
            Assert.Equal(once, File.ReadAllText(oturum));
        }

        [Fact]
        public void Query_npcnin_fikrini_modifierlari_ve_inanclarini_gosterir()
        {
            var oturum = OturumKaydet();

            var (kod, cikti, hata) = Calistir("query", oturum, "Ali");

            Assert.True(kod == 0, hata);
            Assert.Contains("Ali", cikti);
            Assert.Contains("han", cikti);
            Assert.Contains("fikir oyuncu: 15", cikti);
            Assert.Contains("modifier:minnet", cikti);
            Assert.Contains("yardim_etti(oyuncu,Ali)", cikti);
            Assert.Contains("gordu", cikti);
        }

        [Fact]
        public void Query_bilinmeyen_npc_icin_hata_verir()
        {
            var oturum = OturumKaydet();
            var (kod, _, hata) = Calistir("query", oturum, "Hayalet");
            Assert.Equal(2, kod);
            Assert.Contains("Hayalet", hata);
        }

        [Fact]
        public void Why_modifierdan_oyuncu_eylemine_inen_zinciri_yazar()
        {
            var oturum = OturumKaydet();
            var (_, run, _) = Calistir("run", Yol("senaryo.json"), "--seed", "42", "--eylem", "yardim_etti:Ali", "--tick", "3", "--hepsi");
            var satir = Array.Find(run.Split('\n'), l => l.Contains("modifier_eklendi") && l.Contains("npc=Ali"))!;
            var id = satir.TrimStart().Split(' ')[0].TrimStart('#');

            var (kod, cikti, hata) = Calistir("why", oturum, id);

            Assert.True(kod == 0, hata);
            var satirlar = cikti.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.StartsWith("#" + id + " modifier_eklendi", satirlar[0]);
            Assert.Contains("yardim_minneti", satirlar[0]);
            Assert.Contains(satirlar, l => l.Contains("kanit_eklendi"));
            Assert.Contains(satirlar, l => l.Contains("yardim_etti") && l.Contains("kural player"));
        }

        [Fact]
        public void Why_olmayan_olay_icin_hata_verir()
        {
            var oturum = OturumKaydet();
            var (kod, _, hata) = Calistir("why", oturum, "9999");
            Assert.Equal(2, kod);
            Assert.Contains("9999", hata);
        }

        [Fact]
        public void Gecersiz_kural_dosyasi_dosya_satir_ve_kural_kimligiyle_reddedilir()
        {
            File.WriteAllText(Yol("kurallar.json"), "{ \"kurallar\": [\n { \"id\": \"bozuk\", \"ne_zaman\": \"yok_olay\", \"etkiler\": [] } ] }");

            var (kod, _, hata) = Calistir("run", Yol("senaryo.json"), "--tick", "1");

            Assert.Equal(2, kod);
            Assert.Contains("kurallar.json:2:", hata);
            Assert.Contains("kural \"bozuk\"", hata);
        }

        [Fact]
        public void Eksik_dosya_ve_kullanim_hatalari()
        {
            Assert.Equal(2, Calistir("run", Yol("yok.json"), "--tick", "1").Kod);
            Assert.Equal(1, Calistir("run").Kod);
            Assert.Equal(1, Calistir("replay").Kod);
            Assert.Equal(1, Calistir("bilinmeyen_komut").Kod);
            Assert.Equal(1, Calistir("run", Yol("senaryo.json"), "--bilinmeyen").Kod);
            Assert.Equal(1, Calistir("run", Yol("senaryo.json"), "--tick", "abc").Kod);
        }

        [Fact]
        public void Run_son_eylemden_sonra_tick_yoksa_hata_verir()
        {
            var (kod, _, hata) = Calistir("run", Yol("senaryo.json"), "--eylem", "yardim_etti:Ali");
            Assert.Equal(1, kod);
            Assert.Contains("--tick", hata);
        }

        [Fact]
        public void Run_gecersiz_eylemi_reddeder()
        {
            var (kod, _, hata) = Calistir("run", Yol("senaryo.json"), "--eylem", "ucmak:Ali", "--tick", "1");
            Assert.Equal(2, kod);
            Assert.Contains("ucmak", hata);
        }

        [Fact]
        public void Run_eylem_yuku_ve_gorunurlugu_alir()
        {
            var (kod, cikti, hata) = Calistir("run", Yol("senaryo.json"), "--eylem", "yardim_etti:Ali:gorunurluk=gizli", "--tick", "1");

            Assert.True(kod == 0, hata);
            Assert.DoesNotContain("modifier_eklendi", cikti);
            Assert.Contains("gorunurluk=gizli", cikti);
        }

        [Fact]
        public void Version_calismaya_devam_eder()
        {
            var (kod, cikti, _) = Calistir("--version");
            Assert.Equal(0, kod);
            Assert.StartsWith("yanki ", cikti);
        }
    }
}
