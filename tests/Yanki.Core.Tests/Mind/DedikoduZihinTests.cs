using System;
using System.Linq;
using Xunit;
using Yanki.Core.Events;
using Yanki.Core.Mind;

namespace Yanki.Core.Tests.Mind
{
    /// <summary>6. hafta: aktarım kaydı (yalan_soyledi), kanıt kökü ve yankı freni, en güçlü kanıt (ADR-012).</summary>
    public class DedikoduZihinTests
    {
        private const string Borc = "borclu(Mehmet,Ayse,500)";

        private static Zihin Yeni(string id = "Can") =>
            new Zihin(id, new Kisilik(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, 0.5m), new ZihinAyarlari { KanitSonumuTickBasina = 0m });

        private static Olay Aktarim(long id, long tick, string npc, string kime, string onerme, bool yalan) =>
            ZihinOlaylari.AktarimKaydedildi(id, tick, 0, npc, kime, onerme, yalan, new long[0], "eylem:anlat");

        private static Olay Duydu(long id, long tick, string npc, string onerme, string kaynak, decimal guc, string kok) =>
            ZihinOlaylari.KanitEklendi(id, tick, 0, npc, onerme, "duydu", kaynak, guc, new long[0], "test", string.Empty, kok);

        [Fact]
        public void Aktarim_kaydi_zihin_olayidir_ve_yalan_soyledi_okunur()
        {
            Assert.True(ZihinOlaylari.ZihinOlayiMi(ZihinOlaylari.AktarimKaydedildiTuru));
            var z = Yeni("Ayse");
            z.Uygula(Aktarim(1, 3, "Ayse", "Can", Borc, true));

            Assert.True(z.YalanSoyledi("Can"));
            Assert.False(z.YalanSoyledi("Veli"));
            Assert.True(z.AktarimVar(Borc, "Can"));
            Assert.False(z.AktarimVar(Borc, "Veli"));
            Assert.Single(z.Aktarimlar);
        }

        [Fact]
        public void Dogru_aktarim_yalan_soyledi_yapmaz_ama_aktarim_var_olur()
        {
            var z = Yeni("Ayse");
            z.Uygula(Aktarim(1, 3, "Ayse", "Can", Borc, false));

            Assert.False(z.YalanSoyledi("Can"));
            Assert.True(z.AktarimVar(Borc, "Can"));
        }

        [Fact]
        public void Aktarim_kaydi_baska_npcye_uygulanamaz()
        {
            var z = Yeni("Ayse");
            Assert.Throws<ArgumentException>(() => z.Uygula(Aktarim(1, 3, "Can", "Ayse", Borc, true)));
        }

        [Fact]
        public void Kayit_bosken_kanonik_metin_T_satiri_icermez_doluyken_icerir()
        {
            var z = Yeni("Ayse");
            Assert.DoesNotContain("T|", z.KanonikMetin());

            z.Uygula(Aktarim(1, 3, "Ayse", "Can", Borc, true));
            Assert.Contains("T|Can|" + Borc + "|1|3|1\n", z.KanonikMetin());
        }

        [Fact]
        public void Kok_yokken_B_satiri_eski_bicimdedir_varken_kok_eklenir()
        {
            var z = Yeni();
            z.Uygula(Duydu(1, 2, "Can", Borc, "Ayse", 0.7m, string.Empty));
            Assert.Contains("B|" + Borc + "|duydu|Ayse|2|0.7|1\n", z.KanonikMetin());

            var k = Yeni();
            k.Uygula(Duydu(1, 2, "Can", Borc, "Ayse", 0.7m, "oyuncu"));
            Assert.Contains("B|" + Borc + "|duydu|Ayse|2|0.7|1|oyuncu\n", k.KanonikMetin());
        }

        private static Zihin Sonumlu() =>
            new Zihin("Can", new Kisilik(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, 0.5m), new ZihinAyarlari { KanitSonumuTickBasina = 0.01m });

        [Fact]
        public void Ayni_kokten_daha_zayif_tekrar_kanitin_hicbir_alanini_degistirmez()
        {
            var z = Sonumlu();
            z.Uygula(Duydu(1, 2, "Can", Borc, "Ayse", 0.5m, "oyuncu"));
            var tek = Sonumlu();
            tek.Uygula(Duydu(1, 2, "Can", Borc, "Ayse", 0.5m, "oyuncu"));

            z.Uygula(Duydu(2, 12, "Can", Borc, "Veli", 0.3m, "oyuncu"));   // güncel güç 0.4 > 0.3

            var k = Assert.Single(z.Kanitlar(Borc));
            Assert.Equal("Ayse", k.Kaynak);
            Assert.Equal(2, k.Tick);
            Assert.Equal(0.5m, k.Guc);
            Assert.Equal(1, k.OlayId);
            Assert.Equal("oyuncu", k.Kok);
            Assert.Equal(tek.Guven(Borc, 50), z.Guven(Borc, 50));          // sönüm sıfırlanmadı (yankı freni delinmedi)
        }

        [Fact]
        public void Ayni_kokten_esit_guclu_tekrar_mevcudu_korur()
        {
            var z = Yeni();
            z.Uygula(Duydu(1, 2, "Can", Borc, "Ayse", 0.5m, "oyuncu"));
            z.Uygula(Duydu(2, 9, "Can", Borc, "Veli", 0.5m, "oyuncu"));

            var k = Assert.Single(z.Kanitlar(Borc));
            Assert.Equal((1L, 2L, "Ayse"), (k.OlayId, k.Tick, k.Kaynak));
        }

