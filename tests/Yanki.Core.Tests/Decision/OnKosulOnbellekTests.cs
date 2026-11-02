using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Criteria;
using Yanki.Core.Decision;
using Yanki.Core.Mind;
using Yanki.Core.Simulation;
using Yanki.Core.World;

namespace Yanki.Core.Tests.Decision
{
    /// <summary>
    /// Z2: <c>eylem.hedef</c> kullanmayan ön koşullar NPC-eylem başına bir kez hesaplanır. Önbellek yalnızca bir <see cref="AdayUretici.Uret"/>
    /// çağrısının içinde yaşar; sonuç (adaylar ve elenme nedenleri) önbelleksiz değerlendirmeyle birebir aynıdır.
    /// Fikstür: Ali ve N1..N4 han'da (beş NPC), oyuncu han'da; Ali N1 ve N3'e minnet duyar.
    /// </summary>
    public class OnKosulOnbellekTests
    {
        private const string Kisilik = "{ \"acgozluluk\": 0.5, \"durustluk\": 0.5, \"cesaret\": 0.5, \"sosyallik\": 0.5, \"kin_tutma\": 0.5, \"merhamet\": 0.5 }";

        private static SenaryoTanimi BesNpc()
        {
            var json = "{ \"surum\": 1, \"mekanlar\": [\"han\", \"pazar\"], \"baglantilar\": [[\"han\", \"pazar\"]], \"oyuncu\": { \"id\": \"oyuncu\", \"konum\": \"han\" }, \"npcler\": ["
                + string.Join(", ", new[] { "Ali", "N1", "N2", "N3", "N4" }.Select(n => "{ \"id\": \"" + n + "\", \"konum\": \"han\", \"kisilik\": " + Kisilik + " }"))
                + "], \"olay_turleri\": [\"x_oldu\"] }";
            var sonuc = SenaryoYukleyici.Yukle(json, "s.json");
            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            return sonuc.Senaryo!;
        }

        private sealed class SayacliCozucu : ISorguCozucu
        {
            private readonly ISorguCozucu _ic;

            public SayacliCozucu(ISorguCozucu ic)
            {
                _ic = ic;
            }

            public SortedDictionary<string, int> Sayac { get; } = new SortedDictionary<string, int>(StringComparer.Ordinal);

            public SorguDegeri Coz(SorguCagrisi cagri, SorguBaglami baglam)
            {
                Sayac.TryGetValue(cagri.Ad, out var n);
                Sayac[cagri.Ad] = n + 1;
                return _ic.Coz(cagri, baglam);
            }
        }

        private static (Simulasyon Sim, KararBaglami Baglam, SayacliCozucu Sayac) Kur(IReadOnlyList<EylemTanimi> eylemler)
        {
            var sim = KararFikstur.Sim(BesNpc(), eylemler);
            foreach (var hedef in new[] { "N1", "N3" })
            {
                sim.Zihinler["Ali"].Uygula(ZihinOlaylari.ModifierEklendi(900, 1, 0, "Ali", hedef, "minnet", 10m, 1000, new long[0], "test"));
            }

            var sayac = new SayacliCozucu(new ZihinSorguCozucusu(sim.Dunya, sim.Zihinler, sim.Gunluk, 1));
            var b = new KararBaglami("Ali", sim.OyuncuId, sim.Seed, 1, sim.Dunya, sim.Zihinler["Ali"], sim.Gunluk, sayac);
            return (sim, b, sayac);
        }

        private static string Kosul(string alan, string deger = "true") =>
            "{ \"alan\": \"" + alan + "\", \"op\": \"==\", \"deger\": " + deger + " }";

        private static string EylemJson(string id, params string[] kosullar) =>
            KararFikstur.Eylem(id, "npc", "\"on_kosullar\": [ " + string.Join(", ", kosullar) + " ]");

        private static string BagimsizKosul(bool dogru) =>
            "{ \"alan\": \"kisilik(merhamet)\", \"op\": \">=\", \"deger\": " + (dogru ? "0.4" : "0.9") + " }";

        private static string BagliKosul() => Kosul("modifier_var(eylem.hedef, minnet)"); // N1 ve N3 için doğru

        // ---- çağrı sayısı ----

