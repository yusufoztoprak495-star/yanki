using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsCheck;
using Xunit;
using Yanki.Cli;
using Yanki.Core.Events;
using Yanki.Core.Mind;
using Yanki.Core.Simulation;

namespace Yanki.Scenarios.Tests
{
    /// <summary>
    /// 8. hafta: <c>Zihin.Inanilan</c> çelişki dizini (konuşan başına O(P²) → O(P)) üretim kodundaki tam-tarama referansı OLMADAN doğrulanır. Referans burada,
    /// genel API ile yazılmış naif tam taramadır. Kapsam: <c>degil(...)</c> çiftleri, aynı tek değerli (<c>!</c>) anahtarın farklı değerleri, <c>gordu</c> ve kökü
    /// olan <c>duydu</c> kanıtları, eşit güçte eşitliğin son kanıt tick'iyle kırılması. Ayrıca dizinin günlükten (baştan oynatmayla) yeniden kurulabildiği ve
    /// kanıt eklenme sırasına bağlı olmadığı sınanır. Örnek sayısı ve tohum <c>OzellikTestleri</c> ile aynı ortam değişkenleriyle ayarlanır.
    /// </summary>
    public class CeliskiDizinOzellikTestleri
    {
        private static readonly int Ornek = int.TryParse(Environment.GetEnvironmentVariable("YANKI_CSCHECK_ORNEK"), out var o) && o > 0 ? o : 20;
        private static readonly string Tohum = Environment.GetEnvironmentVariable("YANKI_CSCHECK_TOHUM") is { Length: > 0 } t ? t : "7Hjd1O9bXz0e";

        // Olumlu/olumsuz çiftler, tek değerli anahtarlar (aynı anahtar üç değer), olumsuz tek değerliler ve çelişmeyen önermeler.
        private static readonly string[] Havuz =
        {
            "borclu(Ali,Veli,100)", "degil(borclu(Ali,Veli,100))", "borclu(Ali,Veli,101)", "degil(borclu(Ali,Veli,101))", "borclu(Can,Ali,200)", "degil(borclu(Can,Ali,200))",
            "konum!(Ali,han)", "konum!(Ali,pazar)", "konum!(Ali,kilise)", "degil(konum!(Ali,han))", "degil(konum!(Ali,pazar))",
            "konum!(Veli,han)", "konum!(Veli,pazar)", "degil(konum!(Veli,han))",
            "kardes(Ali,Can)", "degil(kardes(Ali,Can))", "gordu(Can,han)", "gordu(Veli,han)",
        };

        private sealed class Kanit
        {
            public Kanit(int onerme, int guc, int tickFarki, int kaynak, int anahtar)
            {
                Onerme = onerme;
                Guc = guc;
                TickFarki = tickFarki;
                Kaynak = kaynak;
                Anahtar = anahtar;
            }

            public int Onerme { get; }

            public int Guc { get; }

            public int TickFarki { get; }

            public int Kaynak { get; }   // 0 = gordu, 1 = duydu (Can), 2 = duydu (Veli)

            public int Anahtar { get; }  // aynı tick içindeki eklenme sırasını belirler

            public override string ToString() => $"({Havuz[Onerme]} g{Guc} +{TickFarki} k{Kaynak} s{Anahtar})";
        }

        private static readonly Gen<List<Kanit>> Kanitlar =
            Gen.Select(Gen.Int[0, Havuz.Length - 1], Gen.Int[5, 95], Gen.Int[0, 3], Gen.Int[0, 2], Gen.Int[0, 1000], (a, b, c, d, e) => new Kanit(a, b, c, d, e)).List[1, 45];

        // Kanıtları tick'e göre, aynı tick içinde `anahtar` sırasına göre (ya da ters) dizer; günlük tick'i geriye gitmez.
        private static List<(long Tick, Kanit Kanit)> Sirala(List<Kanit> kanitlar, bool ters)
        {
            var tick = 1L;
            var liste = new List<(long, Kanit)>();
            foreach (var k in kanitlar)
            {
                tick += k.TickFarki;
                liste.Add((tick, k));
            }

            return liste.GroupBy(x => x.Item1).OrderBy(g => g.Key)
                .SelectMany(g => (ters ? g.OrderByDescending(x => x.Item2.Anahtar).ThenByDescending(x => x.Item2.Onerme) : g.OrderBy(x => x.Item2.Anahtar).ThenBy(x => x.Item2.Onerme)).ToList())
                .ToList();
        }

