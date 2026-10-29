using System;
using Xunit;
using Yanki.Core.Criteria;
using Yanki.Core.Tests.World;
using Yanki.Core.World;

namespace Yanki.Core.Tests.Criteria
{
    public class DunyaSorguCozucusuTests
    {
        // Sabit dünya: konum!(Ali,han), konum!(Ayse,han), konum!(Mehmet,pazar), borclu(Mehmet,Ayse,500)
        private static bool Tutar(string alan, SorguBaglami baglam)
        {
            var cozucu = new DunyaSorguCozucusu(OlayUretici.SabitDunya());
            var kriter = Kriter.Olustur(alan, KriterOperatoru.Esit, SorguDegeri.Mantiksal(true));
            return KriterDegerlendirici.Degerlendir(kriter, cozucu, baglam).Tuttu;
        }

        [Fact]
        public void Gercek_tekdegerli_gercegi_bulur()
        {
            var b = new SorguBaglami("Ali");

            Assert.True(Tutar("gercek(konum!(Ali, han))", b));
            Assert.False(Tutar("gercek(konum!(Ali, pazar))", b));
            Assert.False(Tutar("gercek(konum!(Veli, han))", b));
        }

        [Fact]
        public void Gercek_joker_ve_sayi_argumani()
        {
            var b = new SorguBaglami("Ali");

            Assert.True(Tutar("gercek(borclu(Mehmet, *, *))", b));
            Assert.True(Tutar("gercek(borclu(Mehmet, Ayse, 500))", b));
            Assert.True(Tutar("gercek(borclu(*, Ayse, *))", b));
            Assert.False(Tutar("gercek(borclu(Ayse, *, *))", b));
            Assert.False(Tutar("gercek(borclu(Mehmet, Ayse, 400))", b));
            Assert.False(Tutar("gercek(borclu(Mehmet, *))", b));          // argüman sayısı farklı
            Assert.False(Tutar("gercek(alacakli(*, *, *))", b));
        }

        // Karar (Yusuf): '*' önermenin her argüman yerinde serbesttir.
        [Theory]
        [InlineData("gercek(borclu(*, Ayse, *))", true)]      // ilk ve son argüman joker
        [InlineData("gercek(borclu(*, Mehmet, *))", false)]   // Mehmet kimseye alacaklı değil
        [InlineData("gercek(borclu(Mehmet, *, 500))", true)]  // ortadaki joker
        [InlineData("gercek(borclu(Mehmet, *, 400))", false)]
        [InlineData("gercek(borclu(*, *, 500))", true)]
        [InlineData("gercek(borclu(*, *, *))", true)]
        [InlineData("gercek(konum!(*, han))", true)]          // tek değerli yüklemde de
        [InlineData("gercek(konum!(*, kale))", false)]
        [InlineData("gercek(konum!(Ali, *))", true)]
        [InlineData("gercek(konum!(Veli, *))", false)]
        [InlineData("gercek(faiz(*, *, 0.000002))", true)]    // sayı argümanı ve jokerler
        public void Gercek_joker_her_arguman_yerinde_serbest(string alan, bool beklenen)
        {
            Assert.Equal(beklenen, Tutar(alan, new SorguBaglami("Ali")));
        }

        [Fact]
        public void Gercek_olay_hedef_ve_ben_rolleri_baglamdan_cozulur()
        {
            var b = new SorguBaglami("Ali", olayHedef: "Mehmet");

            Assert.True(Tutar("gercek(konum!(ben, han))", b));
            Assert.True(Tutar("gercek(borclu(olay.hedef, Ayse, 500))", b));
            Assert.False(Tutar("gercek(konum!(olay.hedef, han))", b));   // Mehmet pazarda
        }

        [Fact]
        public void Gercek_rol_baglamda_yoksa_hata()
        {
            var b = new SorguBaglami("Ali");

            Assert.Throws<InvalidOperationException>(() => Tutar("gercek(konum!(olay.fail, han))", b));
        }

        [Fact]
        public void AyniMekanda_ayni_mekandaysa_true_degilse_false()
        {
            var b = new SorguBaglami("Ali", oyuncu: "Mehmet", dinleyen: "Ayse");

            Assert.True(Tutar("ayni_mekanda(dinleyen)", b));    // Ali ve Ayse han'da
            Assert.True(Tutar("ayni_mekanda(Ayse)", b));
            Assert.False(Tutar("ayni_mekanda(oyuncu)", b));     // Mehmet pazarda
        }

        [Fact]
        public void AyniMekanda_konumu_olmayan_varlik_icin_false()
        {
            var b = new SorguBaglami("Ali");

            Assert.False(Tutar("ayni_mekanda(Veli)", b));
            Assert.False(Tutar("ayni_mekanda(han)", b));          // mekanın kendi konumu yok
            Assert.False(Tutar("ayni_mekanda(Ali)", new SorguBaglami("Veli")));  // ben'in konumu yok
        }

        [Fact]
        public void AyniMekanda_kendisiyle_konumu_varsa_true()
        {
            Assert.True(Tutar("ayni_mekanda(ben)", new SorguBaglami("Ali")));
        }

        [Fact]
        public void AyniMekanda_konum_degisince_sonuc_degisir()
        {
            var dunya = OlayUretici.SabitDunya();
            var u = new OlayUretici();
            var b = new SorguBaglami("Ali", dinleyen: "Ayse");
            var cozucu = new DunyaSorguCozucusu(dunya);
            var kriter = Kriter.Olustur("ayni_mekanda(dinleyen)", KriterOperatoru.Esit, SorguDegeri.Mantiksal(true));

            Assert.True(KriterDegerlendirici.Degerlendir(kriter, cozucu, b).Tuttu);
            dunya.Uygula(u.GercekEkle("konum!(Ayse,pazar)"));
            Assert.False(KriterDegerlendirici.Degerlendir(kriter, cozucu, b).Tuttu);
        }

        [Theory]
        [InlineData("kisilik(acgozluluk)")]
        [InlineData("eksen(oyuncu, guven)")]
        [InlineData("fikir(dinleyen)")]
        [InlineData("fikir_bandi(oyuncu)")]
        [InlineData("modifier_var(oyuncu, x)")]
        [InlineData("duygu(ofke)")]
        [InlineData("duygu_hedefi()")]
        [InlineData("stres()")]
        [InlineData("inanc(borclu(Mehmet, *, *))")]
        [InlineData("kaynak(borclu(Mehmet, *, *), Ayse)")]
        [InlineData("hafiza_var(yardim_etti, oyuncu, 168)")]
        [InlineData("yalan_soyledi(olay.hedef)")]
        [InlineData("yay(kasaba_gerginligi)")]
        public void Diger_13_fonksiyon_bu_cozucuda_desteklenmez(string alan)
        {
            var cozucu = new DunyaSorguCozucusu(new Dunya());
            var cagri = IfadeAyristirici.Ayristir(alan);

            var hata = Assert.Throws<NotSupportedException>(() => cozucu.Coz(cagri, new SorguBaglami("Ali", "Oyuncu1", "Ali", "Ayse", "Ali", "Ayse")));

            Assert.Contains(cagri.Ad, hata.Message);
        }
    }
}
