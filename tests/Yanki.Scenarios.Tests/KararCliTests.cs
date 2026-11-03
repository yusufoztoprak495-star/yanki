using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Yanki.Cli;

namespace Yanki.Scenarios.Tests
{
    /// <summary>
    /// CLI: NPC eylemli senaryolarda <c>run</c>/<c>replay</c>, oyuncu hareketi (<c>--eylem hareket:pazar</c>), <c>why</c> zincirinin karar izine
    /// inmesi ve <c>query</c> ile son karar. Sahte (fikstür) senaryo geçici klasöre yazılır, Kasaba Hanı içeriği değildir.
    /// </summary>
    public sealed class KararCliTests : IDisposable
    {
        private const string Senaryo = @"{
  ""surum"": 1,
  ""ad"": ""Deneme"",
  ""mekanlar"": [""han"", ""pazar""],
  ""baglantilar"": [[""han"", ""pazar""]],
  ""oyuncu"": { ""id"": ""oyuncu"", ""konum"": ""han"" },
  ""npcler"": [
    { ""id"": ""Ali"", ""konum"": ""han"", ""kisilik"": { ""acgozluluk"": 0.5, ""durustluk"": 0.5, ""cesaret"": 0.5, ""sosyallik"": 0.9, ""kin_tutma"": 0.5, ""merhamet"": 0.5 } },
    { ""id"": ""Can"", ""konum"": ""han"", ""kisilik"": { ""acgozluluk"": 0.5, ""durustluk"": 0.5, ""cesaret"": 0.5, ""sosyallik"": 0.5, ""kin_tutma"": 0.5, ""merhamet"": 0.5 } }
  ],
  ""gercekler"": [],
  ""olay_turleri"": [""yardim_etti""],
  ""kural_dosyalari"": [""kurallar.json""],
  ""eylem_dosyalari"": [""eylemler.json""]
}";

