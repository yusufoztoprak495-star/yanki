using System;
using Xunit;
using Yanki.Core.Mind;

namespace Yanki.Core.Tests.Mind
{
    public class RuhHaliStresTests
    {
        private static Zihin Yeni() => new Zihin("Can", new Kisilik(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, 0.5m), new ZihinAyarlari
        {
            DuyguSonumuTickBasina = 0.1m,
            StresSonumuTickBasina = 1m,
        });

        [Fact]
        public void Baslangicta_duygu_yoktur()
        {
            var z = Yeni();

            Assert.Null(z.Duygu);
            Assert.Null(z.OncekiDuygu);
            Assert.Equal(0m, z.DuyguSiddeti("ofke", 0));
            Assert.Equal(0m, z.BaskinDuyguSiddeti(0));
        }

        [Fact]
        public void Ilk_duygu_baskin_olur_hedefi_ve_neden_olayi_tutulur()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Duygu("Can", "ofke", 0.6m, "oyuncu"));

            Assert.Equal("ofke", z.Duygu!.Tur);
            Assert.Equal("oyuncu", z.Duygu.Hedef);
            Assert.Equal(1, z.Duygu.NedenOlayId);
            Assert.Equal(0.6m, z.DuyguSiddeti("ofke", 0));
            Assert.Equal(0m, z.DuyguSiddeti("korku", 0));
        }

        [Fact]
        public void Hedefsiz_duygu_kabul_edilir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Duygu("Can", "huzun", 0.3m));

            Assert.Equal(string.Empty, z.Duygu!.Hedef);
        }

        [Fact]
        public void Daha_siddetli_yeni_duygu_baskin_olur_eskisi_onceki_kalir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Duygu("Can", "ofke", 0.4m, "oyuncu"));
            z.Uygula(u.Duygu("Can", "korku", 0.8m, "Ali"));

            Assert.Equal("korku", z.Duygu!.Tur);
            Assert.Equal("ofke", z.OncekiDuygu!.Tur);
            Assert.Equal("oyuncu", z.OncekiDuygu.Hedef);
        }

        [Fact]
        public void Daha_zayif_ya_da_esit_yeni_duygu_baskin_olmaz()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Duygu("Can", "ofke", 0.5m, "oyuncu"));
            z.Uygula(u.Duygu("Can", "korku", 0.3m, "Ali"));
            z.Uygula(u.Duygu("Can", "huzun", 0.5m));

            Assert.Equal("ofke", z.Duygu!.Tur);
            Assert.Null(z.OncekiDuygu);
        }

        [Fact]
        public void Eskisi_sonmusse_zayif_yeni_duygu_baskin_olur()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Duygu("Can", "ofke", 0.5m, "oyuncu"));
            u.Tick = 10; // öfke: 0.5 - 1.0 → 0
            z.Uygula(u.Duygu("Can", "korku", 0.1m, "Ali"));

            Assert.Equal("korku", z.Duygu!.Tur);
        }

        [Fact]
        public void Duygu_siddeti_zamanla_soner()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Duygu("Can", "ofke", 0.6m, "oyuncu"));

            Assert.Equal(0.4m, z.DuyguSiddeti("ofke", 2));
            Assert.Equal(0m, z.DuyguSiddeti("ofke", 6));
            Assert.Equal(0.4m, z.BaskinDuyguSiddeti(2));
        }

        [Theory]
        [InlineData(-0.1)]
        [InlineData(1.1)]
        public void Siddet_aralik_disinda_reddedilir(double siddet)
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();

            Assert.ThrowsAny<ArgumentException>(() => z.Uygula(u.Duygu("Can", "ofke", (decimal)siddet)));
        }

        [Fact]
        public void Stres_baslangicta_sifirdir_ve_delta_ile_artar()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();

            Assert.Equal(0m, z.Stres(0));
            z.Uygula(u.Stres("Can", 30m));
            z.Uygula(u.Stres("Can", 15m));

            Assert.Equal(45m, z.Stres(0));
        }

        [Fact]
        public void Stres_0_ile_100_arasinda_kirpilir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Stres("Can", 250m));
            Assert.Equal(100m, z.Stres(0));

            z.Uygula(u.Stres("Can", -500m));
            Assert.Equal(0m, z.Stres(0));
        }

        [Fact]
        public void Stres_yavasca_soner_ve_yeni_artis_sonmus_degerden_hesaplanir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Stres("Can", 50m));

            Assert.Equal(40m, z.Stres(10));

            u.Tick = 10;
            z.Uygula(u.Stres("Can", 20m));

            Assert.Equal(60m, z.Stres(10));
            Assert.Equal(55m, z.Stres(15));
        }
    }
}
