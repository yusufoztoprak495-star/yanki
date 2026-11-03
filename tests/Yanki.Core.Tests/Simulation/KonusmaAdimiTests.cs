using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Criteria;
using Yanki.Core.Decision;
using Yanki.Core.Events;
using Yanki.Core.Mind;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;
using Yanki.Core.Tests.Decision;

namespace Yanki.Core.Tests.Simulation
{
    /// <summary>
    /// NPC konuşma adımı (6. hafta, Teslim 1 ve 4): konu seçimi, <c>anlatti</c> olayları, algı, <c>duydu</c> kanıtı, aktarım kaydı,
    /// yalan gizliliği. Fikstür: Ayse, Can, Zeki han'da; Veli pazar'da; oyuncu han'da. Kasaba Hanı içeriği DEĞİLDİR.
    /// </summary>
    public class KonusmaAdimiTests
    {
        private const string Borc = "borclu(Mehmet,Ayse,500)";
        private const string Kardes = "kardes(Mehmet,Can)";
        private const string DegilBorc = "degil(" + Borc + ")";

        internal static string Senaryo(string npcSirasi = "Ayse,Can,Zeki,Veli", string gercekler = "", string ayseDurustluk = "0.5") =>
            @"{ ""surum"": 1, ""ad"": ""Dedikodu"", ""mekanlar"": [""han"", ""pazar""], ""baglantilar"": [[""han"", ""pazar""]],
  ""oyuncu"": { ""id"": ""oyuncu"", ""konum"": ""han"" },
  ""npcler"": [" + string.Join(",", npcSirasi.Split(',').Select(id => Npc(id, id == "Ayse" ? ayseDurustluk : "0.5"))) + @"],
  ""gercekler"": [" + gercekler + @"], ""olay_turleri"": [""haber""], ""kural_dosyalari"": [""k.json""] }";

