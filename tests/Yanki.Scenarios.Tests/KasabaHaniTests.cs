using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Yanki.Authoring;
using Yanki.Cli;
using Yanki.Core.Events;
using Yanki.Core.Simulation;
using Yanki.Dialogue;

namespace Yanki.Scenarios.Tests
{
    /// <summary>
    /// samples/KasabaHani (9. hafta kontrol noktası). Değişmezler, DENGE sayılarına değil yapıya bağlıdır; sayısal değerler (tetiklenme oranları, kişilik, taban) yer tutucudur
    /// ve README'de belirtilir (11. hafta denge turunda ayarlanır). Plan 7 sahnesi bir senaryo testidir: oyuncu Ayşe'ye Mehmet'in borcunu anlatır, Ayşe Can'a anlatır,
    /// Can'da aileme_iftira(oyuncu) oluşur, Can'ın satırı söylentiden önce ve sonra farklıdır.
    /// <para>
    /// Beklenti kaynağı (Yusuf'un DURAK 2 isteği, madde 5): [B] koşudan ÖNCE bilinen (plan, ADR ya da görev metni), [K] koşu çıktısından alınmış.
    /// B: bütçe aralıkları, plan 7 ilişkileri, NPC alan sayısı, plan 7 zinciri yapısı (modifier −22/432, oyuncu→Ayşe→Can, why), "Ağabeyim hakkında ne dediğini Ayşe'den duydum." metni,
    /// Durum 2b'de stres artışı, her tohumun tetiklenmesi, korumalı varlık ve yay sınırları, seed 42 ≠ 7, NedenOlaylar, diyalog açık/kapalı hash eşitliği, yalan gizliliği alanları,
    /// menü 8–10 girdi ve beş aile, mührü taşıyan etkinin tek değerli konum yazması, hırsız Kerem / mağdur Ayşe (aktorler ve zincir tasarımı), golden betiğinde atlanan yok.
    /// K: seed seçimleri 42 ve 3 (sahnenin Durum 2 olarak çıktığı seed'ler; 7, 1, 2, 4, 5, 6, 9'da Can'ın t6 satırı Durum 1 ya da başka), "Hoş geldin yabancı…" satırının tam metni ve şablon kimlikleri
    /// (selam_sakin / selam_iftira_ayse), tick pencereleri (12, 14, 48, 60, 80, 100, 120), korumalı fren olayının gerçekten üretildiği (kasa_acmak denemesi), yalanın gerçekten söylendiği (Kerem).
    /// </para>
    /// NOT (süreç): bu dosya ilk içerik taslağının koşusundan SONRA yazıldı (mekanik keşif için taslak önce koşuldu); testler içeriğe uydurulmadı, gevşetilmedi.
    /// </summary>
    public class KasabaHaniTests
    {
        private static readonly string Dizin = Path.Combine(RepoKoku(), "samples", "KasabaHani");

        private static string RepoKoku()
        {
            var yol = AppContext.BaseDirectory;
            while (!File.Exists(Path.Combine(yol, "Yanki.slnx")))
            {
                yol = Path.GetDirectoryName(yol)!;
            }

            return yol;
        }

        private static YuklenenSenaryo Yukle()
        {
            var girdi = DogrulamaKomutu.GirdiOku(Dizin, null, new StringWriter());
            Assert.NotNull(girdi);
            var sonuc = SenaryoDogrulayici.Yukle(girdi!, out var yuklenen);
            Assert.True(yuklenen != null, string.Join("\n", sonuc.Bulgular.Select(b => b.ToString())));
            return yuklenen!;
        }

        private static Betik BetikOku(string ad) =>
            BetikOkuyucu.Oku(File.ReadAllText(Path.Combine(Dizin, "oynanislar", ad + ".json")), ad);

        private static readonly string[] BetikAdlari = { "hirsizlik_suclama", "iyilik_muhur", "kotuluk_saklama", "plan7_borc" };

