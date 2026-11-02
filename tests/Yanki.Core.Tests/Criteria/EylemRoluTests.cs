using System;
using System.Linq;
using Xunit;
using Yanki.Core.Criteria;
using Yanki.Core.Rules;

namespace Yanki.Core.Tests.Criteria
{
    /// <summary><c>eylem.hedef</c> rolü (ADR-011): yalnızca eylem bağlamında geçerlidir; kural bağlamında reddedilir.</summary>
    public class EylemRoluTests
    {
        [Fact]
        public void Rol_adlari_butun_enum_degerleri_icin_gidis_donus_yapar()
        {
            var degerler = Enum.GetValues(typeof(Rol)).Cast<Rol>().ToList();
            Assert.Contains(Rol.EylemHedef, degerler);

            foreach (var rol in degerler)
            {
                var ad = RolAdlari.Ad(rol);
                Assert.True(RolAdlari.TryCoz(ad, out var geri), $"{rol} → \"{ad}\" geri çözülmeli");
                Assert.Equal(rol, geri);
            }

            Assert.Equal(degerler.Count, degerler.Select(RolAdlari.Ad).Distinct().Count());
            Assert.Equal("eylem.hedef", RolAdlari.Ad(Rol.EylemHedef));
            Assert.Equal("olay.hedef", RolAdlari.Ad(Rol.OlayHedef));
        }

        [Fact]
        public void Tanimsiz_enum_degeri_sessizce_olay_hedef_olmaz() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => RolAdlari.Ad((Rol)999));

        [Fact]
        public void Eylem_hedef_ayristirilir()
        {
            var c = IfadeAyristirici.Ayristir("fikir(eylem.hedef)");

            var arg = Assert.IsType<RolArgumani>(Assert.Single(c.Argumanlar));
            Assert.Equal(Rol.EylemHedef, arg.Rol);
        }

        [Fact]
        public void Eylem_hedef_onerme_icinde_de_ayristirilir()
        {
            var c = IfadeAyristirici.Ayristir("inanc(tehlikeli(eylem.hedef))");

            var onerme = Assert.IsType<OnermeArgumani>(Assert.Single(c.Argumanlar)).Cagri;
            Assert.Equal(Rol.EylemHedef, Assert.IsType<RolArgumani>(onerme.Argumanlar[0]).Rol);
        }

        [Theory]
        [InlineData("fikir(eylem.fail)")]
        [InlineData("fikir(eylem.)")]
        public void Eksik_ya_da_yanlis_eylem_rolu_hatadir(string ifade) =>
            Assert.Throws<KriterHatasi>(() => IfadeAyristirici.Ayristir(ifade));

        [Fact]
        public void Baglamda_eylem_hedef_varsayilan_olarak_tanimsizdir()
        {
            var baglam = new SorguBaglami("Ayse");

            Assert.False(baglam.Tanimli(Rol.EylemHedef));
            Assert.Throws<InvalidOperationException>(() => baglam.Coz(Rol.EylemHedef));
        }

        [Fact]
        public void Baglamda_eylem_hedef_olay_hedefinden_ayridir()
        {
            var baglam = new SorguBaglami("Ayse", olayHedef: "Veli", eylemHedef: "Can");

            Assert.Equal("Can", baglam.Coz(Rol.EylemHedef));
            Assert.Equal("Veli", baglam.Coz(Rol.OlayHedef));
            Assert.True(baglam.Tanimli(Rol.EylemHedef));
        }

        // ---- kural bağlamında reddedilir ----

        private static KuralYuklemeSonucu Yukle(string kuralGovdesi) =>
            KuralYukleyici.Yukle("{ \"kurallar\": [ { \"id\": \"k1\", \"ne_zaman\": \"yardim_etti\", " + kuralGovdesi + " } ] }", "k.json", new[] { "yardim_etti" });

        [Fact]
        public void Kural_kriterinde_eylem_hedef_reddedilir()
        {
            var sonuc = Yukle("\"kriterler\": [ { \"alan\": \"fikir(eylem.hedef)\", \"op\": \">\", \"deger\": 0 } ], \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.5 } ]");

            Assert.False(sonuc.Basarili);
            Assert.Contains(sonuc.Hatalar, h => h.ToString().Contains("eylem.hedef") && h.ToString().Contains("k.json:"));
        }

        [Fact]
        public void Kural_etki_hedefinde_eylem_hedef_reddedilir()
        {
            var sonuc = Yukle("\"etkiler\": [ { \"tur\": \"modifier\", \"hedef\": \"eylem.hedef\", \"modifier\": \"minnet\", \"deger\": 1, \"sure\": 10 } ]");

            Assert.False(sonuc.Basarili);
            Assert.Contains(sonuc.Hatalar, h => h.ToString().Contains("eylem.hedef"));
        }

        [Fact]
        public void Kural_gercek_sablonunda_eylem_hedef_reddedilir()
        {
            var sonuc = Yukle("\"etkiler\": [ { \"tur\": \"gercek\", \"islem\": \"ekle\", \"gercek\": \"x({eylem.hedef})\" } ]");

            Assert.False(sonuc.Basarili);
            Assert.Contains(sonuc.Hatalar, h => h.ToString().Contains("eylem.hedef"));
        }
    }
}
