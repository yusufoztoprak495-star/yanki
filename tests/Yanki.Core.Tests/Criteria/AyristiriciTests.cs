using System.Linq;
using Xunit;
using Yanki.Core.Criteria;

namespace Yanki.Core.Tests.Criteria
{
    public class AyristiriciTests
    {
        [Fact]
        public void Basit_cagri_ayristirilir()
        {
            var c = IfadeAyristirici.Ayristir("fikir(dinleyen)");

            Assert.Equal("fikir", c.Ad);
            Assert.False(c.TekDegerli);
            var arg = Assert.IsType<RolArgumani>(Assert.Single(c.Argumanlar));
            Assert.Equal(Rol.Dinleyen, arg.Rol);
        }

        [Fact]
        public void Sifir_argumanli_cagri()
        {
            var c = IfadeAyristirici.Ayristir("stres()");

            Assert.Equal("stres", c.Ad);
            Assert.Empty(c.Argumanlar);
        }

        [Fact]
        public void Ic_ice_onerme_joker_ve_tekdegerli_isareti()
        {
            var c = IfadeAyristirici.Ayristir("inanc(borclu(Mehmet, *, *))");

            var onerme = Assert.IsType<OnermeArgumani>(Assert.Single(c.Argumanlar)).Cagri;
            Assert.Equal("borclu", onerme.Ad);
            Assert.IsType<TanimlayiciArgumani>(onerme.Argumanlar[0]);
            Assert.IsType<JokerArgumani>(onerme.Argumanlar[1]);
            Assert.IsType<JokerArgumani>(onerme.Argumanlar[2]);

            var tek = IfadeAyristirici.Ayristir("gercek(konum!(Ali, han))");
            var konum = Assert.IsType<OnermeArgumani>(Assert.Single(tek.Argumanlar)).Cagri;
            Assert.Equal("konum", konum.Ad);
            Assert.True(konum.TekDegerli);
        }

        [Theory]
        [InlineData("ben", Rol.Ben)]
        [InlineData("oyuncu", Rol.Oyuncu)]
        [InlineData("konusan", Rol.Konusan)]
        [InlineData("dinleyen", Rol.Dinleyen)]
        [InlineData("olay.fail", Rol.OlayFail)]
        [InlineData("olay.hedef", Rol.OlayHedef)]
        public void Roller_taninir(string metin, Rol beklenen)
        {
            var c = IfadeAyristirici.Ayristir("kaynak(x(1), " + metin + ")");

            var arg = Assert.IsType<RolArgumani>(c.Argumanlar[1]);
            Assert.Equal(beklenen, arg.Rol);
        }

        [Fact]
        public void Sayilar_negatif_ve_ondalik_decimal_olarak_okunur()
        {
            var c = IfadeAyristirici.Ayristir("hafiza_var(a, -20, 0.5)");

            Assert.Equal(-20m, Assert.IsType<SayiArgumani>(c.Argumanlar[1]).Deger);
            Assert.Equal(0.5m, Assert.IsType<SayiArgumani>(c.Argumanlar[2]).Deger);
        }

        [Fact]
        public void ToString_kanonik_ve_bosluktan_bagimsiz()
        {
            var a = IfadeAyristirici.Ayristir("  inanc ( borclu( Mehmet ,*, *) )  ");
            var b = IfadeAyristirici.Ayristir("inanc(borclu(Mehmet,*,*))");

            Assert.Equal("inanc(borclu(Mehmet,*,*))", a.ToString());
            Assert.Equal(a.ToString(), b.ToString());
            Assert.Equal("gercek(konum!(ben,han))", IfadeAyristirici.Ayristir("gercek(konum!(ben, han))").ToString());
            Assert.Equal("hafiza_var(a,olay.fail,168)", IfadeAyristirici.Ayristir("hafiza_var(a, olay.fail, 168)").ToString());
        }

        [Fact]
        public void Konumlar_kaynak_metindeki_indekslerdir()
        {
            var c = IfadeAyristirici.Ayristir("inanc(borclu(Mehmet))");

            Assert.Equal(0, c.Konum);
            Assert.Equal(6, c.Argumanlar.Single().Konum);
        }

        [Theory]
        [InlineData("", 0)]
        [InlineData("   ", 3)]
        [InlineData("1fikir(a)", 0)]
        [InlineData("fikir", 5)]
        [InlineData("fikir(", 6)]
        [InlineData("fikir(a", 7)]
        [InlineData("fikir(a,)", 8)]
        [InlineData("fikir(a b)", 8)]
        [InlineData("fikir(a) x", 9)]
        [InlineData("fikir(@)", 6)]
        [InlineData("olay.", 4)]
        [InlineData("fikir(olay.x)", 11)]
        [InlineData("fikir(olay)", 10)]
        [InlineData("fikir!", 6)]
        [InlineData("fikir(-)", 7)]
        [InlineData("fikir(1.)", 8)]
        public void Hatali_ifadede_KriterHatasi_konumu_dogru(string ifade, int beklenenKonum)
        {
            var hata = Assert.Throws<KriterHatasi>(() => IfadeAyristirici.Ayristir(ifade));

            Assert.Equal(beklenenKonum, hata.Konum);
            Assert.Contains("konum " + beklenenKonum, hata.Message);
        }

        [Fact]
        public void Cok_buyuk_sayi_KriterHatasi_verir()
        {
            var hata = Assert.Throws<KriterHatasi>(() =>
                IfadeAyristirici.Ayristir("yay(99999999999999999999999999999999999)"));

            Assert.Equal(4, hata.Konum);
            Assert.Contains("konum 4", hata.Message);
        }
    }
}