        [Fact]
        public void Ayni_kokten_guncel_guce_gore_daha_guclu_tekrar_butun_alanlari_yeniler()
        {
            var z = Sonumlu();
            z.Uygula(Duydu(1, 2, "Can", Borc, "Ayse", 0.5m, "oyuncu"));   // tick 12'de güncel 0.4

            z.Uygula(Duydu(2, 12, "Can", Borc, "Veli", 0.45m, "oyuncu"));  // 0.45 > 0.4

            var k = Assert.Single(z.Kanitlar(Borc));
            Assert.Equal("Veli", k.Kaynak);
            Assert.Equal(12, k.Tick);
            Assert.Equal(0.45m, k.Guc);
            Assert.Equal(2, k.OlayId);
            Assert.Equal("oyuncu", k.Kok);
        }

        [Fact]
        public void Guc_karsilastirmasi_ilk_gucle_degil_guncel_sonmus_gucledir()
        {
            var z = Sonumlu();
            z.Uygula(Duydu(1, 2, "Can", Borc, "Ayse", 0.5m, "oyuncu"));
            // ham güç 0.5 > 0.45 ama mevcut 20 tick sonra 0.3'e sönmüş: yeni kanıt (0.45) daha güçlüdür
            z.Uygula(Duydu(2, 22, "Can", Borc, "Veli", 0.45m, "oyuncu"));

            Assert.Equal(2, Assert.Single(z.Kanitlar(Borc)).OlayId);
        }

        [Fact]
        public void Why_zayif_tekrarda_guclu_kaynagin_olayina_iner()
        {
            var z = Sonumlu();
            z.Uygula(Duydu(1, 2, "Can", Borc, "Ayse", 0.5m, "oyuncu"));
            z.Uygula(Duydu(2, 5, "Can", Borc, "Veli", 0.2m, "oyuncu"));

            Assert.Equal(1, z.EnGucluKanit(Borc, 6)!.OlayId);
            Assert.Equal("Ayse", Assert.Single(z.KimdenDuydu(Borc)).Kaynak);
        }

        [Fact]
        public void Farkli_koklerden_kanitlar_birikir()
        {
            var z = Yeni();
            z.Uygula(Duydu(1, 2, "Can", Borc, "Ayse", 0.5m, "oyuncu"));
            z.Uygula(Duydu(2, 3, "Can", Borc, "Veli", 0.5m, "Ali"));

            Assert.Equal(2, z.Kanitlar(Borc).Count);
            Assert.Equal(0.75m, z.Guven(Borc, 3));
        }

        [Fact]
        public void Kok_zihin_sahibiyse_yankidir_ve_guveni_arttirmaz()
        {
            var z = Yeni("Ayse");
            z.Uygula(ZihinOlaylari.KanitEklendi(1, 1, 0, "Ayse", Borc, "gordu", "Ayse", 0.9m, new long[0], "algi"));
            var once = z.Guven(Borc, 4);

            z.Uygula(Duydu(2, 4, "Ayse", Borc, "Can", 0.4m, "Ayse"));

            Assert.Equal(once, z.Guven(Borc, 4));
            Assert.Single(z.Kanitlar(Borc));
        }

        [Fact]
        public void En_guclu_kanit_guncel_gucle_secilir_esitlikte_kucuk_olay_kimligi()
        {
            var z = Yeni();
            z.Uygula(Duydu(5, 2, "Can", Borc, "Ayse", 0.5m, "Ayse"));
            z.Uygula(Duydu(3, 2, "Can", Borc, "Veli", 0.5m, "Veli"));
            z.Uygula(Duydu(9, 2, "Can", Borc, "Ali", 0.4m, "Ali"));

            var en = z.EnGucluKanit(Borc, 2)!;
            Assert.Equal(3, en.OlayId);
            Assert.Null(z.EnGucluKanit("borclu(X,Y,1)", 2));
        }

        [Fact]
        public void Etkin_kok_gordu_icin_sahip_duydu_icin_kok_yoksa_kaynaktir()
        {
            var z = Yeni("Can");
            z.Uygula(ZihinOlaylari.KanitEklendi(1, 1, 0, "Can", Borc, "gordu", "Can", 0.9m, new long[0], "algi"));
            z.Uygula(Duydu(2, 1, "Can", "borclu(A,B,1)", "Ayse", 0.5m, string.Empty));
            z.Uygula(Duydu(3, 1, "Can", "borclu(C,D,1)", "Ayse", 0.5m, "Ali"));

            Assert.Equal("Can", z.EtkinKok(z.Kanitlar(Borc)[0]));
            Assert.Equal("Ayse", z.EtkinKok(z.Kanitlar("borclu(A,B,1)")[0]));
            Assert.Equal("Ali", z.EtkinKok(z.Kanitlar("borclu(C,D,1)")[0]));
        }

        [Fact]
        public void Hafiza_onemi_kayit_varsa_doner_yoksa_null()
        {
            var z = Yeni();
            z.Uygula(ZihinOlaylari.HafizaEklendi(7, 1, 0, "Can", 4, 0.8m, new long[0], "test"));

            Assert.Equal(0.8m, z.HafizaOnemi(4, 1));
            Assert.Null(z.HafizaOnemi(99, 1));
        }
    }
}
