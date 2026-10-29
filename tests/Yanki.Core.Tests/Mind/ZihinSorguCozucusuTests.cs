using System;
using System.Collections.Generic;
using Xunit;
using Yanki.Core.Criteria;
using Yanki.Core.Events;
using Yanki.Core.Mind;
using Yanki.Core.Tests.World;

namespace Yanki.Core.Tests.Mind
{
    /// <summary>
    /// Sözlükteki zihne dayanan fonksiyonların gerçek veriye bağlanması. Her fonksiyon, kriter diliyle uçtan uca
    /// (ayrıştır → doğrula → çöz → karşılaştır) sınanır. Sabit dünya: Ali ve Ayse han'da, Mehmet pazar'da.
    /// </summary>
    public class ZihinSorguCozucusuTests
    {
        private sealed class Duzen
        {
            public readonly ZihinDeposu Zihinler = new ZihinDeposu();
            public readonly OlayGunlugu Gunluk = new OlayGunlugu();
            public readonly ZihinOlayUretici Uretici = new ZihinOlayUretici();

            public Duzen()
            {
                Zihinler.Ekle(new Zihin("Can", new Kisilik(0.8m, 0.3m, 0.5m, 0.9m, 0.4m, 0.6m), new ZihinAyarlari
                {
                    DuyguSonumuTickBasina = 0.1m,
                    StresSonumuTickBasina = 1m,
                    KanitSonumuTickBasina = 0m,
                }));
            }

            public void Uygula(Olay olay) => Zihinler.Uygula(olay);
        }

        private static bool Tutar(Duzen d, string alan, KriterOperatoru op, SorguDegeri deger, long tick = 0, string ben = "Can")
        {
            var cozucu = new ZihinSorguCozucusu(OlayUretici.SabitDunya(), d.Zihinler, d.Gunluk, tick);
            var kriter = Kriter.Olustur(alan, op, deger);
            var baglam = new SorguBaglami(ben, oyuncu: "oyuncu", konusan: "Can", dinleyen: "Ayse", olayFail: "Ali", olayHedef: "Mehmet");
            return KriterDegerlendirici.Degerlendir(kriter, cozucu, baglam).Tuttu;
        }

        private static bool Mantik(Duzen d, string alan, long tick = 0) =>
            Tutar(d, alan, KriterOperatoru.Esit, SorguDegeri.Mantiksal(true), tick);

        private static bool Sayi(Duzen d, string alan, KriterOperatoru op, decimal deger, long tick = 0) =>
            Tutar(d, alan, op, SorguDegeri.Sayi(deger), tick);

        // 1. kisilik
        [Fact]
        public void Kisilik_ozelligi_okunur()
        {
            var d = new Duzen();

            Assert.True(Sayi(d, "kisilik(acgozluluk)", KriterOperatoru.Esit, 0.8m));
            Assert.True(Sayi(d, "kisilik(kin_tutma)", KriterOperatoru.Kucuk, 0.5m));
            Assert.False(Sayi(d, "kisilik(durustluk)", KriterOperatoru.Buyuk, 0.5m));
        }

        // 2. eksen
        [Fact]
        public void Eksen_hedefe_karsi_kalici_degeri_verir()
        {
            var d = new Duzen();
            d.Uygula(d.Uretici.Eksen("Can", "oyuncu", "guven", -30m));

            Assert.True(Sayi(d, "eksen(oyuncu, guven)", KriterOperatoru.Kucuk, -20m));
            Assert.True(Sayi(d, "eksen(dinleyen, guven)", KriterOperatoru.Esit, 0m));
            Assert.True(Sayi(d, "eksen(oyuncu, sevgi)", KriterOperatoru.Esit, 0m));
        }

