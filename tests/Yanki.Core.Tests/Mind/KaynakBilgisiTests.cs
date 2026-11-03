using System.Linq;
using Xunit;
using Yanki.Core.Events;
using Yanki.Core.Mind;

namespace Yanki.Core.Tests.Mind
{
    /// <summary>
    /// 7. hafta (G, Seçenek 1): her (önerme, kök, kaynak) için ayrı <c>duydu</c> kanıtı tutulur, hiçbiri silinmez;
    /// güven hesabında her kökten yalnızca en güçlü güncel kanıt sayılır (yankı freni: güven şişmez, kaynak bilgisi silinmez).
    /// </summary>
    public class KaynakBilgisiTests
    {
        private const string Borc = "borclu(Mehmet,Ayse,500)";

        private static Zihin Yeni(string id = "Can", decimal sonum = 0m) =>
            new Zihin(id, new Kisilik(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, 0.5m), new ZihinAyarlari { KanitSonumuTickBasina = sonum });

        private static Olay Duydu(long id, long tick, string npc, string kaynak, decimal guc, string kok) =>
            ZihinOlaylari.KanitEklendi(id, tick, 0, npc, Borc, "duydu", kaynak, guc, new long[0], "test", string.Empty, kok);

        [Fact]
        public void Guven_ayni_kokten_iki_kaynakta_sismez_ama_iki_kanit_da_tutulur()
        {
            var z = Yeni();
            z.Uygula(Duydu(1, 2, "Can", "Ayse", 0.5m, "oyuncu"));
            z.Uygula(Duydu(2, 3, "Can", "Hatice", 0.5m, "oyuncu"));

            Assert.Equal(0.5m, z.Guven(Borc, 3));                       // noisy-or 0.75 olmamalı
            Assert.Equal(new[] { "Ayse", "Hatice" }, z.Kanitlar(Borc).Select(k => k.Kaynak).ToArray());
        }

        [Fact]
        public void Guven_kok_basina_en_guclu_guncel_kaniti_sayar_farkli_kokler_birikir()
        {
            var z = Yeni(sonum: 0.01m);
            z.Uygula(Duydu(1, 2, "Can", "Ayse", 0.5m, "oyuncu"));      // tick 12'de 0.4
            z.Uygula(Duydu(2, 12, "Can", "Hatice", 0.45m, "oyuncu"));  // en güçlü
            z.Uygula(Duydu(3, 12, "Can", "Veli", 0.3m, "Ali"));        // başka kök

            Assert.Equal(1m - (1m - 0.45m) * (1m - 0.3m), z.Guven(Borc, 12));
        }

        [Fact]
        public void Ayni_kok_ve_kaynak_guncel_gucle_tazelenir_daha_zayifi_kanitin_alanlarini_degistirmez()
        {
            var z = Yeni(sonum: 0.01m);
            z.Uygula(Duydu(1, 2, "Can", "Ayse", 0.5m, "oyuncu"));
            z.Uygula(Duydu(2, 5, "Can", "Ayse", 0.2m, "oyuncu"));       // güncel 0.47 > 0.2: değişmez
            var ayni = Assert.Single(z.Kanitlar(Borc));
            Assert.Equal((1L, 2L, 0.5m), (ayni.OlayId, ayni.Tick, ayni.Guc));

            z.Uygula(Duydu(3, 12, "Can", "Ayse", 0.45m, "oyuncu"));     // güncel 0.4 < 0.45: tüm alanlar yenilenir
            var yeni = Assert.Single(z.Kanitlar(Borc));
            Assert.Equal((3L, 12L, 0.45m, "Ayse", "oyuncu"), (yeni.OlayId, yeni.Tick, yeni.Guc, yeni.Kaynak, yeni.Kok));
        }

