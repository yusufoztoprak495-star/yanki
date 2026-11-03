using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Decision;
using Yanki.Core.Events;
using Yanki.Core.Mind;

namespace Yanki.Core.Tests.Decision
{
    /// <summary>Konu seçimi ve aktarım gücü (ADR-012). Tek sınıf: karar ve aktarım aynı sonucu kullanır.</summary>
    public class KonuHesabiTests
    {
        private const string Borc = "borclu(Mehmet,Ayse,500)";

        /// <summary>Küçük bir dünya: olaylar günlüğe ve zihinlere sırayla eklenir.</summary>
        private sealed class Duzen
        {
            public readonly ZihinDeposu Zihinler = new ZihinDeposu();
            public readonly OlayGunlugu Gunluk = new OlayGunlugu();

            public Duzen(decimal ayseSosyallik = 0.5m)
            {
                var ayar = new ZihinAyarlari { KanitSonumuTickBasina = 0m, HafizaSonumuTickBasina = 0m };
                Zihinler.Ekle(new Zihin("Ayse", new Kisilik(0.5m, 0.5m, 0.5m, ayseSosyallik, 0.5m, 0.5m), ayar));
                foreach (var d in new[] { "Can", "Veli" })
                {
                    Zihinler.Ekle(new Zihin(d, new Kisilik(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, 0.5m), ayar));
                }
            }

            public Olay Ekle(Func<long, int, Olay> yap, long tick)
            {
                var olay = yap(Gunluk.SonrakiId, Gunluk.SonrakiSira(tick));
                Gunluk.Ekle(olay);
                Zihinler.Uygula(olay);
                return olay;
            }

            /// <summary>Ayşe bir önermeyi duyar: anlatti olayı, hafıza kaydı ve duydu kanıtı.</summary>
            public void AyseDuyar(string onerme, long tick, decimal guc = 0.7m, decimal onem = 0.5m, string kaynak = "oyuncu", string kok = "oyuncu")
            {
                var anlatti = Ekle((id, sira) => new Olay(id, tick, sira, "anlatti", kaynak, "Ayse", "han",
                    new[] { new KeyValuePair<string, string>("onerme", onerme) }, new long[0], "player"), tick);
                Ekle((id, sira) => ZihinOlaylari.HafizaEklendi(id, tick, sira, "Ayse", anlatti.Id, onem, new[] { anlatti.Id }, "algi"), tick);
                Ekle((id, sira) => ZihinOlaylari.KanitEklendi(id, tick, sira, "Ayse", onerme, "duydu", kaynak, guc, new[] { anlatti.Id }, "player", "han", kok), tick);
            }

            /// <summary>Ayşe'nin kendi gördüğü (gordu) bir önerme, hafızalı.</summary>
            public void AyseGorur(string onerme, long tick, decimal guc = 0.9m)
            {
                var olay = Ekle((id, sira) => new Olay(id, tick, sira, "gozlem", "Ali", "Ayse", "han", new KeyValuePair<string, string>[0], new long[0], "player"), tick);
                Ekle((id, sira) => ZihinOlaylari.HafizaEklendi(id, tick, sira, "Ayse", olay.Id, 0.5m, new[] { olay.Id }, "algi"), tick);
                Ekle((id, sira) => ZihinOlaylari.KanitEklendi(id, tick, sira, "Ayse", onerme, "gordu", "Ayse", guc, new[] { olay.Id }, "algi", "han"), tick);
            }

            public KonuHesabi Hesap(long tick) => new KonuHesabi(Zihinler, Gunluk, tick);
        }

        private static OnermeDeseni[] Desen(params string[] d) => d.Select(OnermeDeseni.Ayristir).ToArray();

        [Fact]
        public void Puan_onem_yenilik_ilgi_carpimidir()
        {
            var d = new Duzen();
            d.AyseDuyar(Borc, 10);
            d.AyseGorur("kardes(Mehmet,Can)", 10);

            // Can önermede geçmiyor ama kardes(Mehmet,Can) onu Mehmet üzerinden bağlıyor: ilgi 0.7. Tick 34: yenilik 1 − 24/168.
            var secim = d.Hesap(34).Dogru("Ayse", "Can", Desen("borclu(*,*,*)"));

            var konu = Assert.Single(secim.Konular);
            Assert.Equal(Borc, konu.Onerme);
            Assert.Equal(0.5m, konu.Onem);
            Assert.Equal(0.7m, konu.Ilgi);
            Assert.Equal(0.857143m, Math.Round(konu.Yenilik, 6));
            Assert.Equal(0.3m, konu.Puan);
            Assert.Equal("oyuncu", konu.Kok);
        }

