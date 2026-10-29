using System;
using Xunit;
using Yanki.Core.Criteria;

namespace Yanki.Core.Tests.Criteria
{
    public class DegerlendirmeTests
    {
        private static readonly SorguBaglami Baglam = new SorguBaglami("Ali", oyuncu: "Oyuncu1", dinleyen: "Ayse");

        private static KriterSonucu Degerlendir(string alan, KriterOperatoru op, SorguDegeri deger, SorguDegeri gozlenen, decimal agirlik = 1m)
        {
            var cozucu = new FakeSorguCozucu().Ayarla(alan, gozlenen);
            return KriterDegerlendirici.Degerlendir(Kriter.Olustur(alan, op, deger, agirlik), cozucu, Baglam);
        }

        [Theory]
        [InlineData(KriterOperatoru.Kucuk, 4, 5, true)]
        [InlineData(KriterOperatoru.Kucuk, 5, 5, false)]
        [InlineData(KriterOperatoru.KucukEsit, 5, 5, true)]
        [InlineData(KriterOperatoru.KucukEsit, 6, 5, false)]
        [InlineData(KriterOperatoru.Buyuk, 6, 5, true)]
        [InlineData(KriterOperatoru.Buyuk, 5, 5, false)]
        [InlineData(KriterOperatoru.BuyukEsit, 5, 5, true)]
        [InlineData(KriterOperatoru.BuyukEsit, 4, 5, false)]
        [InlineData(KriterOperatoru.Esit, 5, 5, true)]
        [InlineData(KriterOperatoru.Esit, 4, 5, false)]
        [InlineData(KriterOperatoru.EsitDegil, 4, 5, true)]
        [InlineData(KriterOperatoru.EsitDegil, 5, 5, false)]
        public void Sayi_operatorleri(KriterOperatoru op, int gozlenen, int esik, bool beklenen)
        {
            var sonuc = Degerlendir("stres()", op, SorguDegeri.Sayi(esik), SorguDegeri.Sayi(gozlenen));

            Assert.Equal(beklenen, sonuc.Tuttu);
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(2, true)]
        [InlineData(3, true)]
        [InlineData(4, true)]
        [InlineData(5, false)]
        public void Aralik_uclari_dahildir(int gozlenen, bool beklenen)
        {
            var sonuc = Degerlendir("yay(kasaba_gerginligi)", KriterOperatoru.Aralik, SorguDegeri.Aralik(2, 4), SorguDegeri.Sayi(gozlenen));

            Assert.Equal(beklenen, sonuc.Tuttu);
        }

        [Fact]
        public void Mantiksal_ve_metin_karsilastirmasi()
        {
            Assert.True(Degerlendir("ayni_mekanda(oyuncu)", KriterOperatoru.Esit, SorguDegeri.Mantiksal(true), SorguDegeri.Mantiksal(true)).Tuttu);
            Assert.False(Degerlendir("ayni_mekanda(oyuncu)", KriterOperatoru.Esit, SorguDegeri.Mantiksal(true), SorguDegeri.Mantiksal(false)).Tuttu);
            Assert.True(Degerlendir("ayni_mekanda(oyuncu)", KriterOperatoru.EsitDegil, SorguDegeri.Mantiksal(true), SorguDegeri.Mantiksal(false)).Tuttu);
            Assert.True(Degerlendir("fikir_bandi(oyuncu)", KriterOperatoru.Esit, SorguDegeri.Metin("soguk"), SorguDegeri.Metin("soguk")).Tuttu);
            Assert.False(Degerlendir("fikir_bandi(oyuncu)", KriterOperatoru.Esit, SorguDegeri.Metin("soguk"), SorguDegeri.Metin("dost")).Tuttu);
        }

        [Fact]
        public void Varlik_degeri_rol_adiysa_baglamdan_cozulur()
        {
            // duygu_hedefi() == oyuncu → bağlamda oyuncu = "Oyuncu1"
            Assert.True(Degerlendir("duygu_hedefi()", KriterOperatoru.Esit, SorguDegeri.Metin("oyuncu"), SorguDegeri.Metin("Oyuncu1")).Tuttu);
            Assert.False(Degerlendir("duygu_hedefi()", KriterOperatoru.Esit, SorguDegeri.Metin("oyuncu"), SorguDegeri.Metin("Ayse")).Tuttu);
            Assert.True(Degerlendir("duygu_hedefi()", KriterOperatoru.Esit, SorguDegeri.Metin("Mehmet"), SorguDegeri.Metin("Mehmet")).Tuttu);
        }

        [Fact]
        public void Katki_tutarsa_agirlik_tutmazsa_sifir()
        {
            var tutan = Degerlendir("stres()", KriterOperatoru.Buyuk, SorguDegeri.Sayi(1), SorguDegeri.Sayi(2), 3m);
            var tutmayan = Degerlendir("stres()", KriterOperatoru.Buyuk, SorguDegeri.Sayi(5), SorguDegeri.Sayi(2), 3m);

            Assert.Equal(3m, tutan.Katki);
            Assert.Equal(0m, tutmayan.Katki);
            Assert.Equal(2m, tutan.Gozlenen.SayiDegeri);
        }

        [Fact]
        public void Rol_baglamda_tanimsizsa_anlasilir_hata()
        {
            var cozucu = new FakeSorguCozucu().Ayarla("duygu_hedefi()", SorguDegeri.Metin("Ali"));
            var kriter = Kriter.Olustur("duygu_hedefi()", KriterOperatoru.Esit, SorguDegeri.Metin("olay.fail"));

            var hata = Assert.Throws<InvalidOperationException>(() => KriterDegerlendirici.Degerlendir(kriter, cozucu, Baglam));

            Assert.Contains("olay.fail", hata.Message);
            Assert.Contains("tanımlı değil", hata.Message);
        }

        [Fact]
        public void SorguBaglami_roller_cozulur_ve_ben_zorunlu()
        {
            Assert.Equal("Ali", Baglam.Coz(Rol.Ben));
            Assert.Equal("Oyuncu1", Baglam.Coz(Rol.Oyuncu));
            Assert.Equal("Ayse", Baglam.Coz(Rol.Dinleyen));
            Assert.Throws<InvalidOperationException>(() => Baglam.Coz(Rol.Konusan));
            Assert.Throws<ArgumentException>(() => new SorguBaglami(""));
        }

        [Fact]
        public void Cozucu_beklenmeyen_turde_deger_dondururse_hata()
        {
            var cozucu = new FakeSorguCozucu().Ayarla("stres()", SorguDegeri.Mantiksal(true));
            var kriter = Kriter.Olustur("stres()", KriterOperatoru.Buyuk, SorguDegeri.Sayi(1));

            var hata = Assert.Throws<InvalidOperationException>(() => KriterDegerlendirici.Degerlendir(kriter, cozucu, Baglam));

            Assert.Contains("stres", hata.Message);
        }
    }
}