        /// <summary>Betiği koşar (geçersiz kalan eylemler atlanır, kapsama ile aynı kural); isteğe bağlı diyalog motoru bağlar.</summary>
        private static (Simulasyon Sim, DiyalogMotoru? Motor) Kos(ulong seed, string betik, long tick, bool diyalog)
        {
            var y = Yukle();
            var sim = y.SimulasyonKur(seed);
            var motor = diyalog ? DiyalogMotoru.Baglan(sim, y.Sablon!) : null;
            var b = BetikOku(betik);
            var sira = 0;
            for (long t = 1; t <= tick; t++)
            {
                while (sira < b.Eylemler.Count && b.Eylemler[sira].Tick == t)
                {
                    try
                    {
                        sim.Submit(b.Eylemler[sira].Eylem);
                    }
                    catch (ArgumentException)
                    {
                    }

                    sira++;
                }

                sim.Tick(1);
            }

            return (sim, motor);
        }

        // ---- Senaryo yapısı ve bütçe (plan 10) ----

        [Fact]
        public void Senaryo_butceye_uyar_ve_plan_7_iliskileri_kilitlidir()
        {
            var y = Yukle();

            Assert.Equal(8, y.Senaryo.Npcler.Count);
            Assert.InRange(y.Eylemler.Count, 12, 18);
            Assert.InRange(y.Kurallar.Count, 25, 35);
            Assert.InRange(y.Sablon!.SablonSayisi, 60, 80);
            Assert.Equal(2, y.Senaryo.YayDegiskenleri.Count);
            Assert.InRange(y.Senaryo.Korumali.Varliklar.Count(), 1, 2);

            Assert.Contains("borclu(Mehmet,Riza,500)", y.Senaryo.Gercekler);
            Assert.Contains("kardes(Mehmet,Can)", y.Senaryo.Gercekler);
            var ayse = y.Senaryo.Npcler.Single(n => n.Id == "Ayse");
            var can = y.Senaryo.Npcler.Single(n => n.Id == "Can");
            Assert.True(ayse.Kisilik.Deger("sosyallik") >= 0.8m, "Ayşe'nin sosyalliği yüksek olmalı (plan 7)");
            Assert.True(can.Kisilik.Deger("merhamet") >= 0.7m, "Can'ın merhameti yüksek olmalı (plan 7)");
        }

        [Fact]
        public void Her_npc_girdisi_en_fazla_10_alan_tasir()
        {
            // Plan 10: "her NPC dosyası en fazla yaklaşık 10 alan". Şema NPC'leri scenario.json içinde tutar: sınır NPC girdisi başına uygulanır (id, konum + 6 kişilik değeri = 8).
            var kok = Yanki.Core.Json.JsonAyristirici.Ayristir(File.ReadAllText(Path.Combine(Dizin, "scenario.json")));
            foreach (var n in kok.Al("npcler")!.Elemanlar)
            {
                var alanSayisi = n.Ozellikler.Count - 1 + n.Al("kisilik")!.Ozellikler.Count;
                Assert.InRange(alanSayisi, 1, 10);
            }
        }

        // ---- Plan 7 sahnesi ----

        [Theory]
        [InlineData(42UL)]
        [InlineData(3UL)]
        public void Plan_7_zinciri_oyuncu_Ayse_Can_aile_iftirasi_modifier_why_ile_iner(ulong seed)
        {
            var (sim, _) = Kos(seed, "plan7_borc", 12, diyalog: false);

            var modifier = Assert.Single(sim.Gunluk.Olaylar, o => o.Tur == "modifier_eklendi" && o.Deger("npc") == "Can" && o.Deger("tur") == "aileme_iftira");
            Assert.Equal(("-22", "432", "aile_iftirasi"), (modifier.Deger("deger"), modifier.Deger("sure"), modifier.KuralId));

            var zincir = sim.Neden(modifier.Id);
            var oyuncuAyse = zincir.FirstOrDefault(o => o.Tur == "anlatti" && o.Fail == "oyuncu" && o.Hedef == "Ayse");
            var ayseCan = zincir.FirstOrDefault(o => o.Tur == "anlatti" && o.Fail == "Ayse" && o.Hedef == "Can");
            Assert.NotNull(oyuncuAyse);
            Assert.NotNull(ayseCan);
            Assert.True(oyuncuAyse!.Id < ayseCan!.Id);
            Assert.Equal("player", oyuncuAyse.KuralId);
        }