        [Theory]
        [InlineData("Can", "borclu(Mehmet,Can,500)", 1.0)]    // dinleyen önermede geçiyor
        [InlineData("Can", "borclu(Mehmet,Ayse,500)", 0.7)]   // kardes(Mehmet,Can) ile bağlı
        [InlineData("Veli", "borclu(Mehmet,Ayse,500)", 0.5)]  // yalnızca konuşan geçiyor
        [InlineData("Veli", "borclu(Mehmet,Ali,500)", 0.25)]  // hiçbiri
        public void Ilgi_dort_kademe(string dinleyen, string onerme, double beklenen)
        {
            var d = new Duzen();
            d.AyseDuyar(onerme, 10);
            d.AyseGorur("kardes(Mehmet,Can)", 10);

            var secim = d.Hesap(10).Dogru("Ayse", dinleyen, Desen("borclu(*,*,*)"));

            // en düşük puan 0.5 × 0.25 = 0.125 ≥ 0.1 olduğundan her biri listede
            Assert.Equal((decimal)beklenen, Assert.Single(secim.Konular).Ilgi);
        }

        [Fact]
        public void Baglanti_sayi_argumanlarindan_kurulmaz()
        {
            var d = new Duzen();
            d.AyseDuyar("borclu(Mehmet,Ali,500)", 10);
            d.AyseGorur("maas(Can,500)", 10);

            var konu = Assert.Single(d.Hesap(10).Dogru("Ayse", "Can", Desen("borclu(*,*,*)")).Konular);

            Assert.Equal(0.25m, konu.Ilgi);   // 500 ortak ama bir varlık değil
        }

        [Fact]
        public void Yenilik_pencere_sonunda_sifirlanir_ve_konu_esik_altinda_elenir()
        {
            var d = new Duzen();
            d.AyseDuyar(Borc, 10);

            var secim = d.Hesap(10 + 168).Dogru("Ayse", "Can", Desen("borclu(*,*,*)"));

            Assert.Empty(secim.Konular);
            var elenen = Assert.Single(secim.Elenenler);
            Assert.Equal(Borc, elenen.Onerme);
            Assert.Contains("konu puani", elenen.Neden);
        }

        [Fact]
        public void Hafiza_kaydi_yoksa_onem_varsayilan_0_5()
        {
            var d = new Duzen();
            d.Ekle((id, sira) => ZihinOlaylari.KanitEklendi(id, 10, sira, "Ayse", Borc, "duydu", "oyuncu", 0.7m, new long[0], "t", string.Empty, "oyuncu"), 10);

            var konu = Assert.Single(d.Hesap(10).Dogru("Ayse", "Can", Desen("borclu(*,*,*)")).Konular);

            Assert.Equal(0.5m, konu.Onem);
        }

        [Fact]
        public void Yazarin_hafiza_etkisiyle_yukselen_onem_puani_arttirir()
        {
            var d = new Duzen();
            d.AyseDuyar(Borc, 10, onem: 0.9m);

            var konu = Assert.Single(d.Hesap(10).Dogru("Ayse", "Can", Desen("borclu(*,*,*)")).Konular);

            Assert.Equal(0.9m, konu.Onem);
        }

        [Theory]
        [InlineData(0.0, 1)]
        [InlineData(0.49, 1)]
        [InlineData(0.5, 2)]
        [InlineData(0.99, 2)]
        [InlineData(1.0, 3)]
        public void Konu_siniri_sosyallige_baglidir(double sosyallik, int sinir)
        {
            var d = new Duzen((decimal)sosyallik);
            Assert.Equal(sinir, d.Hesap(10).KonuSiniri("Ayse", "Can"));
        }

