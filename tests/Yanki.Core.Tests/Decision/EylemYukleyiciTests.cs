using System.Linq;
using Xunit;
using Yanki.Core.Decision;
using Yanki.Core.Rules;

namespace Yanki.Core.Tests.Decision
{
    /// <summary>Eylem tanımı okuyucusu ve doğrulayıcısı (ADR-011). Hata biçimi: <c>dosya:satir: eylem "id": mesaj</c>.</summary>
    public class EylemYukleyiciTests
    {
        private const string Gecerli = @"{
  ""eylemler"": [
    {
      ""id"": ""yardim_et"",
      ""olay_turu"": ""yardim_etti"",
      ""hedef"": ""npc"",
      ""taban"": 1,
      ""on_kosullar"": [
        { ""alan"": ""ayni_mekanda(eylem.hedef)"", ""op"": ""=="", ""deger"": true },
        { ""alan"": ""hafiza_var(yardim_etti, ben, 24)"", ""op"": ""=="", ""deger"": false }
      ],
      ""olcutler"": [
        { ""alan"": ""fikir(eylem.hedef)"", ""min"": -100, ""max"": 100, ""egri"": { ""tur"": ""lojistik"", ""k"": 6, ""m"": 0.5 } },
        { ""alan"": ""kisilik(merhamet)"", ""egri"": { ""tur"": ""ustel"", ""k"": 2 } }
      ],
      ""kisilik_carpani"": { ""ozellikler"": [ { ""ozellik"": ""merhamet"", ""agirlik"": 2 }, { ""ozellik"": ""acgozluluk"", ""ters"": true } ] },
      ""fikir_carpani"": { ""yon"": ""artan"" },
      ""ruh_hali_carpani"": { ""duygular"": [ { ""duygu"": ""korku"", ""ters"": true } ] },
      ""celisen_ozellikler"": [ ""acgozluluk"" ], ""stres_tabani"": 10,
      ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.6 } ]
    },
    {
      ""id"": ""git"",
      ""hedef"": ""komsu_mekan"",
      ""taban"": 0.5,
      ""olcutler"": [ { ""alan"": ""kisilik(cesaret)"" } ]
    }
  ]
}";

        private static EylemYuklemeSonucu Yukle(string json) => EylemYukleyici.Yukle(json, "eylem.json");

        // Tek eylemli dosya; gövde "id" ve ek alanları içerir.
        private static string Tek(string govde) => "{ \"eylemler\": [ { " + govde + " } ] }";

        private const string Temel = "\"id\": \"e1\", \"olay_turu\": \"selam_verdi\", \"hedef\": \"yok\", \"taban\": 1, \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.5 } ]";

        private static string TemelIle(string ek) => Tek(Temel + ", " + ek);

        private static string TemelDegisik(string eski, string yeni) => Tek(Temel.Replace(eski, yeni));

        private static void HataVar(string json, string parca)
        {
            var sonuc = Yukle(json);
            Assert.False(sonuc.Basarili, "Hata bekleniyordu: " + parca);
            Assert.Contains(sonuc.Hatalar, h => h.ToString().Contains(parca));
        }

        private static void Gecer(string json)
        {
            var sonuc = Yukle(json);
            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
        }

        // ---- geçerli okuma ----

        [Fact]
        public void Gecerli_dosya_okunur()
        {
            var sonuc = Yukle(Gecerli);

            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            Assert.Equal(new[] { "git", "yardim_et" }, sonuc.Eylemler.Select(e => e.Id).ToArray());

            var e = sonuc.Eylemler.Single(x => x.Id == "yardim_et");
            Assert.Equal("yardim_etti", e.OlayTuru);
            Assert.Equal(EylemHedefTuru.Npc, e.Hedef);
            Assert.False(e.Hareket);
            Assert.Equal(1m, e.Taban);
            Assert.Equal(2, e.OnKosullar.Count);
            Assert.Equal(2, e.Olcutler.Count);
            Assert.Equal("fikir(eylem.hedef)", e.Olcutler[0].Alan.ToString());
            Assert.Equal(-100m, e.Olcutler[0].Min);
            Assert.Equal(100m, e.Olcutler[0].Max);
            Assert.Equal(EgriTuru.Lojistik, e.Olcutler[0].Egri.Tur);
            Assert.Equal(6m, e.Olcutler[0].Egri.K);
            Assert.Equal(0.5m, e.Olcutler[0].Egri.M);
            Assert.Equal(0m, e.Olcutler[1].Min);
            Assert.Equal(1m, e.Olcutler[1].Max);
            Assert.Equal(EgriTuru.Ustel, e.Olcutler[1].Egri.Tur);
            Assert.Equal(2m, e.KisilikCarpani!.Ozellikler[0].Agirlik);
            Assert.False(e.KisilikCarpani.Ozellikler[0].Ters);
            Assert.Equal(1m, e.KisilikCarpani.Ozellikler[1].Agirlik);
            Assert.True(e.KisilikCarpani.Ozellikler[1].Ters);
            Assert.Equal(FikirYonu.Artan, e.FikirCarpani!.Yon);
            Assert.True(e.RuhHaliCarpani!.Duygular[0].Ters);
            Assert.Equal(new[] { "acgozluluk" }, e.CelisenOzellikler.ToArray());
            Assert.Equal(EtkiTuru.Hafiza, Assert.Single(e.Etkiler).Tur);
        }

        [Fact]
        public void Hareket_eylemi_olay_turu_ve_etkisiz_gecerlidir()
        {
            var g = Yukle(Gecerli).Eylemler.Single(x => x.Id == "git");

            Assert.True(g.Hareket);
            Assert.Equal(EylemHedefTuru.KomsuMekan, g.Hedef);
            Assert.Equal(string.Empty, g.OlayTuru);
            Assert.Empty(g.Etkiler);
            Assert.Null(g.FikirCarpani);
            Assert.Null(g.KisilikCarpani);
            Assert.Null(g.RuhHaliCarpani);
            Assert.Empty(g.CelisenOzellikler);
            Assert.Equal(EgriTuru.Dogrusal, g.Olcutler[0].Egri.Tur);
        }

        [Fact]
        public void Eylemler_kimlige_gore_ordinal_siralidir()
        {
            var json = "{ \"eylemler\": [ { \"id\": \"z\", \"olay_turu\": \"a_t\", \"hedef\": \"yok\", \"taban\": 1, \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.5 } ] },"
                + " { \"id\": \"B\", \"olay_turu\": \"b_t\", \"hedef\": \"yok\", \"taban\": 1, \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.5 } ] } ] }";

            Assert.Equal(new[] { "B", "z" }, Yukle(json).Eylemler.Select(e => e.Id).ToArray());
        }

        [Fact]
        public void Olcutsuz_eylem_gecerlidir() => Gecer(Tek(Temel));

        // ---- hata biçimi ----

        [Fact]
        public void Hata_bicimi_dosya_satir_eylem_id_mesaj()
        {
            var json = "{\n  \"eylemler\": [\n    {\n      \"id\": \"e1\",\n      \"olay_turu\": \"x_t\",\n      \"hedef\": \"yok\",\n      \"taban\": 1,\n      \"etkiler\": []\n    }\n  ]\n}";

            var hata = Assert.Single(Yukle(json).Hatalar);

            Assert.StartsWith("eylem.json:8: eylem \"e1\": ", hata.ToString());
            Assert.Contains("etkisi yok", hata.ToString());
        }

        [Fact]
        public void Tum_hatalar_toplanir()
        {
            var json = "{ \"eylemler\": [ { \"id\": \"a\", \"olay_turu\": \"t1\", \"hedef\": \"yok\", \"taban\": 0, \"etkiler\": [] }, "
                + "{ \"id\": \"b\", \"olay_turu\": \"t2\", \"hedef\": \"kimse\", \"taban\": 1, \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.5 } ] } ] }";

            var sonuc = Yukle(json);

            Assert.True(sonuc.Hatalar.Count >= 3, string.Join("\n", sonuc.Hatalar));
            Assert.Empty(sonuc.Eylemler);
        }

        // ---- şema ----

        [Fact]
        public void Bozuk_json_okunamadi_hatasi_verir() => HataVar("{ \"eylemler\": [", "JSON okunamadı");

        [Fact]
        public void Kok_eylemler_dizisi_gerekir() => HataVar("{ }", "\"eylemler\" dizisi gerekli");

        [Fact]
        public void Kokte_bilinmeyen_alan_reddedilir() => HataVar("{ \"eylemler\": [], \"kurallar\": [] }", "bilinmeyen alan \"kurallar\"");

        [Fact]
        public void Eylem_nesne_olmali() => HataVar("{ \"eylemler\": [ 5 ] }", "Eylem bir JSON nesnesi olmalı");

        [Fact]
        public void Eylemde_bilinmeyen_alan_reddedilir() => HataVar(TemelIle("\"hiz\": 3"), "bilinmeyen alan \"hiz\"");

        [Theory]
        [InlineData("\"id\": \"e1\"", "\"id\": 5")]
        [InlineData("\"id\": \"e1\"", "\"id\": \"1abc\"")]
        [InlineData("\"id\": \"e1\"", "\"id\": \"a:b\"")]
        [InlineData("\"id\": \"e1\"", "\"id\": \"\"")]
        public void Gecersiz_id_reddedilir(string eski, string yeni) => HataVar(TemelDegisik(eski, yeni), "id");

        [Fact]
        public void Id_zorunlu() => HataVar(TemelDegisik("\"id\": \"e1\", ", string.Empty), "\"id\" alanı gerekli");

        [Fact]
        public void Yinelenen_kimlik_reddedilir()
        {
            var e = "{ \"id\": \"a\", \"olay_turu\": \"t_t\", \"hedef\": \"yok\", \"taban\": 1, \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.5 } ] }";

            HataVar("{ \"eylemler\": [ " + e + ", " + e + " ] }", "Eylem kimliği tekrar ediyor: \"a\"");
        }

        [Theory]
        [InlineData("\"hedef\": \"yok\"", "\"hedef\": \"kimse\"")]
        [InlineData("\"hedef\": \"yok\"", "\"hedef\": 3")]
        [InlineData("\"hedef\": \"yok\", ", "")]
        public void Hedef_turu_gecerli_olmali(string eski, string yeni) => HataVar(TemelDegisik(eski, yeni), "hedef");

        [Theory]
        [InlineData("yok")]
        [InlineData("npc")]
        [InlineData("oyuncu")]
        public void Hedef_turleri_kabul_edilir(string tur) => Gecer(TemelDegisik("\"hedef\": \"yok\"", "\"hedef\": \"" + tur + "\""));

        [Theory]
        [InlineData("\"taban\": 1", "\"taban\": 0")]
        [InlineData("\"taban\": 1", "\"taban\": -2")]
        [InlineData("\"taban\": 1", "\"taban\": \"bir\"")]
        [InlineData("\"taban\": 1", "\"taban\": 1001")]
        [InlineData("\"taban\": 1, ", "")]
        public void Taban_pozitif_sayi_olmali(string eski, string yeni) => HataVar(TemelDegisik(eski, yeni), "taban");

        // ---- olay türü ----

        [Fact]
        public void Olay_turu_zorunlu_hareket_disinda() => HataVar(TemelDegisik("\"olay_turu\": \"selam_verdi\", ", string.Empty), "\"olay_turu\" alanı gerekli");

        [Fact]
        public void Olay_turu_gecerli_kimlik_olmali() => HataVar(TemelDegisik("selam_verdi", "selam verdi"), "olay_turu");

        [Theory]
        [InlineData("ayrildi")]
        [InlineData("geldi")]
        [InlineData("karar_verildi")]
        [InlineData("hareket")]
        [InlineData("modifier_eklendi")]
        [InlineData("gercek_eklendi")]
        [InlineData("hafiza_eklendi")]
        public void Yerlesik_olay_turleri_eylem_olay_turu_olamaz(string tur) =>
            HataVar(TemelDegisik("selam_verdi", tur), "yerleşik");

        [Fact]
        public void Hareket_eyleminde_olay_turu_yazmak_hatadir() =>
            HataVar(Tek("\"id\": \"git\", \"olay_turu\": \"yurudu\", \"hedef\": \"komsu_mekan\", \"taban\": 1"), "komsu_mekan");

        // ---- etkiler ----

        [Fact]
        public void Etkiler_bos_olamaz() => HataVar(TemelDegisik("[ { \"tur\": \"hafiza\", \"onem\": 0.5 } ]", "[]"), "etkisi yok");

        [Fact]
        public void Etkiler_alani_zorunlu() => HataVar(TemelDegisik(", \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.5 } ]", string.Empty), "etkisi yok");

        [Fact]
        public void Etki_biçimi_kural_etkisiyle_ayni_dogrulanir()
        {
            HataVar(TemelDegisik("{ \"tur\": \"hafiza\", \"onem\": 0.5 }", "{ \"tur\": \"uc\" }"), "Etki #1: bilinmeyen etki türü \"uc\"");
            HataVar(TemelDegisik("{ \"tur\": \"hafiza\", \"onem\": 0.5 }", "{ \"tur\": \"hafiza\", \"onem\": 3 }"), "\"onem\" 0..1");
        }

        [Fact]
        public void Etkiler_tum_etki_turlerini_kabul_eder() =>
            Gecer(TemelDegisik("[ { \"tur\": \"hafiza\", \"onem\": 0.5 } ]",
                "[ { \"tur\": \"hafiza\", \"onem\": 0.5 }, { \"tur\": \"duygu\", \"duygu\": \"neseli\", \"siddet\": 0.4 },"
                + " { \"tur\": \"gercek\", \"islem\": \"ekle\", \"gercek\": \"selamladi({ben}, {oyuncu})\" },"
                + " { \"tur\": \"modifier\", \"hedef\": \"oyuncu\", \"modifier\": \"selam\", \"deger\": 3, \"sure\": 10 } ]"));

        [Fact]
        public void Hedefsiz_eylemde_etki_hedefi_eylem_hedef_olamaz() =>
            HataVar(TemelDegisik("{ \"tur\": \"hafiza\", \"onem\": 0.5 }",
                "{ \"tur\": \"eksen\", \"hedef\": \"eylem.hedef\", \"eksen\": \"guven\", \"delta\": 1 }"), "'eylem.hedef'");

        [Fact]
        public void Hedefli_eylemde_etki_hedefi_eylem_hedef_olabilir() =>
            Gecer(TemelDegisik("\"hedef\": \"yok\"", "\"hedef\": \"npc\"").Replace(
                "{ \"tur\": \"hafiza\", \"onem\": 0.5 }",
                "{ \"tur\": \"modifier\", \"hedef\": \"eylem.hedef\", \"modifier\": \"selam\", \"deger\": 2, \"sure\": 10 }"));

        [Theory]
        [InlineData("olay.fail")]
        [InlineData("olay.hedef")]
        public void Etki_hedefinde_olay_rolleri_reddedilir(string rol) =>
            HataVar(TemelDegisik("{ \"tur\": \"hafiza\", \"onem\": 0.5 }",
                "{ \"tur\": \"eksen\", \"hedef\": \"" + rol + "\", \"eksen\": \"guven\", \"delta\": 1 }"), "eylemlerde tanımsız");

        [Fact]
        public void Gercek_sablonunda_olay_rolleri_reddedilir() =>
            HataVar(TemelDegisik("{ \"tur\": \"hafiza\", \"onem\": 0.5 }",
                "{ \"tur\": \"gercek\", \"islem\": \"ekle\", \"gercek\": \"x({olay.fail})\" }"), "olay.fail");

        // ---- komsu_mekan: kişi bekleyen etkiler ----

        private static string Hareket(string ek) =>
            Tek("\"id\": \"git\", \"hedef\": \"komsu_mekan\", \"taban\": 1" + ek);

        [Theory]
        [InlineData("modifier", "\"hedef\": \"eylem.hedef\", \"modifier\": \"x\", \"deger\": 1, \"sure\": 5")]
        [InlineData("eksen", "\"hedef\": \"eylem.hedef\", \"eksen\": \"guven\", \"delta\": 1")]
        [InlineData("duygu", "\"duygu\": \"korku\", \"siddet\": 0.3, \"hedef\": \"eylem.hedef\"")]
        public void Komsu_mekan_icin_kisi_bekleyen_etkiler_reddedilir(string tur, string alanlar) =>
            HataVar(Hareket(", \"etkiler\": [ { \"tur\": \"" + tur + "\", " + alanlar + " } ]"), "kişi bekler");

        [Fact]
        public void Komsu_mekan_gercek_sablonunda_eylem_hedef_kullanabilir() =>
            Gecer(Hareket(", \"etkiler\": [ { \"tur\": \"gercek\", \"islem\": \"ekle\", \"gercek\": \"gitti({ben}, {eylem.hedef})\" } ]"));

        // ---- kriterler: eylem bağlamı ----

        private static string Kosul(string alan, string op = "==", string deger = "true", string hedef = "npc") =>
            TemelDegisik("\"hedef\": \"yok\"", "\"hedef\": \"" + hedef + "\"").Replace(
                "\"etkiler\"", "\"on_kosullar\": [ { \"alan\": \"" + alan + "\", \"op\": \"" + op + "\", \"deger\": " + deger + " } ], \"etkiler\"");

        [Theory]
        [InlineData("fikir(olay.fail)", "olay.fail")]
        [InlineData("fikir(olay.hedef)", "olay.hedef")]
        [InlineData("fikir(konusan)", "konusan")]
        [InlineData("fikir(dinleyen)", "dinleyen")]
        public void Eylem_baglaminda_gecersiz_roller_reddedilir(string alan, string rol) =>
            HataVar(Kosul(alan, "<", "5"), $"'{rol}' rolü eylemlerde tanımsız");

        [Fact]
        public void Olay_rolu_fonksiyonu_reddedilir() =>
            HataVar(Kosul("olay_rolu(fail)"), "'olay_rolu'");

        [Fact]
        public void Gercek_sorgusu_npc_zihnini_astigi_icin_reddedilir() =>
            HataVar(Kosul("gercek(konum!(Ali, han))"), "'gercek'");

        [Theory]
        [InlineData("yay(gerginlik)", "<", "5", "yay")]
        public void Bagli_olmayan_fonksiyonlar_acik_mesajla_reddedilir(string alan, string op, string deger, string ad) =>
            HataVar(Kosul(alan, op, deger), $"'{ad}' fonksiyonu henüz bağlı değil");

        [Fact]
        public void Yalan_soyledi_eylem_on_kosulunda_gecerlidir() =>
            Gecer(Kosul("yalan_soyledi(eylem.hedef)", "==", "false"));

        [Fact]
        public void Hedefsiz_eylemde_eylem_hedef_reddedilir() =>
            HataVar(Kosul("fikir(eylem.hedef)", "<", "5", "yok"), "hedefsiz");

        [Fact]
        public void Gecersiz_kriter_reddedilir()
        {
            HataVar(Kosul("fikir(ben", "<", "5"), "kriter #1");
            HataVar(Kosul("bilinmeyen_fn(ben)"), "Bilinmeyen fonksiyon");
        }

        [Theory]
        [InlineData("fikir(eylem.hedef)", "<", "5")]
        [InlineData("eksen(eylem.hedef, guven)", "<", "5")]
        [InlineData("ayni_mekanda(eylem.hedef)", "==", "true")]
        [InlineData("modifier_var(eylem.hedef, minnet)", "==", "true")]
        public void Komsu_mekan_icin_kisi_bekleyen_sorgular_reddedilir(string alan, string op, string deger) =>
            HataVar(Kosul(alan, op, deger, "komsu_mekan").Replace("\"olay_turu\": \"selam_verdi\", ", string.Empty), "kişi bekler");

        [Fact]
        public void Komsu_mekan_icin_onerme_icinde_eylem_hedef_kabul_edilir() =>
            Gecer(Kosul("inanc(tehlikeli(eylem.hedef))", "<", "0.5", "komsu_mekan").Replace("\"olay_turu\": \"selam_verdi\", ", string.Empty));

        [Fact]
        public void Agirlik_ve_zorunlu_false_icin_uyari_verilir()
        {
            var sonuc = Yukle(Kosul("ayni_mekanda(eylem.hedef)").Replace("\"deger\": true", "\"deger\": true, \"agirlik\": 2, \"zorunlu\": false"));

            Assert.True(sonuc.Basarili);
            Assert.Contains(sonuc.Uyarilar, u => u.ToString().Contains("agirlik") && u.ToString().Contains("eylem \"e1\""));
            Assert.Contains(sonuc.Uyarilar, u => u.ToString().Contains("zorunlu"));
        }

        // ---- ölçütler ----

        private static string Olcut(string olcutGovdesi, string hedef = "yok") =>
            TemelDegisik("\"hedef\": \"yok\"", "\"hedef\": \"" + hedef + "\"").Replace("\"etkiler\"", "\"olcutler\": [ { " + olcutGovdesi + " } ], \"etkiler\"");

        [Fact]
        public void Olcut_alan_zorunlu() => HataVar(Olcut("\"min\": 0"), "\"alan\"");

        [Fact]
        public void Olcut_bilinmeyen_alan_reddedilir() => HataVar(Olcut("\"alan\": \"stres()\", \"hiz\": 1"), "bilinmeyen alan \"hiz\"");

        [Theory]
        [InlineData("\"min\": 5, \"max\": 5")]
        [InlineData("\"min\": 10, \"max\": 1")]
        public void Olcut_min_max_sirali_olmali(string aralik) => HataVar(Olcut("\"alan\": \"stres()\", " + aralik), "min");

        [Fact]
        public void Olcut_alani_sayisal_ya_da_dogru_yanlis_donmeli()
        {
            Gecer(Olcut("\"alan\": \"stres()\", \"max\": 100"));
            Gecer(Olcut("\"alan\": \"modifier_var(oyuncu, minnet)\""));
            HataVar(Olcut("\"alan\": \"fikir_bandi(oyuncu)\""), "sayısal");
            HataVar(Olcut("\"alan\": \"duygu_hedefi()\""), "sayısal");
        }

        [Fact]
        public void Olcut_baglam_denetimleri_kriterle_aynidir()
        {
            HataVar(Olcut("\"alan\": \"fikir(olay.hedef)\"", "npc"), "'olay.hedef' rolü eylemlerde tanımsız");
            HataVar(Olcut("\"alan\": \"gercek(konum!(Ali, han))\""), "'gercek'");
            HataVar(Olcut("\"alan\": \"yay(g)\""), "'yay' fonksiyonu henüz bağlı değil");
            HataVar(Olcut("\"alan\": \"fikir(eylem.hedef)\""), "hedefsiz");
        }

        [Theory]
        [InlineData("{ \"tur\": \"kare\" }", "kare")]
        [InlineData("{ \"tur\": \"ustel\", \"k\": 9 }", "k")]
        [InlineData("{ \"tur\": \"ustel\", \"k\": 2.5 }", "k")]
        [InlineData("{ \"tur\": \"ustel\" }", "k")]
        [InlineData("{ \"tur\": \"lojistik\", \"k\": 0, \"m\": 0.5 }", "k")]
        [InlineData("{ \"tur\": \"lojistik\", \"k\": 101, \"m\": 0.5 }", "k")]
        [InlineData("{ \"tur\": \"lojistik\", \"k\": 5, \"m\": 2 }", "\"m\"")]
        [InlineData("{ \"tur\": \"lojistik\", \"k\": 5 }", "\"m\"")]
        [InlineData("{ \"tur\": \"basamak\" }", "esik")]
        [InlineData("{ \"tur\": \"basamak\", \"esik\": 1.5 }", "esik")]
        [InlineData("{ \"tur\": \"dogrusal\", \"k\": 2 }", "bilinmeyen alan")]
        [InlineData("{ }", "\"tur\"")]
        [InlineData("5", "nesne")]
        public void Gecersiz_egri_reddedilir(string egri, string parca) =>
            HataVar(Olcut("\"alan\": \"stres()\", \"egri\": " + egri), parca);

        [Fact]
        public void Bilinmeyen_egri_mesaji_gecerli_turleri_sayar() =>
            HataVar(Olcut("\"alan\": \"stres()\", \"egri\": { \"tur\": \"kare\" }"), "dogrusal, ustel, lojistik, basamak");

        [Theory]
        [InlineData("{ \"tur\": \"dogrusal\" }")]
        [InlineData("{ \"tur\": \"ustel\", \"k\": 8 }")]
        [InlineData("{ \"tur\": \"lojistik\", \"k\": 100, \"m\": 0 }")]
        [InlineData("{ \"tur\": \"basamak\", \"esik\": 0.5 }")]
        public void Gecerli_egriler_kabul_edilir(string egri) =>
            Gecer(Olcut("\"alan\": \"stres()\", \"egri\": " + egri));

        // ---- çarpan tanımları ----

        [Fact]
        public void Bilinmeyen_kisilik_ozelligi_reddedilir() =>
            HataVar(TemelIle("\"kisilik_carpani\": { \"ozellikler\": [ { \"ozellik\": \"sakinlik\" } ] }"), "bilinmeyen kişilik özelliği \"sakinlik\"");

        [Fact]
        public void Celisen_ozelliklerde_bilinmeyen_ozellik_reddedilir() =>
            HataVar(TemelIle("\"celisen_ozellikler\": [ \"sakinlik\" ]"), "bilinmeyen kişilik özelliği \"sakinlik\"");

        [Fact]
        public void Celisen_ozellikler_tekrar_edemez() =>
            HataVar(TemelIle("\"celisen_ozellikler\": [ \"durustluk\", \"durustluk\" ]"), "tekrar");

        [Theory]
        [InlineData("\"kisilik_carpani\": { \"ozellikler\": [] }", "en az bir")]
        [InlineData("\"kisilik_carpani\": { \"ozellikler\": [ { \"ozellik\": \"cesaret\", \"agirlik\": 0 } ] }", "agirlik")]
        [InlineData("\"kisilik_carpani\": { \"ozellikler\": [ { \"ozellik\": \"cesaret\", \"agirlik\": -1 } ] }", "agirlik")]
        [InlineData("\"kisilik_carpani\": { \"ozellikler\": [ { \"ozellik\": \"cesaret\" }, { \"ozellik\": \"cesaret\" } ] }", "tekrar")]
        [InlineData("\"kisilik_carpani\": { \"ozellikler\": [ { \"ozellik\": \"cesaret\", \"ters\": \"evet\" } ] }", "ters")]
        [InlineData("\"kisilik_carpani\": { }", "ozellikler")]
        [InlineData("\"ruh_hali_carpani\": { \"duygular\": [] }", "en az bir")]
        [InlineData("\"ruh_hali_carpani\": { \"duygular\": [ { \"duygu\": \"korku\", \"agirlik\": 0 } ] }", "agirlik")]
        [InlineData("\"ruh_hali_carpani\": { \"duygular\": [ { \"duygu\": \"korku\" }, { \"duygu\": \"korku\" } ] }", "tekrar")]
        [InlineData("\"ruh_hali_carpani\": { \"duygular\": [ { \"duygu\": \"Kor ku\" } ] }", "duygu")]
        [InlineData("\"fikir_carpani\": { \"yon\": \"yukari\" }", "artan, azalan")]
        [InlineData("\"fikir_carpani\": { }", "yon")]
        public void Gecersiz_carpan_tanimlari_reddedilir(string ek, string parca) =>
            HataVar(TemelIle(ek).Replace("\"hedef\": \"yok\"", "\"hedef\": \"npc\""), parca);

        [Theory]
        [InlineData("yok")]
        [InlineData("komsu_mekan")]
        public void Fikir_carpani_yalnizca_kisi_hedefli_eylemde_tanimlanabilir(string hedef) =>
            HataVar(TemelIle("\"fikir_carpani\": { \"yon\": \"artan\" }").Replace("\"hedef\": \"yok\"", "\"hedef\": \"" + hedef + "\"")
                .Replace("\"olay_turu\": \"selam_verdi\", ", hedef == "komsu_mekan" ? string.Empty : "\"olay_turu\": \"selam_verdi\", "), "fikir_carpani");

        [Fact]
        public void Fikir_carpani_npc_ve_oyuncu_hedefli_eylemde_kabul_edilir()
        {
            foreach (var hedef in new[] { "npc", "oyuncu" })
            {
                Gecer(TemelIle("\"fikir_carpani\": { \"yon\": \"azalan\" }").Replace("\"hedef\": \"yok\"", "\"hedef\": \"" + hedef + "\""));
            }
        }
    }
}
