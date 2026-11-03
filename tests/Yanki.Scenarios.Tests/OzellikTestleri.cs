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
    /// Özellik tabanlı testler (CsCheck, plan 9): HanGerilim ve FrenliHan üzerinde rastgele GEÇERLİ oyuncu eylem dizileriyle değişmezler. Örnek sayısı ve seed sabittir
    /// (aynı koşu her seferinde aynı örnekleri üretir, kararsız test yok); bir örnek başarısız olursa CsCheck'in raporladığı <c>Seed</c> test çıktısına yazılır ve
    /// aşağıdaki <c>Tohum</c> sabitleri yerine o değer verilerek yeniden üretilir. Geçersiz bir eylem (ör. aynı tick'te çift hareket) <c>Submit</c> tarafından reddedilirse
    /// atlanır; reddedilen eylem dizisi de geçerli bir dizidir. Kapsam sınırı: <c>oldurulemez</c> ve <c>calinamaz</c> yalnızca <c>ihlal_bayraklari</c> bildiren eylem
    /// ve kurallarda uygulanır (ADR-013); korumalılık değişmezi bu kapsamdadır.
    /// </summary>
    public class OzellikTestleri
    {
        private const int Ornek = 20;
        private const string Tohum = "7Hjd1O9bXz0e";
        private const int Tick = 24;

        private static readonly string[] HanOnermeleri =
        {
            "borclu(Mehmet,Ayse,500)", "kardes(Mehmet,Can)", "sir_sakliyor(Hatice,Mehmet)", "borclu(Selim,Hatice,200)",
        };

        private static readonly string[] HanKisileri = { "Ayse", "Can", "Hatice", "Kemal", "Selim" };

        // HanGerilim eylem havuzu: 5 NPC × 4 önerme anlatım + 5 hakaret.
        private static readonly PlayerAction[] HanHavuzu = HanKisileri
            .SelectMany(k => HanOnermeleri.Select(o => new PlayerAction("anlatti", k, new[] { new KeyValuePair<string, string>("onerme", o) })))
            .Concat(HanKisileri.Select(k => new PlayerAction("hakaret_et", k)))
            .ToArray();

        private static readonly PlayerAction[] FrenHavuzu =
            new[] { "Hanci", "Ciftci", "Yabanci" }.Select(k => new PlayerAction("tehdit_etti", k)).ToArray();

        /// <summary>Üretilen girdi: (tick, havuz indeksi) listesi, sim seed'i, kayıt noktası N ve N+1. tick'in eylemlerinin bekleyen kaydedilip kaydedilmeyeceği.</summary>
        private sealed class Vaka
        {
            public Vaka((int Tick, int Eylem)[] eylemler, int seed, int n, bool bekleyen)
            {
                Eylemler = eylemler;
                Seed = seed;
                N = n;
                Bekleyen = bekleyen;
            }

            public (int Tick, int Eylem)[] Eylemler { get; }

            public int Seed { get; }

            public int N { get; }

            public bool Bekleyen { get; }

            public override string ToString() =>
                $"seed {Seed}, N {N}, bekleyen {Bekleyen}, eylemler [{string.Join(", ", Eylemler.Select(e => "t" + e.Tick + ":#" + e.Eylem))}]";
        }

        private static Gen<Vaka> Girdi(int havuz) =>
            Gen.Select(
                Gen.Int[1, Tick].Select(Gen.Int[0, havuz - 1]).List[0, 8], Gen.Int[1, 60], Gen.Int[1, Tick - 1], Gen.Bool,
                (l, seed, n, bekleyen) => new Vaka(l.ToArray(), seed, n, bekleyen));

        private static string Kok()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "Yanki.slnx")))
            {
                d = d.Parent;
            }

            return d!.FullName;
        }

        private static string Yol(string klasor) => Path.Combine(Kok(), "samples", klasor, "scenario.json");

        private static Simulasyon Yukle(string klasor, int seed)
        {
            var hata = new StringWriter();
            var sim = SenaryoDosyasi.Yukle(Yol(klasor), (ulong)seed, hata);
            Assert.True(sim != null, hata.ToString());
            return sim!;
        }

        private static void Gonder(Simulasyon sim, IEnumerable<PlayerAction> eylemler)
        {
            foreach (var e in eylemler)
            {
                try
                {
                    sim.Submit(e);
                }
                catch (ArgumentException)
                {
                    // Geçersiz eylem reddedilir ve atlanır.
                }
            }
        }

        private static ILookup<int, PlayerAction> Takvim((int Tick, int Eylem)[] eylemler, PlayerAction[] havuz) =>
            eylemler.ToLookup(e => e.Tick, e => havuz[e.Eylem]);

        private static Simulasyon Kos(string klasor, int seed, ILookup<int, PlayerAction> takvim, int tick, Action<Simulasyon>? herTick = null)
        {
            var sim = Yukle(klasor, seed);
            for (var t = 1; t <= tick; t++)
            {
                Gonder(sim, takvim[t]);
                sim.Tick(1);
                herTick?.Invoke(sim);
            }

            return sim;
        }

        private static string Ozet(Simulasyon s) =>
            string.Join("\n", s.Gunluk.Olaylar.Select(o => o.Id + "|" + o.Tick + "|" + o.Sira + "|" + o.Tur + "|" + o.Fail + "|" + o.Hedef + "|" + o.KuralId + "|"
                + string.Join(",", o.Yuk.Select(kv => kv.Key + "=" + kv.Value)) + "|" + string.Join(",", o.NedenOlaylar)));

        // ---- değişmezler (ortak) ----

        /// <summary>Z3: önbellekli kanonik metin ve durum hash'i, önbelleksiz tam hesapla her tick birebir aynı olmalı (bayat önbellek yok).</summary>
        private static void OnbellekTaze(Simulasyon sim)
        {
            var tam = new System.Text.StringBuilder();
            foreach (var npc in sim.Zihinler.Kimlikler)
            {
                var z = sim.Zihinler[npc];
                var tamMetin = z.KanonikMetinTam();
                Assert.Equal(tamMetin, z.KanonikMetin());                                            // tick {sim.SimdikiTick}: {npc} önbellekli kanonik metin bayat olmamalı
                Assert.Equal(tamMetin, z.KanonikMetin());                                            // ikinci çağrı (önbellekten) de aynı
                tam.Append(tamMetin);
            }

            var beklenen = sim.Zihinler.Kimlikler.Count == 0
                ? Yanki.Core.World.Hash64.Hex(Yanki.Core.World.Hash64.Hesapla(string.Empty))
                : Yanki.Core.World.Hash64.Hex(Yanki.Core.World.Hash64.Hesapla("ZIHIN|1\n" + tam));
            Assert.Equal(beklenen, sim.Zihinler.DurumHash());
        }

        private static void SinirlarIcinde(Simulasyon sim)
        {
            OnbellekTaze(sim);
            var kisiler = sim.Zihinler.Kimlikler.Concat(new[] { sim.OyuncuId }).ToArray();
            foreach (var npc in sim.Zihinler.Kimlikler)
            {
                var z = sim.Zihinler[npc];
                var stres = z.Stres(sim.SimdikiTick);
                Assert.InRange(stres, 0m, 100m);
                foreach (var hedef in kisiler)
                {
                    foreach (var eksen in new[] { "guven", "sevgi", "korku" })
                    {
                        Assert.InRange(z.Eksen(hedef, eksen), -100m, 100m);
                    }
                }
            }

            foreach (var yay in sim.Senaryo.YayDegiskenleri)
            {
                Assert.InRange(sim.Yay(yay.Ad), yay.Min, yay.Max);
            }
        }

        private static void NedenleriGunluktedir(Simulasyon sim)
        {
            foreach (var o in sim.Gunluk.Olaylar)
            {
                foreach (var n in o.NedenOlaylar)
                {
                    Assert.True(n < o.Id && sim.Gunluk.Var(n), $"olay #{o.Id} ({o.Tur}) nedeni #{n} günlükte yok ya da kendisinden küçük değil");
                }
            }
        }

        private static void ZincirSiniriAsilmaz(Simulasyon sim)
        {
            // Bağımsız yeniden hesap: kural çıktısı 1 + nedenlerinin en büyüğü, algı eklemez, diğerleri 0.
            var kurallar = new HashSet<string>(sim.Kurallar.Select(k => k.Id), StringComparer.Ordinal);
            var derinlik = new Dictionary<long, int>();
            foreach (var o in sim.Gunluk.Olaylar)
            {
                var en = o.NedenOlaylar.Count == 0 ? 0 : o.NedenOlaylar.Max(n => derinlik[n]);
                derinlik[o.Id] = kurallar.Contains(o.KuralId) ? en + 1 : o.KuralId == "algi" ? en : 0;
                Assert.True(derinlik[o.Id] <= 4, $"olay #{o.Id} ({o.KuralId}) zincir derinliği {derinlik[o.Id]} > 4");
                Assert.Equal(derinlik[o.Id], sim.ZincirDerinligi(o.Id));
            }
        }

        private static void YalanGizliligi(Simulasyon sim)
        {
            // Doğru ve yalan anlatım dışarıdan ayırt edilemez: aynı yük anahtarları, aynı neden sayısı, yalan bilgisi yük/fren/olay türünde yok.
            var anlatimlar = sim.Gunluk.Olaylar.Where(o => o.Tur == "anlatti" && o.KuralId.StartsWith("eylem:", StringComparison.Ordinal)).ToArray();
            foreach (var o in anlatimlar)
            {
                Assert.Equal(new[] { "gorunurluk", "kok", "onerme" }, o.Yuk.Select(kv => kv.Key).OrderBy(x => x, StringComparer.Ordinal).ToArray());
                Assert.Equal(2, o.NedenOlaylar.Count);
                Assert.DoesNotContain(o.Yuk, kv => kv.Key.Contains("yalan") || kv.Value.Contains("yalan"));
            }

            Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.Tur != "aktarim_kaydedildi" && o.Yuk.Any(kv => kv.Key == "yalan"));
        }

        // ---- HanGerilim ----

        [Fact]
        public void HanGerilim_degismezleri_rastgele_eylem_dizilerinde_korunur()
        {
            Girdi(HanHavuzu.Length).Sample(
                g =>
                {
                    var takvim = Takvim(g.Eylemler, HanHavuzu);
                    var sim = Kos("HanGerilim", g.Seed, takvim, Tick, SinirlarIcinde);

                    SinirlarIcinde(sim);
                    NedenleriGunluktedir(sim);
                    ZincirSiniriAsilmaz(sim);
                    YalanGizliligi(sim);

                    // aile_iftirasi (en_fazla: 1, kapsam npc): her NPC için en çok bir kez.
                    foreach (var grup in sim.Gunluk.Olaylar.Where(o => o.KuralId == "aile_iftirasi").GroupBy(o => o.Deger("npc")))
                    {
                        Assert.True(grup.Count() <= 1, $"aile_iftirasi {grup.Key} için {grup.Count()} kez yazıldı");
                    }
                },
                iter: Ornek, seed: Tohum, threads: 1);
        }

        [Fact]
        public void HanGerilim_ayni_seed_ve_girdi_ayni_son_hash_ve_gunluk_hash_verir()
        {
            Girdi(HanHavuzu.Length).Sample(
                g =>
                {
                    var takvim = Takvim(g.Eylemler, HanHavuzu);
                    var a = Kos("HanGerilim", g.Seed, takvim, Tick);
                    var b = Kos("HanGerilim", g.Seed, takvim, Tick);

                    Assert.Equal(a.SonHash, b.SonHash);
                    Assert.Equal(GunlukHashu.Hesapla(a.Gunluk), GunlukHashu.Hesapla(b.Gunluk));
                    Assert.Equal(Ozet(a), Ozet(b));
                },
                iter: Ornek, seed: Tohum, threads: 1);
        }

        [Fact]
        public void HanGerilim_N_tick_kos_kaydet_yukle_M_tick_kos_ile_N_arti_M_tick_kesintisiz_kosu_ayni_sonucu_verir()
        {
            Girdi(HanHavuzu.Length).Sample(
                x =>
                {
                    var (eylemler, seed, n, bekleyenKaydet) = (x.Eylemler, x.Seed, x.N, x.Bekleyen);
                    var takvim = Takvim(eylemler, HanHavuzu);

                    var kesintisiz = Kos("HanGerilim", seed, takvim, Tick);

                    // N tick koş; bekleyenKaydet ise N+1. tick'in eylemleri Submit edilmiş (Tick görmemiş) hâlde kaydedilir.
                    var ilk = Kos("HanGerilim", seed, takvim, n);
                    if (bekleyenKaydet)
                    {
                        Gonder(ilk, takvim[n + 1]);
                    }

                    var kayit = OturumKaydi.Oku(OturumKaydi.Yaz(OturumKaydi.Uret(ilk, "../../samples/HanGerilim/scenario.json")), "kayit.json").Kayit!;
                    var yuklenen = Yukle("HanGerilim", seed);
                    var sonuc = OturumOynatici.Oynat(yuklenen, kayit);
                    Assert.True(sonuc.HashTutuyor);
                    Assert.True(sonuc.GunlukHashTutuyor);

                    for (var t = n + 1; t <= Tick; t++)
                    {
                        if (!(bekleyenKaydet && t == n + 1))
                        {
                            Gonder(yuklenen, takvim[t]);
                        }

                        yuklenen.Tick(1);
                    }

                    Assert.Equal(Ozet(kesintisiz), Ozet(yuklenen));
                    Assert.Equal(kesintisiz.SonHash, yuklenen.SonHash);
                    Assert.Equal(GunlukHashu.Hesapla(kesintisiz.Gunluk), GunlukHashu.Hesapla(yuklenen.Gunluk));
                },
                iter: Ornek, seed: Tohum, threads: 1);
        }

        // ---- FrenliHan ----

        [Fact]
        public void FrenliHan_degismezleri_rastgele_tehdit_dizilerinde_korunur()
        {
            Girdi(FrenHavuzu.Length).Sample(
                g =>
                {
                    var takvim = Takvim(g.Eylemler, FrenHavuzu);
                    var sim = Kos("FrenliHan", g.Seed, takvim, Tick, SinirlarIcinde);

                    SinirlarIcinde(sim);
                    NedenleriGunluktedir(sim);
                    ZincirSiniriAsilmaz(sim);
                    YalanGizliligi(sim);

                    // korumali varlık ihlal edilmez (kapsam: ilan edilen bayraklar): Hancı mekandan ayrılmaz, kese çalınmaz.
                    Assert.DoesNotContain(sim.Gunluk.Olaylar, o => (o.Tur == "ayrildi" || o.Tur == "geldi") && o.Fail == "Hanci");
                    Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.Tur == "gercek_eklendi" && o.Deger("gercek").StartsWith("konum!(Hanci,", StringComparison.Ordinal)
                        && o.KuralId != "senaryo");
                    Assert.Equal("han", sim.Dunya.TekDeger("konum!", "Hanci"));
                    Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.Tur == "hirsizlik_yapildi");

                    // cooldown 12 (kapsam npc): aynı NPC için tehdit_korkusu iki çalışması arasında ≥ 12 tick.
                    foreach (var grup in sim.Gunluk.Olaylar.Where(o => o.KuralId == "tehdit_korkusu").GroupBy(o => o.Deger("npc")))
                    {
                        var ticks = grup.Select(o => o.Tick).Distinct().OrderBy(x => x).ToArray();
                        for (var i = 1; i < ticks.Length; i++)
                        {
                            Assert.True(ticks[i] - ticks[i - 1] >= 12, $"tehdit_korkusu {grup.Key}: t{ticks[i - 1]} ve t{ticks[i]} cooldown penceresi içinde");
                        }
                    }

                    // en_fazla 3 (kapsam kural): tehdit_gerginligi en çok 3 çalışma (her çalışma bir yay_degisti).
                    Assert.True(sim.Gunluk.Olaylar.Count(o => o.KuralId == "tehdit_gerginligi" && o.Tur == "yay_degisti") <= 3);
                    Assert.InRange(sim.Yay("kasaba_gerginligi"), 0m, 5m);
                },
                iter: Ornek, seed: Tohum, threads: 1);
        }

        [Fact]
        public void FrenliHan_ayni_seed_ve_girdi_ayni_son_hash_ve_gunluk_hash_verir_ve_N_arti_M_denkligi_korunur()
        {
            Girdi(FrenHavuzu.Length).Sample(
                x =>
                {
                    var (eylemler, seed, n) = (x.Eylemler, x.Seed, x.N);
                    var takvim = Takvim(eylemler, FrenHavuzu);
                    var a = Kos("FrenliHan", seed, takvim, Tick);
                    var b = Kos("FrenliHan", seed, takvim, Tick);
                    Assert.Equal(a.SonHash, b.SonHash);
                    Assert.Equal(GunlukHashu.Hesapla(a.Gunluk), GunlukHashu.Hesapla(b.Gunluk));

                    var ilk = Kos("FrenliHan", seed, takvim, n);
                    var kayit = OturumKaydi.Oku(OturumKaydi.Yaz(OturumKaydi.Uret(ilk, "../../samples/FrenliHan/scenario.json")), "kayit.json").Kayit!;
                    var yuklenen = Yukle("FrenliHan", seed);
                    Assert.True(OturumOynatici.Oynat(yuklenen, kayit).GunlukHashTutuyor);
                    for (var t = n + 1; t <= Tick; t++)
                    {
                        Gonder(yuklenen, takvim[t]);
                        yuklenen.Tick(1);
                    }

                    Assert.Equal(Ozet(a), Ozet(yuklenen));
                    Assert.Equal(a.SonHash, yuklenen.SonHash);
                },
                iter: Ornek, seed: Tohum, threads: 1);
        }
    }
}