        private static (Zihin Zihin, OlayGunlugu Gunluk, long SonTick) Kur(List<(long Tick, Kanit Kanit)> sirali)
        {
            var zihin = new Zihin("Ayse", new Kisilik(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, 0.5m), new ZihinAyarlari { KanitSonumuTickBasina = 0.01m });
            var gunluk = new OlayGunlugu();
            foreach (var (tick, k) in sirali)
            {
                var duydu = k.Kaynak != 0;
                var kaynak = k.Kaynak == 1 ? "Can" : k.Kaynak == 2 ? "Veli" : "Ayse";
                var olay = ZihinOlaylari.KanitEklendi(
                    gunluk.SonrakiId, tick, gunluk.SonrakiSira(tick), "Ayse", Havuz[k.Onerme], duydu ? "duydu" : "gordu", kaynak, k.Guc / 100m, new long[0], "algi", "han", duydu ? kaynak : string.Empty);
                gunluk.Ekle(olay);
                zihin.Uygula(olay);
            }

            return (zihin, gunluk, sirali.Count == 0 ? 1 : sirali[sirali.Count - 1].Tick);
        }

        /// <summary>Eski gövdenin davranışı: genel API ile, bütün inançları tarayan referans.</summary>
        private static bool Referans(Zihin zihin, string onerme, long tick)
        {
            var p = Onerme.Ayristir(onerme);
            var guven = zihin.Guven(onerme, tick);
            if (guven <= 0m)
            {
                return false;
            }

            foreach (var digerMetin in zihin.Onermeler)
            {
                var d = Onerme.Ayristir(digerMetin);
                if (string.Equals(d.ToString(), p.ToString(), StringComparison.Ordinal) || !Onerme.Celisir(p, d))
                {
                    continue;
                }

                var digerGuven = zihin.Guven(digerMetin, tick);
                if (digerGuven <= 0m)
                {
                    continue;
                }

                var c = guven.CompareTo(digerGuven);
                if (c == 0)
                {
                    c = zihin.Kanitlar(onerme).Max(x => x.Tick).CompareTo(zihin.Kanitlar(digerMetin).Max(x => x.Tick));
                }

                if (c <= 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static string Yorum(Zihin zihin, long tick) =>
            string.Join(";", zihin.Onermeler.Select(p => p + "=" + zihin.Inanilan(p, tick)));

        [Fact]
        public void Dizinli_Inanilan_rastgele_kanit_dizilerinde_naif_referansla_ayni()
        {
            long olumsuzKaybeden = 0, tekDegerKaybeden = 0, kazanan = 0;

            Kanitlar.Sample(
                kanitlar =>
                {
                    var (zihin, _, sonTick) = Kur(Sirala(kanitlar, false));
                    foreach (var tick in new[] { sonTick, sonTick + 10, sonTick + 120 })
                    {
                        foreach (var p in zihin.Onermeler)
                        {
                            var hizli = zihin.Inanilan(p, tick);
                            Assert.True(Referans(zihin, p, tick) == hizli, $"tick {tick}, {p}: dizinli {hizli}, referans {!hizli}");
                            if (hizli)
                            {
                                kazanan++;
                            }
                            else if (zihin.Guven(p, tick) > 0m)
                            {
                                if (p.Contains("konum!", StringComparison.Ordinal))
                                {
                                    tekDegerKaybeden++;
                                }
                                else
                                {
                                    olumsuzKaybeden++;
                                }
                            }
                        }
                    }

                },
                iter: Ornek, seed: Tohum, threads: 1);

            // Test boş geçmesin: her çelişki türünde gerçekten kaybeden önerme üretildi.
            Assert.True(kazanan > 0, "inanılan önerme üretilmeli");
            Assert.True(olumsuzKaybeden > 0, "olumlu/olumsuz çiftte kaybeden üretilmeli (degil(...) kapsamı)");
            Assert.True(tekDegerKaybeden > 0, "tek değerli anahtarda kaybeden üretilmeli (konum! kapsamı)");
        }

        [Fact]
        public void Dizin_gunlukten_yeniden_kurulabilir_ve_ayni_sonucu_verir()
        {
            Kanitlar.Sample(
                kanitlar =>
                {
                    var sirali = Sirala(kanitlar, false);
                    var (zihin, gunluk, sonTick) = Kur(sirali);

                    // Aynı günlüğün olayları yeni bir zihne uygulanır (kayıt/yükleme ve baştan oynatmanın yaptığı iş): dizin sıfırdan kurulur.
                    var yeni = new Zihin("Ayse", new Kisilik(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, 0.5m), new ZihinAyarlari { KanitSonumuTickBasina = 0.01m });
                    foreach (var olay in gunluk.Olaylar)
                    {
                        yeni.Uygula(olay);
                    }

                    Assert.Equal(zihin.KanonikMetin(), yeni.KanonikMetin());
                    foreach (var tick in new[] { sonTick, sonTick + 60 })
                    {
                        Assert.Equal(Yorum(zihin, tick), Yorum(yeni, tick));
                        foreach (var p in yeni.Onermeler)
                        {
                            Assert.True(Referans(yeni, p, tick) == yeni.Inanilan(p, tick), $"yeniden kurulan zihin tick {tick}, {p}");
                        }
                    }
                },
                iter: Ornek, seed: Tohum, threads: 1);
        }

        [Fact]
        public void Dizin_sonucu_ayni_tick_icindeki_kanit_eklenme_sirasina_bagli_degildir()
        {
            Kanitlar.Sample(
                kanitlar =>
                {
                    var duz = Kur(Sirala(kanitlar, false));
                    var ters = Kur(Sirala(kanitlar, true));

                    Assert.Equal(duz.SonTick, ters.SonTick);
                    foreach (var tick in new[] { duz.SonTick, duz.SonTick + 60 })
                    {
                        Assert.Equal(Yorum(duz.Zihin, tick), Yorum(ters.Zihin, tick));
                    }
                },
                iter: Ornek, seed: Tohum, threads: 1);
        }

        // ---- gerçek simülasyon: baştan oynatmayla yeniden kurulan zihinlerde dizin ----

        private static string Kok()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "Yanki.slnx")))
            {
                d = d.Parent;
            }

