using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Authoring;

namespace Yanki.Scenarios.Tests
{
    /// <summary>9. hafta, V kararı: <c>yanki validate</c> mantığı (Yanki.Authoring). Girdi metin + dosya adıdır; dosya sistemi yok.</summary>
    public class DogrulayiciTests
    {
        private const string Senaryo = @"{
  ""surum"": 1, ""ad"": ""Mini"",
  ""mekanlar"": [""han"", ""pazar""], ""baglantilar"": [[""han"", ""pazar""]],
  ""oyuncu"": { ""id"": ""oyuncu"", ""konum"": ""han"" },
  ""npcler"": [
    { ""id"": ""Ayse"", ""konum"": ""han"", ""kisilik"": { ""acgozluluk"": 0.5, ""durustluk"": 0.5, ""cesaret"": 0.5, ""sosyallik"": 0.5, ""kin_tutma"": 0.5, ""merhamet"": 0.5 } },
    { ""id"": ""Can"", ""konum"": ""han"", ""kisilik"": { ""acgozluluk"": 0.5, ""durustluk"": 0.5, ""cesaret"": 0.5, ""sosyallik"": 0.5, ""kin_tutma"": 0.5, ""merhamet"": 0.5 } }
  ],
  ""gercekler"": [], ""olay_turleri"": [""hakaret_et""],
  ""kural_dosyalari"": [""kurallar.json""], ""eylem_dosyalari"": [""eylemler.json""]
}";

