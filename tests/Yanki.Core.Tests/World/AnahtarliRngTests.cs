using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.World;

namespace Yanki.Core.Tests.World
{
    public class AnahtarliRngTests
    {
        private static ulong[] Cek(AnahtarliRng rng, int adet) =>
            Enumerable.Range(0, adet).Select(_ => rng.SonrakiUlong()).ToArray();

        [Fact]
        public void SplitMix64_durum_sifir_ilk_bes_cikti_referans_degerleri()
        {
            // Referans: Vigna'nın splitmix64.c uygulaması, başlangıç durumu 0.
            var rng = AnahtarliRng.DurumdanKur(0UL);

            Assert.Equal(
                new[] { 0xE220A8397B1DCDAFUL, 0x6E789E6AA1B965F4UL, 0x06C45D188009454FUL, 0xF88BB8A8724C81ECUL, 0x1B39896A51A8749BUL },
                Cek(rng, 5));
        }

        [Fact]
        public void Ayni_girdi_ayni_akis()
        {
            var a = Cek(AnahtarliRng.Icin(42UL, 7, "Ali", "karar"), 8);
            var b = Cek(AnahtarliRng.Icin(42UL, 7, "Ali", "karar"), 8);

            Assert.Equal(a, b);
        }

        [Fact]
        public void Seed_tick_ya_da_anahtar_farkliysa_akis_farkli()
        {
            var temel = Cek(AnahtarliRng.Icin(42UL, 7, "Ali", "karar"), 4);

            Assert.NotEqual(temel, Cek(AnahtarliRng.Icin(43UL, 7, "Ali", "karar"), 4));
            Assert.NotEqual(temel, Cek(AnahtarliRng.Icin(42UL, 8, "Ali", "karar"), 4));
            Assert.NotEqual(temel, Cek(AnahtarliRng.Icin(42UL, 7, "Ayse", "karar"), 4));
            Assert.NotEqual(temel, Cek(AnahtarliRng.Icin(42UL, 7, "Ali"), 4));
            Assert.NotEqual(temel, Cek(AnahtarliRng.Icin(42UL, 7, "karar", "Ali"), 4));
        }

        [Fact]
        public void Anahtar_sinirlari_cakismaz()
        {
            var a = Cek(AnahtarliRng.Icin(1UL, 0, "ab", "c"), 4);
            var b = Cek(AnahtarliRng.Icin(1UL, 0, "a", "bc"), 4);
            var c = Cek(AnahtarliRng.Icin(1UL, 0, "abc"), 4);

            Assert.NotEqual(a, b);
            Assert.NotEqual(a, c);
            Assert.NotEqual(b, c);
        }

        [Fact]
        public void Cagri_sirasi_ve_baska_akislar_sonucu_etkilemez()
        {
            var anahtarlar = new[] { "Ali", "Ayse", "Mehmet" };

            // Sıra 1: A, B, C ve her akıştan art arda çekim.
            var ilk = anahtarlar.ToDictionary(k => k, k => Cek(AnahtarliRng.Icin(9UL, 3, k), 6));

            // Sıra 2: ters sırada, araya başka akışların çekimleri girerek.
            var rngler = anahtarlar.Reverse().ToDictionary(k => k, k => AnahtarliRng.Icin(9UL, 3, k));
            var ikinci = new Dictionary<string, List<ulong>>();
            foreach (var k in anahtarlar)
            {
                ikinci[k] = new List<ulong>();
            }

            for (var i = 0; i < 6; i++)
            {
                foreach (var k in anahtarlar.Reverse())
                {
                    ikinci[k].Add(rngler[k].SonrakiUlong());
                }
            }

            foreach (var k in anahtarlar)
            {
                Assert.Equal(ilk[k], ikinci[k].ToArray());
            }
        }

        [Fact]
        public void Aralik_sinirlar_icinde_ve_tum_degerler_uretilir()
        {
            var rng = AnahtarliRng.Icin(5UL, 0, "aralik");
            var gorulen = new SortedSet<int>();

            for (var i = 0; i < 10_000; i++)
            {
                var v = rng.Aralik(-3, 4);
                Assert.InRange(v, -3, 3);
                gorulen.Add(v);
            }

            Assert.Equal(new[] { -3, -2, -1, 0, 1, 2, 3 }, gorulen.ToArray());
        }

        [Fact]
        public void Aralik_tek_elemanli_aralikta_hep_min_doner()
        {
            var rng = AnahtarliRng.Icin(5UL, 0, "tek");

            Assert.Equal(7, rng.Aralik(7, 8));
        }

        [Fact]
        public void Aralik_int_sinirlarinda_tasma_olmaz()
        {
            var rng = AnahtarliRng.Icin(5UL, 0, "genis");

            for (var i = 0; i < 100; i++)
            {
                var v = rng.Aralik(int.MinValue, int.MaxValue);
                Assert.InRange(v, int.MinValue, int.MaxValue - 1);
            }
        }

        [Theory]
        [InlineData(3, 3)]
        [InlineData(4, 3)]
        public void Aralik_gecersiz_aralik_hata_verir(int min, int maxHaric)
        {
            var rng = AnahtarliRng.Icin(1UL, 0, "x");

            Assert.Throws<ArgumentOutOfRangeException>(() => rng.Aralik(min, maxHaric));
        }

        [Fact]
        public void SonrakiDecimal01_sifir_dahil_bir_haric()
        {
            var rng = AnahtarliRng.Icin(11UL, 2, "d");

            for (var i = 0; i < 5_000; i++)
            {
                var d = rng.SonrakiDecimal01();
                Assert.True(d >= 0m && d < 1m, $"Aralık dışı: {d}");
            }
        }

        [Fact]
        public void Icin_anahtarlar_null_olamaz()
        {
            Assert.Throws<ArgumentNullException>(() => AnahtarliRng.Icin(1UL, 0, null!));
            Assert.Throws<ArgumentNullException>(() => AnahtarliRng.Icin(1UL, 0, "a", null!));
        }
    }
}
