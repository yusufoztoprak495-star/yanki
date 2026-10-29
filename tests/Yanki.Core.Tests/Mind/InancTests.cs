using System;
using System.Linq;
using Xunit;
using Yanki.Core.Mind;

namespace Yanki.Core.Tests.Mind
{
    public class InancTests
    {
        private static Zihin Yeni(ZihinAyarlari? ayar = null) =>
            new Zihin("Can", new Kisilik(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, 0.5m), ayar ?? new ZihinAyarlari { KanitSonumuTickBasina = 0m });

        private const string Borc = "borclu(Mehmet,Ayse,500)";

        [Fact]
        public void Tek_kanitin_guveni_kanitin_gucudur()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Kanit("Can", Borc, "duydu", "Ayse", 0.7m));

            Assert.Equal(0.7m, z.Guven(Borc, 0));
        }

        [Fact]
        public void Guven_1_eksi_carpim_formuluyle_birikir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Kanit("Can", Borc, "duydu", "Ayse", 0.5m));
            z.Uygula(u.Kanit("Can", Borc, "gordu", "Can", 0.5m));
            z.Uygula(u.Kanit("Can", Borc, "cikarim", "mantik", 0.2m));

            // 1 - 0.5 * 0.5 * 0.8 = 0.8
            Assert.Equal(0.8m, z.Guven(Borc, 0));
        }

        [Fact]
        public void Kanitsiz_onermenin_guveni_sifirdir()
        {
            Assert.Equal(0m, Yeni().Guven(Borc, 0));
        }

        [Fact]
        public void Kanit_zayiflar_ama_silinmez_guven_buna_gore_duser()
        {
            var z = Yeni(new ZihinAyarlari { KanitSonumuTickBasina = 0.1m });
            var u = new ZihinOlayUretici();
            z.Uygula(u.Kanit("Can", Borc, "duydu", "Ayse", 0.7m));

            Assert.Equal(0.7m, z.Guven(Borc, 0));
            Assert.Equal(0.4m, z.Guven(Borc, 3));
            Assert.Equal(0m, z.Guven(Borc, 100));
            Assert.Single(z.Kanitlar(Borc));
            Assert.Equal(0.7m, z.Kanitlar(Borc).Single().Guc);
        }

        [Theory]
        [InlineData("sahit", 0.5)]
        [InlineData("duydu", 1.5)]
        [InlineData("duydu", -0.1)]
        public void Gecersiz_kanit_reddedilir(string tur, double guc)
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();

            Assert.ThrowsAny<ArgumentException>(() => z.Uygula(u.Kanit("Can", Borc, tur, "Ayse", (decimal)guc)));
        }

        [Fact]
        public void Gecersiz_onerme_reddedilir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();

            Assert.Throws<ArgumentException>(() => z.Uygula(u.Kanit("Can", "borclu Mehmet", "duydu", "Ayse", 0.5m)));
        }

        [Fact]
        public void Tek_degerli_onermenin_farkli_degerleri_celisir_guclu_olan_inanilir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Kanit("Can", "konum!(Ali,han)", "gordu", "Can", 0.8m));
            z.Uygula(u.Kanit("Can", "konum!(Ali,pazar)", "duydu", "Ayse", 0.5m));

            Assert.True(z.Inanilan("konum!(Ali,han)", 0));
            Assert.False(z.Inanilan("konum!(Ali,pazar)", 0));
            Assert.Equal(0.8m, z.InancDerecesi("konum!(Ali,*)", 0));
            Assert.Equal(0m, z.InancDerecesi("konum!(Ali,pazar)", 0));
            Assert.Equal(0.5m, z.Guven("konum!(Ali,pazar)", 0)); // ham güven kayıtlı
        }

        [Fact]
        public void Birikim_celiskide_kazanani_degistirebilir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Kanit("Can", "konum!(Ali,han)", "duydu", "Ayse", 0.6m));
            z.Uygula(u.Kanit("Can", "konum!(Ali,pazar)", "duydu", "Veli", 0.4m));
            Assert.True(z.Inanilan("konum!(Ali,han)", 0));

            z.Uygula(u.Kanit("Can", "konum!(Ali,pazar)", "gordu", "Can", 0.5m)); // 1 - 0.6*0.5 = 0.7 > 0.6

            Assert.True(z.Inanilan("konum!(Ali,pazar)", 0));
            Assert.False(z.Inanilan("konum!(Ali,han)", 0));
        }

        [Fact]
        public void Onerme_ile_degili_celisir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Kanit("Can", Borc, "duydu", "Ayse", 0.4m));
            z.Uygula(u.Kanit("Can", "degil(" + Borc + ")", "duydu", "Mehmet", 0.9m));

            Assert.False(z.Inanilan(Borc, 0));
            Assert.True(z.Inanilan("degil(" + Borc + ")", 0));
        }

        [Fact]
        public void Celiskisiz_onermeler_birlikte_inanilir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Kanit("Can", "borclu(Mehmet,Ayse,500)", "duydu", "Ayse", 0.5m));
            z.Uygula(u.Kanit("Can", "borclu(Mehmet,Ayse,300)", "duydu", "Veli", 0.4m)); // ! yok: çelişmez
            z.Uygula(u.Kanit("Can", "degil(konum!(Ali,han))", "duydu", "Veli", 0.4m));
            z.Uygula(u.Kanit("Can", "konum!(Ali,pazar)", "duydu", "Veli", 0.4m)); // degil(p) ile farklı değer çelişmez

            Assert.True(z.Inanilan("borclu(Mehmet,Ayse,500)", 0));
            Assert.True(z.Inanilan("borclu(Mehmet,Ayse,300)", 0));
            Assert.True(z.Inanilan("degil(konum!(Ali,han))", 0));
            Assert.True(z.Inanilan("konum!(Ali,pazar)", 0));
        }

        [Fact]
        public void Farkli_anahtarli_tek_degerliler_celismez()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Kanit("Can", "konum!(Ali,han)", "duydu", "Ayse", 0.4m));
            z.Uygula(u.Kanit("Can", "konum!(Veli,pazar)", "duydu", "Ayse", 0.4m));

            Assert.True(z.Inanilan("konum!(Ali,han)", 0));
            Assert.True(z.Inanilan("konum!(Veli,pazar)", 0));
        }

        [Fact]
        public void Esit_guvende_en_yeni_kanitli_olan_inanilir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici { Tick = 1 };
            z.Uygula(u.Kanit("Can", "konum!(Ali,han)", "duydu", "Ayse", 0.5m));
            u.Tick = 9;
            z.Uygula(u.Kanit("Can", "konum!(Ali,pazar)", "duydu", "Veli", 0.5m));

            Assert.False(z.Inanilan("konum!(Ali,han)", 10));
            Assert.True(z.Inanilan("konum!(Ali,pazar)", 10));
        }

        [Fact]
        public void Tam_esitlikte_hicbiri_inanilmaz()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici { Tick = 4 };
            z.Uygula(u.Kanit("Can", "konum!(Ali,han)", "duydu", "Ayse", 0.5m));
            z.Uygula(u.Kanit("Can", "konum!(Ali,pazar)", "duydu", "Veli", 0.5m));

            Assert.False(z.Inanilan("konum!(Ali,han)", 10));
            Assert.False(z.Inanilan("konum!(Ali,pazar)", 10));
            Assert.Equal(0m, z.InancDerecesi("konum!(Ali,*)", 10));
        }

        [Fact]
        public void Guveni_sifir_olan_onerme_inanilmaz()
        {
            var z = Yeni(new ZihinAyarlari { KanitSonumuTickBasina = 1m });
            var u = new ZihinOlayUretici();
            z.Uygula(u.Kanit("Can", Borc, "duydu", "Ayse", 0.5m));

            Assert.False(z.Inanilan(Borc, 100));
        }

        [Fact]
        public void Joker_her_argumanda_calisir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Kanit("Can", "borclu(Mehmet,Ayse,500)", "duydu", "Ayse", 0.3m));
            z.Uygula(u.Kanit("Can", "borclu(Mehmet,Veli,200)", "duydu", "Ayse", 0.6m));
            z.Uygula(u.Kanit("Can", "borclu(Ali,Ayse,100)", "duydu", "Ayse", 0.9m));

            Assert.Equal(0.6m, z.InancDerecesi("borclu(Mehmet,*,*)", 0));
            Assert.Equal(0.9m, z.InancDerecesi("borclu(*,Ayse,*)", 0));
            Assert.Equal(0.3m, z.InancDerecesi("borclu(Mehmet,*,500)", 0));
            Assert.Equal(0.9m, z.InancDerecesi("borclu(*,*,*)", 0));
            Assert.Equal(0m, z.InancDerecesi("borclu(Veli,*,*)", 0));
            Assert.Equal(0m, z.InancDerecesi("borclu(Mehmet,*)", 0)); // argüman sayısı farklı
        }

        [Fact]
        public void Kimden_duydu_yalniz_duydu_kanitlarini_tick_ve_kaynak_sirasiyla_verir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici { Tick = 5 };
            z.Uygula(u.Kanit("Can", Borc, "duydu", "Veli", 0.4m));
            u.Tick = 2;
            z.Uygula(u.Kanit("Can", Borc, "duydu", "Ayse", 0.7m));
            z.Uygula(u.Kanit("Can", Borc, "gordu", "Can", 0.9m));
            z.Uygula(u.Kanit("Can", "borclu(Mehmet,Veli,200)", "duydu", "Ali", 0.5m));

            var sonuc = z.KimdenDuydu("borclu(Mehmet,Ayse,500)");

            Assert.Equal(new[] { "Ayse", "Veli" }, sonuc.Select(k => k.Kaynak).ToArray());
            Assert.Equal(new long[] { 2, 5 }, sonuc.Select(k => k.Tick).ToArray());
            Assert.All(sonuc, k => Assert.Equal(KanitTuru.Duydu, k.Tur));
        }

        [Fact]
        public void Kimden_duydu_jokerle_birden_cok_onermeyi_kapsar()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Kanit("Can", Borc, "duydu", "Ayse", 0.7m));
            z.Uygula(u.Kanit("Can", "borclu(Mehmet,Veli,200)", "duydu", "Ali", 0.5m));

            Assert.Equal(
                new[] { "Ayse", "Ali" },
                z.KimdenDuydu("borclu(Mehmet,*,*)").Select(k => k.Kaynak).ToArray());
            Assert.Empty(z.KimdenDuydu("hirsiz(*)"));
        }

        [Fact]
        public void Kanit_kaynagi_sorgulanir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Kanit("Can", Borc, "duydu", "Ayse", 0.7m));
            z.Uygula(u.Kanit("Can", Borc, "gordu", "Can", 0.7m));

            Assert.True(z.KaynakVar("borclu(Mehmet,*,*)", "Ayse"));
            Assert.True(z.KaynakVar(Borc, "Can"));
            Assert.False(z.KaynakVar(Borc, "Veli"));
        }

        [Fact]
        public void Deposu_kimden_duydu_npc_ile_sorulur()
        {
            var can = Yeni();
            var u = new ZihinOlayUretici();
            can.Uygula(u.Kanit("Can", Borc, "duydu", "Ayse", 0.7m));
            var depo = new ZihinDeposu();
            depo.Ekle(can);
            depo.Ekle(new Zihin("Ayse", new Kisilik(0m, 0m, 0m, 0m, 0m, 0m)));

            Assert.Single(depo.KimdenDuydu("Can", Borc));
            Assert.Empty(depo.KimdenDuydu("Ayse", Borc));
        }

        [Fact]
        public void Onerme_ayristirma_olumsuzlugu_ve_joker_desenini_tanir()
        {
            var o = Onerme.Ayristir("degil(konum!(Ali,han))");
            Assert.True(o.Olumsuz);
            Assert.Equal("konum!", o.Yuklem);
            Assert.Equal("degil(konum!(Ali,han))", o.ToString());

            var d = OnermeDeseni.Ayristir("borclu(Mehmet,*,*)");
            Assert.True(d.Uyar(Onerme.Ayristir("borclu(Mehmet,Ayse,500)")));
            Assert.False(d.Uyar(Onerme.Ayristir("degil(borclu(Mehmet,Ayse,500))")));
            Assert.False(d.Uyar(Onerme.Ayristir("borclu(Ali,Ayse,500)")));
        }

        [Fact]
        public void Sonen_kanitla_celiski_karari_degisir_beklenen_davranis_budur()
        {
            // han: tick 0'da 0.9; pazar: tick 50'de 0.8. Oran 0.01/tick: tick 50'de han 0.4'e iner, pazar kazanır.
            var z = Yeni(new ZihinAyarlari { KanitSonumuTickBasina = 0.01m });
            var u = new ZihinOlayUretici();
            z.Uygula(u.Kanit("Can", "konum!(Ali,han)", "duydu", "Ayse", 0.9m));
            u.Tick = 50;
            z.Uygula(u.Kanit("Can", "konum!(Ali,pazar)", "duydu", "Veli", 0.8m));

            Assert.True(z.Inanilan("konum!(Ali,han)", 0));
            Assert.False(z.Inanilan("konum!(Ali,pazar)", 0));
            Assert.False(z.Inanilan("konum!(Ali,han)", 50));
            Assert.True(z.Inanilan("konum!(Ali,pazar)", 50));
        }

        [Fact]
        public void Ayni_tur_ve_kaynaktan_tekrar_eden_kanit_mevcut_kaniti_tazeler()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici { Tick = 2 };
            z.Uygula(u.Kanit("Can", Borc, "duydu", "Ayse", 0.5m));
            u.Tick = 9;
            z.Uygula(u.Kanit("Can", Borc, "duydu", "Ayse", 0.7m));

            var k = z.Kanitlar(Borc).Single();
            Assert.Equal(9, k.Tick);
            Assert.Equal(0.7m, k.Guc);
            Assert.Equal(0.7m, z.Guven(Borc, 9)); // 1 - 0.5*0.3 = 0.85 olmaz
        }

        [Fact]
        public void Tazelemede_guc_ikisinin_buyugu_olur()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici { Tick = 2 };
            z.Uygula(u.Kanit("Can", Borc, "duydu", "Ayse", 0.8m));
            u.Tick = 9;
            z.Uygula(u.Kanit("Can", Borc, "duydu", "Ayse", 0.3m));

            var k = z.Kanitlar(Borc).Single();
            Assert.Equal(9, k.Tick);
            Assert.Equal(0.8m, k.Guc);
        }

        [Fact]
        public void Farkli_kaynak_ya_da_tur_yeni_kanit_olarak_birikir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Kanit("Can", Borc, "duydu", "Ayse", 0.5m));
            z.Uygula(u.Kanit("Can", Borc, "duydu", "Veli", 0.5m));
            z.Uygula(u.Kanit("Can", Borc, "gordu", "Ayse", 0.5m));

            Assert.Equal(3, z.Kanitlar(Borc).Count);
        }

        [Fact]
        public void Tazeleme_farkli_npc_ya_da_onerme_icin_ayri_tutulur()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Kanit("Can", Borc, "duydu", "Ayse", 0.5m));
            z.Uygula(u.Kanit("Can", "borclu(Mehmet,Veli,200)", "duydu", "Ayse", 0.5m));

            Assert.Single(z.Kanitlar(Borc));
            Assert.Single(z.Kanitlar("borclu(Mehmet,Veli,200)"));
        }

        [Fact]
        public void Olumsuz_onerme_deseni_olumsuz_inancla_eslesir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Kanit("Can", "degil(konum!(Ali,han))", "duydu", "Ayse", 0.6m));

            Assert.Equal(0.6m, z.InancDerecesi("degil(konum!(Ali,han))", 0));
            Assert.Equal(0.6m, z.InancDerecesi("degil(konum!(Ali,*))", 0));
            Assert.Equal(0m, z.InancDerecesi("konum!(Ali,*)", 0));
        }
    }
}
