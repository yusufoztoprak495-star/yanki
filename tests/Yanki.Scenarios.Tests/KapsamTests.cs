using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;
using Yanki.Authoring;
using Yanki.Core.Simulation;

namespace Yanki.Scenarios.Tests
{
    /// <summary>9. hafta, K kararı: <c>yanki coverage</c> mantığı (Yanki.Authoring). Sayım yalnızca günlükten ve diyalog satırlarından türer; simülasyona sayaç eklenmez.</summary>
    public class KapsamTests
    {
        private const string Senaryo = @"{
  ""surum"": 1, ""ad"": ""Mini"",
  ""mekanlar"": [""han"", ""pazar""], ""baglantilar"": [[""han"", ""pazar""]],
  ""oyuncu"": { ""id"": ""oyuncu"", ""konum"": ""han"" },
  ""npcler"": [
    { ""id"": ""Ayse"", ""konum"": ""han"", ""kisilik"": { ""acgozluluk"": 0.5, ""durustluk"": 0.5, ""cesaret"": 0.5, ""sosyallik"": 0.5, ""kin_tutma"": 0.5, ""merhamet"": 0.5 } },
    { ""id"": ""Can"", ""konum"": ""han"", ""kisilik"": { ""acgozluluk"": 0.5, ""durustluk"": 0.5, ""cesaret"": 0.5, ""sosyallik"": 0.5, ""kin_tutma"": 0.5, ""merhamet"": 0.5 } }
  ],
  ""gercekler"": [], ""olay_turleri"": [""hakaret_et"", ""yardim_etti""],
  ""kural_dosyalari"": [""kurallar.json""], ""eylem_dosyalari"": [""eylemler.json""]
}";