        [Fact]
        public void Farkli_kaynak_korunur_kimden_duydu_ve_kaynak_varligi_hepsini_gorur()
        {
            var z = Yeni(sonum: 0.01m);
            z.Uygula(Duydu(1, 2, "Can", "Ayse", 0.49m, "oyuncu"));
            z.Uygula(Duydu(2, 3, "Can", "Hatice", 0.4886m, "oyuncu"));

            Assert.Equal(new[] { "Ayse", "Hatice" }, z.KimdenDuydu(Borc).Select(k => k.Kaynak).ToArray());
            Assert.True(z.KaynakVar(Borc, "Ayse"));
            Assert.True(z.KaynakVar(Borc, "Hatice"));
            Assert.Equal("Hatice", z.EnGucluKanit(Borc, 3)!.Kaynak);   // aktarım gücü/dayanak en güçlü tek kanıttan
        }

        [Fact]
        public void Zayif_tekrar_gucluyu_tazelemez_ama_zayif_kaynak_da_silinmez()
        {
            var z = Yeni(sonum: 0.01m);
            z.Uygula(Duydu(1, 2, "Can", "Ayse", 0.5m, "oyuncu"));
            z.Uygula(Duydu(2, 5, "Can", "Veli", 0.2m, "oyuncu"));

            Assert.Equal(1, z.EnGucluKanit(Borc, 6)!.OlayId);
            Assert.Equal(2, z.KimdenDuydu(Borc).Count);
            Assert.Equal(z.Guven(Borc, 6), Yeni(sonum: 0.01m).Also(t => t.Uygula(Duydu(1, 2, "Can", "Ayse", 0.5m, "oyuncu"))).Guven(Borc, 6));
        }

        [Fact]
        public void A_B_C_A_dongusunde_A_nin_guveni_artmaz_ve_kanit_eklenmez()
        {
            var a = Yeni("Ayse");
            a.Uygula(ZihinOlaylari.KanitEklendi(1, 1, 0, "Ayse", Borc, "gordu", "Ayse", 0.9m, new long[0], "algi"));
            var once = a.Guven(Borc, 6);

            a.Uygula(Duydu(2, 4, "Ayse", "Can", 0.4m, "Ayse"));       // Ayşe→Berk→Can→Ayşe: kök Ayşe
            a.Uygula(Duydu(3, 5, "Ayse", "Veli", 0.6m, "Ayse"));

            Assert.Equal(once, a.Guven(Borc, 6));
            Assert.Single(a.Kanitlar(Borc));
        }

        [Fact]
        public void Gordu_kaniti_etkilenmez_duydu_kokunden_bagimsiz_birikir()
        {
            var z = Yeni("Can");
            z.Uygula(ZihinOlaylari.KanitEklendi(1, 1, 0, "Can", Borc, "gordu", "Can", 0.6m, new long[0], "algi"));
            z.Uygula(Duydu(2, 2, "Can", "Ayse", 0.5m, "oyuncu"));
            z.Uygula(Duydu(3, 3, "Can", "Hatice", 0.4m, "oyuncu"));

            Assert.Equal(1m - (1m - 0.6m) * (1m - 0.5m), z.Guven(Borc, 3));
            Assert.Equal(3, z.Kanitlar(Borc).Count);
            Assert.Equal("gordu", z.Kanitlar(Borc)[0].Tur.ToString().ToLowerInvariant());
        }

        [Fact]
        public void Kanonik_metin_her_kaynagin_B_satirini_yazar()
        {
            var z = Yeni();
            z.Uygula(Duydu(1, 2, "Can", "Ayse", 0.5m, "oyuncu"));
            z.Uygula(Duydu(2, 3, "Can", "Hatice", 0.4m, "oyuncu"));

            var metin = z.KanonikMetin();
            Assert.Contains("B|" + Borc + "|duydu|Ayse|2|0.5|1|oyuncu\n", metin);
            Assert.Contains("B|" + Borc + "|duydu|Hatice|3|0.4|2|oyuncu\n", metin);
        }
    }

    internal static class Uzanti
    {
        public static T Also<T>(this T x, System.Action<T> f)
        {
            f(x);
            return x;
        }
    }
}