        [Fact]
        public void Yakin_ya_da_dost_iliski_siniri_bir_arttirir()
        {
            var d = new Duzen(0.5m);
            Assert.Equal(2, d.Hesap(10).KonuSiniri("Ayse", "Can"));

            d.Ekle((id, sira) => ZihinOlaylari.EksenDegisti(id, 10, sira, "Ayse", "Can", "guven", 50m, new long[0], "t"), 10);   // fikir 20: yakın
            Assert.Equal(3, d.Hesap(10).KonuSiniri("Ayse", "Can"));

            d.Ekle((id, sira) => ZihinOlaylari.EksenDegisti(id, 10, sira, "Ayse", "Veli", "guven", -100m, new long[0], "t"), 10);  // düşman
            Assert.Equal(2, d.Hesap(10).KonuSiniri("Ayse", "Veli"));
        }

        [Fact]
        public void Konular_puana_gore_azalan_esitlikte_onerme_metnine_gore_secilir()
        {
            var d = new Duzen(1.0m);   // sınır 3
            d.AyseDuyar("borclu(Mehmet,Can,1)", 10);    // ilgi 1.0
            d.AyseDuyar("borclu(Zeki,Ali,2)", 10);      // ilgi 0.25
            d.AyseDuyar("borclu(Ali,Zeki,3)", 10);      // ilgi 0.25 (eşitlik)
            d.AyseDuyar("borclu(Yasar,Can,4)", 10);     // ilgi 1.0 (eşitlik)

            var secim = d.Hesap(10).Dogru("Ayse", "Can", Desen("borclu(*,*,*)"));

            Assert.Equal(3, secim.Sinir);
            Assert.Equal(
                new[] { "borclu(Mehmet,Can,1)", "borclu(Yasar,Can,4)", "borclu(Ali,Zeki,3)" },
                secim.Konular.Select(k => k.Onerme).ToArray());
        }

        [Fact]
        public void Inanilmayan_ya_da_guveni_dusuk_konu_anlatilmaz()
        {
            var d = new Duzen();
            d.AyseDuyar("borclu(A,Can,1)", 10, guc: 0.29m);   // güven < 0.3
            d.AyseDuyar("borclu(B,Can,1)", 10, guc: 0.3m);    // tam eşik: anlatılır
            d.AyseDuyar("borclu(C,Can,1)", 10, guc: 0.6m);
            d.AyseDuyar("degil(borclu(C,Can,1))", 11, guc: 0.8m);   // C'nin tersi daha güçlü: borclu(C...) inanılmıyor

            var secim = d.Hesap(11).Dogru("Ayse", "Can", Desen("borclu(*,*,*)", "degil(borclu(*,*,*))"));

            Assert.Equal(
                new[] { "borclu(B,Can,1)", "degil(borclu(C,Can,1))" },
                secim.Konular.Select(k => k.Onerme).OrderBy(x => x, StringComparer.Ordinal).ToArray());
            Assert.Equal(2, secim.Elenenler.Count);
        }

        [Fact]
        public void Eyleme_ozel_desene_uymayan_konu_havuza_girmez()
        {
            var d = new Duzen();
            d.AyseDuyar("sevdi(Can,Mehmet)", 10);

            Assert.Empty(d.Hesap(10).Dogru("Ayse", "Can", Desen("borclu(*,*,*)")).Konular);
        }

        [Fact]
        public void Anlatti_onermeleri_desene_uysa_bile_konu_olamaz()
        {
            var d = new Duzen();
            d.AyseGorur("anlatti(Ali,Ayse)", 10);

            Assert.Empty(d.Hesap(10).Dogru("Ayse", "Can", Desen("anlatti(*,*)")).Konular);
        }

        [Fact]
        public void Ayni_konu_ayni_kisiye_iki_kez_anlatilmaz()
        {
            var d = new Duzen();
            d.AyseDuyar(Borc, 10);
            d.Ekle((id, sira) => ZihinOlaylari.AktarimKaydedildi(id, 11, sira, "Ayse", "Can", Borc, false, new long[0], "eylem:anlat"), 11);

            var hesap = d.Hesap(12);
            Assert.Empty(hesap.Dogru("Ayse", "Can", Desen("borclu(*,*,*)")).Konular);
            Assert.Single(hesap.Dogru("Ayse", "Veli", Desen("borclu(*,*,*)")).Konular);
        }