        private const string Kurallar = @"{ ""kurallar"": [ { ""id"": ""kirgin"", ""ne_zaman"": ""hakaret_et"", ""kriterler"": [ { ""alan"": ""olay_rolu(hedef)"", ""op"": ""=="", ""deger"": true } ],
  ""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""olay.fail"", ""eksen"": ""guven"", ""delta"": -10 } ] } ] }";

        private const string Eylemler = @"{ ""eylemler"": [ { ""id"": ""dinlen"", ""olay_turu"": ""dinlendi"", ""hedef"": ""yok"", ""taban"": 1, ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.2 } ] } ] }";

        private static DogrulamaGirdisi Girdi(string senaryo = Senaryo, string kurallar = Kurallar, string eylemler = Eylemler, string? sablon = null, string? menu = null)
        {
            var dosyalar = new List<DosyaGirdisi>
            {
                new DosyaGirdisi("kurallar.json", "x/kurallar.json", kurallar),
                new DosyaGirdisi("eylemler.json", "x/eylemler.json", eylemler),
            };
            return new DogrulamaGirdisi(
                "x/scenario.json", senaryo, dosyalar,
                sablon == null ? null : new DosyaGirdisi("sablonlar.json", "x/sablonlar.json", sablon),
                menu == null ? null : new DosyaGirdisi("oyuncu_eylemleri.json", "x/oyuncu_eylemleri.json", menu));
        }

        private static string[] Mesajlar(DogrulamaSonucu s) => s.Bulgular.Select(b => b.ToString()).ToArray();

        [Fact]
        public void Temiz_senaryoda_hata_olmaz_kisilik_uyarilari_ayri_ayri_listelenir()
        {
            var s = SenaryoDogrulayici.Dogrula(Girdi());

            Assert.True(s.HataSayisi == 0, string.Join(" | ", Mesajlar(s)));
            // Hiçbir eylemde kullanılmayan kişilik özellikleri uyarı verir; şablon dosyası verilmediyse diyalog tarafı bilinemez, uyarı çıkmaz.
            Assert.Contains(s.Bulgular, b => !b.Hata && b.Tur == "kisilik" && b.Kimlik == "sosyallik" && b.Mesaj.Contains("eylem ölçütünde", StringComparison.Ordinal));
            Assert.DoesNotContain(s.Bulgular, b => b.Mesaj.Contains("diyalog kriterinde", StringComparison.Ordinal));
            Assert.Equal(6, s.UyariSayisi);

            // Şablon dosyası verilince "diyalog kriterinde yok" ayrı uyarılardır (6 özellik daha).
            var sablonlu = SenaryoDogrulayici.Dogrula(Girdi(sablon: Sablon(DinlenmeGenel)));
            Assert.Equal(12, sablonlu.UyariSayisi);
        }

        [Fact]
        public void Hata_ve_uyarilar_dosya_ve_satir_sirasiyla_birlikte_listelenir()
        {
            var bozukKural = Kurallar.Replace("\"delta\": -10", "\"delta\": -10, \"kotu\": 1");
            var bozukEylem = Eylemler.Replace("\"taban\": 1", "\"taban\": 1, \"yok_boyle\": 1");

            var s = SenaryoDogrulayici.Dogrula(Girdi(kurallar: bozukKural, eylemler: bozukEylem));

            var hatalar = s.Bulgular.Where(b => b.Hata).ToArray();
            Assert.True(hatalar.Length >= 2);
            Assert.Contains(hatalar, b => b.Dosya == "x/eylemler.json");
            Assert.Contains(hatalar, b => b.Dosya == "x/kurallar.json");
            var sirali = s.Bulgular.OrderBy(b => b.Dosya, StringComparer.Ordinal).ThenBy(b => b.Satir).Select(b => b.ToString()).ToArray();
            Assert.Equal(sirali, Mesajlar(s));
            Assert.All(hatalar, b => Assert.StartsWith(b.Dosya + ":", b.ToString(), StringComparison.Ordinal));
        }

        [Fact]
        public void Etkisi_bos_kural_hatadir()
        {
            var bos = Kurallar.Replace(@"""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""olay.fail"", ""eksen"": ""guven"", ""delta"": -10 } ]", @"""etkiler"": []");

            var s = SenaryoDogrulayici.Dogrula(Girdi(kurallar: bos));

            Assert.Contains(s.Bulgular, b => b.Hata && b.Dosya == "x/kurallar.json" && b.Kimlik == "kirgin");
        }

        [Fact]
        public void Eksik_dosya_hata_olarak_raporlanir()
        {
            var g = new DogrulamaGirdisi("x/scenario.json", Senaryo, new List<DosyaGirdisi> { new DosyaGirdisi("kurallar.json", "x/kurallar.json", Kurallar) }, null, null);

            var s = SenaryoDogrulayici.Dogrula(g);

            Assert.Contains(s.Bulgular, b => b.Hata && b.Mesaj.Contains("eylemler.json", StringComparison.Ordinal) && b.Mesaj.Contains("bulunamadı", StringComparison.Ordinal));
        }

        [Fact]
        public void Kisilik_ozelligi_eylem_olcutunde_kullaniliyorsa_olcut_uyarisi_gitmez()
        {
            var eylem = @"{ ""eylemler"": [ { ""id"": ""dinlen"", ""olay_turu"": ""dinlendi"", ""hedef"": ""yok"", ""taban"": 1, ""olcutler"": [ { ""alan"": ""kisilik(sosyallik)"" } ], ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.2 } ] } ] }";

            var s = SenaryoDogrulayici.Dogrula(Girdi(eylemler: eylem, sablon: Sablon(DinlenmeGenel)));

            Assert.DoesNotContain(s.Bulgular, b => b.Tur == "kisilik" && b.Kimlik == "sosyallik" && b.Mesaj.Contains("eylem ölçütünde", StringComparison.Ordinal));
            Assert.Contains(s.Bulgular, b => b.Tur == "kisilik" && b.Kimlik == "sosyallik" && b.Mesaj.Contains("diyalog kriterinde", StringComparison.Ordinal));
        }

        // ---- Şablon denetimleri ----

        private const string SablonGovde = @"{
  ""dil"": ""tr"",
  ""adlar"": { ""Ayse"": ""Ayşe"", ""Can"": ""Can"", ""oyuncu"": { ""ad"": ""yabancı"", ""cins"": true } },
  ""onerme_metinleri"": { ""borclu"": { ""metin"": ""{0} {1.yonelme} {2} lira borçlu"", ""olumsuz"": ""{0} {1.yonelme} {2} lira borçlu değil"", ""sayilar"": [2] } },
  ""esleme"": [ { ""olay_turu"": ""dinlendi"", ""niyet"": ""dinlenme"", ""konusan"": ""fail"", ""dinleyensiz"": true } ],
  ""sablonlar"": [ __SABLONLAR__ ]
}";

        private static string Sablon(string sablonlar) => SablonGovde.Replace("__SABLONLAR__", sablonlar);

        private const string DinlenmeGenel = @"{ ""id"": ""dinlenme_genel"", ""concept"": ""dinlenme"", ""genel"": true, ""metin"": ""Biraz dinleneyim."" }";

        [Fact]
        public void Sablon_dosyasi_yuklenir_ve_yukleyici_hatalari_listelenir()
        {
            var s = SenaryoDogrulayici.Dogrula(Girdi(sablon: Sablon(DinlenmeGenel)));

            Assert.True(s.HataSayisi == 0, string.Join(" | ", Mesajlar(s)));
        }

        [Fact]
        public void Esleme_olmayan_concept_ulasilamaz_sablon_uyarisidir()
        {
            var s = SenaryoDogrulayici.Dogrula(Girdi(sablon: Sablon(DinlenmeGenel + @", { ""id"": ""hayalet"", ""concept"": ""hayalet_niyet"", ""genel"": true, ""metin"": ""Bu hiç söylenmez."" }")));

            Assert.True(s.HataSayisi == 0, string.Join(" | ", Mesajlar(s)));
            var b = Assert.Single(s.Bulgular, x => x.Tur == "sablon" && x.Kimlik == "hayalet");
            Assert.False(b.Hata);
            Assert.Equal("x/sablonlar.json", b.Dosya);
            Assert.True(b.Satir > 0);
            Assert.DoesNotContain(s.Bulgular, x => x.Tur == "sablon" && x.Kimlik == "dinlenme_genel");
        }

        [Fact]
        public void Sabit_kimlik_senaryoda_yoksa_ulasilamaz_sablon_uyarisidir()
        {
            var s = SenaryoDogrulayici.Dogrula(Girdi(sablon: Sablon(DinlenmeGenel + @", { ""id"": ""veli_ozel"", ""concept"": ""dinlenme"", ""kriterler"": [ { ""alan"": ""fikir(Veli)"", ""op"": ""<"", ""deger"": 0, ""zorunlu"": true } ], ""metin"": ""x"" }")));

            Assert.Contains(s.Bulgular, b => !b.Hata && b.Tur == "sablon" && b.Kimlik == "veli_ozel" && b.Mesaj.Contains("Veli", StringComparison.Ordinal));
        }

        [Fact]
        public void Sonra_hedefi_senaryoda_olmayan_kimlikse_uyaridir()
        {
            var s = SenaryoDogrulayici.Dogrula(Girdi(sablon: Sablon(DinlenmeGenel + @", { ""id"": ""cevap_genel"", ""concept"": ""cevap"", ""genel"": true, ""metin"": ""Peki."" }, { ""id"": ""takip"", ""concept"": ""dinlenme"", ""kriterler"": [ { ""alan"": ""stres()"", ""op"": "">="", ""deger"": 0, ""zorunlu"": true } ], ""sonra"": { ""hedef"": ""Veli"", ""concept"": ""cevap"" }, ""metin"": ""x"" }")));

            Assert.True(s.HataSayisi == 0, string.Join(" | ", Mesajlar(s)));
            Assert.Contains(s.Bulgular, b => b.Tur == "sablon" && b.Kimlik == "takip" && b.Mesaj.Contains("Veli", StringComparison.Ordinal));
            // Beşinci tanım: followup'ı yalnızca ulaşılamaz bir şablondan geldiği için "cevap" concept'i de ulaşılamaz.
            Assert.Contains(s.Bulgular, b => b.Tur == "sablon" && b.Kimlik == "cevap_genel" && b.Mesaj.Contains("followup", StringComparison.Ordinal));
        }

        // ---- Korumalı kimlik ----

        [Fact]
        public void Korumali_kimlik_senaryoda_hic_gecmiyorsa_uyaridir()
        {
            var senaryo = Senaryo.Replace("\"gercekler\": [],", "\"gercekler\": [], \"korumali\": { \"Hanci\": [\"mekandan_ayrilamaz\"] },");

            var s = SenaryoDogrulayici.Dogrula(Girdi(senaryo: senaryo));

            Assert.Contains(s.Bulgular, b => !b.Hata && b.Tur == "korumali" && b.Kimlik == "Hanci");
        }

        // ---- Oyuncu menüsü (M kararı) ----

        private static string Menu(params string[] girdiler) => "{ \"surum\": 1, \"menu\": [ " + string.Join(", ", girdiler) + " ] }";

        private const string Hakaret = @"{ ""id"": ""hakaret"", ""aile"": ""kotuluk"", ""tur"": ""hakaret_et"", ""hedef_turu"": ""npc"", ""metin"": ""Hakaret et"" }";

        [Fact]
        public void Menu_gecerliyse_hata_yoktur_ve_Submit_ile_denenir()
        {
            var s = SenaryoDogrulayici.Dogrula(Girdi(menu: Menu(Hakaret)));

            Assert.True(s.HataSayisi == 0, string.Join(" | ", Mesajlar(s)));
        }

        [Fact]
        public void Menu_senaryoda_olmayan_tur_ya_da_bozuk_onerme_ya_da_fazla_girdide_hatadir()
        {
            var bilinmeyen = Hakaret.Replace("\"id\": \"hakaret\"", "\"id\": \"b1\"").Replace("hakaret_et", "yok_boyle_bir_tur");
            var bozukOnerme = @"{ ""id"": ""b2"", ""aile"": ""bilgi_ver"", ""tur"": ""anlatti"", ""hedef_turu"": ""npc"", ""yuk"": { ""onerme"": ""borclu((("" }, ""metin"": ""x"" }";

            var s = SenaryoDogrulayici.Dogrula(Girdi(menu: Menu(bilinmeyen, bozukOnerme)));

            Assert.Contains(s.Bulgular, b => b.Hata && b.Dosya == "x/oyuncu_eylemleri.json" && b.Kimlik == "b1");
            Assert.Contains(s.Bulgular, b => b.Hata && b.Dosya == "x/oyuncu_eylemleri.json" && b.Kimlik == "b2");

            var onbir = Enumerable.Range(0, 11).Select(i => Hakaret.Replace("\"id\": \"hakaret\"", "\"id\": \"m" + i + "\"")).ToArray();
            var fazla = SenaryoDogrulayici.Dogrula(Girdi(menu: Menu(onbir)));
            Assert.Contains(fazla.Bulgular, b => b.Hata && b.Mesaj.Contains("10", StringComparison.Ordinal));
        }
    }
}