        private const string Eylemler = @"{ ""eylemler"": [ {
  ""id"": ""selam_ver"", ""olay_turu"": ""selam_verdi"", ""hedef"": ""yok"", ""taban"": 1,
  ""olcutler"": [ { ""alan"": ""kisilik(sosyallik)"", ""egri"": { ""tur"": ""lojistik"", ""k"": 8, ""m"": 0.4 } } ],
  ""kisilik_carpani"": { ""ozellikler"": [ { ""ozellik"": ""sosyallik"" } ] },
  ""celisen_ozellikler"": [ ""durustluk"" ], ""stres_tabani"": 10,
  ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.6 } ]
} ] }";

        private const string Kurallar = @"{ ""kurallar"": [ {
  ""id"": ""selam_tanigi"", ""ne_zaman"": ""selam_verdi"",
  ""kriterler"": [ { ""alan"": ""olay_rolu(tanik)"", ""op"": ""=="", ""deger"": true } ],
  ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""selamlandi"", ""deger"": 5, ""sure"": 50 } ]
} ] }";

        private readonly string _dizin;

        public KararCliTests()
        {
            _dizin = Path.Combine(Path.GetTempPath(), "yanki-karar-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dizin);
            File.WriteAllText(Yol("senaryo.json"), Senaryo);
            File.WriteAllText(Yol("eylemler.json"), Eylemler);
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

        private string Kaydet(params string[] ekler)
        {
            var oturum = Yol("oturum.json");
            var args = new[] { "run", Yol("senaryo.json"), "--seed", "7" }.Concat(ekler).Concat(new[] { "--kaydet", oturum }).ToArray();
            var (kod, _, hata) = Calistir(args);
            Assert.True(kod == 0, hata);
            return oturum;
        }

        private static long OlayKimligi(string cikti, string tur, int sira = 0)
        {
            var eslesmeler = Regex.Matches(cikti, @"^#(\d+) " + Regex.Escape(tur) + @" ", RegexOptions.Multiline);
            return long.Parse(eslesmeler[sira].Groups[1].Value);
        }

        [Fact]
        public void Run_npc_eylemlerini_ve_karar_olaylarini_yazar_izi_dokmez()
        {
            var (kod, cikti, hata) = Calistir("run", Yol("senaryo.json"), "--seed", "7", "--tick", "2");

            Assert.True(kod == 0, hata);
            Assert.Contains("karar_verildi", cikti);
            Assert.Contains("kural karar", cikti);
            Assert.Contains("selam_verdi", cikti);
            Assert.Contains("kural eylem:selam_ver", cikti);
            Assert.DoesNotContain("iz=", cikti);
            Assert.DoesNotContain("{\"npc\"", cikti);
        }

        [Fact]
        public void Run_oyuncu_hareketini_eylem_olarak_alir()
        {
            var (kod, cikti, hata) = Calistir("run", Yol("senaryo.json"), "--seed", "7", "--eylem", "hareket:pazar", "--tick", "1");

            Assert.True(kod == 0, hata);
            Assert.Contains("ayrildi", cikti);
            Assert.Contains("geldi", cikti);
            Assert.Contains("konum!(oyuncu,pazar)", cikti);
        }

        [Fact]
        public void Run_gecersiz_hareketi_acik_mesajla_reddeder()
        {
            var (kod, _, hata) = Calistir("run", Yol("senaryo.json"), "--eylem", "hareket:han", "--tick", "1");

            Assert.Equal(2, kod);
            Assert.Contains("Eylem reddedildi", hata);
            Assert.Contains("bağlı", hata);
        }

        [Fact]
        public void Replay_npc_kararlari_ve_oyuncu_hareketi_olan_oturumu_dogrular()
        {
            var oturum = Kaydet("--eylem", "hareket:pazar", "--tick", "2", "--eylem", "hareket:han", "--tick", "3");

            var (kod, cikti, hata) = Calistir("replay", oturum);

            Assert.True(kod == 0, hata);
            Assert.Contains("hash tutuyor", cikti);
            var json = File.ReadAllText(oturum);
            Assert.Contains("\"hareket\"", json);
            Assert.DoesNotContain("karar_verildi", json);
            Assert.DoesNotContain("selam_ver", json);
        }

        [Fact]
        public void Why_karar_olayina_gelince_adaylari_puan_bilesenlerini_ve_secimi_yazar()
        {
            var oturum = Kaydet("--tick", "2");
            var (_, run, _) = Calistir("run", Yol("senaryo.json"), "--seed", "7", "--tick", "2");
            var karar = OlayKimligi(run, "karar_verildi");

            var (kod, cikti, hata) = Calistir("why", oturum, karar.ToString());

            Assert.True(kod == 0, hata);
            Assert.Contains("karar izi: Ali", cikti);
            Assert.Contains("seçilen: selam_ver", cikti);
            Assert.Contains("aday selam_ver: puan", cikti);
            Assert.Contains("lojistik(k=8, m=0.4)", cikti);
            Assert.Contains("çelişen özellikler: durustluk", cikti);
        }

        [Fact]
        public void Why_modifierdan_algiya_eyleme_ve_karar_izine_iner()
        {
            var oturum = Kaydet("--tick", "2");
            var (_, run, _) = Calistir("run", Yol("senaryo.json"), "--seed", "7", "--tick", "2");
            var modifier = OlayKimligi(Regex.Replace(run, "^(#\\d+ modifier_eklendi \\(tick \\d+, kural (?!selam_tanigi)).*$", string.Empty, RegexOptions.Multiline), "modifier_eklendi");

            var (kod, cikti, hata) = Calistir("why", oturum, modifier.ToString());

            Assert.True(kod == 0, hata);
            Assert.Contains("modifier_eklendi", cikti);
            Assert.Contains("kural selam_tanigi", cikti);
            Assert.Contains("kanit_eklendi", cikti);
            Assert.Contains("selam_verdi", cikti);
            Assert.Contains("karar_verildi", cikti);
            Assert.Contains("karar izi:", cikti);
            Assert.Contains("aday selam_ver: puan", cikti);
        }

        [Fact]
        public void Query_npcnin_son_kararini_gosterir()
        {
            var oturum = Kaydet("--tick", "2");

            var (kod, cikti, hata) = Calistir("query", oturum, "Ali");

            Assert.True(kod == 0, hata);
            Assert.Contains("son karar (#", cikti);
            Assert.Contains("karar izi: Ali, tick 2", cikti);
        }

        [Fact]
        public void Query_eylemsiz_senaryoda_son_karar_satiri_yazmaz()
        {
            // Kaynak dosyanın satır sonundan bağımsız (CRLF/LF): satır sonu ve boşluk regex ile eşleşir.
            var senaryo = Regex.Replace(Senaryo, @",\s*""eylem_dosyalari"": \[""eylemler\.json""\]", string.Empty);
            File.WriteAllText(Yol("senaryo2.json"), senaryo);
            File.WriteAllText(Yol("kurallar.json"), "{ \"kurallar\": [] }");
            var oturum = Yol("oturum2.json");
            Assert.Equal(0, Calistir("run", Yol("senaryo2.json"), "--tick", "1", "--kaydet", oturum).Kod);

            var (kod, cikti, _) = Calistir("query", oturum, "Ali");

            Assert.Equal(0, kod);
            Assert.DoesNotContain("son karar", cikti);
        }
    }
}
