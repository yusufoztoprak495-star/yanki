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

        [Fact]
        public void Ayni_kokten_ikinci_kanit_yeni_kanit_eklemez_mevcudu_tazeler()
        {
            var z = Yeni();
            z.Uygula(Duydu(1, 2, "Can", Borc, "Ayse", 0.5m, "oyuncu"));
            z.Uygula(Duydu(2, 5, "Can", Borc, "Veli", 0.3m, "oyuncu"));

            var kanitlar = z.Kanitlar(Borc);
            Assert.Single(kanitlar);
            Assert.Equal(5, kanitlar[0].Tick);
            Assert.Equal(0.5m, kanitlar[0].Guc);
            Assert.Equal(2, kanitlar[0].OlayId);
            Assert.Equal(0.5m, z.Guven(Borc, 5));
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