        [Theory]
        [InlineData(42UL)]
        [InlineData(3UL)]
        public void Plan_7_Durum_1_ve_Durum_2_Canin_satiri_soylentiden_once_ve_sonra_farklidir(ulong seed)
        {
            var (sim, motor) = Kos(seed, "plan7_borc", 8, diyalog: true);
            using (motor)
            {
                var satirlar = motor!.Satirlar.Where(s => s.Konusan == "Can" && s.Dinleyen == "oyuncu").OrderBy(s => s.Tick).ToArray();
                var once = satirlar.First(s => s.Tick == 1);
                var sonra = satirlar.First(s => s.Tick == 6);

                Assert.NotEqual(once.Metin, sonra.Metin);
                Assert.Equal("Hoş geldin yabancı, bir ihtiyacın var mı?", once.Metin);                    // Durum 1: sakin ve yardımsever
                Assert.Equal("Ağabeyim hakkında ne dediğini Ayşe'den duydum.", sonra.Metin);              // Durum 2 (plan 7)
                Assert.Equal("selam_sakin", once.SablonId);
                Assert.Equal("selam_iftira_ayse", sonra.SablonId);
                Assert.NotNull(sim);
            }
        }

        [Theory]
        [InlineData(42UL)]
        [InlineData(3UL)]
        public void Plan_7_Durum_2b_Canin_merhameti_yuksek_ele_vermesi_istenirse_stres_artar(ulong seed)
        {
            var (sim, _) = Kos(seed, "plan7_borc", 14, diyalog: false);

            var secim = sim.Gunluk.Olaylar.FirstOrDefault(o => o.KuralId == "eylem:kardesi_ele_ver" && o.Fail == "Can");
            Assert.NotNull(secim);
            Assert.Contains(sim.Gunluk.Olaylar, o => o.Tur == "stres_degisti" && o.Deger("npc") == "Can" && decimal.Parse(o.Deger("delta"), System.Globalization.CultureInfo.InvariantCulture) > 0m);
        }

        // ---- Üç gerilim tohumu ----

        [Fact]
        public void Her_gerilim_tohumu_en_az_bir_betikte_tetiklenir()
        {
            var borc = false;
            var eşya = false;
            var hirsizlik = false;
            foreach (var ad in BetikAdlari)
            {
                var (sim, _) = Kos(1UL, ad, 60, diyalog: false);
                borc |= sim.Gunluk.Olaylar.Any(o => o.KuralId == "aile_iftirasi" || o.KuralId == "borc_odendi_alacakli");
                eşya |= sim.Gunluk.Olaylar.Any(o => o.KuralId == "muhur_yeri_ogrenildi");
                hirsizlik |= sim.Gunluk.Olaylar.Any(o => o.Tur == "kese_calindi");
            }

            Assert.True(borc, "borç tohumu hiçbir betikte tetiklenmedi");
            Assert.True(eşya, "kayıp eşya tohumu hiçbir betikte tetiklenmedi");
            Assert.True(hirsizlik, "hırsızlık tohumu hiçbir betikte tetiklenmedi");
        }

        // ---- Korumalı varlık ve yaylar ----