        // 3. fikir, 4. fikir_bandi, 5. modifier_var
        [Fact]
        public void Fikir_eksenleri_ve_modifierlari_toplar_bandi_hesaplar()
        {
            var d = new Duzen();
            d.Uygula(d.Uretici.Eksen("Can", "oyuncu", "guven", -10m)); // -4
            d.Uygula(d.Uretici.Modifier("Can", "oyuncu", "aileme_iftira", -22m, 18));

            Assert.True(Sayi(d, "fikir(oyuncu)", KriterOperatoru.Esit, -26m));
            Assert.True(Tutar(d, "fikir_bandi(oyuncu)", KriterOperatoru.Esit, SorguDegeri.Metin("soguk")));
            Assert.True(Tutar(d, "fikir_bandi(dinleyen)", KriterOperatoru.Esit, SorguDegeri.Metin("notr")));
            Assert.True(Tutar(d, "fikir_bandi(oyuncu)", KriterOperatoru.EsitDegil, SorguDegeri.Metin("dost")));
        }

        [Fact]
        public void Fikir_tick_ilerleyince_modifier_dusunce_degisir()
        {
            var d = new Duzen();
            d.Uygula(d.Uretici.Modifier("Can", "oyuncu", "aileme_iftira", -22m, 18));

            Assert.True(Sayi(d, "fikir(oyuncu)", KriterOperatoru.Esit, -22m, tick: 17));
            Assert.True(Sayi(d, "fikir(oyuncu)", KriterOperatoru.Esit, 0m, tick: 18));
        }

        [Fact]
        public void Modifier_var_aktif_modifieri_gorur()
        {
            var d = new Duzen();
            d.Uygula(d.Uretici.Modifier("Can", "oyuncu", "yalanini_yakaladim", -30m, 30));

            Assert.True(Mantik(d, "modifier_var(oyuncu, yalanini_yakaladim)"));
            Assert.False(Mantik(d, "modifier_var(oyuncu, minnet)"));
            Assert.False(Mantik(d, "modifier_var(dinleyen, yalanini_yakaladim)"));
            Assert.False(Mantik(d, "modifier_var(oyuncu, yalanini_yakaladim)", tick: 30));
        }

        // 6. duygu, 7. duygu_hedefi
        [Fact]
        public void Duygu_baskin_duygunun_guncel_siddetini_verir_digerleri_icin_sifir()
        {
            var d = new Duzen();
            d.Uygula(d.Uretici.Duygu("Can", "ofke", 0.6m, "oyuncu"));

            Assert.True(Sayi(d, "duygu(ofke)", KriterOperatoru.Esit, 0.6m));
            Assert.True(Sayi(d, "duygu(ofke)", KriterOperatoru.Esit, 0.4m, tick: 2));
            Assert.True(Sayi(d, "duygu(korku)", KriterOperatoru.Esit, 0m));
            Assert.True(Sayi(d, "duygu(ofke)", KriterOperatoru.BuyukEsit, 0.5m));
        }

        [Fact]
        public void Duygu_hedefi_baskin_duygunun_hedefini_verir()
        {
            var d = new Duzen();
            d.Uygula(d.Uretici.Duygu("Can", "ofke", 0.6m, "oyuncu"));

            Assert.True(Tutar(d, "duygu_hedefi()", KriterOperatoru.Esit, SorguDegeri.Metin("oyuncu")));
            Assert.True(Tutar(d, "duygu_hedefi()", KriterOperatoru.Esit, SorguDegeri.Metin("oyuncu")));
            Assert.True(Tutar(d, "duygu_hedefi()", KriterOperatoru.EsitDegil, SorguDegeri.Metin("Ayse")));
        }

        [Fact]
        public void Duygu_hedefi_duygu_yoksa_ya_da_sonmusse_hicbir_varliga_esit_degildir()
        {
            var d = new Duzen();
            Assert.False(Tutar(d, "duygu_hedefi()", KriterOperatoru.Esit, SorguDegeri.Metin("oyuncu")));

            d.Uygula(d.Uretici.Duygu("Can", "ofke", 0.6m, "oyuncu"));
            Assert.False(Tutar(d, "duygu_hedefi()", KriterOperatoru.Esit, SorguDegeri.Metin("oyuncu"), tick: 100));
        }

        // 8. stres
        [Fact]
        public void Stres_guncel_degeri_verir()
        {
            var d = new Duzen();
            d.Uygula(d.Uretici.Stres("Can", 60m));

            Assert.True(Sayi(d, "stres()", KriterOperatoru.BuyukEsit, 50m));
            Assert.True(Sayi(d, "stres()", KriterOperatoru.Esit, 40m, tick: 20));
            Assert.False(Sayi(d, "stres()", KriterOperatoru.BuyukEsit, 50m, tick: 20));
        }