        [Fact]
        public void Konu_onu_duydugu_kisiye_ya_da_kokune_geri_anlatilmaz()
        {
            var d = new Duzen();
            d.AyseDuyar(Borc, 10, kaynak: "Veli", kok: "Can");

            var hesap = d.Hesap(10);
            Assert.Empty(hesap.Dogru("Ayse", "Veli", Desen("borclu(*,*,*)")).Konular);   // kanıt Veli'den geldi
            Assert.Empty(hesap.Dogru("Ayse", "Can", Desen("borclu(*,*,*)")).Konular);    // kök Can
            Assert.Single(hesap.Dogru("Ayse", "oyuncu", Desen("borclu(*,*,*)")).Konular);
        }

        [Fact]
        public void Kok_ve_dayanak_en_guclu_tek_kanittan_gelir_esitlikte_kucuk_olay_kimligi()
        {
            var d = new Duzen();
            d.AyseDuyar(Borc, 10, guc: 0.5m, kaynak: "Veli", kok: "Veli");
            d.AyseDuyar(Borc, 10, guc: 0.5m, kaynak: "Ali", kok: "Ali");
            d.AyseDuyar(Borc, 10, guc: 0.4m, kaynak: "Zeki", kok: "Zeki");

            var konu = Assert.Single(d.Hesap(10).Dogru("Ayse", "Can", Desen("borclu(*,*,*)")).Konular);

            Assert.Equal("Veli", konu.Kok);    // 0.5 eşitliğinde küçük olay kimliği (Veli önce eklendi)
            Assert.Equal(0.5m, d.Zihinler["Ayse"].KanitGucu(konu.Kanit, 10));
        }

        [Theory]
        [InlineData(0, 0.56)]      // 0.8 × 0.7
        [InlineData(100, 0.76)]    // 0.8 × min(0.95, 1.0)
        [InlineData(-100, 0.32)]   // 0.8 × 0.4
        [InlineData(50, 0.68)]     // 0.8 × 0.85
        public void Aktarim_gucu_kanit_gucu_carpi_guven_carpani(int eksen, double beklenen)
        {
            var d = new Duzen();
            d.Ekle((id, sira) => ZihinOlaylari.EksenDegisti(id, 1, sira, "Can", "Ayse", "guven", eksen, new long[0], "t"), 1);

            var guc = d.Hesap(1).AktarimGucu(0.8m, "Ayse", "Can");

            Assert.Equal((decimal)beklenen, guc);
            Assert.True(guc <= 0.8m);
        }

        [Fact]
        public void Aktarim_gucu_kanit_gucunu_hicbir_durumda_gecemez()
        {
            var d = new Duzen();
            d.Ekle((id, sira) => ZihinOlaylari.EksenDegisti(id, 1, sira, "Can", "Ayse", "guven", 100m, new long[0], "t"), 1);
            foreach (var kanit in new[] { 0.01m, 0.15m, 0.5m, 0.99m, 1m })
            {
                Assert.True(d.Hesap(1).AktarimGucu(kanit, "Ayse", "Can") <= kanit);
            }
        }

        [Fact]
        public void Oyuncu_dinleyen_icin_guven_sifir_sayilir()
        {
            var d = new Duzen();
            Assert.Equal(0.56m, d.Hesap(1).AktarimGucu(0.8m, "Ayse", "oyuncu"));
        }

        [Fact]
        public void Guc_zincirde_her_adimda_azalir()
        {
            var d = new Duzen();
            var h = d.Hesap(1);
            var g1 = h.AktarimGucu(1m, "oyuncu", "Ayse");    // 0.7
            var g2 = h.AktarimGucu(g1, "Ayse", "Can");       // 0.49
            var g3 = h.AktarimGucu(g2, "Can", "Veli");       // 0.343

            Assert.Equal(0.7m, g1);
            Assert.Equal(0.49m, g2);
            Assert.True(g1 > g2 && g2 > g3);
        }

        // ---- yalan ----

        [Fact]
        public void Yalan_celisen_inanctan_secilir_gucu_ve_dayanagi_o_inancin_en_guclu_kanitindandir()
        {
            var d = new Duzen();
            d.AyseDuyar(Borc, 10, guc: 0.6m, kaynak: "Veli", kok: "Veli");
            d.AyseGorur(Borc, 11, guc: 0.9m);

            var secim = d.Hesap(12).Yalan("Ayse", "Can", new[] { "degil(" + Borc + ")" }, 42);

            var yalan = Assert.Single(secim.Konular);
            Assert.True(yalan.Yalan);
            Assert.Equal("degil(" + Borc + ")", yalan.Onerme);
            Assert.Equal("Ayse", yalan.Kok);   // yalanın kökü konuşandır
            Assert.Equal(0.9m, d.Zihinler["Ayse"].KanitGucu(yalan.Kanit, 12));
        }