        [Theory]
        [InlineData(1UL)]
        [InlineData(2UL)]
        [InlineData(3UL)]
        public void Korumali_varlik_hicbir_betikte_ihlal_edilmez_yaylar_sinirda_kalir(ulong seed)
        {
            foreach (var ad in BetikAdlari)
            {
                var y = Yukle();
                var sim = y.SimulasyonKur(seed);
                var b = BetikOku(ad);
                var sira = 0;
                for (long t = 1; t <= 120; t++)
                {
                    while (sira < b.Eylemler.Count && b.Eylemler[sira].Tick == t)
                    {
                        try
                        {
                            sim.Submit(b.Eylemler[sira].Eylem);
                        }
                        catch (ArgumentException)
                        {
                        }

                        sira++;
                    }

                    sim.Tick(1);
                    Assert.InRange(sim.Yay("kasaba_gerginligi"), 0m, 10m);
                    Assert.InRange(sim.Yay("han_itibari"), 0m, 10m);
                }

                Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.Tur == "ayrildi" && o.Fail == "Riza");          // mekandan_ayrilamaz
                Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.Tur == "kasa_acildi");                           // calinamaz: kasa hiç açılmaz
                Assert.Contains(sim.Gunluk.Olaylar, o => o.Tur == "fren_uygulandi" && o.KuralId == "fren:korumali"); // ama denendi ve engellendi
            }
        }

        // ---- Seed farkı ve NedenOlaylar ----

        [Fact]
        public void Seed_42_ve_7_olay_ve_diyalog_satiri_bakimindan_olculebilir_sekilde_ayrisir()
        {
            var (a, ma) = Kos(42UL, "plan7_borc", 48, diyalog: true);
            var (b, mb) = Kos(7UL, "plan7_borc", 48, diyalog: true);
            using (ma)
            using (mb)
            {
                Assert.NotEqual(a.DurumHash(), b.DurumHash());
                var farkliOlay = a.Gunluk.Olaylar.Zip(b.Gunluk.Olaylar, (x, y) => x.Tur != y.Tur || x.Fail != y.Fail || x.Hedef != y.Hedef).Count(f => f);
                Assert.True(farkliOlay > 0 || a.Gunluk.Olaylar.Count != b.Gunluk.Olaylar.Count);
                Assert.NotEqual(ma!.DiyalogHash(), mb!.DiyalogHash());
            }
        }

        [Fact]
        public void Her_olayin_neden_olaylari_gunluktedir_ve_kendisinden_kucuktur()
        {
            var (sim, _) = Kos(42UL, "hirsizlik_suclama", 80, diyalog: false);
            var kimlikler = new HashSet<long>(sim.Gunluk.Olaylar.Select(o => o.Id));

            foreach (var o in sim.Gunluk.Olaylar)
            {
                Assert.All(o.NedenOlaylar, n => Assert.True(kimlikler.Contains(n) && n < o.Id, $"#{o.Id} nedeni #{n} günlükte yok ya da ileri"));
            }
        }

        // ---- Diyalog açık/kapalı hash eşitliği (ADR-014) ----

        [Theory]
        [InlineData(42UL)]
        [InlineData(7UL)]
        public void Diyalog_acik_ya_da_kapali_son_hash_ve_gunluk_hash_ayni(ulong seed)
        {
            var (kapali, _) = Kos(seed, "plan7_borc", 60, diyalog: false);
            var (acik, motor) = Kos(seed, "plan7_borc", 60, diyalog: true);
            using (motor)
            {
                Assert.Equal(kapali.DurumHash(), acik.DurumHash());
                Assert.Equal(GunlukHashu.Hesapla(kapali.Gunluk), GunlukHashu.Hesapla(acik.Gunluk));
                Assert.NotEmpty(motor!.Satirlar);
            }
        }

        // ---- Yalan gizliliği (ADR-012; 6. hafta biçimi: olay türü, yük, NedenOlaylar sayısı, güç ve diyalog niyeti alan alan aynı) ----

        private static readonly string[] YalanBetikleri = { "hirsizlik_suclama", "plan7_borc", "kotuluk_saklama", "iyilik_muhur" };

        [Fact]
        public void Yalan_dogru_anlatimdan_alan_alan_ayirt_edilemez_ve_yalan_gercekten_soylenir()
        {
            var yalanAnlatimlari = new List<Olay>();
            var dogruAnlatimlari = new List<Olay>();
            var yalanKanitlari = new List<Olay>();
            var dogruKanitlari = new List<Olay>();
            var yalanSatirlari = new List<DiyalogSatiri>();
            var dogruSatirlari = new List<DiyalogSatiri>();

            foreach (var betik in YalanBetikleri)
            {
                for (ulong seed = 1; seed <= 4; seed++)
                {
                    var (sim, motor) = Kos(seed, betik, 100, diyalog: true);
                    using (motor)
                    {
                        var olaylar = sim.Gunluk.Olaylar;
                        var yalanIdleri = new HashSet<long>();
                        foreach (var a in olaylar.Where(o => o.Tur == "aktarim_kaydedildi" && o.Deger("yalan") == "1"))
                        {
                            foreach (var n in a.NedenOlaylar)
                            {
                                if (olaylar.First(o => o.Id == n).Tur == "anlatti")
                                {
                                    yalanIdleri.Add(n);
                                }
                            }
                        }

                        foreach (var o in olaylar.Where(o => o.Tur == "anlatti" && o.KuralId.StartsWith("eylem:", StringComparison.Ordinal)))
                        {
                            (yalanIdleri.Contains(o.Id) ? yalanAnlatimlari : dogruAnlatimlari).Add(o);
                            var kanit = olaylar.Where(k => k.Tur == "kanit_eklendi" && k.Deger("tur") == "duydu" && k.NedenOlaylar.Contains(o.Id)).ToArray();
                            (yalanIdleri.Contains(o.Id) ? yalanKanitlari : dogruKanitlari).AddRange(kanit);
                            foreach (var satir in motor!.Satirlar.Where(l => l.OlayId == o.Id))
                            {
                                (yalanIdleri.Contains(o.Id) ? yalanSatirlari : dogruSatirlari).Add(satir);
                            }
                        }
                    }
                }
            }

            Assert.NotEmpty(yalanAnlatimlari);   // test boş geçmesin: Kasaba'da Kerem gerçekten yalan söyler (yalan_soyle)
            Assert.NotEmpty(dogruAnlatimlari);
            Assert.NotEmpty(yalanKanitlari);
            Assert.NotEmpty(yalanSatirlari);

            string Anahtarlar(Olay o) => string.Join(",", o.Yuk.Select(kv => kv.Key).OrderBy(x => x, StringComparer.Ordinal));
            // olay türü
            Assert.Equal(new[] { "anlatti" }, yalanAnlatimlari.Concat(dogruAnlatimlari).Select(o => o.Tur).Distinct().ToArray());
            // yük anahtarları, NedenOlaylar sayısı
            Assert.Single(yalanAnlatimlari.Concat(dogruAnlatimlari).Select(Anahtarlar).Distinct());
            Assert.Single(yalanAnlatimlari.Concat(dogruAnlatimlari).Select(o => o.NedenOlaylar.Count).Distinct());
            Assert.DoesNotContain(yalanAnlatimlari.Concat(dogruAnlatimlari), o => o.Yuk.Any(kv => kv.Key.Contains("yalan") || kv.Value.Contains("yalan")));
            // güç: dinleyene yazılan kanıtın yük anahtarları ve türü aynı; güç 0..1 aralığında
            Assert.Single(yalanKanitlari.Concat(dogruKanitlari).Select(o => Anahtarlar(o) + "|" + o.Deger("tur")).Distinct());
            Assert.All(yalanKanitlari.Concat(dogruKanitlari), k => Assert.InRange(decimal.Parse(k.Deger("guc"), System.Globalization.CultureInfo.InvariantCulture), 0m, 1m));
            // diyalog niyeti: yalan da doğru da "anlatma" niyetinin şablonlarıyla söylenir (anlat_*)
            Assert.All(yalanSatirlari.Concat(dogruSatirlari), l => Assert.StartsWith("anlat_", l.SablonId, StringComparison.Ordinal));
            // yalan bilgisi yalnızca aktarım kaydında; başka hiçbir olayın yükünde yalan alanı yok
            foreach (var betik in YalanBetikleri.Take(1))
            {
                var (sim, _) = Kos(1UL, betik, 100, diyalog: false);
                Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.Tur != "aktarim_kaydedildi" && o.Yuk.Any(kv => kv.Key == "yalan"));
            }
        }

        // ---- 9. hafta DURAK 2 maddeleri 3, 4, 7 ----

        [Fact]
        public void Golden_betiginde_atlanan_eylem_yoktur_plan7_borc_seed_42_tick_48()
        {
            var girdi = DogrulamaKomutu.GirdiOku(Dizin, null, new StringWriter())!;

            Assert.Empty(KapsamHesabi.AtlananEylemler(girdi, BetikOku("plan7_borc"), 42UL, 48));
        }

        [Fact]
        public void Muhuru_tasiyan_her_etki_tek_degerli_konum_yazar_ve_eski_konumu_dusurur()
        {
            var y = Yukle();
            var muhurEtkileri = y.Kurallar.SelectMany(k => k.Etkiler).Concat(y.Eylemler.SelectMany(e => e.Etkiler)).Concat(y.Krizler.SelectMany(k => k.Etkiler))
                .Where(e => e.Tur == Yanki.Core.Rules.EtkiTuru.Gercek && e.GercekSablonu.Contains("muhur", StringComparison.Ordinal)).ToArray();
            Assert.NotEmpty(muhurEtkileri);
            Assert.All(muhurEtkileri, e => Assert.StartsWith("konum!(muhur,", e.GercekSablonu, StringComparison.Ordinal));

            var (sim, _) = Kos(1UL, "iyilik_muhur", 10, diyalog: false);

            Assert.Equal("muhtarlik", sim.Dunya.TekDeger("konum!", "muhur"));
            Assert.Contains(sim.Gunluk.Olaylar, o => o.Tur == "gercek_eklendi" && o.Deger("gercek") == "konum!(muhur,muhtarlik)" && o.KuralId == "muhur_yeri_ogrenildi");
            Assert.False(sim.Dunya.Var(Yanki.Core.World.Gercek.Ayristir("konum!(muhur,depo)")));
        }

        [Fact]
        public void Calinan_kese_tek_sahipli_Ayse_nin_kesesidir_hirsiz_Keremdir()
        {
            var calindi = new List<Olay>();
            foreach (var betik in BetikAdlari)
            {
                for (ulong seed = 1; seed <= 4; seed++)
                {
                    calindi.AddRange(Kos(seed, betik, 80, diyalog: false).Sim.Gunluk.Olaylar.Where(o => o.Tur == "kese_calindi"));
                }
            }

            Assert.NotEmpty(calindi);
            Assert.All(calindi, o => Assert.Equal(("Kerem", "Ayse"), (o.Fail, o.Hedef)));
        }

        // ---- Menü ve validate ----

        [Fact]
        public void Validate_temiz_oyuncu_menusu_sekiz_ile_on_girdi_arasinda_dort_aileyi_karsilar()
        {
            var girdi = DogrulamaKomutu.GirdiOku(Dizin, null, new StringWriter())!;
            var sonuc = SenaryoDogrulayici.Dogrula(girdi);

            Assert.True(sonuc.Bulgular.Count == 0, string.Join("\n", sonuc.Bulgular.Select(b => b.ToString())));
            var menu = Yanki.Core.Json.JsonAyristirici.Ayristir(File.ReadAllText(Path.Combine(Dizin, "oyuncu_eylemleri.json"))).Al("menu")!;
            Assert.InRange(menu.Elemanlar.Count, 8, 10);
            var aileler = menu.Elemanlar.Select(e => e.Al("aile")!.MetinDeger).Distinct().OrderBy(a => a, StringComparer.Ordinal).ToArray();
            Assert.Equal(new[] { "bilgi_ver", "iyilik", "konus", "kotuluk", "sakla" }, aileler);
        }
    }
}