        [Fact]
        public void Hedefe_bagli_olmayan_on_kosul_eylem_basina_bir_kez_hesaplanir()
        {
            var (_, baglam, sayac) = Kur(KararFikstur.Yukle(EylemJson("yardim", BagimsizKosul(true), BagliKosul())));

            var u = AdayUretici.Uret(baglam, KararFikstur.Yukle(EylemJson("yardim", BagimsizKosul(true), BagliKosul())));

            Assert.Equal(new[] { "yardim|N1", "yardim|N3" }, u.Adaylar.Select(a => a.Anahtar).ToArray());
            Assert.Equal(1, sayac.Sayac["kisilik"]); // 4 hedef, tek hesap
            Assert.Equal(4, sayac.Sayac["modifier_var"]); // hedefe bağlı: hedef başına
        }

        [Fact]
        public void Hedefe_bagli_on_kosul_onbelleklenmez_ic_ice_onermede_eylem_hedef_de_baglidir()
        {
            var eylem = EylemJson("yardim", Kosul("inanc(tehlikeli(eylem.hedef))", "0").Replace("\"==\"", "\"==\""));
            var (_, baglam, sayac) = Kur(KararFikstur.Yukle(eylem));

            AdayUretici.Uret(baglam, KararFikstur.Yukle(eylem));

            Assert.Equal(4, sayac.Sayac["inanc"]);
        }

        [Fact]
        public void Bagimsiz_on_kosul_yanlissa_butun_hedefler_ayni_nedenle_elenir_tek_hesapla()
        {
            var eylem = EylemJson("yardim", BagimsizKosul(false), BagliKosul());
            var (_, baglam, sayac) = Kur(KararFikstur.Yukle(eylem));

            var u = AdayUretici.Uret(baglam, KararFikstur.Yukle(eylem));

            Assert.Empty(u.Adaylar);
            Assert.Equal(4, u.Elenenler.Count);
            Assert.All(u.Elenenler, e => Assert.StartsWith("on_kosul #1 tutmadi: kisilik(merhamet) >= 0.9", e.Neden));
            Assert.Equal(1, sayac.Sayac["kisilik"]);
            Assert.False(sayac.Sayac.ContainsKey("modifier_var")); // ilk tutmayan koşulda durulur
        }

        [Fact]
        public void Sonradan_gelen_bagimsiz_kosul_once_gelen_bagli_kosuldan_sonra_degerlendirilir_neden_degismez()
        {
            // #1 hedefe bağlı (N2, N4 için yanlış), #2 bağımsız yanlış: N1/N3 #2 ile, N2/N4 #1 ile elenir.
            var eylem = EylemJson("yardim", BagliKosul(), BagimsizKosul(false));
            var (_, baglam, _) = Kur(KararFikstur.Yukle(eylem));

            var u = AdayUretici.Uret(baglam, KararFikstur.Yukle(eylem));

            var nedenler = u.Elenenler.ToDictionary(e => e.Hedef, e => e.Neden.Substring(0, "on_kosul #1".Length));
            Assert.Equal("on_kosul #2", nedenler["N1"]);
            Assert.Equal("on_kosul #1", nedenler["N2"]);
            Assert.Equal("on_kosul #2", nedenler["N3"]);
            Assert.Equal("on_kosul #1", nedenler["N4"]);
        }

        // ---- önbelleksiz referansla birebir eşitlik ----

        // Z2'den ÖNCEKİ değerlendirmenin birebir karşılığı: her hedef için ön koşulların hepsi baştan değerlendirilir.
        private static (string[] Adaylar, string[] Elenenler) Referans(KararBaglami b, IReadOnlyList<EylemTanimi> eylemler)
        {
            var adaylar = new List<string>();
            var elenenler = new List<string>();
            foreach (var eylem in eylemler)
            {
                var hedefler = b.Dunya.Varliklar(VarlikTuru.Npc).Where(n => n != b.Npc).ToList();
                foreach (var hedef in hedefler)
                {
                    var sorgu = b.SorguBaglami(hedef);
                    string? neden = null;
                    for (var i = 0; i < eylem.OnKosullar.Count && neden == null; i++)
                    {
                        if (!KriterDegerlendirici.Degerlendir(eylem.OnKosullar[i], b.Cozucu, sorgu).Tuttu)
                        {
                            neden = $"on_kosul #{i + 1} tutmadi: {eylem.OnKosullar[i]}";
                        }
                    }

                    if (neden == null)
                    {
                        adaylar.Add(eylem.Id + "|" + hedef);
                    }
                    else
                    {
                        elenenler.Add(eylem.Id + "|" + hedef + " => " + neden);
                    }
                }
            }

            adaylar.Sort(string.CompareOrdinal);
            elenenler.Sort(string.CompareOrdinal);
            return (adaylar.ToArray(), elenenler.ToArray());
        }

