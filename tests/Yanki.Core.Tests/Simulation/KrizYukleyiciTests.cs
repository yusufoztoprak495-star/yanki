using System.Linq;
using Xunit;
using Yanki.Core.Simulation;

namespace Yanki.Core.Tests.Simulation
{
    /// <summary>Kriz tablosu şeması ve doğrulayıcısı (6. hafta, Teslim 8): <c>dosya:satir: kriz "id": mesaj</c>.</summary>
    public class KrizYukleyiciTests
    {
        internal const string Gecerli = @"{ ""krizler"": [
  { ""id"": ""itiraf"", ""seviye"": 1, ""olay_turu"": ""itiraf_etti"", ""hedef"": ""yalan_kurbani"",
    ""kriterler"": [ { ""alan"": ""kisilik(durustluk)"", ""op"": "">="", ""deger"": 0.7 } ],
    ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.9 } ] },
  { ""id"": ""sakinles"", ""seviye"": 1, ""olay_turu"": ""sakinlesti"", ""hedef"": ""yok"" },
  { ""id"": ""patla"", ""seviye"": 2, ""olay_turu"": ""patladi"", ""hedef"": ""en_soguk"",
    ""kriterler"": [ { ""alan"": ""kisilik(kin_tutma)"", ""op"": "">="", ""deger"": 0.7 } ] },
  { ""id"": ""kac"", ""seviye"": 2, ""olay_turu"": ""kacti"", ""hedef"": ""yok"" }
] }";

        private static KrizYuklemeSonucu Yukle(string json, int seviye = 2) => KrizYukleyici.Yukle(json, "krizler.json", seviye);

        private static void HataVar(string json, string parca, int seviye = 2)
        {
            var s = Yukle(json, seviye);
            Assert.False(s.Basarili, "Hata bekleniyordu: " + parca);
            Assert.Contains(s.Hatalar, h => h.ToString().Contains(parca));
        }

        private static string Satir(string id = "a", string seviye = "1", string olay = "\"kriz_oldu\"", string hedef = "\"yok\"", string ek = "") =>
            "{ \"id\": \"" + id + "\", \"seviye\": " + seviye + ", \"olay_turu\": " + olay + ", \"hedef\": " + hedef + (ek.Length > 0 ? ", " + ek : string.Empty) + " }";

        private static string Dosya(params string[] satirlar) => "{ \"krizler\": [ " + string.Join(", ", satirlar) + " ] }";

        // her iki seviyenin yedek satırları
        private static readonly string Yedekler = Satir("y1", "1", "\"y1_oldu\"") + ", " + Satir("y2", "2", "\"y2_oldu\"");

        [Fact]
        public void Gecerli_dosya_okunur_kimlige_gore_siralanir()
        {
            var s = Yukle(Gecerli);
            Assert.True(s.Basarili, string.Join("\n", s.Hatalar));
            Assert.Equal(new[] { "itiraf", "kac", "patla", "sakinles" }, s.Satirlar.Select(x => x.Id).ToArray());

            var itiraf = s.Satirlar.Single(x => x.Id == "itiraf");
            Assert.Equal(1, itiraf.Seviye);
            Assert.Equal("itiraf_etti", itiraf.OlayTuru);
            Assert.Equal(KrizHedefTuru.YalanKurbani, itiraf.Hedef);
            Assert.Equal("kriz:itiraf", itiraf.KuralId);
            Assert.True(itiraf.Kriterler.All(k => k.Zorunlu));   // ADR-010: hepsi tutmalı
            Assert.Single(itiraf.Etkiler);
            Assert.False(itiraf.Yedek);
            Assert.True(s.Satirlar.Single(x => x.Id == "kac").Yedek);
        }

        [Fact]
        public void Yedek_satirlar_hedefsiz_oldugundan_uyari_yoktur()
        {
            Assert.Empty(Yukle(Gecerli).Uyarilar);
        }

        [Fact]
        public void Hata_biclimi_dosya_satir_kriz_id_mesajdir()
        {
            var s = Yukle(Dosya(Satir("a", "9") + ", " + Yedekler));
            Assert.Contains(s.Hatalar, h => h.ToString().StartsWith("krizler.json:") && h.ToString().Contains("kriz \"a\":"));
        }

        [Theory]
        [InlineData("0")]
        [InlineData("3")]
        [InlineData("1.5")]
        [InlineData("\"1\"")]
        [InlineData("-1")]
        public void Gecersiz_seviye_reddedilir(string seviye) =>
            HataVar(Dosya(Satir("a", seviye) + ", " + Yedekler), "seviye");

        [Fact]
        public void Seviye_alani_zorunludur() =>
            HataVar(Dosya("{ \"id\": \"a\", \"olay_turu\": \"x_oldu\", \"hedef\": \"yok\" }, " + Yedekler), "seviye");

        [Fact]
        public void Seviye_siniri_esik_sayisina_baglidir()
        {
            var tek = Dosya(Satir("y1", "1", "\"y1_oldu\""));
            Assert.True(Yukle(tek, 1).Basarili);
            HataVar(Dosya(Satir("y1", "1", "\"y1_oldu\"") + ", " + Satir("a", "2")), "seviye", 1);
        }

        [Theory]
        [InlineData("\"yok_boyle\"")]
        [InlineData("5")]
        public void Gecersiz_hedef_reddedilir(string hedef) =>
            HataVar(Dosya(Satir("a", "1", "\"x_oldu\"", hedef) + ", " + Yedekler), "hedef");

        [Theory]
        [InlineData("\"anlatti\"")]
        [InlineData("\"ayrildi\"")]
        [InlineData("\"stres_degisti\"")]
        [InlineData("\"karar_verildi\"")]
        [InlineData("\"3x\"")]
        [InlineData("5")]
        public void Gecersiz_ya_da_ayrilmis_olay_turu_reddedilir(string olay) =>
            HataVar(Dosya(Satir("a", "1", olay) + ", " + Yedekler), "olay_turu");

        [Fact]
        public void Bilinmeyen_alan_ve_tekrar_eden_kimlik_reddedilir()
        {
            HataVar(Dosya(Satir("a", "1", "\"x_oldu\"", "\"yok\"", "\"foo\": 1") + ", " + Yedekler), "bilinmeyen alan");
            HataVar(Dosya(Satir("a") + ", " + Satir("a") + ", " + Yedekler), "tekrar");
        }

        [Fact]
        public void Gecersiz_kriter_ve_etki_reddedilir()
        {
            HataVar(Dosya(Satir("a", "1", "\"x_oldu\"", "\"yok\"", "\"kriterler\": [ { \"alan\": \"bilinmeyen(x)\", \"op\": \"<\", \"deger\": 1 } ]") + ", " + Yedekler), "kriter #1");
            HataVar(Dosya(Satir("a", "1", "\"x_oldu\"", "\"yok\"", "\"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 5 } ]") + ", " + Yedekler), "onem");
        }

        [Fact]
        public void Eylem_hedef_yalnizca_hedefli_satirda_gecerlidir()
        {
            const string kriter = "\"kriterler\": [ { \"alan\": \"fikir(eylem.hedef)\", \"op\": \"<\", \"deger\": 0 } ]";
            HataVar(Dosya(Satir("a", "1", "\"x_oldu\"", "\"yok\"", kriter) + ", " + Yedekler), "eylem.hedef");
            Assert.True(Yukle(Dosya(Satir("a", "1", "\"x_oldu\"", "\"en_yakin\"", kriter) + ", " + Yedekler)).Basarili);
        }

        [Theory]
        [InlineData("olay_rolu(fail)")]
        [InlineData("gercek(konum!(Ali, han))")]
        [InlineData("olay_konusu(borclu(*,*,*))")]
        [InlineData("konu_puani(oyuncu)")]
        [InlineData("yay(x)")]
        public void Kriz_baglaminda_tanimsiz_sorgular_reddedilir(string alan) =>
            HataVar(Dosya(Satir("a", "1", "\"x_oldu\"", "\"yok\"", "\"kriterler\": [ { \"alan\": \"" + alan + "\", \"op\": \"==\", \"deger\": true } ]") + ", " + Yedekler), "kriter #1");

        [Fact]
        public void Her_seviye_icin_kriterli_olmayan_yedek_satir_zorunludur()
        {
            const string kriterli = "\"kriterler\": [ { \"alan\": \"kisilik(cesaret)\", \"op\": \">\", \"deger\": 0.5 } ]";
            var s = Yukle(Dosya(Satir("y1", "1", "\"y1_oldu\"") + ", " + Satir("a", "2", "\"x_oldu\"", "\"yok\"", kriterli)));
            Assert.False(s.Basarili);
            Assert.Contains(s.Hatalar, h => h.ToString().Contains("seviye 2 için kriterli olmayan bir yedek satır yok"));
            Assert.DoesNotContain(s.Hatalar, h => h.ToString().Contains("seviye 1"));
        }

        [Fact]
        public void Yedek_denetimi_birden_cok_dosyada_birlesik_yapilabilir()
        {
            var bir = KrizYukleyici.Yukle(Dosya(Satir("y1", "1", "\"y1_oldu\"")), "a.json", 2, yedekDenetle: false);
            var iki = KrizYukleyici.Yukle(Dosya(Satir("y2", "2", "\"y2_oldu\"")), "b.json", 2, yedekDenetle: false);
            Assert.True(bir.Basarili && iki.Basarili);

            Assert.Empty(KrizYukleyici.YedekHatalari(bir.Satirlar.Concat(iki.Satirlar), 2, "senaryo"));
            Assert.Single(KrizYukleyici.YedekHatalari(bir.Satirlar, 2, "senaryo"));
        }

        [Fact]
        public void Hedefli_kriterli_olmayan_satir_yedek_sayilmaz()
        {
            var s = Yukle(Dosya(Satir("y1", "1", "\"y1_oldu\"", "\"en_yakin\"") + ", " + Satir("y2", "2", "\"y2_oldu\"")));
            Assert.False(s.Basarili);
            Assert.Contains(s.Hatalar, h => h.ToString().Contains("seviye 1 için kriterli olmayan bir yedek satır yok"));
            Assert.Contains(s.Uyarilar, u => u.ToString().Contains("YEDEK sayılmaz"));
        }

        [Fact]
        public void Kok_bicimi_hatalari()
        {
            HataVar("{ }", "krizler");
            HataVar("{ \"krizler\": 5 }", "krizler");
            HataVar("[]", "krizler");
            HataVar("{ \"krizler\": [], \"x\": 1 }", "bilinmeyen alan");
            HataVar("{", "JSON okunamadı");
        }
    }
}