            return d!.FullName;
        }

        private static Simulasyon Yukle(int seed)
        {
            var hata = new StringWriter();
            var sim = SenaryoDosyasi.Yukle(Path.Combine(Kok(), "samples", "HanGerilim", "scenario.json"), (ulong)seed, hata);
            Assert.True(sim != null, hata.ToString());
            return sim!;
        }

        [Fact]
        public void HanGerilim_oynatma_ile_yeniden_kurulan_zihinlerde_dizin_referansla_ve_orijinalle_ayni()
        {
            var onermeler = new[] { "borclu(Mehmet,Ayse,500)", "kardes(Mehmet,Can)", "sir_sakliyor(Hatice,Mehmet)", "borclu(Selim,Hatice,200)" };
            var kisiler = new[] { "Ayse", "Can", "Hatice", "Kemal", "Selim" };

            Gen.Select(Gen.Int[1, 60], Gen.Select(Gen.Int[0, kisiler.Length - 1], Gen.Int[0, onermeler.Length - 1], Gen.Int[1, 20], (a, b, c) => (a, b, c)).List[0, 6], (seed, eylemler) => (seed, eylemler))
                .Sample(
                    g =>
                    {
                        var sim = Yukle(g.seed);
                        for (var t = 1; t <= 24; t++)
                        {
                            foreach (var (kisi, onerme, tick) in g.eylemler.Where(e => e.Item3 == t).Select(e => (e.Item1, e.Item2, e.Item3)))
                            {
                                try
                                {
                                    sim.Submit(new PlayerAction("anlatti", kisiler[kisi], new[] { new KeyValuePair<string, string>("onerme", onermeler[onerme]) }));
                                }
                                catch (ArgumentException)
                                {
                                    // geçersiz eylem atlanır
                                }
                            }

                            sim.Tick(1);
                        }

                        var yeni = Yukle(g.seed);
                        var sonuc = OturumOynatici.Oynat(yeni, OturumKaydi.Uret(sim, "HanGerilim"));
                        Assert.True(sonuc.HashTutuyor && sonuc.GunlukHashTutuyor, "baştan oynatma hash'i tutmalı");

                        foreach (var npc in sim.Zihinler.Kimlikler)
                        {
                            foreach (var p in yeni.Zihinler[npc].Onermeler)
                            {
                                var beklenen = Referans(yeni.Zihinler[npc], p, yeni.SimdikiTick);
                                Assert.True(beklenen == yeni.Zihinler[npc].Inanilan(p, yeni.SimdikiTick), $"{npc} {p}: oynatmayla kurulan dizin referanstan farklı");
                                Assert.True(sim.Zihinler[npc].Inanilan(p, sim.SimdikiTick) == yeni.Zihinler[npc].Inanilan(p, yeni.SimdikiTick), $"{npc} {p}: orijinal ve oynatılan farklı");
                            }
                        }
                    },
                    iter: Ornek, seed: Tohum, threads: 1);
        }
    }
}