        // 9. inanc, 10. kaynak
        [Fact]
        public void Inanc_joker_onermede_en_yuksek_inanilan_guveni_verir()
        {
            var d = new Duzen();
            d.Uygula(d.Uretici.Kanit("Can", "borclu(Mehmet,Ayse,500)", "duydu", "Ayse", 0.7m));
            d.Uygula(d.Uretici.Kanit("Can", "borclu(Mehmet,Veli,200)", "duydu", "Ali", 0.3m));

            Assert.True(Sayi(d, "inanc(borclu(Mehmet, *, *))", KriterOperatoru.Esit, 0.7m));
            Assert.True(Sayi(d, "inanc(borclu(Mehmet, Veli, 200))", KriterOperatoru.Esit, 0.3m));
            Assert.True(Sayi(d, "inanc(borclu(Ali, *, *))", KriterOperatoru.Esit, 0m));
        }

        [Fact]
        public void Inanc_celiskide_kaybedeni_sifir_verir_rol_argumani_cozulur()
        {
            var d = new Duzen();
            d.Uygula(d.Uretici.Kanit("Can", "konum!(Mehmet,han)", "duydu", "Ayse", 0.4m));
            d.Uygula(d.Uretici.Kanit("Can", "konum!(Mehmet,pazar)", "gordu", "Can", 0.9m));
            d.Uygula(d.Uretici.Kanit("Can", "yardim_etti(oyuncu,Ayse)", "gordu", "Can", 0.9m));

            Assert.True(Sayi(d, "inanc(konum!(Mehmet, han))", KriterOperatoru.Esit, 0m));
            Assert.True(Sayi(d, "inanc(konum!(Mehmet, pazar))", KriterOperatoru.Esit, 0.9m));
            Assert.True(Sayi(d, "inanc(yardim_etti(oyuncu, dinleyen))", KriterOperatoru.Esit, 0.9m));
        }

        [Fact]
        public void Kaynak_kimden_geldigini_sorar()
        {
            var d = new Duzen();
            d.Uygula(d.Uretici.Kanit("Can", "borclu(Mehmet,Ayse,500)", "duydu", "Ayse", 0.7m));

            Assert.True(Mantik(d, "kaynak(borclu(Mehmet, *, *), Ayse)"));
            Assert.True(Mantik(d, "kaynak(borclu(Mehmet, *, *), dinleyen)"));
            Assert.False(Mantik(d, "kaynak(borclu(Mehmet, *, *), Veli)"));
            Assert.False(Mantik(d, "kaynak(borclu(Ali, *, *), Ayse)"));
        }

        // 11. hafiza_var
        [Fact]
        public void Hafiza_var_olay_turu_fail_ve_pencereyi_gunlukten_okur()
        {
            var d = new Duzen();
            d.Gunluk.Ekle(new Olay(1, 100, 0, "yardim_etti", "oyuncu", "Can", "han", new List<KeyValuePair<string, string>>(), new long[0], "player"));
            d.Uygula(ZihinOlaylari.HafizaEklendi(2, 100, 1, "Can", 1, 0.9m, new long[] { 1 }, "algi"));

            Assert.True(Mantik(d, "hafiza_var(yardim_etti, oyuncu, 168)", tick: 200));
            Assert.True(Mantik(d, "hafiza_var(yardim_etti, oyuncu, 100)", tick: 200));
            Assert.False(Mantik(d, "hafiza_var(yardim_etti, oyuncu, 99)", tick: 200));
            Assert.False(Mantik(d, "hafiza_var(tehdit_etti, oyuncu, 168)", tick: 200));
            Assert.False(Mantik(d, "hafiza_var(yardim_etti, Ali, 168)", tick: 200));
        }

