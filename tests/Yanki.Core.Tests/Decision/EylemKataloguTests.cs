using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Decision;

namespace Yanki.Core.Tests.Decision
{
    /// <summary>Birden çok dosyadan gelen eylemlerin birleşik, sıralı kataloğu.</summary>
    public class EylemKataloguTests
    {
        private static EylemTanimi Eylem(string id, string olayTuru, EylemHedefTuru hedef = EylemHedefTuru.Yok) =>
            EylemYukleyici.Yukle(
                "{ \"eylemler\": [ { \"id\": \"" + id + "\", " + (hedef == EylemHedefTuru.KomsuMekan ? string.Empty : "\"olay_turu\": \"" + olayTuru + "\", ")
                + "\"hedef\": \"" + EylemHedefTurleri.Ad(hedef) + "\", \"taban\": 1"
                + (hedef == EylemHedefTuru.KomsuMekan ? string.Empty : ", \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.5 } ]") + " } ] }",
                "e.json").Eylemler.Single();

        [Fact]
        public void Eylemler_kimlige_gore_ordinal_siralanir()
        {
            var katalog = new EylemKatalogu(new[] { Eylem("z_eylem", "z_t"), Eylem("B_eylem", "b_t"), Eylem("a_eylem", "a_t") });

            Assert.Equal(new[] { "B_eylem", "a_eylem", "z_eylem" }, katalog.Eylemler.Select(e => e.Id).ToArray());
        }

        [Fact]
        public void Olay_turleri_tekrarsiz_ordinal_sirali_ve_hareketi_icermez()
        {
            var katalog = new EylemKatalogu(new[]
            {
                Eylem("a", "selam_verdi"), Eylem("b", "selam_verdi"), Eylem("c", "armagan_verdi"),
                Eylem("git", string.Empty, EylemHedefTuru.KomsuMekan),
            });

            Assert.Equal(new[] { "armagan_verdi", "selam_verdi" }, katalog.OlayTurleri.ToArray());
        }

        [Fact]
        public void Yinelenen_kimlik_reddedilir()
        {
            var ex = Assert.Throws<ArgumentException>(() => new EylemKatalogu(new[] { Eylem("a", "x_t"), Eylem("a", "y_t") }));

            Assert.Contains("\"a\"", ex.Message);
        }

        [Fact]
        public void Bos_katalog_gecerlidir()
        {
            var katalog = new EylemKatalogu(new EylemTanimi[0]);

            Assert.Empty(katalog.Eylemler);
            Assert.Empty(katalog.OlayTurleri);
        }

        [Fact]
        public void Bul_kimlikle_eylemi_verir()
        {
            var katalog = new EylemKatalogu(new[] { Eylem("a", "x_t") });

            Assert.NotNull(katalog.Bul("a"));
            Assert.Null(katalog.Bul("yok"));
        }
    }
}
