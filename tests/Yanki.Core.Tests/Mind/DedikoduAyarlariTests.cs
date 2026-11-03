using System;
using Xunit;
using Yanki.Core.Mind;

namespace Yanki.Core.Tests.Mind
{
    public class DedikoduAyarlariTests
    {
        [Fact]
        public void Varsayilanlar_Yusufun_onayladigi_degerlerdir()
        {
            var a = new DedikoduAyarlari();
            Assert.Equal(168L, a.YenilikPenceresi);
            Assert.Equal(0.1m, a.KonuEsigi);
            Assert.Equal(1m, a.IlgiDinleyen);
            Assert.Equal(0.7m, a.IlgiBagli);
            Assert.Equal(0.5m, a.IlgiKonusan);
            Assert.Equal(0.25m, a.IlgiYok);
            Assert.Equal(0.5m, a.VarsayilanOnem);
            Assert.Equal(0.7m, a.GuvenCarpaniTaban);
            Assert.Equal(0.003m, a.GuvenCarpaniEgim);
            Assert.Equal(0.95m, a.GuvenCarpaniUst);
            Assert.Equal(0.15m, a.GucEsigi);
            Assert.Equal(0.3m, a.AnlatmaGuveniEsigi);
            Assert.Equal(1m, a.OyuncuKanitGucu);
            a.Dogrula();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Yenilik_penceresi_pozitif_olmali(long pencere) =>
            Assert.Throws<ArgumentException>(() => new DedikoduAyarlari { YenilikPenceresi = pencere }.Dogrula());

        [Fact]
        public void Esik_ve_carpanlar_0_1_araligindadir()
        {
            Assert.Throws<ArgumentException>(() => new DedikoduAyarlari { KonuEsigi = 1.5m }.Dogrula());
            Assert.Throws<ArgumentException>(() => new DedikoduAyarlari { GucEsigi = -0.1m }.Dogrula());
            Assert.Throws<ArgumentException>(() => new DedikoduAyarlari { GuvenCarpaniUst = 1.2m }.Dogrula());
        }

        [Fact]
        public void Stres_varsayilanlari_50_80_ve_30_60()
        {
            var s = new StresAyarlari();
            Assert.Equal(new[] { 50m, 80m }, s.Esikler);
            Assert.Equal(new[] { 30m, 60m }, s.Dususler);
            s.Dogrula();
        }

        [Theory]
        [InlineData(50, 80, 30, 60, true)]
        [InlineData(50, 80, 20, 60, false)]   // 80 - 20 = 60 > 50: stres 79'dayken 50 krizinden sonra yine 50'nin üstünde kalır
        [InlineData(50, 80, 30, 20, false)]   // 100 - 20 = 80 >= 50
        [InlineData(50, 80, 30, 50, false)]   // 100 - 50 = 50, alt eşiğin ALTINDA değil
        [InlineData(50, 80, 30, 51, true)]
        public void Kriz_sonrasi_stres_en_alt_esigin_altinda_kalmali(decimal e1, decimal e2, decimal d1, decimal d2, bool gecerli)
        {
            var s = new StresAyarlari { Esikler = new[] { e1, e2 }, Dususler = new[] { d1, d2 } };
            if (gecerli)
            {
                s.Dogrula();
            }
            else
            {
                Assert.Throws<ArgumentException>(() => s.Dogrula());
            }
        }

        [Fact]
        public void Tek_seviyede_100_eksi_dusus_esikten_kucuk_olmali()
        {
            new StresAyarlari { Esikler = new[] { 50m }, Dususler = new[] { 60m } }.Dogrula();
            Assert.Throws<ArgumentException>(() => new StresAyarlari { Esikler = new[] { 50m }, Dususler = new[] { 40m } }.Dogrula());
        }

        [Fact]
        public void Esikler_artan_ve_dususlerle_ayni_sayida_olmali()
        {
            Assert.Throws<ArgumentException>(() => new StresAyarlari { Esikler = new[] { 80m, 50m }, Dususler = new[] { 30m, 60m } }.Dogrula());
            Assert.Throws<ArgumentException>(() => new StresAyarlari { Esikler = new[] { 50m, 80m }, Dususler = new[] { 30m } }.Dogrula());
            Assert.Throws<ArgumentException>(() => new StresAyarlari { Esikler = new decimal[0], Dususler = new decimal[0] }.Dogrula());
            Assert.Throws<ArgumentException>(() => new StresAyarlari { Esikler = new[] { 0m }, Dususler = new[] { 60m } }.Dogrula());
        }
    }
}
