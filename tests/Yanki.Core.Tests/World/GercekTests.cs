using System;
using System.Linq;
using Xunit;
using Yanki.Core.World;

namespace Yanki.Core.Tests.World
{
    public class GercekTests
    {
        [Fact]
        public void Ayristir_ToString_gidis_donus()
        {
            foreach (var metin in new[] { "konum!(Ali,han)", "borclu(Mehmet,Ayse,500)", "yas(Ali,-3.5)", "ortak(a)" })
            {
                Assert.Equal(metin, Gercek.Ayristir(metin).ToString());
            }
        }

        [Fact]
        public void Ayristir_bosluklari_tolere_eder_ama_kanonik_yazar()
        {
            Assert.Equal("konum!(Ali,han)", Gercek.Ayristir(" konum! ( Ali , han ) ").ToString());
        }

        [Fact]
        public void TekDegerli_bayragi_ve_anahtar()
        {
            var g = Gercek.Ayristir("konum!(Ali,han)");

            Assert.True(g.TekDegerli);
            Assert.Equal("konum!", g.Yuklem);
            Assert.Equal(new[] { "Ali", "han" }, g.Argumanlar.ToArray());
            Assert.Equal("konum!(Ali", g.TekDegerAnahtari);
            Assert.False(Gercek.Ayristir("borclu(Mehmet,Ayse,500)").TekDegerli);
        }

        [Fact]
        public void Sayi_argumani_kanonik_biciminde_olmali()
        {
            Assert.Throws<ArgumentException>(() => new Gercek("borclu", new[] { "Mehmet", "Ayse", "500.0" }));
            Assert.Throws<ArgumentException>(() => new Gercek("borclu", new[] { "Mehmet", "Ayse", "05" }));
            Assert.Equal("borclu(Mehmet,Ayse,500)", new Gercek("borclu", new[] { "Mehmet", "Ayse", "500" }).ToString());
        }

        [Theory]
        [InlineData("")]
        [InlineData("konum")]
        [InlineData("konum()")]
        [InlineData("konum!(Ali,han")]
        [InlineData("konum!(Ali,,han)")]
        [InlineData("konum!(Ali Veli,han)")]
        [InlineData("konum!(Ali,*)")]
        [InlineData("konum!(Ali,ğ)")]
        [InlineData("1konum(Ali)")]
        [InlineData("konum!!(Ali)")]
        [InlineData("konum!(Ali,han)x")]
        public void Gecersiz_gercek_metni_hata_verir(string metin)
        {
            Assert.Throws<ArgumentException>(() => Gercek.Ayristir(metin));
        }

        [Fact]
        public void Joker_deseni_eslesir()
        {
            var g = Gercek.Ayristir("borclu(Mehmet,Ayse,500)");

            Assert.True(new GercekDeseni("borclu", new string?[] { "Mehmet", null, null }).Uyar(g));
            Assert.True(new GercekDeseni("borclu", new string?[] { null, null, null }).Uyar(g));
            Assert.True(new GercekDeseni("borclu", new string?[] { "Mehmet", "Ayse", "500" }).Uyar(g));
            Assert.False(new GercekDeseni("borclu", new string?[] { "Ali", null, null }).Uyar(g));
            Assert.False(new GercekDeseni("borclu", new string?[] { "Mehmet", null }).Uyar(g));   // argüman sayısı farklı
            Assert.False(new GercekDeseni("alacakli", new string?[] { null, null, null }).Uyar(g)); // yüklem farklı
        }
    }
}
