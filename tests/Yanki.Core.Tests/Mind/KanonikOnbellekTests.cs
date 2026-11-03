using System.Linq;
using Xunit;
using Yanki.Core.Events;
using Yanki.Core.Mind;

namespace Yanki.Core.Tests.Mind
{
    /// <summary>7. hafta (Z3): kanonik metin önbelleği. Her mutasyon yolundan sonra önbellekli metin, önbelleksiz tam hesapla birebir aynı olmalıdır.</summary>
    public class KanonikOnbellekTests
    {
        private const string Borc = "borclu(Mehmet,Ayse,500)";

        private static Zihin Yeni() =>
            new Zihin("Can", new Kisilik(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, 0.5m), new ZihinAyarlari { KanitSonumuTickBasina = 0.01m });

        private static void Uygula(Zihin z, Olay olay)
        {
            var once = z.KanonikMetin();                 // önbelleği doldur
            Assert.Equal(z.KanonikMetinTam(), once);
            z.Uygula(olay);
            Assert.Equal(z.KanonikMetinTam(), z.KanonikMetin());   // mutasyondan sonra bayat kalmamalı
        }

        [Fact]
        public void Her_mutasyon_yolu_onbellegi_gecersiz_kilar()
        {
            var z = Yeni();
            long id = 0;
            Olay Duydu(long tick, string kaynak, decimal guc, string kok) =>
                ZihinOlaylari.KanitEklendi(++id, tick, 0, "Can", Borc, "duydu", kaynak, guc, new long[0], "t", string.Empty, kok);

            Uygula(z, Duydu(1, "Ayse", 0.5m, "oyuncu"));                       // yeni önerme + kanıt
            Uygula(z, Duydu(2, "Hatice", 0.4m, "oyuncu"));                     // aynı kökten farklı kaynak (ekleme)
            Uygula(z, Duydu(12, "Ayse", 0.45m, "oyuncu"));                     // aynı (kök, kaynak) güncel güçle yenileme
            Uygula(z, Duydu(13, "Veli", 0.9m, "Ali"));                         // başka kök
            Uygula(z, ZihinOlaylari.KanitEklendi(++id, 14, 0, "Can", Borc, "gordu", "Can", 0.9m, new long[0], "algi"));
            Uygula(z, ZihinOlaylari.KanitEklendi(++id, 14, 0, "Can", "kardes(Mehmet,Can)", "duydu", "Ayse", 0.6m, new long[0], "t", string.Empty, "oyuncu"));
            Uygula(z, ZihinOlaylari.HafizaEklendi(++id, 15, 0, "Can", 5, 0.3m, new long[0], "t"));             // yeni hafıza
            Uygula(z, ZihinOlaylari.HafizaEklendi(++id, 16, 0, "Can", 3, 0.2m, new long[0], "t"));             // araya giren (daha küçük olay kimliği)
            Uygula(z, ZihinOlaylari.HafizaEklendi(++id, 17, 0, "Can", 5, 0.8m, new long[0], "t"));             // mevcut kaydın önemi artar
            Uygula(z, ZihinOlaylari.AktarimKaydedildi(++id, 18, 0, "Can", "Ali", Borc, false, new long[0], "t"));
            Uygula(z, ZihinOlaylari.ModifierEklendi(++id, 19, 0, "Can", "oyuncu", "minnet", 15m, 100, new long[0], "t"));
            Uygula(z, ZihinOlaylari.EksenDegisti(++id, 20, 0, "Can", "oyuncu", "guven", 10m, new long[0], "t"));
            Uygula(z, ZihinOlaylari.StresDegisti(++id, 21, 0, "Can", 12m, new long[0], "t"));
            Uygula(z, ZihinOlaylari.DuyguDegisti(++id, 22, 0, "Can", "ofke", 0.5m, "oyuncu", new long[0], "t"));

            Assert.NotEqual(string.Empty, z.KanonikMetin());
            Assert.Contains("B|" + Borc, z.KanonikMetin());
        }

        [Fact]
        public void Degismeyen_zihinde_onbellek_ayni_metni_verir_ve_ikinci_cagri_ayni_nesnedir()
        {
            var z = Yeni();
            z.Uygula(ZihinOlaylari.HafizaEklendi(1, 1, 0, "Can", 5, 0.3m, new long[0], "t"));

            var a = z.KanonikMetin();
            var b = z.KanonikMetin();

            Assert.Same(a, b);
            Assert.Equal(z.KanonikMetinTam(), a);
        }

        [Fact]
        public void Baska_NPCye_ait_olay_reddedilir_onbellek_bozulmaz()
        {
            var z = Yeni();
            z.Uygula(ZihinOlaylari.HafizaEklendi(1, 1, 0, "Can", 5, 0.3m, new long[0], "t"));
            var once = z.KanonikMetin();

            Assert.ThrowsAny<System.ArgumentException>(() => z.Uygula(ZihinOlaylari.HafizaEklendi(2, 2, 0, "Ali", 5, 0.3m, new long[0], "t")));

            Assert.Equal(z.KanonikMetinTam(), z.KanonikMetin());
            Assert.Equal(once, z.KanonikMetin());
        }
    }
}
