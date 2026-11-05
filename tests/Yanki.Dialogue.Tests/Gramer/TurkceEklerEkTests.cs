using System;
using System.Globalization;
using Xunit;
using Yanki.Dialogue.Gramer;

namespace Yanki.Dialogue.Tests.Gramer
{
    /// <summary>
    /// DURAK 2 kararları (8. hafta): <c>.tamlayan</c> (altın tablo elle), ünsüz yumuşaması (cins isimde p, ç, t, k ile biten sözcük için yumuşak gövde zorunlu; özel adda yumuşama yok),
    /// kişi zamirleri (ben/sen ve ekli hâlleri) ve kültürden bağımsız büyük harf (i → İ, ı → I).
    /// </summary>
    public class TurkceEklerEkTests
    {
        [Theory]
        [InlineData("Ayşe", true, "Ayşe'nin")]
        [InlineData("Can", true, "Can'ın")]
        [InlineData("Mehmet", true, "Mehmet'in")]
        [InlineData("Ali", true, "Ali'nin")]
        [InlineData("hancı", false, "hancının")]
        [InlineData("muhtar", false, "muhtarın")]
        [InlineData("Ömer", true, "Ömer'in")]
        [InlineData("Yusuf", true, "Yusuf'un")]
        [InlineData("Müslüm", true, "Müslüm'ün")]
        [InlineData("Kemal", true, "Kemal'in")]
        [InlineData("çiftçi", false, "çiftçinin")]
        [InlineData("kömür", false, "kömürün")]
        public void Tamlayan_altin_tablo_elle_yazilmis(string ad, bool ozel, string beklenen)
        {
            Assert.Equal(beklenen, TurkceEkler.Uygula(ad, ozel, "tamlayan"));
        }

        [Fact]
        public void Cins_isim_p_c_t_k_ile_bitiyorsa_unluyle_baslayan_ek_icin_yumusak_govde_zorunlu_sessiz_yanlis_ek_uretilmez()
        {
            foreach (var ek in new[] { "yonelme", "belirtme", "iyelik", "tamlayan" })
            {
                var hata = Assert.Throws<ArgumentException>(() => TurkceEkler.Uygula("borç", false, ek));
                Assert.Contains("yumuşak gövde", hata.Message, StringComparison.Ordinal);
            }

            Assert.Equal("borca", TurkceEkler.Uygula("borç", false, "yonelme", yumusakGovde: "borc"));
            Assert.Equal("borcu", TurkceEkler.Uygula("borç", false, "belirtme", yumusakGovde: "borc"));
            Assert.Equal("borcu", TurkceEkler.Uygula("borç", false, "iyelik", yumusakGovde: "borc"));
            Assert.Equal("borcun", TurkceEkler.Uygula("borç", false, "tamlayan", yumusakGovde: "borc"));
            Assert.Equal("kitaba", TurkceEkler.Uygula("kitap", false, "yonelme", yumusakGovde: "kitab"));
            Assert.Equal("ağaca", TurkceEkler.Uygula("ağaç", false, "yonelme", yumusakGovde: "ağac"));
            Assert.Equal("bebeğe", TurkceEkler.Uygula("bebek", false, "yonelme", yumusakGovde: "bebeğ"));
            Assert.Equal("ota", TurkceEkler.Uygula("ot", false, "yonelme", yumusakGovde: "ot"));   // yumuşamayan sözcük: gövde aynen yazılır
        }

        [Fact]
        public void Unsuzle_baslayan_ekler_yumusak_govde_istemez_ozel_adda_yumusama_yoktur()
        {
            Assert.Equal("borçta", TurkceEkler.Uygula("borç", false, "bulunma"));
            Assert.Equal("borçtan", TurkceEkler.Uygula("borç", false, "ayrilma"));
            Assert.Equal("Mehmet'e", TurkceEkler.Uygula("Mehmet", true, "yonelme"));
            Assert.Equal("Burak'a", TurkceEkler.Uygula("Burak", true, "yonelme"));   // özel ad: Burağa değil
            Assert.Equal("Selçuk'un", TurkceEkler.Uygula("Selçuk", true, "tamlayan"));
        }

        [Theory]
        [InlineData("ben", null, "ben")]
        [InlineData("ben", "yonelme", "bana")]
        [InlineData("ben", "belirtme", "beni")]
        [InlineData("ben", "bulunma", "bende")]
        [InlineData("ben", "ayrilma", "benden")]
        [InlineData("ben", "tamlayan", "benim")]
        [InlineData("sen", null, "sen")]
        [InlineData("sen", "yonelme", "sana")]
        [InlineData("sen", "belirtme", "seni")]
        [InlineData("sen", "bulunma", "sende")]
        [InlineData("sen", "ayrilma", "senden")]
        [InlineData("sen", "tamlayan", "senin")]
        public void Kisi_zamirleri_ekli_hallerde_elle_tablo(string kisi, string? ek, string beklenen)
        {
            Assert.Equal(beklenen, TurkceEkler.Zamir(kisi, ek));
        }

        [Fact]
        public void Zamire_iyelik_getirilmez_bilinmeyen_kisi_hata()
        {
            Assert.Throws<ArgumentException>(() => TurkceEkler.Zamir("ben", "iyelik"));
            Assert.Throws<ArgumentException>(() => TurkceEkler.Zamir("o", null));
        }

        [Theory]
        [InlineData("ibrahim", "İbrahim")]
        [InlineData("ışık", "Işık")]
        [InlineData("bugün de uyarmıştım", "Bugün de uyarmıştım")]
        [InlineData("üç gün önce", "Üç gün önce")]
        [InlineData("evvelsi gün", "Evvelsi gün")]
        [InlineData("ağaç", "Ağaç")]
        [InlineData("çok önce", "Çok önce")]
        [InlineData("şimdi", "Şimdi")]
        [InlineData("öfke", "Öfke")]
        [InlineData("Zaten büyük", "Zaten büyük")]
        [InlineData("İpek", "İpek")]
        [InlineData("1 gün", "1 gün")]
        [InlineData("", "")]
        public void Satir_basi_buyuk_harf_noktali_i_ve_noktasiz_i_elle_tablo(string girdi, string beklenen)
        {
            Assert.Equal(beklenen, TurkceEkler.BasHarfBuyut(girdi, true));
        }

        [Fact]
        public void Ingilizce_dosyada_i_noktalidir_buyuk_I_olur()
        {
            Assert.Equal("Iron", TurkceEkler.BasHarfBuyut("iron", false));
            Assert.Equal("Apple", TurkceEkler.BasHarfBuyut("apple", false));
        }

        [Fact]
        public void Buyuk_harf_tr_TR_ve_invariant_kulturde_ayni()
        {
            var onceki = CultureInfo.CurrentCulture;
            try
            {
                foreach (var kultur in new[] { "tr-TR", "en-US", string.Empty })
                {
                    CultureInfo.CurrentCulture = new CultureInfo(kultur);
                    Assert.Equal("İbrahim Işık", TurkceEkler.BasHarfBuyut("ibrahim Işık", true));
                    Assert.Equal("Işık", TurkceEkler.BasHarfBuyut("ışık", true));
                    Assert.Equal("Ağaç", TurkceEkler.BasHarfBuyut("ağaç", true));
                    Assert.Equal("Borca", TurkceEkler.BasHarfBuyut(TurkceEkler.Uygula("borç", false, "yonelme", yumusakGovde: "borc"), true));
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = onceki;
            }
        }
    }
}