        private static string Kural(string id, string neZaman) =>
            @"{ ""id"": """ + id + @""", ""ne_zaman"": """ + neZaman + @""", ""kriterler"": [ { ""alan"": ""olay_rolu(hedef)"", ""op"": ""=="", ""deger"": true } ],
  ""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""olay.fail"", ""eksen"": ""guven"", ""delta"": -1 } ] }";

        private static readonly string DortKural = "{ \"kurallar\": [ " + Kural("a", "hakaret_et") + ", " + Kural("b", "hakaret_et") + ", " + Kural("c", "hakaret_et") + ", " + Kural("d", "hakaret_et") + " ] }";

        private static readonly string TekKuralVeHayalet = "{ \"kurallar\": [ " + Kural("a", "hakaret_et") + ", " + Kural("hayalet", "yardim_etti") + " ] }";

        private const string Eylemler = @"{ ""eylemler"": [ { ""id"": ""dinlen"", ""olay_turu"": ""dinlendi"", ""hedef"": ""yok"", ""taban"": 1, ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.2 } ] } ] }";

        private static DogrulamaGirdisi Girdi(string kurallar, bool crlf = false)
        {
            string C(string s) => crlf ? s.Replace("\n", "\r\n") : s;
            return new DogrulamaGirdisi(
                "x/scenario.json", C(Senaryo),
                new List<DosyaGirdisi> { new DosyaGirdisi("kurallar.json", "x/kurallar.json", C(kurallar)), new DosyaGirdisi("eylemler.json", "x/eylemler.json", C(Eylemler)) },
                null, null);
        }

        private static Betik HakaretBetigi() =>
            BetikOkuyucu.Oku(@"{ ""ad"": ""hakaret"", ""eylemler"": [ { ""tick"": 1, ""tur"": ""hakaret_et"", ""hedef"": ""Ayse"" } ] }", "x/oynanislar/hakaret.json");

        [Fact]
        public void Betik_kayit_bicimindeki_eylem_gunlugunu_okur_ve_hatalari_dosya_satirla_verir()
        {
            var b = HakaretBetigi();
            Assert.Equal("hakaret", b.Ad);
            var e = Assert.Single(b.Eylemler);
            Assert.Equal((1L, "hakaret_et", "Ayse"), (e.Tick, e.Eylem.Tur, e.Eylem.Hedef));

            var ex = Assert.Throws<BetikHatasi>(() => BetikOkuyucu.Oku(@"{ ""ad"": ""x"", ""eylemler"": [ { ""tick"": 0, ""tur"": ""hakaret_et"" } ] }", "x/b.json"));
            Assert.StartsWith("x/b.json:", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Kural_tetiklenmeleri_betik_basina_seed_basina_sayilir_hayalet_kural_sifir_ve_isaretli()
        {
            var s = KapsamHesabi.Hesapla(Girdi(TekKuralVeHayalet), new[] { HakaretBetigi() }, seedSayisi: 5, tick: 3, new KapsamAyarlari());

            var a = s.Kurallar.Single(k => k.Id == "a");
            var hayalet = s.Kurallar.Single(k => k.Id == "hayalet");
            Assert.Equal(5, a.Toplam);            // her seed'de bir kez
            Assert.Equal(0, hayalet.Toplam);
            Assert.Equal(new[] { "hayalet" }, s.HicTetiklenmeyenKurallar.ToArray());
            Assert.Contains("hiç tetiklenmedi", s.Markdown, StringComparison.Ordinal);
            Assert.False(s.Basarili);
        }

        [Fact]
        public void Oyuncusuz_kosu_ayri_satirdir_toplama_ve_paya_girmez()
        {
            var s = KapsamHesabi.Hesapla(Girdi(TekKuralVeHayalet), new[] { HakaretBetigi() }, 4, 3, new KapsamAyarlari());

            var a = s.Kurallar.Single(k => k.Id == "a");
            Assert.Equal(0, a.OyuncusuzToplam);
            Assert.Equal(4, a.Toplam);
            Assert.Equal(4, a.BetikBasina.Single(p => p.Key == "hakaret").Value);
        }

        [Fact]
        public void Pay_esigini_asan_kural_isaretlenir_dengeli_kurallarda_kriter_tutar()
        {
            var dengesiz = KapsamHesabi.Hesapla(Girdi("{ \"kurallar\": [ " + Kural("a", "hakaret_et") + " ] }"), new[] { HakaretBetigi() }, 3, 3, new KapsamAyarlari());
            Assert.Equal(new[] { "a" }, dengesiz.PayiAsanKurallar.ToArray());
            Assert.False(dengesiz.Basarili);

            var dengeli = KapsamHesabi.Hesapla(Girdi(DortKural), new[] { HakaretBetigi() }, 3, 3, new KapsamAyarlari());
            Assert.All(dengeli.Kurallar, k => Assert.Equal(3, k.Toplam));
            Assert.Empty(dengeli.PayiAsanKurallar);
            Assert.Empty(dengeli.HicTetiklenmeyenKurallar);
            Assert.True(dengeli.Basarili);
            Assert.Contains("25.0", dengeli.Markdown, StringComparison.Ordinal);
        }

        [Fact]
        public void Esikler_ayar_sinifindadir_varsayilan_plan_3()
        {
            var ayar = new KapsamAyarlari();

            Assert.Equal(0.30m, ayar.PayEsigi);
            Assert.Equal(168L, KapsamAyarlari.VarsayilanTick);
        }

        [Fact]
        public void Ayni_girdi_iki_koşuda_ayni_rapor_CRLF_ve_tr_TR_kulturu_degistirmez()
        {
            var betikler = new[] { HakaretBetigi() };
            var invariant = KapsamHesabi.Hesapla(Girdi(DortKural), betikler, 3, 5, new KapsamAyarlari()).Markdown;
            var tekrar = KapsamHesabi.Hesapla(Girdi(DortKural), betikler, 3, 5, new KapsamAyarlari()).Markdown;
            var crlf = KapsamHesabi.Hesapla(Girdi(DortKural, crlf: true), betikler, 3, 5, new KapsamAyarlari()).Markdown;

            string tr;
            var eski = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                tr = KapsamHesabi.Hesapla(Girdi(DortKural), betikler, 3, 5, new KapsamAyarlari()).Markdown;
            }
            finally
            {
                CultureInfo.CurrentCulture = eski;
            }

            Assert.Equal(invariant, tekrar);
            Assert.Equal(invariant, crlf);
            Assert.Equal(invariant, tr);
            Assert.DoesNotContain("\r", invariant, StringComparison.Ordinal);
        }

        [Fact]
        public void Sayim_gunlukten_bagimsiz_yeniden_hesaplanınca_ayni_cikar()
        {
            var g = Girdi(DortKural);
            var rapor = KapsamHesabi.Hesapla(g, new[] { HakaretBetigi() }, 1, 6, new KapsamAyarlari());

            // Bağımsız yeniden hesap: aynı kosuyu elle kur, günlüğü tara, KuralId'si kural kimliği olan olayları (tick, npc, nedenler) imzasıyla say.
            var yuk = KapsamHesabi.TestIcinKos(g, HakaretBetigi(), seed: 1, tick: 6);
            var kuralKimlikleri = new[] { "a", "b", "c", "d" };
            foreach (var id in kuralKimlikleri)
            {
                var imzalar = new SortedSet<string>(StringComparer.Ordinal);
                foreach (var o in yuk.Gunluk.Olaylar.Where(o => o.KuralId == id))
                {
                    var npc = o.Yuk.Where(kv => kv.Key == "npc").Select(kv => kv.Value).DefaultIfEmpty(string.Empty).First();
                    imzalar.Add(o.Tick.ToString(CultureInfo.InvariantCulture) + "|" + npc + "|" + string.Join(",", o.NedenOlaylar));
                }

                Assert.Equal(imzalar.Count, rapor.Kurallar.Single(k => k.Id == id).Toplam);
            }
        }

        [Fact]
        public void Algi_eylem_kriz_ve_fren_kural_payina_girmez_eylem_secilmesi_ayri_tablodur()
        {
            var s = KapsamHesabi.Hesapla(Girdi(DortKural), new[] { HakaretBetigi() }, 2, 12, new KapsamAyarlari());

            Assert.DoesNotContain(s.Kurallar, k => k.Id == "algi" || k.Id.StartsWith("eylem:", StringComparison.Ordinal) || k.Id == "player");
            Assert.Contains(s.Eylemler, e => e.Id == "dinlen");
            Assert.Contains("## Eylemler", s.Markdown, StringComparison.Ordinal);
        }

        [Fact]
        public void Hic_secilmeyen_sablonlar_uc_gruba_ayrilir_a_konsept_satir_uretmedi_b_genel_kaybetti_c_spesifik_tutmadi()
        {
            var senaryo = Senaryo.Replace("\"olay_turleri\": [\"hakaret_et\", \"yardim_etti\"]", "\"olay_turleri\": [\"hakaret_et\", \"yardim_etti\", \"hayalet_oldu\"]");
            var sablon = @"{ ""dil"": ""tr"", ""adlar"": { ""Ayse"": ""Ayşe"", ""Can"": ""Can"", ""oyuncu"": { ""ad"": ""yabancı"", ""cins"": true } },
  ""onerme_metinleri"": { ""borclu"": { ""metin"": ""{0} {1.yonelme} {2} lira borçlu"", ""olumsuz"": ""{0} {1.yonelme} {2} lira borçlu değil"", ""sayilar"": [2] } },
  ""esleme"": [ { ""olay_turu"": ""dinlendi"", ""niyet"": ""dinlenme"", ""konusan"": ""fail"", ""dinleyensiz"": true }, { ""olay_turu"": ""hayalet_oldu"", ""niyet"": ""hayalet"", ""konusan"": ""hedef"" } ],
  ""sablonlar"": [
    { ""id"": ""dinlenme_genel"", ""concept"": ""dinlenme"", ""genel"": true, ""metin"": ""Dinleniyorum."" },
    { ""id"": ""dinlenme_hep_tutan"", ""concept"": ""dinlenme"", ""kriterler"": [ { ""alan"": ""kisilik(sosyallik)"", ""op"": "">="", ""deger"": 0.1, ""zorunlu"": true, ""agirlik"": 1 } ], ""metin"": ""Hep tutan."" },
    { ""id"": ""dinlenme_hic_tutmayan"", ""concept"": ""dinlenme"", ""kriterler"": [ { ""alan"": ""kisilik(sosyallik)"", ""op"": "">="", ""deger"": 0.99, ""zorunlu"": true, ""agirlik"": 1 } ], ""metin"": ""Hiç tutmayan."" },
    { ""id"": ""hayalet_genel"", ""concept"": ""hayalet"", ""genel"": true, ""metin"": ""Hayalet."" } ] }";
            var dosyalar = new List<DosyaGirdisi> { new DosyaGirdisi("kurallar.json", "x/kurallar.json", DortKural), new DosyaGirdisi("eylemler.json", "x/eylemler.json", Eylemler) };
            var girdi = new DogrulamaGirdisi("x/scenario.json", senaryo, dosyalar, new DosyaGirdisi("sablonlar.json", "x/sablonlar.json", sablon), null);

            var s = KapsamHesabi.Hesapla(girdi, new[] { HakaretBetigi() }, 2, 10, new KapsamAyarlari());

            Assert.Equal(new[] { ("a", "hayalet_genel"), ("b", "dinlenme_genel"), ("c", "dinlenme_hic_tutmayan") },
                s.SablonGruplari.Select(g => (g.Grup.ToString(), g.Id)).ToArray());
            Assert.Contains("hayalet_oldu hiç oluşmadı", s.SablonGruplari.Single(g => g.Grup == 'a').Aciklama, StringComparison.Ordinal);
            Assert.Contains("### (a)", s.Markdown, StringComparison.Ordinal);
            Assert.Contains("| dinlenme_genel | dinlenme |", s.Markdown, StringComparison.Ordinal);
        }
    }
}
