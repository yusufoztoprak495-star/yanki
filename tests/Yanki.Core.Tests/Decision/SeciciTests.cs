using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Decision;
using Yanki.Core.World;

namespace Yanki.Core.Tests.Decision
{
    /// <summary>
    /// <see cref="UtilitySelector"/>: puanı en yüksek adayın %70'ine ulaşan ilk 3 aday arasından puan ağırlıklı seçim; N=1 argmax;
    /// puanı 0 olan seçilemez; eşitlikte eylem kimliği ve hedef ordinal; RNG <c>AnahtarliRng.Icin(Seed, tick, npcId, "karar")</c>.
    /// </summary>
    public class SeciciTests
    {
        private const string Sifir = "\"olcutler\": [ { \"alan\": \"kisilik(cesaret)\", \"egri\": { \"tur\": \"basamak\", \"esik\": 0.9 } } ]";

        // (eylem kimliği, taban) çiftlerinden hedefsiz adaylar; puan = taban. taban 0 → ölçütle puan 0.
        private static (IReadOnlyList<Aday> Adaylar, Yanki.Core.Simulation.Simulasyon Sim) Kur(params (string Id, decimal Taban)[] girdiler)
        {
            var json = girdiler.Select(g => g.Taban > 0m ? KararFikstur.Eylem(g.Id, taban: g.Taban) : KararFikstur.Eylem(g.Id, ek: Sifir)).ToArray();
            var eylemler = KararFikstur.Yukle(json);
            var sim = KararFikstur.Sim(eylemler);
            return (eylemler.Select(e => new Aday(e, string.Empty)).ToList(), sim);
        }

        private static SecimSonucu Sec(IActionSelector secici, (IReadOnlyList<Aday> Adaylar, Yanki.Core.Simulation.Simulasyon Sim) k, string npc = "Ali", ulong? seed = null, long? tick = null)
        {
            var b = KararFikstur.Baglam(k.Sim, npc);
            if (seed.HasValue || tick.HasValue)
            {
                b = new KararBaglami(npc, b.OyuncuId, seed ?? b.Seed, tick ?? b.Tick, b.Dunya, b.Zihin, b.Cozucu);
            }

            return secici.Sec(b, k.Adaylar, new ElenenAday[0]);
        }

        private static string[] Havuz(SecimSonucu s) => s.Iz.Havuz.ToArray();

        // ---- ayarlar ----

        [Fact]
        public void Varsayilan_ayarlar_ilk_3_ve_yuzde_70tir()
        {
            var a = new SecimAyarlari();

            Assert.Equal(3, a.EnIyiN);
            Assert.Equal(0.7m, a.Esik);
        }

        [Theory]
        [InlineData(0, "0.7")]
        [InlineData(-1, "0.7")]
        [InlineData(3, "0")]
        [InlineData(3, "-0.1")]
        [InlineData(3, "1.1")]
        public void Gecersiz_ayarlar_reddedilir(int n, string esik) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new SecimAyarlari(n, decimal.Parse(esik, System.Globalization.CultureInfo.InvariantCulture)));

        // ---- havuz ----

        [Fact]
        public void Havuz_en_yuksek_puanin_yuzde_70ine_ulasan_ilk_3_adaydir()
        {
            var k = Kur(("a", 10m), ("b", 8m), ("c", 7m), ("d", 6.9m), ("e", 5m));

            var s = Sec(new UtilitySelector(), k);

            Assert.Equal(new[] { "a|", "b|", "c|" }, Havuz(s));
        }

        [Fact]
        public void Esikte_kalan_dahil_esik_altinda_kalan_haric_edilir()
        {
            Assert.Equal(new[] { "a|", "b|" }, Havuz(Sec(new UtilitySelector(), Kur(("a", 10m), ("b", 7m)))));
            Assert.Equal(new[] { "a|" }, Havuz(Sec(new UtilitySelector(), Kur(("a", 10m), ("b", 6.999999m)))));
        }

        [Fact]
        public void Havuz_en_fazla_n_adaydir_esitlikte_ordinal_sira_belirler()
        {
            var k = Kur(("d", 10m), ("c", 10m), ("b", 10m), ("a", 10m));

            Assert.Equal(new[] { "a|", "b|", "c|" }, Havuz(Sec(new UtilitySelector(), k)));
            Assert.Equal(new[] { "a|", "b|", "c|", "d|" }, Havuz(Sec(new UtilitySelector(new SecimAyarlari(4, 0.7m)), k)));
        }

        [Fact]
        public void Esit_puanda_ayni_eylemin_hedefleri_ordinal_siralanir()
        {
            var eylemler = KararFikstur.Yukle(KararFikstur.Eylem("yardim", "npc"));
            var sim = KararFikstur.Sim(eylemler);
            var adaylar = new[] { new Aday(eylemler[0], "Veli"), new Aday(eylemler[0], "Ali") };

            var s = new UtilitySelector().Sec(KararFikstur.Baglam(sim, "Can"), adaylar, new ElenenAday[0]);

            Assert.Equal(new[] { "yardim|Ali", "yardim|Veli" }, Havuz(s));
        }

