using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Yanki.Cli;
using Yanki.Core.Events;
using Yanki.Core.Simulation;

namespace Yanki.Scenarios.Tests
{
    /// <summary>
    /// samples/HanGerilim uçtan uca (6. hafta kontrol noktası). Değişmezler KOŞUDAN ÖNCE yazıldı ve sonuca uydurulmadı (Yusuf'un kararı):
    /// seed 42, 24 tick. Akış: tick 1'de oyuncu Ayşe'ye borç ve kardeşlik söylentisini, Hatice'ye borç söylentisini, `borclu(Selim,Hatice,200)`'ü
    /// ve sırrı (`sir_sakliyor(Hatice,Mehmet)`) anlatır; tick 10'da Selim'e hakaret eder. Golden: <c>tests/golden/han-gerilim-1.json</c>.
    /// </summary>
    public class HanGerilimTests
    {
        private const string Borc = "borclu(Mehmet,Ayse,500)";

        internal static Simulasyon Calistir(ulong seed = 42)
        {
            var yol = Path.Combine(RepoKoku(), "samples", "HanGerilim", "scenario.json");
            var hata = new StringWriter();
            var sim = SenaryoDosyasi.Yukle(yol, seed, hata);
            Assert.True(sim != null, hata.ToString());
            Anlat(sim, "Ayse", Borc);
            Anlat(sim, "Ayse", "kardes(Mehmet,Can)");
            Anlat(sim, "Hatice", Borc);
            Anlat(sim, "Hatice", "borclu(Selim,Hatice,200)");
            Anlat(sim, "Hatice", "sir_sakliyor(Hatice,Mehmet)");
            sim!.Tick(9);
            sim.Submit(new PlayerAction("hakaret_et", "Selim"));
            sim.Tick(15);
            Assert.Equal(24, sim.SimdikiTick);
            return sim;
        }

        private static void Anlat(Simulasyon sim, string hedef, string onerme) =>
            sim.Submit(new PlayerAction("anlatti", hedef, new[] { new KeyValuePair<string, string>("onerme", onerme) }));

        private static IEnumerable<Olay> Olaylar(Simulasyon s, string tur, string fail) =>
            s.Gunluk.Olaylar.Where(o => o.Tur == tur && o.Fail == fail);

        private static IEnumerable<Olay> Yalanlar(Simulasyon s, string npc) =>
            s.Gunluk.Olaylar.Where(o => o.Tur == "aktarim_kaydedildi" && o.Deger("npc") == npc && o.Deger("yalan") == "1");

        // ---- I1: Ayşe → Can söylentisi (plan 7) ----

        [Theory]
        [InlineData(42UL)]
        [InlineData(7UL)]
        public void I1_Ayse_Cana_borc_soylentisini_t1de_anlatir_Can_oyuncuya_soguur_why_oyuncunun_eylemine_iner(ulong seed)
        {
            var sim = Calistir(seed);

            // t1'de fail Ayşe, hedef Can, önerme Borc.
            Assert.Contains(sim.Gunluk.Olaylar, o => o.Tur == "anlatti" && o.Tick == 1 && o.Fail == "Ayse" && o.Hedef == "Can" && o.Deger("onerme") == Borc && o.KuralId == "eylem:anlat");

            // aileme_iftira modifier'ının why zinciri bu anlatımdan oyuncunun eylemine iner.
            var modifier = sim.Gunluk.Olaylar.First(o => o.Tur == "modifier_eklendi" && o.Deger("npc") == "Can" && o.Deger("tur") == "aileme_iftira");
            Assert.Equal("oyuncu", modifier.Deger("hedef"));
            Assert.Equal("-22", modifier.Deger("deger"));
            var zincir = sim.Neden(modifier.Id);
            Assert.Contains(zincir, o => o.Tur == "anlatti" && o.Tick == 1 && o.Fail == "Ayse" && o.Hedef == "Can" && o.Deger("onerme") == Borc);
            Assert.Contains(zincir, o => o.Tur == "anlatti" && o.Fail == "oyuncu" && o.Hedef == "Ayse" && o.KuralId == "player");

            // Can'ın Borc kanıtının kökü her seed için oyuncudur.
            var kanitlar = sim.Zihinler["Can"].Kanitlar(Borc);
            Assert.All(kanitlar, k => Assert.Equal("oyuncu", k.Kok));
        }