        [Fact]
        public void Rastgele_on_kosul_dizilimlerinde_onbellekli_sonuc_onbelleksizle_birebir_aynidir()
        {
            var havuz = new Func<string>[]
            {
                () => BagimsizKosul(true),
                () => BagimsizKosul(false),
                BagliKosul,
                () => Kosul("modifier_var(eylem.hedef, minnet)", "false"),
                () => Kosul("ayni_mekanda(eylem.hedef)"),
            };
            var elenenGoren = 0;
            var adayGoren = 0;
            for (ulong seed = 1; seed <= 80; seed++)
            {
                var rng = AnahtarliRng.Icin(seed, 0, "on_kosul_onbellek");
                var eylemJsonlari = new List<string>();
                for (var e = 0; e < 3; e++)
                {
                    var sayi = rng.Aralik(1, 5);
                    var kosullar = Enumerable.Range(0, sayi).Select(_ => havuz[rng.Aralik(0, havuz.Length)]()).ToArray();
                    eylemJsonlari.Add(EylemJson("e" + e, kosullar));
                }

                var eylemler = KararFikstur.Yukle(eylemJsonlari.ToArray());
                var (_, baglam, _) = Kur(eylemler);

                var gercek = AdayUretici.Uret(baglam, eylemler);
                var beklenen = Referans(baglam, eylemler);

                Assert.Equal(beklenen.Adaylar, gercek.Adaylar.Select(a => a.Anahtar).OrderBy(x => x, StringComparer.Ordinal).ToArray());
                Assert.Equal(
                    beklenen.Elenenler,
                    gercek.Elenenler.Select(e => e.Eylem.Id + "|" + e.Hedef + " => " + e.Neden).OrderBy(x => x, StringComparer.Ordinal).ToArray());
                elenenGoren += gercek.Elenenler.Count;
                adayGoren += gercek.Adaylar.Count;
            }

            Assert.True(elenenGoren > 100 && adayGoren > 100, $"karşılaştırma boş değil: aday {adayGoren}, elenen {elenenGoren}");
        }

        // ---- ömür ----

        [Fact]
        public void Onbellek_cagrilar_arasinda_yasamaz_durum_degisince_yeni_sonuc_verir()
        {
            var eylem = EylemJson("yardim", Kosul("modifier_var(eylem.hedef, minnet)", "true"));
            var eylemler = KararFikstur.Yukle(eylem);
            var (sim, baglam, _) = Kur(eylemler);
            var once = AdayUretici.Uret(baglam, eylemler).Adaylar.Select(a => a.Hedef).ToArray();

            sim.Zihinler["Ali"].Uygula(ZihinOlaylari.ModifierEklendi(901, 1, 1, "Ali", "N2", "minnet", 10m, 1000, new long[0], "test"));
            var sonra = AdayUretici.Uret(baglam, eylemler).Adaylar.Select(a => a.Hedef).ToArray();

            Assert.Equal(new[] { "N1", "N3" }, once);
            Assert.Equal(new[] { "N1", "N2", "N3" }, sonra);
        }

        [Fact]
        public void Bagimsiz_kosul_NPC_basina_ayri_hesaplanir()
        {
            var eylemler = KararFikstur.Yukle(EylemJson("yardim", BagimsizKosul(true)));
            var (sim, ali, sayac) = Kur(eylemler);
            var n1 = new KararBaglami("N1", sim.OyuncuId, sim.Seed, 1, sim.Dunya, sim.Zihinler["N1"], sim.Gunluk, sayac);

            AdayUretici.Uret(ali, eylemler);
            AdayUretici.Uret(n1, eylemler);

            Assert.Equal(2, sayac.Sayac["kisilik"]); // her çağrı kendi önbelleğini kurar
        }
    }
}