        [Fact]
        public void Yuksek_puan_once_gelir_giris_sirasi_onemsizdir()
        {
            var k = Kur(("a", 2m), ("b", 3m), ("c", 2.5m));

            var s = Sec(new UtilitySelector(new SecimAyarlari(1, 0.7m)), k);

            Assert.Equal(new[] { "b|" }, Havuz(s));
            Assert.Equal("b", s.Secilen!.Eylem.Id);
        }

        // ---- N=1 argmax ----

        [Fact]
        public void N_bir_her_seed_ve_tick_icin_argmaxtir_zar_atilmaz()
        {
            var k = Kur(("a", 3m), ("b", 2.9m), ("c", 2m));
            var secici = new UtilitySelector(new SecimAyarlari(1, 0.7m));

            for (ulong seed = 0; seed < 50; seed++)
            {
                var s = Sec(secici, k, seed: seed, tick: (long)seed + 1);
                Assert.Equal("a", s.Secilen!.Eylem.Id);
                Assert.Null(s.Iz.Zar);
            }
        }

        // ---- puanı 0 ----

        [Fact]
        public void Puani_sifir_olan_aday_secilemez()
        {
            var k = Kur(("a", 0m), ("b", 0m));

            var s = Sec(new UtilitySelector(), k);

            Assert.Null(s.Secilen);
            Assert.Empty(s.Iz.Havuz);
            Assert.Null(s.Iz.Zar);
            Assert.Equal(2, s.Iz.Adaylar.Count);
            Assert.All(s.Iz.Adaylar, a => Assert.Equal(0m, a.Puan!.Puan));
        }

        [Fact]
        public void Sifir_puanli_aday_pozitiflerin_yaninda_da_havuza_girmez()
        {
            var k = Kur(("a", 0m), ("b", 1m), ("c", 0m));

            for (ulong seed = 0; seed < 30; seed++)
            {
                var s = Sec(new UtilitySelector(), k, seed: seed);
                Assert.Equal("b", s.Secilen!.Eylem.Id);
                Assert.Equal(new[] { "b|" }, Havuz(s));
            }
        }

        [Fact]
        public void Aday_yoksa_secilen_bos_iz_bostur()
        {
            var sim = KararFikstur.Sim(new EylemTanimi[0]);

            var s = new UtilitySelector().Sec(KararFikstur.Baglam(sim, "Ali"), new Aday[0], new ElenenAday[0]);

            Assert.Null(s.Secilen);
            Assert.Empty(s.Iz.Adaylar);
            Assert.Empty(s.Iz.Havuz);
        }

        // ---- ağırlıklı seçim ve RNG ----

        [Fact]
        public void Agirlikli_secim_puanla_orantilidir_ve_her_iki_aday_secilebilir()
        {
            // 3 ve 2.4 (eşik 2.1 üstünde): a'nın payı 3 / 5.4 ≈ %55.6 → 400 denemede ≈ 222.
            var k = Kur(("a", 3m), ("b", 2.4m));
            var a = 0;
            for (ulong seed = 0; seed < 400; seed++)
            {
                if (Sec(new UtilitySelector(), k, seed: seed).Secilen!.Eylem.Id == "a")
                {
                    a++;
                }
            }

            Assert.InRange(a, 180, 270);
            Assert.InRange(400 - a, 130, 220);
        }

        [Fact]
        public void Farkli_seed_top_n_icinde_farkli_secim_uretebilir()
        {
            var k = Kur(("a", 3m), ("b", 3m), ("c", 3m));

            var secimler = new SortedSet<string>(StringComparer.Ordinal);
            for (ulong seed = 0; seed < 50; seed++)
            {
                secimler.Add(Sec(new UtilitySelector(), k, seed: seed).Secilen!.Eylem.Id);
            }

            Assert.True(secimler.Count > 1, "Farklı seed'ler farklı seçim üretmeli: " + string.Join(",", secimler));
        }

        [Fact]
        public void Ayni_seed_tick_ve_npc_ayni_secimi_verir()
        {
            var k = Kur(("a", 3m), ("b", 3m), ("c", 3m));

            for (ulong seed = 0; seed < 20; seed++)
            {
                Assert.Equal(
                    Sec(new UtilitySelector(), k, seed: seed, tick: 9).Secilen!.Eylem.Id,
                    Sec(new UtilitySelector(), k, seed: seed, tick: 9).Secilen!.Eylem.Id);
            }
        }

