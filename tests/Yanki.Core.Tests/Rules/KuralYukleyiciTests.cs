using System.Linq;
using Xunit;
using Yanki.Core.Criteria;
using Yanki.Core.Rules;

namespace Yanki.Core.Tests.Rules
{
    public class KuralYukleyiciTests
    {
        private static readonly string[] Turler = { "yardim_etti", "tehdit_etti" };

        private const string Gecerli = @"{
  ""kurallar"": [
    {
      ""id"": ""yardim_minneti"",
      ""ne_zaman"": ""yardim_etti"",
      ""kriterler"": [
        { ""alan"": ""ayni_mekanda(olay.fail)"", ""op"": ""=="", ""deger"": true, ""zorunlu"": true },
        { ""alan"": ""fikir(olay.fail)"", ""op"": ""<"", ""deger"": 50, ""agirlik"": 2 }
      ],
      ""etkiler"": [
        { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""minnet"", ""deger"": 15, ""sure"": 336 },
        { ""tur"": ""eksen"", ""hedef"": ""olay.fail"", ""eksen"": ""guven"", ""delta"": 5 },
        { ""tur"": ""duygu"", ""duygu"": ""minnet"", ""siddet"": 0.5, ""hedef"": ""olay.fail"" },
        { ""tur"": ""hafiza"", ""onem"": 0.8 },
        { ""tur"": ""gercek"", ""islem"": ""ekle"", ""gercek"": ""yardim_gordu({ben}, {olay.fail})"" }
      ]
    },
    {
      ""id"": ""a_kural"",
      ""ne_zaman"": ""tehdit_etti"",
      ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.2 } ]
    }
  ]
}";

        private static KuralYuklemeSonucu Yukle(string json) => KuralYukleyici.Yukle(json, "kurallar.json", Turler);

        private static string Tek(string kuralGovdesi) =>
            "{ \"kurallar\": [ { \"id\": \"k1\", \"ne_zaman\": \"yardim_etti\", " + kuralGovdesi + " } ] }";

        private static void HataVar(string json, string parca)
        {
            var sonuc = Yukle(json);
            Assert.False(sonuc.Basarili);
            Assert.Contains(sonuc.Hatalar, h => h.ToString().Contains(parca));
        }

        [Fact]
        public void Gecerli_kurallar_okunur()
        {
            var sonuc = Yukle(Gecerli);

            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            Assert.Equal(2, sonuc.Kurallar.Count);
            var k = sonuc.Kurallar.Single(x => x.Id == "yardim_minneti");
            Assert.Equal("yardim_etti", k.NeZaman);
            Assert.Equal(2, k.Kriterler.Count);
            Assert.True(k.Kriterler[0].Zorunlu);
            Assert.Equal(2m, k.Kriterler[1].Agirlik);
            Assert.Equal(
                new[] { EtkiTuru.Modifier, EtkiTuru.Eksen, EtkiTuru.Duygu, EtkiTuru.Hafiza, EtkiTuru.Gercek },
                k.Etkiler.Select(e => e.Tur).ToArray());
            Assert.Equal("minnet", k.Etkiler[0].Ad);
            Assert.Equal(15m, k.Etkiler[0].Deger);
            Assert.Equal(336, k.Etkiler[0].Sure);
            Assert.Equal("olay.fail", k.Etkiler[0].Hedef);
            Assert.Equal(EtkiTuru.Hafiza, k.Etkiler[3].Tur);
            Assert.False(k.Etkiler[4].Kaldir);
        }

        [Fact]
        public void Kurallar_kimlige_gore_ordinal_siralanir()
        {
            var sonuc = Yukle(Gecerli);
            Assert.Equal(new[] { "a_kural", "yardim_minneti" }, sonuc.Kurallar.Select(k => k.Id).ToArray());
        }

        [Fact]
        public void Gerekli_roller_kriter_ve_etki_hedeflerinden_toplanir()
        {
            var k = Yukle(Gecerli).Kurallar.Single(x => x.Id == "yardim_minneti");
            Assert.Equal(new[] { Rol.Ben, Rol.OlayFail }, k.GerekliRoller.ToArray());
        }

        [Fact]
        public void Kriter_listesi_olmayan_kural_gecerlidir()
        {
            var sonuc = Yukle(Tek("\"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.3 } ]"));
            Assert.True(sonuc.Basarili);
            Assert.Empty(sonuc.Kurallar[0].Kriterler);
        }

        [Fact]
        public void Hata_mesaji_dosya_satir_ve_kural_kimligini_icerir()
        {
            var json = "{\n  \"kurallar\": [\n    {\n      \"id\": \"k1\",\n      \"ne_zaman\": \"yardim_etti\",\n      \"etkiler\": []\n    }\n  ]\n}";
            var sonuc = Yukle(json);
            var hata = Assert.Single(sonuc.Hatalar);
            Assert.Equal("kurallar.json", hata.Kaynak);
            Assert.Equal(6, hata.Satir);
            Assert.Equal("k1", hata.KuralId);
            Assert.StartsWith("kurallar.json:6: kural \"k1\":", hata.ToString());
        }

        [Fact]
        public void Bozuk_json_tek_hata_olarak_satiriyla_doner()
        {
            var sonuc = Yukle("{\n  \"kurallar\": [\n");
            var hata = Assert.Single(sonuc.Hatalar);
            Assert.True(hata.Satir >= 2);
            Assert.Empty(sonuc.Kurallar);
        }

        [Fact]
        public void Kok_nesne_degilse_ya_da_kurallar_dizisi_yoksa_reddeder()
        {
            HataVar("[]", "kurallar");
            HataVar("{ \"baska\": 1 }", "kurallar");
            HataVar("{ \"kurallar\": {} }", "dizi");
        }

        [Fact]
        public void Etkisi_bos_kural_reddedilir()
        {
            HataVar(Tek("\"etkiler\": []"), "etki");
            HataVar(Tek("\"kriterler\": []"), "etki");
        }

        [Fact]
        public void Bilinmeyen_olay_turu_reddedilir()
        {
            HataVar("{ \"kurallar\": [ { \"id\": \"k1\", \"ne_zaman\": \"yok_boyle_olay\", \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.3 } ] } ] }",
                "yok_boyle_olay");
        }

        [Fact]
        public void Yerlesik_olay_turleri_kabul_edilir()
        {
            var json = "{ \"kurallar\": [ { \"id\": \"k1\", \"ne_zaman\": \"modifier_eklendi\", \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.3 } ] } ] }";
            Assert.True(Yukle(json).Basarili);
        }

        [Fact]
        public void Gecersiz_ya_da_tekrar_eden_kimlik_reddedilir()
        {
            HataVar("{ \"kurallar\": [ { \"id\": \"Türkçe\", \"ne_zaman\": \"yardim_etti\", \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.3 } ] } ] }", "kimlik");
            HataVar("{ \"kurallar\": [ { \"ne_zaman\": \"yardim_etti\", \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.3 } ] } ] }", "\"id\"");
            const string k = "{ \"id\": \"k1\", \"ne_zaman\": \"yardim_etti\", \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.3 } ] }";
            HataVar("{ \"kurallar\": [ " + k + ", " + k + " ] }", "tekrar");
        }

        [Fact]
        public void Bilinmeyen_alan_sema_hatasidir()
        {
            HataVar(Tek("\"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.3 } ], \"cooldown\": 5"), "cooldown");
            HataVar(Tek("\"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.3, \"fazla\": 1 } ]"), "fazla");
        }

        [Theory]
        [InlineData("{ \"alan\": \"fikir(\", \"op\": \"<\", \"deger\": 1 }", "kriter")]
        [InlineData("{ \"alan\": \"yok_fonksiyon(ben)\", \"op\": \"<\", \"deger\": 1 }", "yok_fonksiyon")]
        [InlineData("{ \"alan\": \"fikir(olay.fail)\", \"op\": \"~\", \"deger\": 1 }", "operatör")]
        [InlineData("{ \"alan\": \"fikir(olay.fail)\", \"op\": \"<\", \"deger\": \"metin\" }", "sayı")]
        [InlineData("{ \"alan\": \"fikir(olay.fail)\", \"op\": \"aralik\", \"deger\": [5, 1] }", "Aralığın")]
        [InlineData("{ \"alan\": \"fikir(olay.fail)\", \"deger\": 1 }", "\"op\"")]
        [InlineData("{ \"alan\": \"fikir(olay.fail)\", \"op\": \"<\", \"deger\": 1, \"agirlik\": \"x\" }", "agirlik")]
        [InlineData("{ \"alan\": \"fikir(olay.fail)\", \"op\": \"<\", \"deger\": 1, \"zorunlu\": 1 }", "zorunlu")]
        public void Gecersiz_kriter_reddedilir(string kriter, string parca)
        {
            HataVar(Tek("\"kriterler\": [ " + kriter + " ], \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.3 } ]"), parca);
        }

        [Fact]
        public void Aralik_degeri_okunur()
        {
            var sonuc = Yukle(Tek("\"kriterler\": [ { \"alan\": \"stres()\", \"op\": \"aralik\", \"deger\": [10, 40] } ], \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.3 } ]"));
            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            Assert.Equal(KriterOperatoru.Aralik, sonuc.Kurallar[0].Kriterler[0].Op);
        }

        [Theory]
        [InlineData("{ \"alan\": \"fikir(konusan)\", \"op\": \"<\", \"deger\": 1 }", "konusan")]
        [InlineData("{ \"alan\": \"yalan_soyledi(olay.fail)\", \"op\": \"==\", \"deger\": true }", "yalan_soyledi")]
        [InlineData("{ \"alan\": \"yay(gerginlik)\", \"op\": \">\", \"deger\": 1 }", "yay")]
        public void Kural_baglaminda_cozulemeyen_kriterler_reddedilir(string kriter, string parca)
        {
            HataVar(Tek("\"kriterler\": [ " + kriter + " ], \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.3 } ]"), parca);
        }

        [Theory]
        [InlineData("{ \"tur\": \"ucus\" }", "ucus")]
        [InlineData("{ \"tur\": \"modifier\", \"hedef\": \"olay.fail\", \"modifier\": \"minnet\", \"deger\": 15 }", "sure")]
        [InlineData("{ \"tur\": \"modifier\", \"hedef\": \"olay.fail\", \"modifier\": \"minnet\", \"deger\": 15, \"sure\": 0 }", "sure")]
        [InlineData("{ \"tur\": \"modifier\", \"hedef\": \"olay.fail\", \"modifier\": \"Türkçe\", \"deger\": 15, \"sure\": 5 }", "modifier")]
        [InlineData("{ \"tur\": \"eksen\", \"hedef\": \"olay.fail\", \"eksen\": \"saygi\", \"delta\": 1 }", "saygi")]
        [InlineData("{ \"tur\": \"eksen\", \"eksen\": \"guven\", \"delta\": 1 }", "hedef")]
        [InlineData("{ \"tur\": \"eksen\", \"hedef\": \"konusan\", \"eksen\": \"guven\", \"delta\": 1 }", "konusan")]
        [InlineData("{ \"tur\": \"eksen\", \"hedef\": \"Türkçe\", \"eksen\": \"guven\", \"delta\": 1 }", "Türkçe")]
        [InlineData("{ \"tur\": \"duygu\", \"duygu\": \"ofke\", \"siddet\": 1.5 }", "siddet")]
        [InlineData("{ \"tur\": \"hafiza\", \"onem\": -0.1 }", "onem")]
        [InlineData("{ \"tur\": \"hafiza\" }", "onem")]
        [InlineData("{ \"tur\": \"gercek\", \"islem\": \"degistir\", \"gercek\": \"a(b)\" }", "islem")]
        [InlineData("{ \"tur\": \"gercek\", \"islem\": \"ekle\", \"gercek\": \"a(b\" }", "gercek")]
        [InlineData("{ \"tur\": \"gercek\", \"islem\": \"ekle\", \"gercek\": \"a({yok})\" }", "yok")]
        public void Gecersiz_etki_reddedilir(string etki, string parca)
        {
            HataVar(Tek("\"etkiler\": [ " + etki + " ]"), parca);
        }

        [Fact]
        public void Agirlik_kullanan_kural_icin_uyari_verir_ama_yuklenir()
        {
            var sonuc = Yukle(Gecerli);

            Assert.True(sonuc.Basarili);
            var uyari = Assert.Single(sonuc.Uyarilar);
            Assert.Equal("yardim_minneti", uyari.KuralId);
            Assert.Contains("agirlik", uyari.Mesaj);
            Assert.StartsWith("kurallar.json:", uyari.ToString());
        }

        [Fact]
        public void Zorunlu_false_olan_kriter_icin_uyari_verir_ama_yuklenir()
        {
            var sonuc = Yukle(Tek("\"kriterler\": [ { \"alan\": \"olay_rolu(hedef)\", \"op\": \"==\", \"deger\": true, \"zorunlu\": false } ], \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.3 } ]"));

            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            Assert.Single(sonuc.Kurallar);
            var uyari = Assert.Single(sonuc.Uyarilar);
            Assert.Equal("k1", uyari.KuralId);
            Assert.Contains("zorunlu", uyari.Mesaj);
            Assert.Contains("zaten zorunlu", uyari.Mesaj);
        }

        [Fact]
        public void Zorunlu_true_uyari_vermez()
        {
            var sonuc = Yukle(Tek("\"kriterler\": [ { \"alan\": \"olay_rolu(hedef)\", \"op\": \"==\", \"deger\": true, \"zorunlu\": true } ], \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.3 } ]"));
            Assert.Empty(sonuc.Uyarilar);
        }

        [Fact]
        public void Agirliksiz_kural_uyari_vermez()
        {
            var sonuc = Yukle(Tek("\"kriterler\": [ { \"alan\": \"olay_rolu(hedef)\", \"op\": \"==\", \"deger\": true } ], \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.3 } ]"));
            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            Assert.Empty(sonuc.Uyarilar);
        }

        [Theory]
        [InlineData("olay_rolu(baska)")]
        [InlineData("olay_rolu(oyuncu)")]
        [InlineData("olay_rolu()")]
        public void Olay_rolu_yalnizca_fail_hedef_tanik_alir(string alan)
        {
            HataVar(Tek("\"kriterler\": [ { \"alan\": \"" + alan + "\", \"op\": \"==\", \"deger\": true } ], \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.3 } ]"), "olay_rolu");
        }

        [Fact]
        public void Birden_cok_hata_toplanir()
        {
            const string json = "{ \"kurallar\": [ " +
                "{ \"id\": \"k1\", \"ne_zaman\": \"yok1\", \"etkiler\": [] }, " +
                "{ \"id\": \"k2\", \"ne_zaman\": \"yok2\", \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.3 } ] } ] }";
            var sonuc = Yukle(json);
            Assert.True(sonuc.Hatalar.Count >= 3);
            Assert.Contains(sonuc.Hatalar, h => h.KuralId == "k1");
            Assert.Contains(sonuc.Hatalar, h => h.KuralId == "k2");
        }

        [Fact]
        public void Yukle_basarisizsa_kurallar_bos_doner()
        {
            var sonuc = Yukle(Tek("\"etkiler\": []"));
            Assert.Empty(sonuc.Kurallar);
        }
    }
}