        [Fact]
        public void I1_Seed_42_Canin_en_guclu_borc_kaniti_Haticenin_kanitidir_ve_Ayse_de_kaynak_olarak_korunur()
        {
            var sim = Calistir(42);
            var can = sim.Zihinler["Can"];

            // Ayşe t1'de 0.49 ile anlattı; Hatice t3'te aynı kökten 0.4886 ile anlattı. Güncel (sönmüş) güçle t3'te Ayşe 0.4880 < Hatice 0.4886:
            // en güçlü kanıt Hatice'nin (Kaynak, Guc, Tick, OlayId hepsi onun olayından). 7. hafta (G): Ayşe'nin kanıtı SİLİNMEZ,
            // Can söylentiyi Ayşe'den de duyduğunu unutmaz; güven yine kök başına tek sayılır.
            var kanit = can.EnGucluKanit(Borc, sim.SimdikiTick)!;
            Assert.Equal("Hatice", kanit.Kaynak);
            Assert.Equal(3, kanit.Tick);
            Assert.Equal(0.4886m, kanit.Guc);
            Assert.Equal("oyuncu", kanit.Kok);

            var kaynaklar = sim.KimdenDuydu("Can", Borc).Select(k => k.Kaynak).ToList();
            Assert.Contains("Ayse", kaynaklar);
            Assert.Contains("Hatice", kaynaklar);
            Assert.True(can.KaynakVar(Borc, "Ayse"));
            Assert.True(can.KaynakVar(Borc, "Hatice"));
            Assert.Equal(can.KanitGucu(kanit, sim.SimdikiTick), can.Guven(Borc, sim.SimdikiTick));   // tek kök: güven en güçlü kanıta eşit
        }

        [Fact]
        public void I1_Aileme_iftira_yalnizca_Mehmetin_kardesi_icin_calisir()
        {
            var sim = Calistir();

            var etkilenenler = sim.Gunluk.Olaylar.Where(o => o.Tur == "modifier_eklendi" && o.Deger("tur") == "aileme_iftira").Select(o => o.Deger("npc")).Distinct().ToArray();
            Assert.Equal(new[] { "Can" }, etkilenenler);
        }

        // ---- I2: Hatice yalan → stres → itiraf ----

        [Fact]
        public void I2_Hatice_iki_farkli_yalan_soyler_bir_kez_itiraf_eder_sonra_yalan_soylemez()
        {
            var sim = Calistir();

            var yalanlar = Yalanlar(sim, "Hatice").ToList();
            Assert.Equal(2, yalanlar.Count);
            Assert.Equal(2, yalanlar.Select(y => y.Deger("onerme")).Distinct().Count());                      // iki farklı yalan önermesi
            Assert.Equal(2, yalanlar.Select(y => y.Deger("onerme") + ">" + y.Deger("kime")).Distinct().Count()); // F: aynı (önerme, kişi) tekrarı yok

            var itiraf = Assert.Single(Olaylar(sim, "itiraf_etti", "Hatice"));
            Assert.Equal("kriz:itiraf", itiraf.KuralId);
            Assert.True(itiraf.Tick > yalanlar.Max(y => y.Tick));
            Assert.Equal(yalanlar.OrderBy(y => y.Id).Last().Deger("kime"), itiraf.Hedef);              // en son yalan söylenen kişi
            Assert.DoesNotContain(yalanlar, y => y.Tick > itiraf.Tick);                                   // itiraftan sonra yalan yok
            Assert.Equal(2, sim.Gunluk.Olaylar.Count(o => o.Tur == "stres_degisti" && o.KuralId == "eylem:yalan_soyle"));
            Assert.True(sim.Zihinler["Hatice"].Stres(sim.SimdikiTick) < 50m);
        }

