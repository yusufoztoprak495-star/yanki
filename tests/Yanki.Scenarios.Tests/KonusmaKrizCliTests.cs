using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Yanki.Cli;

namespace Yanki.Scenarios.Tests
{
    /// <summary>
    /// CLI: konuşmalı ve krizli senaryolarda <c>run</c> (oyuncu anlatımı), <c>query</c> (stres, aktarım, kanıt kökü), <c>why</c> ve <c>replay</c>;
    /// <c>kriz_dosyalari</c> doğrulayıcısı. Sahte (fikstür) senaryo geçici klasöre yazılır, Kasaba Hanı içeriği DEĞİLDİR.
    /// </summary>
    public sealed class KonusmaKrizCliTests : IDisposable
    {
        private const string Senaryo = @"{
  ""surum"": 1, ""ad"": ""Dedikodu"",
  ""mekanlar"": [""han""], ""baglantilar"": [],
  ""oyuncu"": { ""id"": ""oyuncu"", ""konum"": ""han"" },
  ""npcler"": [
    { ""id"": ""Ayse"", ""konum"": ""han"", ""kisilik"": { ""acgozluluk"": 0.5, ""durustluk"": 0.9, ""cesaret"": 0.5, ""sosyallik"": 0.9, ""kin_tutma"": 0.5, ""merhamet"": 0.5 } },
    { ""id"": ""Can"", ""konum"": ""han"", ""kisilik"": { ""acgozluluk"": 0.5, ""durustluk"": 0.5, ""cesaret"": 0.5, ""sosyallik"": 0.5, ""kin_tutma"": 0.5, ""merhamet"": 0.5 } }
  ],
  ""gercekler"": [], ""olay_turleri"": [],
  ""kural_dosyalari"": [""kurallar.json""], ""eylem_dosyalari"": [""eylemler.json""], ""kriz_dosyalari"": [""krizler.json""]
}";