        [Fact]
        public void Inanmadigi_seye_celisen_yalan_yoksa_konu_bos_ve_elenir()
        {
            var d = new Duzen();
            d.AyseDuyar(Borc, 10);

            var secim = d.Hesap(10).Yalan("Ayse", "Can", new[] { "borclu(Mehmet,Ali,1)" }, 42);

            Assert.Empty(secim.Konular);
            Assert.Single(secim.Elenenler);
        }

        [Fact]
        public void Yalan_dunya_gercegine_degil_konusanin_kendi_inancina_gore_tanimlanir()
        {
            var d = new Duzen();
            d.AyseDuyar("degil(" + Borc + ")", 10);   // Ayşe yanlış bir şeye inanıyor

            // Şablon Ayşe'nin inancıyla ÇELİŞEN borclu(...)'dur; dünya gerçeğine bakılmaz.
            var secim = d.Hesap(10).Yalan("Ayse", "Can", new[] { Borc }, 42);

            Assert.True(Assert.Single(secim.Konular).Yalan);
        }

        [Fact]
        public void Ayni_kisiye_ayni_yalan_iki_kez_secilmez()
        {
            var d = new Duzen();
            d.AyseDuyar(Borc, 10);
            d.Ekle((id, sira) => ZihinOlaylari.AktarimKaydedildi(id, 11, sira, "Ayse", "Can", "degil(" + Borc + ")", true, new long[0], "t"), 11);

            Assert.Empty(d.Hesap(12).Yalan("Ayse", "Can", new[] { "degil(" + Borc + ")" }, 42).Konular);
        }

        [Fact]
        public void Birden_cok_uygun_yalandan_secim_anahtarli_rng_ile_deterministiktir()
        {
            var d = new Duzen();
            d.AyseDuyar(Borc, 10);
            d.AyseDuyar("borclu(A,B,1)", 10);
            var sablonlar = new[] { "degil(" + Borc + ")", "degil(borclu(A,B,1))" };

            var ayni1 = d.Hesap(10).Yalan("Ayse", "Can", sablonlar, 42).Konular[0].Onerme;
            var ayni2 = d.Hesap(10).Yalan("Ayse", "Can", sablonlar, 42).Konular[0].Onerme;
            Assert.Equal(ayni1, ayni2);

            var gorulen = new HashSet<string>(StringComparer.Ordinal);
            for (ulong seed = 1; seed <= 40; seed++)
            {
                gorulen.Add(d.Hesap(10).Yalan("Ayse", "Can", sablonlar, seed).Konular[0].Onerme);
            }

            Assert.Equal(2, gorulen.Count);
        }

        [Fact]
        public void En_yuksek_konu_puani_secilebilen_konularin_maksimumudur()
        {
            var d = new Duzen();
            d.AyseDuyar("borclu(Mehmet,Can,1)", 10);
            d.AyseDuyar("borclu(Zeki,Ali,2)", 10);

            var hesap = d.Hesap(10);
            Assert.Equal(0.5m, hesap.EnYuksekKonuPuani("Ayse", "Can", Desen("borclu(*,*,*)")));
            Assert.Equal(0m, hesap.EnYuksekKonuPuani("Ayse", "Can", Desen("sevdi(*,*)")));
        }

        [Fact]
        public void Ayni_girdi_ayni_sonucu_verir_ve_ekleme_sirasi_sonucu_degistirmez()
        {
            var d1 = new Duzen(1.0m);
            d1.AyseDuyar("borclu(Mehmet,Can,1)", 10);
            d1.AyseDuyar("borclu(Yasar,Can,4)", 10);
            var d2 = new Duzen(1.0m);
            d2.AyseDuyar("borclu(Yasar,Can,4)", 10);
            d2.AyseDuyar("borclu(Mehmet,Can,1)", 10);

            var a = d1.Hesap(10).Dogru("Ayse", "Can", Desen("borclu(*,*,*)")).Konular.Select(k => k.Onerme + k.Puan);
            var b = d2.Hesap(10).Dogru("Ayse", "Can", Desen("borclu(*,*,*)")).Konular.Select(k => k.Onerme + k.Puan);

            Assert.Equal(a, b);
        }
    }
}
