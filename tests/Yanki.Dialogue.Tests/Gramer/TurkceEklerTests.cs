using System;
using System.Globalization;
using Xunit;
using Yanki.Dialogue.Gramer;

namespace Yanki.Dialogue.Tests.Gramer
{
    /// <summary>
    /// Türkçe ek değiştiricileri (8. hafta, F kararı): ünlü uyumu ve kaynaştırma elle tablo. Altın tablo ELLE yazılmıştır (koddan kopyalanmadı). Özel ada kesme işareti,
    /// cins isme kesme işareti yok. Yanlış ek sessizce üretilmez. Sayıya ek bu hafta yok.
    /// </summary>
    public class TurkceEklerTests
    {
        // (ad, özel ad mı, ek, beklenen)
        public static readonly object[][] Altin =
        {
            new object[] { "Ayşe", true, "yonelme", "Ayşe'ye" },
            new object[] { "Ayşe", true, "belirtme", "Ayşe'yi" },
            new object[] { "Ayşe", true, "bulunma", "Ayşe'de" },
            new object[] { "Ayşe", true, "ayrilma", "Ayşe'den" },
            new object[] { "Mehmet", true, "yonelme", "Mehmet'e" },
            new object[] { "Mehmet", true, "belirtme", "Mehmet'i" },
            new object[] { "Mehmet", true, "bulunma", "Mehmet'te" },
            new object[] { "Mehmet", true, "ayrilma", "Mehmet'ten" },
            new object[] { "Can", true, "yonelme", "Can'a" },
            new object[] { "Can", true, "belirtme", "Can'ı" },
            new object[] { "Can", true, "bulunma", "Can'da" },
            new object[] { "Can", true, "ayrilma", "Can'dan" },
            new object[] { "Hatice", true, "yonelme", "Hatice'ye" },
            new object[] { "Hatice", true, "belirtme", "Hatice'yi" },
            new object[] { "Selim", true, "yonelme", "Selim'e" },
            new object[] { "Selim", true, "belirtme", "Selim'i" },
            new object[] { "Ali", true, "yonelme", "Ali'ye" },
            new object[] { "Ali", true, "belirtme", "Ali'yi" },
            new object[] { "Ali", true, "bulunma", "Ali'de" },
            new object[] { "hancı", false, "yonelme", "hancıya" },
            new object[] { "hancı", false, "belirtme", "hancıyı" },
            new object[] { "hancı", false, "bulunma", "hancıda" },
            new object[] { "hancı", false, "ayrilma", "hancıdan" },
            new object[] { "muhtar", false, "yonelme", "muhtara" },
            new object[] { "muhtar", false, "belirtme", "muhtarı" },
            new object[] { "muhtar", false, "bulunma", "muhtarda" },
            new object[] { "muhtar", false, "ayrilma", "muhtardan" },
            // iyelik (3. tekil kişi iyelik eki): ünlüyle biten -sI, ünsüzle biten -I
            new object[] { "hancı", false, "iyelik", "hancısı" },
            new object[] { "muhtar", false, "iyelik", "muhtarı" },
            new object[] { "Ayşe", true, "iyelik", "Ayşe'si" },
            new object[] { "Mehmet", true, "iyelik", "Mehmet'i" },
            // ünlü uyumunun dört yönü ve yuvarlak ünlüler
            new object[] { "Ömer", true, "yonelme", "Ömer'e" },
            new object[] { "Ömer", true, "belirtme", "Ömer'i" },
            new object[] { "Murat", true, "belirtme", "Murat'ı" },
            new object[] { "Volkan", true, "belirtme", "Volkan'ı" },
            new object[] { "Yusuf", true, "belirtme", "Yusuf'u" },
            new object[] { "Yusuf", true, "bulunma", "Yusuf'ta" },
            new object[] { "Müslüm", true, "belirtme", "Müslüm'ü" },
            new object[] { "çiftçi", false, "belirtme", "çiftçiyi" },
            new object[] { "çiftçi", false, "iyelik", "çiftçisi" },
            new object[] { "kömür", false, "yonelme", "kömüre" },
            new object[] { "kömür", false, "belirtme", "kömürü" },
            // sert ünsüzle biten sözcüklerde -te/-ta/-ten/-tan: ç f h k p s ş t
            new object[] { "Burak", true, "bulunma", "Burak'ta" },
            new object[] { "Kerem", true, "bulunma", "Kerem'de" },
            new object[] { "Nuş", true, "ayrilma", "Nuş'tan" },
            new object[] { "Selçuk", true, "ayrilma", "Selçuk'tan" },
            new object[] { "Tarık", true, "bulunma", "Tarık'ta" },
            // büyük İ ve I (kültürden bağımsız harf eşlemesi)
            new object[] { "Işık", true, "yonelme", "Işık'a" },
            new object[] { "İpek", true, "yonelme", "İpek'e" },
            new object[] { "İpek", true, "belirtme", "İpek'i" },
            new object[] { "ILGAZ", true, "yonelme", "ILGAZ'a" },
        };

        [Theory]
        [MemberData(nameof(Altin))]
        public void Altin_tablo_elle_yazilmis_ekleri_uretir(string ad, bool ozel, string ek, string beklenen)
        {
            Assert.Equal(beklenen, TurkceEkler.Uygula(ad, ozel, ek));
        }

        [Fact]
        public void Altin_tablo_tr_TR_ve_invariant_kulturde_ayni_cikti_verir()
        {
            var onceki = CultureInfo.CurrentCulture;
            try
            {
                foreach (var kultur in new[] { "tr-TR", "en-US", string.Empty })
                {
                    CultureInfo.CurrentCulture = new CultureInfo(kultur);
                    foreach (var satir in Altin)
                    {
                        Assert.Equal((string)satir[3], TurkceEkler.Uygula((string)satir[0], (bool)satir[1], (string)satir[2]));
                    }
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = onceki;
            }
        }

        [Fact]
        public void Bilinmeyen_ek_sessizce_gecilmez_hata_verir()
        {
            var hata = Assert.Throws<ArgumentException>(() => TurkceEkler.Uygula("Ayşe", true, "tamlayan"));
            Assert.Contains("tamlayan", hata.Message);
        }

        [Theory]
        [InlineData("500")]
        [InlineData("12")]
        [InlineData("-3")]
        public void Sayiya_ek_getirilmez_acik_hata_verir(string sayi)
        {
            var hata = Assert.Throws<ArgumentException>(() => TurkceEkler.Uygula(sayi, false, "yonelme"));
            Assert.Contains("sayı", hata.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("123abc")]
        public void Rakam_iceren_ya_da_bos_sozcuk_acik_hata_verir(string ad)
        {
            Assert.Throws<ArgumentException>(() => TurkceEkler.Uygula(ad, false, "yonelme"));
        }

        [Theory]
        [InlineData("bk")]
        [InlineData("Dr")]
        public void Unlu_icermeyen_sozcuk_icin_ek_uretilmez(string ad)
        {
            var hata = Assert.Throws<ArgumentException>(() => TurkceEkler.Uygula(ad, true, "yonelme"));
            Assert.Contains("ünlü", hata.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Sayi_denetimi_ekten_once_calisir_ozel_ad_sayi_olamaz()
        {
            Assert.True(TurkceEkler.SayiMi("500"));
            Assert.False(TurkceEkler.SayiMi("Ayşe"));
            Assert.False(TurkceEkler.SayiMi("N00"));
        }
    }
}