        [Fact]
        public void Zar_anahtarli_rng_seed_tick_npc_karar_akisindan_gelir()
        {
            // Havuz: a (3), b (2.4) → toplam 5.4. Hedef nokta = zar × 5.4; < 3 ise a, değilse b.
            var k = Kur(("a", 3m), ("b", 2.4m));

            for (ulong seed = 1; seed <= 40; seed++)
            {
                var beklenenZar = Math.Round(AnahtarliRng.Icin(seed, 7, "Ali", "karar").SonrakiDecimal01(), 6, MidpointRounding.ToEven);
                var s = Sec(new UtilitySelector(), k, "Ali", seed, 7);

                Assert.Equal(beklenenZar, s.Iz.Zar);
                Assert.Equal(beklenenZar * 5.4m < 3m ? "a" : "b", s.Secilen!.Eylem.Id);
            }
        }

        [Fact]
        public void Npc_isleme_sirasi_secimi_degistirmez()
        {
            var k = Kur(("a", 3m), ("b", 3m), ("c", 3m));
            var secici = new UtilitySelector();

            var aliOnce = Sec(secici, k, "Ali", 5, 3).Secilen!.Eylem.Id;
            var veli = Sec(secici, k, "Veli", 5, 3).Secilen!.Eylem.Id;
            var veliOnce = Sec(secici, k, "Veli", 5, 3).Secilen!.Eylem.Id;
            var aliSonra = Sec(secici, k, "Ali", 5, 3).Secilen!.Eylem.Id;

            Assert.Equal(aliOnce, aliSonra);
            Assert.Equal(veli, veliOnce);
        }

        // ---- iz ----

        [Fact]
        public void Iz_butun_adaylari_elenenleri_bilesenleri_havuzu_ve_secileni_icerir()
        {
            var eylemler = KararFikstur.Yukle(
                KararFikstur.Eylem("a", taban: 2m, ek: "\"olcutler\": [ { \"alan\": \"kisilik(cesaret)\" } ]"),
                KararFikstur.Eylem("b", taban: 1m),
                KararFikstur.Eylem("c", taban: 5m, ek: "\"celisen_ozellikler\": [ \"durustluk\" ]"));
            var sim = KararFikstur.Sim(eylemler);
            var adaylar = eylemler.Select(e => new Aday(e, string.Empty)).ToList();
            var elenen = new[] { new ElenenAday(eylemler[0], "Veli", "on_kosul #1 tutmadi: x") };

            var s = new UtilitySelector(new SecimAyarlari(2, 0.7m)).Sec(KararFikstur.Baglam(sim, "Ali"), adaylar, elenen);
            var iz = s.Iz;

            Assert.Equal("Ali", iz.Npc);
            Assert.Equal(sim.SimdikiTick + 1, iz.Tick);
            Assert.Equal(2, iz.Ayarlar.EnIyiN);
            Assert.Equal(new[] { "a|", "a|Veli", "b|", "c|" }, iz.Adaylar.Select(x => x.Aday.Eylem.Id + "|" + x.Aday.Hedef).ToArray());

            var elenenIz = iz.Adaylar.Single(x => x.Elendi);
            Assert.Equal("Veli", elenenIz.Aday.Hedef);
            Assert.Equal("on_kosul #1 tutmadi: x", elenenIz.Neden);
            Assert.Null(elenenIz.Puan);

            var a = iz.Adaylar.Single(x => !x.Elendi && x.Aday.Eylem.Id == "a");
            Assert.Equal(0.5m, a.Puan!.Olcutler[0].Cikti);
            Assert.Equal(1.0m, a.Puan.Puan);
            Assert.Equal(2, a.Sira);
            Assert.False(a.HavuzdaMi);

            var c = iz.Adaylar.Single(x => x.Aday.Eylem.Id == "c");
            Assert.Equal(1, c.Sira);
            Assert.True(c.HavuzdaMi);
            Assert.Equal(5m, c.Puan!.Puan);

            Assert.Equal(new[] { "c|" }, iz.Havuz.ToArray());
            Assert.Equal("c", iz.Secilen!.Eylem.Id);
            Assert.Equal(new[] { "durustluk" }, iz.CelisenOzellikler.ToArray());
        }

        [Fact]
        public void Secilen_aday_sonucla_izde_ayni_nesnedir()
        {
            var k = Kur(("a", 1m));

            var s = Sec(new UtilitySelector(), k);

            Assert.Same(s.Secilen, s.Iz.Secilen);
        }

        [Fact]
        public void Secici_arayuz_uzerinden_degistirilebilir()
        {
            IActionSelector ilk = new IlkAdayiSecen();
            var k = Kur(("a", 9m), ("b", 1m));
            var ters = (k.Adaylar.Reverse().ToList(), k.Sim);

            var s = Sec(ilk, ters);

            Assert.Equal("b", s.Secilen!.Eylem.Id); // sahte seçici girdideki ilk adayı seçer (puanı düşük olan); UtilitySelector'a bağımlılık yok
        }

        private sealed class IlkAdayiSecen : IActionSelector
        {
            public SecimSonucu Sec(KararBaglami baglam, IReadOnlyList<Aday> adaylar, IReadOnlyList<ElenenAday> elenenler) =>
                new SecimSonucu(adaylar[0], KararIzi.Basit(baglam, adaylar[0]));
        }
    }
}
