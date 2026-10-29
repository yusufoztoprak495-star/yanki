using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Events;

namespace Yanki.Core.Tests.Events
{
    public class OlayTests
    {
        [Fact]
        public void Null_fail_hedef_mekan_bosa_normalize_edilir()
        {
            var olay = new Olay(1, 0, 0, "tur", null!, null!, null!, EventTestYardimcilari.BosYuk, new long[0], "player");

            Assert.Equal("", olay.Fail);
            Assert.Equal("", olay.Hedef);
            Assert.Equal("", olay.Mekan);
        }

        [Theory]
        [InlineData("", "kural")]
        [InlineData(null, "kural")]
        [InlineData("tur", "")]
        [InlineData("tur", null)]
        public void Bos_tur_ya_da_KuralId_hata_verir(string? tur, string? kuralId)
        {
            Assert.Throws<ArgumentException>(() =>
                new Olay(1, 0, 0, tur!, "", "", "", EventTestYardimcilari.BosYuk, new long[0], kuralId!));
        }

        [Fact]
        public void Yuk_anahtara_gore_ordinal_siralanir()
        {
            var olay = EventTestYardimcilari.YapTur(1, 0, 0, "t",
                EventTestYardimcilari.Y("b", "2"), EventTestYardimcilari.Y("B", "1"), EventTestYardimcilari.Y("a", "3"));

            Assert.Equal(new[] { "B", "a", "b" }, olay.Yuk.Select(k => k.Key).ToArray());
        }

        [Fact]
        public void Tekrar_eden_yuk_anahtari_hata_verir()
        {
            Assert.Throws<ArgumentException>(() => EventTestYardimcilari.YapTur(1, 0, 0, "t",
                EventTestYardimcilari.Y("a", "1"), EventTestYardimcilari.Y("a", "2")));
        }

        [Fact]
        public void Yuk_ve_neden_kaynak_listeler_degisse_de_degismez()
        {
            var yuk = new List<KeyValuePair<string, string>> { EventTestYardimcilari.Y("a", "1") };
            var nedenler = new List<long> { 1 };
            var olay = new Olay(2, 0, 1, "t", "", "", "", yuk, nedenler, "k");

            yuk.Add(EventTestYardimcilari.Y("b", "2"));
            nedenler.Add(9);

            Assert.Single(olay.Yuk);
            Assert.Equal(new long[] { 1 }, olay.NedenOlaylar.ToArray());
        }

        [Fact]
        public void Yuk_ve_neden_disaridan_listeye_cevrilip_degistirilemez()
        {
            var olay = EventTestYardimcilari.Yap(2, 0, 0, 1);

            Assert.Throws<NotSupportedException>(() => ((IList<long>)olay.NedenOlaylar).Add(5));
            Assert.Throws<NotSupportedException>(() => ((IList<KeyValuePair<string, string>>)olay.Yuk).Add(EventTestYardimcilari.Y("a", "b")));
        }

        [Fact]
        public void NedenOlaylar_artan_siralanir_ve_tekrar_hata_verir()
        {
            var olay = EventTestYardimcilari.Yap(10, 0, 0, 5, 2, 9);
            Assert.Equal(new long[] { 2, 5, 9 }, olay.NedenOlaylar.ToArray());

            Assert.Throws<ArgumentException>(() => EventTestYardimcilari.Yap(10, 0, 0, 2, 2));
        }

        [Fact]
        public void Deger_anahtar_yoksa_InvalidOperationException()
        {
            var olay = EventTestYardimcilari.YapTur(1, 0, 0, "t", EventTestYardimcilari.Y("a", "1"));

            Assert.Equal("1", olay.Deger("a"));
            Assert.Throws<InvalidOperationException>(() => olay.Deger("yok"));
        }
    }
}