        // 12. yalan_soyledi, 15. yay: sonraki haftalar
        [Theory]
        [InlineData("yalan_soyledi(oyuncu)")]
        [InlineData("yay(kasaba_gerginligi)")]
        public void Sonraki_haftalarin_fonksiyonlari_net_hata_verir(string alan)
        {
            var d = new Duzen();
            var cozucu = new ZihinSorguCozucusu(OlayUretici.SabitDunya(), d.Zihinler, d.Gunluk, 0);
            var cagri = IfadeAyristirici.Ayristir(alan);

            var hata = Assert.Throws<NotSupportedException>(() => cozucu.Coz(cagri, new SorguBaglami("Can")));
            Assert.Contains("hafta", hata.Message);
        }

        // 13. ayni_mekanda, 14. gercek: dünyadan, aynı çözücü üzerinden
        [Fact]
        public void Dunya_fonksiyonlari_ayni_cozucuden_calisir()
        {
            var d = new Duzen();
            var baglam = new SorguBaglami("Ali", oyuncu: "Ayse");
            var cozucu = new ZihinSorguCozucusu(OlayUretici.SabitDunya(), d.Zihinler, d.Gunluk, 0);
            var kriter1 = Kriter.Olustur("ayni_mekanda(oyuncu)", KriterOperatoru.Esit, SorguDegeri.Mantiksal(true));
            var kriter2 = Kriter.Olustur("gercek(konum!(Ali, han))", KriterOperatoru.Esit, SorguDegeri.Mantiksal(true));

            Assert.True(KriterDegerlendirici.Degerlendir(kriter1, cozucu, baglam).Tuttu);
            Assert.True(KriterDegerlendirici.Degerlendir(kriter2, cozucu, baglam).Tuttu);
        }

        [Fact]
        public void Zihin_fonksiyonu_zihni_olmayan_ben_icin_hata_verir()
        {
            var d = new Duzen();

            Assert.Throws<InvalidOperationException>(() =>
            {
                Tutar(d, "stres()", KriterOperatoru.Esit, SorguDegeri.Sayi(0m), 0, "Yok");
            });
        }

        [Fact]
        public void Her_zihin_fonksiyonu_zihin_ya_da_dunya_cozucusunde_karsilik_bulur()
        {
            // 15 fonksiyonun hiçbiri "bilinmiyor" diye düşmemeli; yalnızca yalan_soyledi (6. hafta) ve yay (7. hafta) NotSupported verir.
            var d = new Duzen();
            var cozucu = new ZihinSorguCozucusu(OlayUretici.SabitDunya(), d.Zihinler, d.Gunluk, 0);
            var baglam = new SorguBaglami("Ali", oyuncu: "Ayse", konusan: "Ali", dinleyen: "Can", olayFail: "Ali", olayHedef: "Can");
            var desteklenmeyen = new List<string>();
            var ornekler = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["ayni_mekanda"] = "ayni_mekanda(oyuncu)",
                ["duygu"] = "duygu(ofke)",
                ["duygu_hedefi"] = "duygu_hedefi()",
                ["eksen"] = "eksen(oyuncu, guven)",
                ["fikir"] = "fikir(Can)",
                ["fikir_bandi"] = "fikir_bandi(Can)",
                ["gercek"] = "gercek(konum!(Ali, han))",
                ["hafiza_var"] = "hafiza_var(yardim_etti, oyuncu, 10)",
                ["inanc"] = "inanc(borclu(*, *, *))",
                ["kaynak"] = "kaynak(borclu(*, *, *), Ayse)",
                ["kisilik"] = "kisilik(cesaret)",
                ["modifier_var"] = "modifier_var(Can, minnet)",
                ["stres"] = "stres()",
                ["yalan_soyledi"] = "yalan_soyledi(oyuncu)",
                ["yay"] = "yay(x)",
            };
            d.Zihinler.Ekle(new Zihin("Ali", new Kisilik(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, 0.5m)));

            foreach (var tanim in SorguSozlugu.Hepsi)
            {
                try
                {
                    cozucu.Coz(IfadeAyristirici.Ayristir(ornekler[tanim.Ad]), baglam);
                }
                catch (NotSupportedException)
                {
                    desteklenmeyen.Add(tanim.Ad);
                }
            }

            Assert.Equal(new[] { "yalan_soyledi", "yay" }, desteklenmeyen);
        }
    }
}
