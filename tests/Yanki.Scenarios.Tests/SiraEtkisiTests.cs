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
    /// Sıra etkisi senaryoları (plan 9: en az 3 "sıra etkisi" testi; 7. hafta, Yusuf'un seçimleri 1a, 2a, 3c). Ortak kural: oyuncu eylemleri aynı,
    /// tick slotları aynı, seed aynı; YALNIZCA eylemlerin hangi slota konduğu (sırası) değişir. Değişmezler senaryolardan ÖNCE yazıldı. Sayılar yer tutucudur
    /// (her senaryonun README'sinde). Kasaba Hanı içeriği DEĞİLDİR.
    /// </summary>
    public class SiraEtkisiTests
    {
        private static string Kok()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "Yanki.slnx")))
            {
                d = d.Parent;
            }

            return d!.FullName;
        }

        /// <summary>Senaryoyu kurar; slotlar (tick → o tick'in başında Submit edilen eylemler) verilen sırayla uygulanır ve toplam tick kadar koşulur.</summary>
        private static Simulasyon Kos(string klasor, IReadOnlyDictionary<int, PlayerAction[]> slotlar, int toplamTick, ulong seed = 42)
        {
            var hata = new StringWriter();
            var sim = SenaryoDosyasi.Yukle(Path.Combine(Kok(), "samples", klasor, "scenario.json"), seed, hata);
            Assert.True(sim != null, hata.ToString());
            for (var t = 1; t <= toplamTick; t++)
            {
                if (slotlar.TryGetValue(t, out var eylemler))
                {
                    foreach (var e in eylemler)
                    {
                        sim!.Submit(e);
                    }
                }

                sim!.Tick(1);
            }

            return sim!;
        }

        private static PlayerAction Anlat(string hedef, string onerme) =>
            new PlayerAction("anlatti", hedef, new[] { new KeyValuePair<string, string>("onerme", onerme) });

        private static string Anahtar(PlayerAction e) => e.Tur + ">" + e.Hedef + ":" + string.Join(",", e.Yuk.Select(kv => kv.Key + "=" + kv.Value));

        private static void AyniEylemlerFarkliSira(Simulasyon a, Simulasyon b)
        {
            Assert.Equal(
                a.EylemGunlugu.Select(k => Anahtar(k.Eylem)).OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                b.EylemGunlugu.Select(k => Anahtar(k.Eylem)).OrderBy(x => x, StringComparer.Ordinal).ToArray());
            Assert.Equal(
                a.EylemGunlugu.Select(k => k.Tick).Distinct().OrderBy(x => x).ToArray(),
                b.EylemGunlugu.Select(k => k.Tick).Distinct().OrderBy(x => x).ToArray());            // aynı tick slotları
            Assert.NotEqual(
                a.EylemGunlugu.Select(k => k.Tick + Anahtar(k.Eylem)).ToArray(),
                b.EylemGunlugu.Select(k => k.Tick + Anahtar(k.Eylem)).ToArray());                    // ama eylem-slot eşleşmesi (sıra) farklı
            Assert.Equal(a.Seed, b.Seed);
        }

        private static Olay[] Modifierlar(Simulasyon s, string npc, string tur) =>
            s.Gunluk.Olaylar.Where(o => o.Tur == "modifier_eklendi" && o.Deger("npc") == npc && o.Deger("tur") == tur).ToArray();

        // ---- 1a: söylenti ve iyilik (plan 7, Durum 1 / Durum 2) ----

        private const string Soylenti1 = "SiraEtkisiSoylenti";

        private static PlayerAction[] Soylenti() => new[] { Anlat("Ayse", "borclu(Mehmet,Ayse,500)"), Anlat("Ayse", "kardes(Mehmet,Can)") };

        private static PlayerAction[] Yardim() => new[] { new PlayerAction("yardim_etti", "Can") };

        private static (Simulasyon A, Simulasyon B) Soylentiler(ulong seed = 42) => (
            Kos(Soylenti1, new Dictionary<int, PlayerAction[]> { [1] = Yardim(), [4] = Soylenti() }, 14, seed),
            Kos(Soylenti1, new Dictionary<int, PlayerAction[]> { [1] = Soylenti(), [4] = Yardim() }, 14, seed));

        [Fact]
        public void Soylenti_yardim_sirasi_Can_minnet_ya_da_supheli_duyar_fikri_ayrisir()
        {
            var (a, b) = Soylentiler();

            AyniEylemlerFarkliSira(a, b);

            // Sıra A (Durum 1): yardım söylentiden ÖNCE: minnet; söylenti sonra gelir ve iftira modifier'ı yazılır.
            Assert.Single(Modifierlar(a, "Can", "minnet"));
            Assert.Empty(Modifierlar(a, "Can", "supheli"));
            Assert.Single(Modifierlar(a, "Can", "aileme_iftira"));

            // Sıra B (Durum 2): yardım söylentiden SONRA: Can artık iftira modifier'ına sahip, iyiliğe şüpheyle bakar; minnet oluşmaz.
            Assert.Empty(Modifierlar(b, "Can", "minnet"));
            Assert.Single(Modifierlar(b, "Can", "supheli"));
            Assert.Single(Modifierlar(b, "Can", "aileme_iftira"));

            // Ölçülebilir fark: Can'ın oyuncu hakkındaki fikri (iki koşu aynı tick'te).
            var fa = a.Zihinler["Can"].Fikir("oyuncu", 14);
            var fb = b.Zihinler["Can"].Fikir("oyuncu", 14);
            Assert.NotEqual(fa, fb);
            Assert.True(fa > fb, $"Sıra A (yardım önce) Can'ın fikrini daha yüksek bırakmalı: A {fa}, B {fb}");
            Assert.NotEqual(a.SonHash, b.SonHash);
        }

        [Fact]
        public void Soylenti_yardim_sirasi_on_kosul_Ayse_Can_a_sira_gerektigi_gibi_gelir()
        {
            var (a, b) = Soylentiler();

            // B'de söylenti t1'de, Ayşe Can'a yardımdan (t4) önce anlatır ve iftira modifier'ı yardımdan önce yazılır; A'da söylenti t4'te, anlatım yardımdan sonra.
            var bAnlatim = b.Gunluk.Olaylar.First(o => o.Tur == "anlatti" && o.Fail == "Ayse" && o.Hedef == "Can");
            var bIftira = Modifierlar(b, "Can", "aileme_iftira").Single();
            var bYardim = b.Gunluk.Olaylar.Single(o => o.Tur == "yardim_etti");
            Assert.True(bAnlatim.Tick < bYardim.Tick, $"Ayşe Can'a yardımdan önce anlatmalı (anlatım t{bAnlatim.Tick}, yardım t{bYardim.Tick})");
            Assert.True(bIftira.Tick < bYardim.Tick);

            var aAnlatim = a.Gunluk.Olaylar.First(o => o.Tur == "anlatti" && o.Fail == "Ayse" && o.Hedef == "Can");
            var aYardim = a.Gunluk.Olaylar.Single(o => o.Tur == "yardim_etti");
            Assert.True(aAnlatim.Tick > aYardim.Tick);
        }

        [Fact]
        public void Soylenti_yardim_why_zincirleri_modifier_lardan_oyuncu_eylemlerine_iner()
        {
            var (a, b) = Soylentiler();

            var minnet = Neden(a, Modifierlar(a, "Can", "minnet").Single());
            Assert.Contains(minnet, o => o.Tur == "yardim_etti" && o.KuralId == "player");

            var supheli = Neden(b, Modifierlar(b, "Can", "supheli").Single());
            Assert.Contains(supheli, o => o.Tur == "yardim_etti" && o.KuralId == "player");

            foreach (var sim in new[] { a, b })
            {
                var iftira = Neden(sim, Modifierlar(sim, "Can", "aileme_iftira").Single());
                Assert.Contains(iftira, o => o.Tur == "anlatti" && o.Fail == "Ayse" && o.Hedef == "Can");
                Assert.Contains(iftira, o => o.Tur == "anlatti" && o.Fail == "oyuncu" && o.Hedef == "Ayse" && o.KuralId == "player");
            }
        }

        private static IReadOnlyList<Olay> Neden(Simulasyon s, Olay olay) => s.Neden(olay.Id);

        // ---- 2a: Hatice'nin krizi ----

        private const string Kriz2 = "SiraEtkisiKriz";

        // Yalan durumunu açan eylem: Hatice sırrı öğrenir (yalanın nedeni) ve yalanın çelişeceği borç inancını edinir.
        private static PlayerAction[] Sir() => new[] { Anlat("Hatice", "sir_sakliyor(Hatice,Mehmet)"), Anlat("Hatice", "borclu(Mehmet,Ayse,500)") };

        private static PlayerAction[] Baski() => new[] { new PlayerAction("hakaret_et", "Hatice") };

        private static (Simulasyon A, Simulasyon B) Krizler() => (
            Kos(Kriz2, new Dictionary<int, PlayerAction[]> { [1] = Sir(), [4] = Baski() }, 14),
            Kos(Kriz2, new Dictionary<int, PlayerAction[]> { [1] = Baski(), [4] = Sir() }, 14));

        private static Olay[] HaticeKrizi(Simulasyon s) =>
            s.Gunluk.Olaylar.Where(o => o.Fail == "Hatice" && o.KuralId.StartsWith("kriz:", StringComparison.Ordinal) && !Yanki.Core.Mind.ZihinOlaylari.ZihinOlayiMi(o.Tur)).ToArray();

        [Fact]
        public void Kriz_sirasi_yalandan_once_esik_asilirsa_kacti_sonra_asilirsa_itiraf()
        {
            var (a, b) = Krizler();

            AyniEylemlerFarkliSira(a, b);

            // Sıra A: sır önce (yalan durumu açılır), baskı sonra: Hatice yalan söyledikten SONRA eşiği aşar → itiraf, hedef yalan kurbanı.
            var ka = Assert.Single(HaticeKrizi(a));
            Assert.Equal(("itiraf_etti", "kriz:itiraf"), (ka.Tur, ka.KuralId));
            Assert.True(a.Zihinler["Hatice"].Aktarimlar.Any(x => x.Yalan), "A: kriz öncesi Hatice yalan söylemiş olmalı");
            Assert.Contains(ka.Hedef, a.Zihinler["Hatice"].Aktarimlar.Where(x => x.Yalan).Select(x => x.Kime));

            // Sıra B: baskı önce: Hatice yalan söylemeden eşiği aşar → yalan kurbanı yok, itiraf elenir, alt seviye yedek satır kacti.
            var kb = Assert.Single(HaticeKrizi(b));
            Assert.Equal(("kacti", "kriz:kac_alt"), (kb.Tur, kb.KuralId));
            Assert.Equal(string.Empty, kb.Hedef);
            Assert.DoesNotContain(b.Zihinler["Hatice"].Aktarimlar, x => x.Yalan);

            Assert.NotEqual(a.SonHash, b.SonHash);
        }

        [Fact]
        public void Kriz_why_her_iki_siradada_krizi_dogru_stres_artislarina_indirir()
        {
            var (a, b) = Krizler();

            foreach (var (sim, yalanVar) in new[] { (a, true), (b, false) })
            {
                var kriz = HaticeKrizi(sim).Single();
                var artislar = kriz.NedenOlaylar.Select(id => sim.Gunluk[id]).ToList();
                Assert.NotEmpty(artislar);
                Assert.All(artislar, o =>
                {
                    Assert.Equal("stres_degisti", o.Tur);
                    Assert.Equal("Hatice", o.Deger("npc"));
                });
                Assert.Contains(artislar, o => o.KuralId == "eylem:tehdit_et");
                Assert.Equal(yalanVar, artislar.Any(o => o.KuralId == "eylem:yalan_soyle"));

                var zincir = sim.Neden(kriz.Id);
                Assert.Contains(zincir, o => o.Tur == "tehdit_etti" && o.Fail == "Hatice");
                Assert.Contains(zincir, o => o.Tur == "hakaret_et" && o.KuralId == "player");
                Assert.Equal(yalanVar, zincir.Any(o => o.Tur == "anlatti" && o.KuralId == "eylem:yalan_soyle"));
            }
        }

        // ---- 3c: tanık sırası ----

        private const string Tanik3 = "SiraEtkisiTanik";

        private static PlayerAction[] Tehdit() => new[] { new PlayerAction("tehdit_etti", "Can") };

        private static PlayerAction[] YardimAyse() => new[] { new PlayerAction("yardim_etti", "Ayse") };

        private static (Simulasyon A, Simulasyon B) Taniklar() => (
            Kos(Tanik3, new Dictionary<int, PlayerAction[]> { [1] = Tehdit(), [5] = YardimAyse() }, 10),
            Kos(Tanik3, new Dictionary<int, PlayerAction[]> { [1] = YardimAyse(), [5] = Tehdit() }, 10));

        [Fact]
        public void Tanik_sirasi_Zeki_yalnizca_B_de_tehdidi_gorur()
        {
            var (a, b) = Taniklar();

            AyniEylemlerFarkliSira(a, b);

            // Önkoşul (seed 42): Zeki t5'ten önce, kendi git eylemiyle pazardan han'a gelmiştir; her iki koşuda da.
            foreach (var sim in new[] { a, b })
            {
                var geldi = sim.Gunluk.Olaylar.First(o => o.Tur == "geldi" && o.Fail == "Zeki");
                Assert.True(geldi.Tick < 5, $"Zeki t5'ten önce han'a gelmeli (geldi t{geldi.Tick})");
                Assert.Equal("han", sim.Dunya.TekDeger("konum!", "Zeki"));
            }

            // Zeki yalnızca B'de tehdidi görür: tehdit_gordu modifier'ı ve gordu kanıtı yalnızca B'de.
            Assert.Empty(Modifierlar(a, "Zeki", "tehdit_gordu"));
            Assert.Single(Modifierlar(b, "Zeki", "tehdit_gordu"));
            Assert.DoesNotContain(a.Zihinler["Zeki"].Onermeler, o => o.StartsWith("tehdit_etti", StringComparison.Ordinal));
            var tehditKanitlari = b.Zihinler["Zeki"].Onermeler.Where(o => o.StartsWith("tehdit_etti", StringComparison.Ordinal)).ToArray();
            Assert.NotEmpty(tehditKanitlari);
            Assert.All(tehditKanitlari, o => Assert.Contains(b.Zihinler["Zeki"].Kanitlar(o), k => k.Tur == Yanki.Core.Mind.KanitTuru.Gordu));

            // Ayşe ve Can tehdidin yakınındaydı: iki koşuda da görürler (sıra Zeki'nin tanıklığını etkiler, onlarınkini değil).
            foreach (var sim in new[] { a, b })
            {
                Assert.Single(Modifierlar(sim, "Ayse", "tehdit_gordu"));
            }

            Assert.NotEqual(a.SonHash, b.SonHash);
        }

        [Fact]
        public void Tanik_why_modifier_dan_algiya_ve_oyuncunun_tehdidine_iner()
        {
            var (_, b) = Taniklar();

            var modifier = Modifierlar(b, "Zeki", "tehdit_gordu").Single();
            var zincir = b.Neden(modifier.Id);

            Assert.Contains(zincir, o => o.Tur == "tehdit_etti" && o.KuralId == "player" && o.Hedef == "Can");
            Assert.Contains(zincir, o => o.KuralId == "algi" && o.Deger("npc") == "Zeki");   // tanıklığın kendisi: Zeki'nin gordu kanıtı/hafızası
        }
    }
}