        private static string Npc(string id, string durustluk = "0.5") =>
            @"{ ""id"": """ + id + @""", ""konum"": """ + (id == "Veli" ? "pazar" : "han") + @""", ""kisilik"": { ""acgozluluk"": 0.5, ""durustluk"": " + durustluk + @", ""cesaret"": 0.5, ""sosyallik"": 0.5, ""kin_tutma"": 0.5, ""merhamet"": 0.5 } }";

        private static readonly string[] Eylemler =
        {
            "{ \"id\": \"anlat\", \"olay_turu\": \"anlatti\", \"hedef\": \"npc\", \"taban\": 1, \"aktarim\": \"dogru\", \"konu_desenleri\": [\"borclu(*,*,*)\", \"degil(borclu(*,*,*))\", \"kardes(*,*)\"] }",
            "{ \"id\": \"anlat_acik\", \"olay_turu\": \"anlatti\", \"hedef\": \"npc\", \"taban\": 1, \"aktarim\": \"dogru\", \"konu_desenleri\": [\"borclu(*,*,*)\"], \"gorunurluk\": \"acik\" }",
            "{ \"id\": \"anlat_oyuncuya\", \"olay_turu\": \"anlatti\", \"hedef\": \"oyuncu\", \"taban\": 1, \"aktarim\": \"dogru\", \"konu_desenleri\": [\"borclu(*,*,*)\"] }",
            "{ \"id\": \"yalan_borc\", \"olay_turu\": \"anlatti\", \"hedef\": \"npc\", \"taban\": 1, \"aktarim\": \"yalan\", \"yalan_onermeleri\": [\"borclu(Mehmet,Ayse,500)\"], \"celisen_ozellikler\": [\"durustluk\"], \"stres_tabani\": 15 }",
            "{ \"id\": \"yalan_soyle\", \"olay_turu\": \"anlatti\", \"hedef\": \"npc\", \"taban\": 1, \"aktarim\": \"yalan\", \"yalan_onermeleri\": [\"degil(borclu(Mehmet,Ayse,500))\"], \"celisen_ozellikler\": [\"durustluk\"], \"stres_tabani\": 15 }",
        };

        private const string Kurallar = @"{ ""kurallar"": [ {
  ""id"": ""aile_iftirasi"", ""ne_zaman"": ""anlatti"",
  ""kriterler"": [ { ""alan"": ""olay_konusu(borclu(Mehmet, *, *))"", ""op"": ""=="", ""deger"": true } ],
  ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""aileme_iftira"", ""deger"": -22, ""sure"": 18 } ]
}, {
  ""id"": ""haber_kurali"", ""ne_zaman"": ""haber"",
  ""etkiler"": [ { ""tur"": ""gercek"", ""islem"": ""ekle"", ""gercek"": ""borclu(Mehmet,Ayse,500)"" } ]
} ] }";

        /// <summary>Test seçicisi: her çağrıda adayları kaydeder; seçimi (npc, tick) tablosundan yapar.</summary>
        internal sealed class Kaydedici : IActionSelector
        {
            private readonly Dictionary<string, string> _secimler = new Dictionary<string, string>(StringComparer.Ordinal);

            public Dictionary<string, string[]> Adaylar { get; } = new Dictionary<string, string[]>(StringComparer.Ordinal);

            public Kaydedici Sec(string npc, long tick, string aday)
            {
                _secimler[npc + "@" + tick] = aday;
                return this;
            }

            public SecimSonucu Sec(KararBaglami baglam, IReadOnlyList<Aday> adaylar, IReadOnlyList<ElenenAday> elenenler)
            {
                var anahtar = baglam.Npc + "@" + baglam.Tick;
                Adaylar[anahtar] = adaylar.Select(a => a.Anahtar).ToArray();
                if (!_secimler.TryGetValue(anahtar, out var secim))
                {
                    return new SecimSonucu(null, new KararIzi(baglam.Npc, baglam.Tick, new SecimAyarlari(), new AdayIzi[0], new string[0], null, null, new string[0], new long[0], new string[0]));
                }

                var aday = adaylar.FirstOrDefault(a => a.Anahtar == secim)
                    ?? throw new InvalidOperationException($"{anahtar}: '{secim}' adayı yok. Adaylar: [{string.Join(", ", adaylar.Select(a => a.Anahtar))}]. Elenenler: [{string.Join("; ", elenenler.Select(e => e.Eylem.Id + "|" + e.Hedef + ": " + e.Neden))}]");
                return new SecimSonucu(aday, KararIzi.Basit(baglam, aday));
            }
        }

        private static Simulasyon Kur(
            IActionSelector? secici = null, string npcSirasi = "Ayse,Can,Zeki,Veli", string gercekler = "", DedikoduAyarlari? ayar = null, ulong seed = 42,
            string kurallar = Kurallar)
        {
            var senaryo = SenaryoYukleyici.Yukle(Senaryo(npcSirasi, gercekler), "s.json");
            Assert.True(senaryo.Basarili, string.Join("\n", senaryo.Hatalar));
            var kural = KuralYukleyici.Yukle(kurallar, "k.json", senaryo.Senaryo!.OlayTurleri);
            Assert.True(kural.Basarili, string.Join("\n", kural.Hatalar));
            var eylem = KararFikstur.Yukle(Eylemler);
            return Simulasyon.Olustur(
                senaryo.Senaryo!, kural.Kurallar, seed, new ZihinAyarlari { KanitSonumuTickBasina = 0m }, eylemler: eylem, secici: secici, dedikoduAyarlari: ayar);
        }

        // Oyuncu Ayşe'ye (ve istenirse bir ikinci önermeyi) tick 1'de anlatır.
        private static void OyuncuAnlatir(Simulasyon sim, params string[] onermeler)
        {
            foreach (var o in onermeler)
            {
                sim.Submit(OyuncuAnlatimiTests.Anlat("Ayse", o));
            }

            sim.Tick(1);
        }

        private static IEnumerable<Olay> Anlatilar(Simulasyon sim) => sim.Gunluk.Olaylar.Where(o => o.Tur == "anlatti" && o.KuralId.StartsWith("eylem:", StringComparison.Ordinal));

        // ---- anlatim: olaylar, algi, kanit, kayit ----

        [Fact]
        public void Secilen_konu_sayisi_kadar_anlatti_olayi_yazilir_her_biri_ayri_konu_ve_dayanakli()
        {
            var sim = Kur(new Kaydedici().Sec("Ayse", 2, "anlat|Can"));
            OyuncuAnlatir(sim, Borc, Kardes);
            sim.Tick(1);

            var olaylar = Anlatilar(sim).ToList();
            Assert.Equal(2, olaylar.Count);
            Assert.All(olaylar, o =>
            {
                Assert.Equal("Ayse", o.Fail);
                Assert.Equal("Can", o.Hedef);
                Assert.Equal("han", o.Mekan);
                Assert.Equal("eylem:anlat", o.KuralId);
            });
            Assert.Equal(new[] { Borc, Kardes }.OrderBy(x => x, StringComparer.Ordinal), olaylar.Select(o => o.Deger("onerme")).OrderBy(x => x, StringComparer.Ordinal));
            Assert.All(olaylar, o => Assert.Equal(new[] { "gorunurluk", "kok", "onerme" }, o.Yuk.Select(kv => kv.Key).ToArray()));
            Assert.All(olaylar, o => Assert.Equal("sessiz", o.Deger("gorunurluk")));
            Assert.All(olaylar, o => Assert.Equal("oyuncu", o.Deger("kok")));
        }

        [Fact]
        public void Nedenler_karar_olayi_ve_konusanin_dayanak_kanitidir()
        {
            var sim = Kur(new Kaydedici().Sec("Ayse", 2, "anlat|Can"));
            OyuncuAnlatir(sim, Borc);
            sim.Tick(1);

            var anlatti = Anlatilar(sim).Single();
            var karar = sim.SonKararOlayi("Ayse")!;
            var dayanak = sim.Zihinler["Ayse"].Kanitlar(Borc).Single();
            Assert.Equal(new[] { karar.Id, dayanak.OlayId }.OrderBy(x => x).ToArray(), anlatti.NedenOlaylar.ToArray());
        }

        [Fact]
        public void Dinleyene_duydu_kanit_konusana_aktarim_kaydi_yazilir_tanik_ve_baskasi_hicbir_sey_almaz()
        {
            var sim = Kur(new Kaydedici().Sec("Ayse", 2, "anlat|Can"));
            OyuncuAnlatir(sim, Borc);
            sim.Tick(1);

            var anlatti = Anlatilar(sim).Single();
            var kanit = sim.Zihinler["Can"].Kanitlar(Borc).Single();
            Assert.Equal(KanitTuru.Duydu, kanit.Tur);
            Assert.Equal("Ayse", kanit.Kaynak);
            Assert.Equal("oyuncu", kanit.Kok);
            Assert.Equal(0.49m, kanit.Guc);   // 0.7 × (0.7 + 0.003 × 0)
            var kanitOlayi = sim.Gunluk[kanit.OlayId];
            Assert.Equal(new[] { anlatti.Id }, kanitOlayi.NedenOlaylar.ToArray());
            Assert.Equal("eylem:anlat", kanitOlayi.KuralId);

            var kayit = Assert.Single(sim.Zihinler["Ayse"].Aktarimlar);
            Assert.Equal("Can", kayit.Kime);
            Assert.Equal(Borc, kayit.Onerme);
            Assert.False(kayit.Yalan);
            Assert.Equal(new[] { anlatti.Id }, sim.Gunluk[kayit.OlayId].NedenOlaylar.ToArray());

            Assert.Empty(sim.Zihinler["Zeki"].Kanitlar(Borc));
            Assert.Empty(sim.Zihinler["Zeki"].Aktarimlar);
            Assert.Empty(sim.Zihinler["Can"].Aktarimlar);
        }

        [Fact]
        public void Algi_akisi_konusan_ve_dinleyene_anlatti_gozlemi_ve_hafiza_yazar_tanik_sessizde_almaz()
        {
            var sim = Kur(new Kaydedici().Sec("Ayse", 2, "anlat|Can"));
            OyuncuAnlatir(sim, Borc);
            sim.Tick(1);

            var anlatti = Anlatilar(sim).Single();
            Assert.Equal(0.9m, sim.Zihinler["Can"].Guven("anlatti(Ayse,Can)", 2));
            Assert.Equal(0.9m, sim.Zihinler["Ayse"].Guven("anlatti(Ayse,Can)", 2));
            Assert.Contains(sim.Zihinler["Can"].HafizaKayitlari, h => h.OlayId == anlatti.Id);
            Assert.DoesNotContain(sim.Zihinler["Zeki"].Onermeler, o => o.StartsWith("anlatti", StringComparison.Ordinal));
            Assert.DoesNotContain(sim.Zihinler["Zeki"].HafizaKayitlari, h => h.OlayId == anlatti.Id);
        }

        [Fact]
        public void Acik_gorunurlukte_tanik_konusmayi_gorur_icerigini_duymaz()
        {
            var sim = Kur(new Kaydedici().Sec("Ayse", 2, "anlat_acik|Can"));
            OyuncuAnlatir(sim, Borc);
            sim.Tick(1);

            Assert.Equal(0.9m, sim.Zihinler["Zeki"].Guven("anlatti(Ayse,Can)", 2));
            Assert.Empty(sim.Zihinler["Zeki"].Kanitlar(Borc));
            Assert.Single(sim.Zihinler["Can"].Kanitlar(Borc));
            sim.Tick(1);
            Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.Tur == "modifier_eklendi" && o.Deger("npc") == "Zeki");
        }

        [Fact]
        public void Kural_anlatimin_konusuna_gore_yalnizca_dinleyen_icin_tetiklenir()
        {
            var sim = Kur(new Kaydedici().Sec("Ayse", 2, "anlat_acik|Can"));
            OyuncuAnlatir(sim, Borc);
            sim.Tick(2);

            Assert.True(sim.Zihinler["Can"].ModifierVar("Ayse", "aileme_iftira", 3));
            Assert.False(sim.Zihinler["Zeki"].ModifierVar("Ayse", "aileme_iftira", 3));   // tanık yalnızca konuşmayı gördü
            Assert.False(sim.Zihinler["Ayse"].ModifierVar("Ayse", "aileme_iftira", 3));   // konuşan içeriği "duyan" değil
        }

        [Fact]
        public void Why_zinciri_dinleyenden_onceki_anlatana_ve_oyuncu_eylemine_iner()
        {
            var sim = Kur(new Kaydedici().Sec("Ayse", 2, "anlat|Can"));
            OyuncuAnlatir(sim, Borc);
            sim.Tick(2);

            var modifier = sim.Gunluk.Olaylar.Last(o => o.Tur == "modifier_eklendi" && o.Deger("npc") == "Can");
            var zincir = sim.Neden(modifier.Id);

            Assert.Contains(zincir, o => o.Tur == "anlatti" && o.Fail == "Ayse" && o.KuralId == "eylem:anlat");
            Assert.Contains(zincir, o => o.Tur == "karar_verildi" && o.Fail == "Ayse");
            Assert.Contains(zincir, o => o.Tur == "anlatti" && o.Fail == "oyuncu" && o.KuralId == "player");
        }

        [Fact]
        public void Zincir_oyuncudan_dorduncu_kisiye_iner_ve_her_adimda_guc_azalir()
        {
            var sim = Kur(new Kaydedici().Sec("Ayse", 2, "anlat|Can").Sec("Can", 3, "anlat|Zeki"));
            OyuncuAnlatir(sim, Borc);
            sim.Tick(2);

            var ayse = sim.Zihinler["Ayse"].Kanitlar(Borc).Single().Guc;
            var can = sim.Zihinler["Can"].Kanitlar(Borc).Single().Guc;
            var zeki = sim.Zihinler["Zeki"].Kanitlar(Borc).Single();
            Assert.True(ayse > can && can > zeki.Guc, $"{ayse} > {can} > {zeki.Guc}");
            Assert.Equal(0.343m, zeki.Guc);
            Assert.Equal("oyuncu", zeki.Kok);
            Assert.Equal("Can", zeki.Kaynak);
        }

        [Fact]
        public void Guc_esigin_altinda_kanit_yazilmaz_soylenti_olur_ama_anlatim_ve_kayit_kalir()
        {
            var sim = Kur(new Kaydedici().Sec("Ayse", 2, "anlat|Can"), ayar: new DedikoduAyarlari { GucEsigi = 0.5m });
            OyuncuAnlatir(sim, Borc);   // 0.7 ≥ 0.5: Ayşe duyar
            sim.Tick(1);                // 0.7 × 0.7 = 0.49 < 0.5: Can'a kanıt yazılmaz

            Assert.Single(Anlatilar(sim));
            Assert.Empty(sim.Zihinler["Can"].Kanitlar(Borc));
            Assert.Single(sim.Zihinler["Ayse"].Aktarimlar);
        }

        [Fact]
        public void Yanki_freni_A_B_C_A_zincirinde_A_kendi_soylentisine_daha_cok_inanmaz()
        {
            var sim = Kur(new Kaydedici().Sec("Ayse", 2, "anlat|Can").Sec("Can", 3, "anlat|Zeki").Sec("Zeki", 4, "anlat|Ayse"));
            OyuncuAnlatir(sim, Borc);
            var once = sim.Zihinler["Ayse"].Guven(Borc, 1);
            sim.Tick(3);

            Assert.Equal(4, sim.SimdikiTick);
            Assert.Contains(sim.Gunluk.Olaylar, o => o.Tur == "anlatti" && o.Fail == "Zeki" && o.Hedef == "Ayse");
            var kanitlar = sim.Zihinler["Ayse"].Kanitlar(Borc);
            Assert.Single(kanitlar);
            Assert.Equal(once, sim.Zihinler["Ayse"].Guven(Borc, 4));
        }

        [Fact]
        public void Konu_onu_duydugu_kisiye_geri_anlatilmaz()
        {
            var k = new Kaydedici().Sec("Ayse", 2, "anlat|Can");
            var sim = Kur(k);
            OyuncuAnlatir(sim, Borc);
            sim.Tick(2);

            // Tick 3: Can Ayşe'den duydu; Ayşe'ye anlatamaz, Zeki'ye anlatabilir. (Can'ın aday listesi kaydedildi.)
            Assert.DoesNotContain("anlat|Ayse", k.Adaylar["Can@3"]);
            Assert.Contains("anlat|Zeki", k.Adaylar["Can@3"]);
        }

        [Fact]
        public void Ayni_konu_ayni_kisiye_iki_kez_anlatilmaz_gercek_secici_ile()
        {
            var sim = Kur(seed: 7);
            OyuncuAnlatir(sim, Borc);
            sim.Tick(30);

            var tekrar = Anlatilar(sim).GroupBy(o => o.Fail + ">" + o.Hedef + ":" + o.Deger("onerme")).Where(g => g.Count() > 1).ToList();
            Assert.Empty(tekrar);
            Assert.NotEmpty(Anlatilar(sim));
        }

        [Fact]
        public void Tek_durum_goruntusu_bu_tick_duyulan_ayni_tickteki_kararlari_etkilemez()
        {
            var secici = new Kaydedici().Sec("Ayse", 2, "anlat|Can");
            var sim = Kur(secici);
            OyuncuAnlatir(sim, Borc);
            sim.Tick(2);

            Assert.False(secici.Adaylar.ContainsKey("Can@2"));          // Can tick 2'de henüz bir şey bilmiyordu: aday yok
            Assert.True(secici.Adaylar.ContainsKey("Can@3"));           // tick 3'te biliyor
            Assert.Single(sim.Zihinler["Can"].Kanitlar(Borc));
        }

        [Fact]
        public void Npc_tanim_sirasi_degisse_de_sonuc_ayni()
        {
            string[] Calistir(string sira)
            {
                var sim = Kur(npcSirasi: sira, seed: 11);
                OyuncuAnlatir(sim, Borc, Kardes);
                sim.Tick(40);
                return new[] { sim.SonHash, GunlukImzasi.Al(sim) };
            }

            Assert.Equal(Calistir("Ayse,Can,Zeki,Veli"), Calistir("Veli,Zeki,Can,Ayse"));
        }

        [Fact]
        public void Ayni_seed_ayni_girdi_ayni_hash_ve_gunluk_farkli_seed_farkli_olabilir()
        {
            string[] Calistir(ulong seed)
            {
                var sim = Kur(seed: seed);
                OyuncuAnlatir(sim, Borc, Kardes);
                sim.Tick(40);
                return new[] { sim.SonHash, GunlukImzasi.Al(sim) };
            }

            Assert.Equal(Calistir(5), Calistir(5));
        }

        [Fact]
        public void Oyuncuya_anlatim_dinleyende_kanit_yazmaz_ama_olay_ve_kayit_yazilir()
        {
            // Ayşe borcu bizzat gördü (kök kendisi); oyuncuya anlatabilir. (Oyuncudan duyduğunu oyuncuya geri anlatamaz.)
            var sim = Kur(new Kaydedici().Sec("Ayse", 3, "anlat_oyuncuya|oyuncu"));
            sim.Submit(new PlayerAction("haber"));
            sim.Tick(3);

            var olay = Anlatilar(sim).Single();
            Assert.Equal("oyuncu", olay.Hedef);
            Assert.Equal("Ayse", olay.Deger("kok"));
            Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.Tur == "kanit_eklendi" && o.Deger("tur") == "duydu");
            var kayit = Assert.Single(sim.Zihinler["Ayse"].Aktarimlar);
            Assert.Equal("oyuncu", kayit.Kime);
            Assert.False(kayit.Yalan);
        }

        [Fact]
        public void Oyuncudan_duyulan_konu_oyuncuya_geri_anlatilmaz()
        {
            var secici = new Kaydedici();
            var sim = Kur(secici);
            OyuncuAnlatir(sim, Borc);
            sim.Tick(2);

            Assert.DoesNotContain(secici.Adaylar["Ayse@2"], a => a.StartsWith("anlat_oyuncuya", StringComparison.Ordinal));
        }

        [Fact]
        public void Konusan_dinleyenin_zihnini_okumaz_karar_dinleyenin_ic_durumundan_bagimsizdir()
        {
            (string[] Karar, decimal CanKanitGucu) Kos(bool guvensiz)
            {
                var sim = Kur(new Kaydedici().Sec("Ayse", 2, "anlat|Can"));
                if (guvensiz)
                {
                    // Can Ayşe'ye hiç güvenmiyor ve ondan nefret ediyor: bu, Ayşe'nin kararını DEĞİL, yalnızca yazılacak kanıtın gücünü etkiler.
                    sim.Zihinler["Can"].Uygula(ZihinOlaylari.EksenDegisti(9000, 0, 0, "Can", "Ayse", "guven", -100m, new long[0], "test"));
                    sim.Zihinler["Can"].Uygula(ZihinOlaylari.ModifierEklendi(9001, 0, 1, "Can", "Ayse", "hakaret_etti", -30m, 500, new long[0], "test"));
                }

                OyuncuAnlatir(sim, Borc, Kardes);
                sim.Tick(1);
                var anlatilar = Anlatilar(sim).Select(o => o.Deger("onerme") + "|" + string.Join(",", o.NedenOlaylar) + "|" + o.Deger("kok") + "|" + o.Deger("gorunurluk"));
                return (new[] { KararIziYazici.Yaz(sim.KararIzi("Ayse")!), string.Join(";", anlatilar) }, sim.Zihinler["Can"].Kanitlar(Borc).Single().Guc);
            }

            var iyi = Kos(false);
            var kotu = Kos(true);

            Assert.Equal(iyi.Karar, kotu.Karar);                // konular, puanlar, nedenler, kök: Ayşe'nin kendi zihninden
            Assert.Equal(0.49m, iyi.CanKanitGucu);
            Assert.Equal(0.28m, kotu.CanKanitGucu);             // 0.7 × (0.7 + 0.003 × −100)
        }

        // ---- yalan gizliligi (Teslim 1) ----

        // yalan=false: Ayşe Borc'a inanır ve Borc'u anlatır. yalan=true: Ayşe degil(Borc)'a inanır ve Borc'u söyler.
        // İki dünyada da dinleyenin ve tanığın gördüğü önerme AYNI (Borc) olduğundan fark yalnızca yalan olup olmamasındadır.
        private static Simulasyon YalanDunyasi(bool yalan)
        {
            var sim = Kur(new Kaydedici().Sec("Ayse", 2, yalan ? "yalan_borc|Can" : "anlat|Can"));
            OyuncuAnlatir(sim, yalan ? DegilBorc : Borc);
            sim.Tick(1);
            return sim;
        }

        // Dinleyen ve tanık için sözlüğün her fonksiyonuna dair gözlenebilir özet; anlatılan önerme parametredir.
        private static string[] Ozet(Simulasyon sim, string npc, string anlatilan)
        {
            var cozucu = new ZihinSorguCozucusu(sim.Dunya, sim.Zihinler, sim.Gunluk, sim.SimdikiTick);
            var baglam = new SorguBaglami(npc, "oyuncu", olayFail: "Ayse", olayHedef: "Can");
            var sonuc = new List<string>();
            foreach (var alan in new[]
            {
                "kisilik(durustluk)", "eksen(Ayse, guven)", "fikir(Ayse)", "fikir_bandi(Ayse)", "modifier_var(Ayse, aileme_iftira)", "duygu(ofke)",
                "stres()", "inanc(" + anlatilan + ")", "inanc(borclu(*,*,*))", "inanc(degil(borclu(*,*,*)))", "kaynak(" + anlatilan + ", Ayse)",
                "kaynak(borclu(*,*,*), oyuncu)", "hafiza_var(anlatti, Ayse, 100)", "yalan_soyledi(Ayse)", "yalan_soyledi(Can)", "yalan_soyledi(oyuncu)",
                "ayni_mekanda(Ayse)", "olay_rolu(fail)", "olay_rolu(hedef)", "olay_rolu(tanik)", "olay_konusu(borclu(*,*,*))", "olay_konusu(degil(borclu(*,*,*)))",
                "inanc(anlatti(Ayse,Can))", "kaynak(anlatti(Ayse,Can), Ayse)",
            })
            {
                var cagri = IfadeAyristirici.Ayristir(alan);
                SorguBaglami b = baglam;
                var d = cozucu.Coz(cagri, b);
                sonuc.Add(alan.Replace(anlatilan, "ANLATILAN") + "=" + d);
            }

            var zihin = sim.Zihinler[npc];
            sonuc.Add("onermeler=" + string.Join(";", zihin.Onermeler.Select(o => o.Replace(anlatilan, "ANLATILAN"))));
            sonuc.Add("aktarimlar=" + zihin.Aktarimlar.Count);
            sonuc.Add("kanonik_T=" + zihin.KanonikMetin().Contains("T|"));
            return sonuc.ToArray();
        }

        [Fact]
        public void Yalan_ve_dogru_anlatim_dis_olayda_ayni_biciminde_cikar()
        {
            var dogru = YalanDunyasi(false);
            var yalan = YalanDunyasi(true);

            var d = Anlatilar(dogru).Single();
            var y = Anlatilar(yalan).Single();

            Assert.Equal(d.Tur, y.Tur);
            Assert.Equal(d.Fail, y.Fail);
            Assert.Equal(d.Hedef, y.Hedef);
            Assert.Equal(d.Mekan, y.Mekan);
            Assert.Equal(d.Yuk.Select(kv => kv.Key), y.Yuk.Select(kv => kv.Key));
            Assert.Equal(d.Deger("gorunurluk"), y.Deger("gorunurluk"));
            Assert.Equal(d.NedenOlaylar.Count, y.NedenOlaylar.Count);
            Assert.Equal("Ayse", y.Deger("kok"));    // yalanın kökü konuşandır
            Assert.DoesNotContain("yalan", string.Join("|", y.Yuk.Select(kv => kv.Key + "=" + kv.Value)));
            Assert.Equal(Borc, y.Deger("onerme"));
            Assert.Equal(Borc, d.Deger("onerme"));
        }

        [Fact]
        public void Yalan_dinleyen_ve_tanik_icin_butun_sorgularda_dogru_anlatimdan_ayirt_edilemez()
        {
            var dogru = YalanDunyasi(false);
            var yalan = YalanDunyasi(true);

            foreach (var kim in new[] { "Can", "Zeki", "Veli" })
            {
                Assert.Equal(Ozet(dogru, kim, Borc), Ozet(yalan, kim, Borc));
            }
        }

        [Fact]
        public void Yalan_bilgisi_yalnizca_konusanin_zihnindeki_algilanmayan_olaydadir()
        {
            var dogru = YalanDunyasi(false);
            var yalan = YalanDunyasi(true);

            Assert.False(dogru.Zihinler["Ayse"].YalanSoyledi("Can"));
            Assert.True(yalan.Zihinler["Ayse"].YalanSoyledi("Can"));
            Assert.Equal(1, yalan.Gunluk.Olaylar.Count(o => o.Tur == "aktarim_kaydedildi"));
            Assert.DoesNotContain("T|", yalan.Zihinler["Can"].KanonikMetin());
            Assert.DoesNotContain("T|", yalan.Zihinler["Zeki"].KanonikMetin());

            // Algılanmaz ve kural tetiklemez: sonraki tick'te hiçbir olayın nedeni aktarim_kaydedildi olmaz.
            yalan.Tick(3);
            var kayit = yalan.Gunluk.Olaylar.Single(o => o.Tur == "aktarim_kaydedildi");
            Assert.DoesNotContain(yalan.Gunluk.Olaylar, o => o.NedenOlaylar.Contains(kayit.Id));
            Assert.DoesNotContain(yalan.Gunluk.Olaylar, o => o.Tur == "kanit_eklendi" && o.Deger("onerme").StartsWith("aktarim", StringComparison.Ordinal));
        }

        [Fact]
        public void Yalanin_dayanagi_celistigi_inancin_kanitidir_ve_why_yalani_eylem_idsinden_gosterebilir()
        {
            var yalan = YalanDunyasi(true);
            var y = Anlatilar(yalan).Single();
            var dayanak = yalan.Zihinler["Ayse"].Kanitlar(DegilBorc).Single();

            Assert.Contains(dayanak.OlayId, y.NedenOlaylar);
            Assert.Equal("eylem:yalan_borc", y.KuralId);
        }

        [Fact]
        public void Yalanin_gucu_celistigi_kanitin_gucu_carpi_guven_carpanidir_sabit_degil()
        {
            var yalan = YalanDunyasi(true);
            var dogru = YalanDunyasi(false);

            var yalanGuc = yalan.Zihinler["Can"].Kanitlar(Borc).Single().Guc;
            var dogruGuc = dogru.Zihinler["Can"].Kanitlar(Borc).Single().Guc;
            Assert.Equal(dogruGuc, yalanGuc);
            Assert.Equal(0.49m, yalanGuc);
        }

        [Fact]
        public void Yanlis_inandigi_seyi_dogru_diye_anlatan_npc_yalanci_sayilmaz_dunya_gercegi_onemsizdir()
        {
            // Dünyada borç GERÇEK; Ayşe ise tersine inanıyor ve onu "dogru" anlatıyor.
            var sim = Kur(new Kaydedici().Sec("Ayse", 2, "anlat|Can"), gercekler: "\"" + Borc + "\"");
            OyuncuAnlatir(sim, DegilBorc);
            sim.Tick(1);

            Assert.Equal(DegilBorc, Anlatilar(sim).Single().Deger("onerme"));
            Assert.False(sim.Zihinler["Ayse"].YalanSoyledi("Can"));
            Assert.False(sim.Zihinler["Ayse"].Aktarimlar.Single().Yalan);
        }

        [Fact]
        public void Yalan_inanilmayan_bir_seye_celismiyorsa_aday_degildir()
        {
            var secici = new Kaydedici().Sec("Ayse", 2, "anlat|Can");
            var sim = Kur(secici);
            OyuncuAnlatir(sim, Kardes);   // Ayşe Borc'a inanmıyor: yalan_soyle için çelişen inanç yok
            sim.Tick(1);

            Assert.DoesNotContain(secici.Adaylar["Ayse@2"], a => a.StartsWith("yalan_soyle", StringComparison.Ordinal));
        }
    }
}
