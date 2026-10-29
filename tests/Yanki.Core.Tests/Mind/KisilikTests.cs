using System;
using Xunit;
using Yanki.Core.Mind;

namespace Yanki.Core.Tests.Mind
{
    public class KisilikTests
    {
        private static Kisilik Ornek() => new Kisilik(
            acgozluluk: 0.8m, durustluk: 0.2m, cesaret: 0.5m, sosyallik: 0.9m, kinTutma: 0.4m, merhamet: 0.6m);

        [Fact]
        public void Alti_ozelligin_degerleri_adlariyla_okunur()
        {
            var k = Ornek();

            Assert.Equal(0.8m, k.Deger("acgozluluk"));
            Assert.Equal(0.2m, k.Deger("durustluk"));
            Assert.Equal(0.5m, k.Deger("cesaret"));
            Assert.Equal(0.9m, k.Deger("sosyallik"));
            Assert.Equal(0.4m, k.Deger("kin_tutma"));
            Assert.Equal(0.6m, k.Deger("merhamet"));
        }

        [Fact]
        public void Ozellik_adlari_ASCII_ve_ordinal_siralidir()
        {
            Assert.Equal(
                new[] { "acgozluluk", "cesaret", "durustluk", "kin_tutma", "merhamet", "sosyallik" },
                Kisilik.Ozellikler);
        }

        [Fact]
        public void Bilinmeyen_ozellik_hata_verir()
        {
            Assert.Throws<ArgumentException>(() => Ornek().Deger("zeka"));
        }

        [Theory]
        [InlineData(-0.01)]
        [InlineData(1.01)]
        public void Aralik_disi_deger_reddedilir(double deger)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Kisilik((decimal)deger, 0m, 0m, 0m, 0m, 0m));
        }

        [Fact]
        public void Uc_ucu_0_ve_1_kabul_edilir()
        {
            var k = new Kisilik(0m, 1m, 0m, 1m, 0m, 1m);

            Assert.Equal(1m, k.Deger("durustluk"));
        }

        [Fact]
        public void Hicbir_olay_kisiligi_degistirmez()
        {
            var zihin = new Zihin("Ali", Ornek());
            var once = zihin.KanonikMetin();
            var u = new ZihinOlayUretici();

            zihin.Uygula(u.Eksen("Ali", "oyuncu", "guven", -50m));
            zihin.Uygula(u.Modifier("Ali", "oyuncu", "hakaret_etti", -15m, 100));
            zihin.Uygula(u.Duygu("Ali", "ofke", 0.9m, "oyuncu"));
            zihin.Uygula(u.Stres("Ali", 40m));
            zihin.Uygula(u.Kanit("Ali", "borclu(Mehmet,Ayse,500)", "duydu", "Ayse", 0.7m));
            zihin.Uygula(u.Hafiza("Ali", 1, 0.9m));

            foreach (var ozellik in Kisilik.Ozellikler)
            {
                Assert.Equal(Ornek().Deger(ozellik), zihin.Kisilik.Deger(ozellik));
            }

            Assert.Equal(KisilikSatirlari(once), KisilikSatirlari(zihin.KanonikMetin()));
        }

        private static string KisilikSatirlari(string metin)
        {
            var sonuc = string.Empty;
            foreach (var satir in metin.Split('\n'))
            {
                if (satir.StartsWith("P|", StringComparison.Ordinal))
                {
                    sonuc += satir + "\n";
                }
            }

            return sonuc;
        }
    }
}