        // Ayşe yalnızca yalan söyleyebilir (tek eylem): dürüstlüğü yüksek olduğundan stres hızla artar.
        private const string Eylemler = @"{ ""eylemler"": [ {
  ""id"": ""yalan_soyle"", ""olay_turu"": ""anlatti"", ""hedef"": ""npc"", ""taban"": 1, ""aktarim"": ""yalan"",
  ""yalan_onermeleri"": [""degil(borclu(Mehmet,Ayse,500))""], ""celisen_ozellikler"": [""durustluk""], ""stres_tabani"": 70
}, {
  ""id"": ""anlat"", ""olay_turu"": ""anlatti"", ""hedef"": ""npc"", ""taban"": 0.1, ""aktarim"": ""dogru"", ""konu_desenleri"": [""borclu(*,*,*)""]
} ] }";

        private const string Kurallar = @"{ ""kurallar"": [ {
  ""id"": ""aile_iftirasi"", ""ne_zaman"": ""anlatti"",
  ""kriterler"": [ { ""alan"": ""olay_konusu(borclu(Mehmet, *, *))"", ""op"": ""=="", ""deger"": true } ],
  ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""aileme_iftira"", ""deger"": -22, ""sure"": 18 } ]
} ] }";

        private const string Krizler = @"{ ""krizler"": [
  { ""id"": ""itiraf"", ""seviye"": 1, ""olay_turu"": ""itiraf_etti"", ""hedef"": ""yalan_kurbani"",
    ""kriterler"": [ { ""alan"": ""kisilik(durustluk)"", ""op"": "">="", ""deger"": 0.7 } ] },
  { ""id"": ""sakinles"", ""seviye"": 1, ""olay_turu"": ""sakinlesti"", ""hedef"": ""yok"" },
  { ""id"": ""patla"", ""seviye"": 2, ""olay_turu"": ""patladi"", ""hedef"": ""en_soguk"" },
  { ""id"": ""kac"", ""seviye"": 2, ""olay_turu"": ""kacti"", ""hedef"": ""yok"" }
] }";

        private const string Borc = "borclu(Mehmet,Ayse,500)";

        private readonly string _dizin;

        public KonusmaKrizCliTests()
        {
            _dizin = Path.Combine(Path.GetTempPath(), "yanki-konusma-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dizin);
            File.WriteAllText(Yol("senaryo.json"), Senaryo);
            File.WriteAllText(Yol("eylemler.json"), Eylemler);
            File.WriteAllText(Yol("kurallar.json"), Kurallar);
            File.WriteAllText(Yol("krizler.json"), Krizler);
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

        private string OyuncuAnlatir(int tick = 6)
        {
            var oturum = Yol("oturum.json");
            var (kod, _, hata) = Calistir("run", Yol("senaryo.json"), "--seed", "7", "--eylem", "anlatti:Ayse:onerme=" + Borc, "--tick", tick.ToString(), "--kaydet", oturum);
            Assert.True(kod == 0, hata);
            return oturum;
        }

        private static long Son(string cikti, string tur) =>
            long.Parse(Regex.Matches(cikti, @"^#(\d+) " + Regex.Escape(tur) + @" ", RegexOptions.Multiline).Last().Groups[1].Value);

        [Fact]
        public void Run_oyuncu_anlatimini_onerme_virgul_icerse_de_eylem_olarak_alir()
        {
            var (kod, cikti, hata) = Calistir("run", Yol("senaryo.json"), "--seed", "7", "--eylem", "anlatti:Ayse:onerme=" + Borc, "--tick", "1");

            Assert.True(kod == 0, hata);
            Assert.Contains("anlatti", cikti);
            Assert.Contains("kural player", cikti);
            Assert.Contains("kanit_eklendi", cikti);
        }

        [Fact]
        public void Run_gecersiz_anlatimi_acik_mesajla_reddeder()
        {
            var (kod, _, hata) = Calistir("run", Yol("senaryo.json"), "--eylem", "anlatti:Yok:onerme=" + Borc, "--tick", "1");

            Assert.Equal(2, kod);
            Assert.Contains("Eylem reddedildi", hata);
        }

        [Fact]
        public void Run_birden_cok_yuk_anahtarini_ayirir()
        {
            var (kod, cikti, hata) = Calistir("run", Yol("senaryo.json"), "--seed", "7", "--eylem", "anlatti:Ayse:onerme=" + Borc + ",gorunurluk=acik", "--tick", "1");

            Assert.True(kod == 0, hata);
            Assert.Contains("gorunurluk=acik", cikti);
        }

        [Fact]
        public void Konusma_ve_kriz_run_sonunda_replay_hash_i_tutar()
        {
            var oturum = OyuncuAnlatir(12);

            var (kod, cikti, hata) = Calistir("replay", oturum);

            Assert.True(kod == 0, hata + cikti);
        }

        [Fact]
        public void Query_stres_aktarim_kaydi_ve_kanit_kokunu_gosterir()
        {
            var oturum = OyuncuAnlatir(3);   // tick 2'de Ayşe yalan söyler; kriz tick 3

            var (kod, ayse, hata) = Calistir("query", oturum, "Ayse");
            Assert.True(kod == 0, hata);
            Assert.Contains("stres:", ayse);
            Assert.Contains("-- aktarımlar --", ayse);
            Assert.Contains("Can", ayse);
            Assert.Contains("yalan", ayse);
            Assert.Contains("kök oyuncu", ayse);   // Ayşe'nin kanıtının kökü

            var (kod2, can, _) = Calistir("query", oturum, "Can");
            Assert.Equal(0, kod2);
            Assert.DoesNotContain("-- aktarımlar --", can);   // yalan bilgisi yalnızca konuşanın zihninde
            Assert.Contains("← Ayse", can);
        }

        [Fact]
        public void Why_krizden_stres_artisina_yalana_ve_karar_izine_iner()
        {
            var oturum = OyuncuAnlatir(4);
            var (_, run, _) = Calistir("run", Yol("senaryo.json"), "--seed", "7", "--eylem", "anlatti:Ayse:onerme=" + Borc, "--tick", "4");
            var kriz = Son(run, "itiraf_etti");

            var (kod, cikti, hata) = Calistir("why", oturum, kriz.ToString());

            Assert.True(kod == 0, hata);
            Assert.Contains("itiraf_etti", cikti);
            Assert.Contains("stres_degisti", cikti);
            Assert.Contains("eylem:yalan_soyle", cikti);
            Assert.Contains("karar_verildi", cikti);
        }

        [Fact]
        public void Gecersiz_kriz_dosyasi_dosya_satir_kriz_id_mesaj_biciminde_reddedilir()
        {
            File.WriteAllText(Yol("krizler.json"), Krizler.Replace("\"seviye\": 2, \"olay_turu\": \"kacti\"", "\"seviye\": 9, \"olay_turu\": \"kacti\""));

            var (kod, _, hata) = Calistir("run", Yol("senaryo.json"), "--tick", "1");

            Assert.Equal(2, kod);
            Assert.Matches(@"krizler\.json:\d+: kriz ""kac"": ", hata);
        }

        [Fact]
        public void Eksik_yedek_satir_senaryo_yuklenirken_reddedilir()
        {
            File.WriteAllText(Yol("krizler.json"), "{ \"krizler\": [ { \"id\": \"sakinles\", \"seviye\": 1, \"olay_turu\": \"sakinlesti\", \"hedef\": \"yok\" } ] }");

            var (kod, _, hata) = Calistir("run", Yol("senaryo.json"), "--tick", "1");

            Assert.Equal(2, kod);
            Assert.Contains("seviye 2 için kriterli olmayan bir yedek satır yok", hata);
        }

        [Fact]
        public void Kriz_dosyasi_olmayan_senaryo_eskisi_gibi_calisir()
        {
            File.WriteAllText(Yol("senaryo.json"), Senaryo.Replace(", \"kriz_dosyalari\": [\"krizler.json\"]", string.Empty));

            var (kod, _, hata) = Calistir("run", Yol("senaryo.json"), "--tick", "3");

            Assert.True(kod == 0, hata);
        }

        [Fact]
        public void Bulunamayan_kriz_dosyasi_hata_verir()
        {
            File.Delete(Yol("krizler.json"));

            var (kod, _, hata) = Calistir("run", Yol("senaryo.json"), "--tick", "1");

            Assert.Equal(2, kod);
            Assert.Contains("kriz dosyası bulunamadı", hata);
        }
    }
}
