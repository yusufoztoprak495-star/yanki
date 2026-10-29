using Xunit;
using Yanki.Core.Mind;
using Yanki.Core.World;

namespace Yanki.Core.Tests.Mind
{
    /// <summary>Rastgele olay dizileriyle zihin değişmezleri (plan 9). Rastgelelik yalnızca anahtarlı RNG'den gelir; seed sabittir.</summary>
    public class DegismezlerTests
    {
        private static ZihinDeposu Kos(ulong seed, int adim)
        {
            var depo = new ZihinDeposu();
            depo.Ekle(new Zihin("Can", new Kisilik(0.2m, 0.4m, 0.6m, 0.8m, 0.5m, 0.1m)));
            var rng = AnahtarliRng.Icin(seed, 0, "Can", "degismezler");
            var u = new ZihinOlayUretici();
            string[] eksenler = { "guven", "sevgi", "korku" };

            for (var i = 0; i < adim; i++)
            {
                u.Tick += (long)(rng.SonrakiUlong() % 5UL);
                var delta = (decimal)(long)(rng.SonrakiUlong() % 401UL) - 200m;
                switch (rng.SonrakiUlong() % 4UL)
                {
                    case 0: depo.Uygula(u.Eksen("Can", "oyuncu", eksenler[(int)(rng.SonrakiUlong() % 3UL)], delta)); break;
                    case 1: depo.Uygula(u.Stres("Can", delta)); break;
                    case 2: depo.Uygula(u.Modifier("Can", "oyuncu", "tur" + (rng.SonrakiUlong() % 3UL), delta / 4m, 1 + (long)(rng.SonrakiUlong() % 50UL))); break;
                    default: depo.Uygula(u.Duygu("Can", "ofke", (decimal)(long)(rng.SonrakiUlong() % 101UL) / 100m, "oyuncu")); break;
                }

                var z = depo["Can"];
                foreach (var e in eksenler)
                {
                    Assert.InRange(z.Eksen("oyuncu", e), -100m, 100m);
                }

                Assert.InRange(z.Stres(u.Tick), 0m, 100m);
                Assert.InRange(z.Fikir("oyuncu", u.Tick), -100m, 100m);
                Assert.InRange(z.BaskinDuyguSiddeti(u.Tick), 0m, 1m);
            }

            return depo;
        }

        [Theory]
        [InlineData(1UL)]
        [InlineData(42UL)]
        [InlineData(2026UL)]
        public void Eksenler_stres_fikir_ve_duygu_her_dizide_sinir_icinde_kalir(ulong seed)
        {
            Kos(seed, 400);
        }

        [Theory]
        [InlineData(1UL)]
        [InlineData(42UL)]
        public void Ayni_seed_ayni_zihin_hashini_verir_farkli_seed_vermez(ulong seed)
        {
            Assert.Equal(Kos(seed, 200).DurumHash(), Kos(seed, 200).DurumHash());
            Assert.NotEqual(Kos(seed, 200).DurumHash(), Kos(seed + 1, 200).DurumHash());
        }
    }
}