        [Fact]
        public void I2_Itiraf_kurbanda_yalanini_yakaladim_modifierini_dogurur_why_krizden_yalana_ve_karar_izine_iner()
        {
            var sim = Calistir();

            var itiraf = Olaylar(sim, "itiraf_etti", "Hatice").Single();
            var modifier = sim.Gunluk.Olaylar.Single(o => o.Tur == "modifier_eklendi" && o.Deger("tur") == "yalanini_yakaladim");
            Assert.Equal(itiraf.Hedef, modifier.Deger("npc"));
            Assert.Equal("Hatice", modifier.Deger("hedef"));
            var zincir = sim.Neden(modifier.Id);
            Assert.Contains(zincir, o => o.Id == itiraf.Id);
            Assert.Contains(zincir, o => o.Tur == "stres_degisti" && o.KuralId == "eylem:yalan_soyle");
            Assert.Contains(zincir, o => o.Tur == "anlatti" && o.KuralId == "eylem:yalan_soyle" && o.Fail == "Hatice");
            Assert.Contains(zincir, o => o.Tur == "karar_verildi" && o.Fail == "Hatice");
        }

        [Fact]
        public void I2_Hatice_yalani_yalnizca_kisiye_ozel_sir_inancindan_dogar()
        {
            var sim = Calistir();

            var yalancilar = sim.Gunluk.Olaylar.Where(o => o.Tur == "aktarim_kaydedildi" && o.Deger("yalan") == "1").Select(o => o.Deger("npc")).Distinct().ToArray();
            Assert.Equal(new[] { "Hatice" }, yalancilar);
            Assert.True(sim.Zihinler["Hatice"].InancDerecesi("sir_sakliyor(Hatice,Mehmet)", 24) >= 0.5m);
            foreach (var npc in new[] { "Ayse", "Can", "Kemal", "Selim", "Mehmet" })
            {
                Assert.Equal(0m, sim.Zihinler[npc].InancDerecesi("sir_sakliyor(Hatice,Mehmet)", 24));
            }
        }

        // ---- I3: Selim hakaret → 2 tehdit → patlama ----

        [Fact]
        public void I3_Selim_iki_tehdit_bir_patlama_sonra_tehdit_yok()
        {
            var sim = Calistir();

            var tehditler = Olaylar(sim, "tehdit_etti", "Selim").ToList();
            Assert.Equal(2, tehditler.Count);
            Assert.All(tehditler, t => Assert.True(t.Tick >= 10));                                       // hakaretten önce tehdit yok
            var patlama = Assert.Single(Olaylar(sim, "patladi", "Selim"));
            Assert.Equal("kriz:patla", patlama.KuralId);
            Assert.Equal("oyuncu", patlama.Hedef);                                                       // en soğuk kişi: hakaret eden oyuncu
            Assert.DoesNotContain(tehditler, t => t.Tick > patlama.Tick);
            Assert.True(sim.Zihinler["Selim"].Stres(sim.SimdikiTick) < 50m);
        }

        // ---- I4: Can'da tehdit döngüsü yok ----

        [Fact]
        public void I4_Can_en_fazla_iki_kez_tehdit_eder_krizden_sonra_tehdit_yok()
        {
            var sim = Calistir();

            var tehditler = Olaylar(sim, "tehdit_etti", "Can").ToList();
            Assert.InRange(tehditler.Count, 1, 2);
            var kriz = Assert.Single(sim.Gunluk.Olaylar, o => o.Fail == "Can" && o.KuralId.StartsWith("kriz:", StringComparison.Ordinal) && !Core.Mind.ZihinOlaylari.ZihinOlayiMi(o.Tur));
            Assert.DoesNotContain(tehditler, t => t.Tick > kriz.Tick);
        }

        // ---- I5: toplam kriz ----

        [Fact]
        public void I5_Yirmi_dort_tickte_tam_uc_kriz_vardir_itiraf_patlama_ve_Canin_sakinlesmesi()
        {
            var sim = Calistir();

            var krizler = sim.Gunluk.Olaylar.Where(o => o.KuralId.StartsWith("kriz:", StringComparison.Ordinal) && !Core.Mind.ZihinOlaylari.ZihinOlayiMi(o.Tur)).ToList();
            Assert.Equal(new[] { "kriz:itiraf", "kriz:patla", "kriz:sakinles" }, krizler.Select(k => k.KuralId).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        }

        // ---- I6: determinizm ----

        [Fact]
        public void I6_Ayni_seed_ayni_hash_ve_gunluk()
        {
            var a = Calistir();
            var b = Calistir();

            Assert.Equal(a.SonHash, b.SonHash);
            Assert.Equal(a.Gunluk.Sayi, b.Gunluk.Sayi);
        }

        private static string RepoKoku()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "Yanki.slnx")))
            {
                d = d.Parent;
            }

            return d!.FullName;
        }
    }
}
