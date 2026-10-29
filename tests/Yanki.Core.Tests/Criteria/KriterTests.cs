using System;
using Xunit;
using Yanki.Core.Criteria;

namespace Yanki.Core.Tests.Criteria
{
    public class KriterTests
    {
        [Fact]
        public void Agirlik_varsayilan_bir_zorunlu_varsayilan_false()
        {
            var k = Kriter.Olustur("fikir(dinleyen)", KriterOperatoru.Kucuk, SorguDegeri.Sayi(-10));

            Assert.Equal(1m, k.Agirlik);
            Assert.False(k.Zorunlu);
            Assert.Equal("fikir(dinleyen)", k.Alan.ToString());
            Assert.Equal(KriterOperatoru.Kucuk, k.Op);
        }

        [Fact]
        public void Agirlik_ve_zorunlu_verilebilir()
        {
            var k = Kriter.Olustur("stres()", KriterOperatoru.BuyukEsit, SorguDegeri.Sayi(50), 2.5m, true);

            Assert.Equal(2.5m, k.Agirlik);
            Assert.True(k.Zorunlu);
        }

        [Fact]
        public void Mantiksal_degerde_kucuktur_hata_fonksiyon_tur_ve_operator_icerir()
        {
            var hata = Assert.Throws<KriterHatasi>(() =>
                Kriter.Olustur("ayni_mekanda(oyuncu)", KriterOperatoru.Kucuk, SorguDegeri.Mantiksal(true)));

            Assert.Contains("ayni_mekanda", hata.Message);
            Assert.Contains("<", hata.Message);
            Assert.Contains("mantıksal", hata.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("duygu_hedefi()", KriterOperatoru.Buyuk)]
        [InlineData("fikir_bandi(oyuncu)", KriterOperatoru.KucukEsit)]
        public void Varlik_ve_bant_yalnizca_esit_ve_esit_degil_alir(string alan, KriterOperatoru op)
        {
            Assert.Throws<KriterHatasi>(() => Kriter.Olustur(alan, op, SorguDegeri.Metin("dost")));
        }

        [Fact]
        public void Tanimsiz_operator_degeri_hata_verir()
        {
            Assert.Throws<KriterHatasi>(() => Kriter.Olustur("stres()", (KriterOperatoru)99, SorguDegeri.Sayi(1)));
        }

        [Fact]
        public void Mantiksal_esit_ve_esit_degil_gecerli()
        {
            Kriter.Olustur("ayni_mekanda(oyuncu)", KriterOperatoru.Esit, SorguDegeri.Mantiksal(true));
            Kriter.Olustur("ayni_mekanda(oyuncu)", KriterOperatoru.EsitDegil, SorguDegeri.Mantiksal(false));
            Kriter.Olustur("fikir_bandi(oyuncu)", KriterOperatoru.Esit, SorguDegeri.Metin("soguk"));
            Kriter.Olustur("duygu_hedefi()", KriterOperatoru.Esit, SorguDegeri.Metin("oyuncu"));
        }

        [Fact]
        public void Sayida_aralik_gecerli_ters_aralik_hata()
        {
            Kriter.Olustur("yay(kasaba_gerginligi)", KriterOperatoru.Aralik, SorguDegeri.Aralik(2, 4));
            Kriter.Olustur("yay(kasaba_gerginligi)", KriterOperatoru.Aralik, SorguDegeri.Aralik(3, 3));

            Assert.Throws<ArgumentException>(() => SorguDegeri.Aralik(4, 2));
        }

        [Fact]
        public void Aralik_operatoru_sayi_olmayan_donuste_ya_da_aralik_olmayan_degerle_hata()
        {
            Assert.Throws<KriterHatasi>(() => Kriter.Olustur("ayni_mekanda(oyuncu)", KriterOperatoru.Aralik, SorguDegeri.Aralik(0, 1)));
            Assert.Throws<KriterHatasi>(() => Kriter.Olustur("stres()", KriterOperatoru.Aralik, SorguDegeri.Sayi(5)));
            Assert.Throws<KriterHatasi>(() => Kriter.Olustur("stres()", KriterOperatoru.Buyuk, SorguDegeri.Aralik(1, 2)));
        }

        [Fact]
        public void Deger_turu_donus_turuyle_uyusmazsa_hata()
        {
            Assert.Throws<KriterHatasi>(() => Kriter.Olustur("stres()", KriterOperatoru.Buyuk, SorguDegeri.Metin("cok")));
            Assert.Throws<KriterHatasi>(() => Kriter.Olustur("ayni_mekanda(oyuncu)", KriterOperatoru.Esit, SorguDegeri.Sayi(1)));
            Assert.Throws<KriterHatasi>(() => Kriter.Olustur("fikir_bandi(oyuncu)", KriterOperatoru.Esit, SorguDegeri.Mantiksal(true)));
        }

        [Fact]
        public void Bant_gecersiz_deger_hata()
        {
            var hata = Assert.Throws<KriterHatasi>(() =>
                Kriter.Olustur("fikir_bandi(oyuncu)", KriterOperatoru.Esit, SorguDegeri.Metin("sicak")));

            Assert.Contains("sicak", hata.Message);
        }

        [Fact]
        public void Varlik_degeri_kimlik_ya_da_rol_olmali()
        {
            Kriter.Olustur("duygu_hedefi()", KriterOperatoru.Esit, SorguDegeri.Metin("olay.fail"));
            Assert.Throws<KriterHatasi>(() => Kriter.Olustur("duygu_hedefi()", KriterOperatoru.Esit, SorguDegeri.Metin("Ali Veli")));
        }

        [Fact]
        public void Gecersiz_alan_ifadesi_KriterHatasi_verir()
        {
            Assert.Throws<KriterHatasi>(() => Kriter.Olustur("bilinmeyen(x)", KriterOperatoru.Esit, SorguDegeri.Sayi(1)));
            Assert.Throws<KriterHatasi>(() => Kriter.Olustur("fikir(", KriterOperatoru.Esit, SorguDegeri.Sayi(1)));
        }

        [Fact]
        public void ToString_okunur_kriter_metni()
        {
            Assert.Equal(
                "fikir(dinleyen) < -10",
                Kriter.Olustur("fikir(dinleyen)", KriterOperatoru.Kucuk, SorguDegeri.Sayi(-10)).ToString());
            Assert.Equal(
                "yay(kasaba_gerginligi) aralik [2, 4]",
                Kriter.Olustur("yay(kasaba_gerginligi)", KriterOperatoru.Aralik, SorguDegeri.Aralik(2, 4)).ToString());
            Assert.Equal(
                "gercek(konum!(Ali,han)) == true",
                Kriter.Olustur("gercek(konum!(Ali, han))", KriterOperatoru.Esit, SorguDegeri.Mantiksal(true)).ToString());
        }
    }
}
