using System;
using System.Linq;
using Xunit;
using Yanki.Core.Json;

namespace Yanki.Core.Tests.Json
{
    public class JsonTests
    {
        [Fact]
        public void Nesne_dizi_ve_ilkel_degerleri_okur()
        {
            var j = JsonAyristirici.Ayristir("{\"a\": 1.50, \"b\": [true, false, null, \"x\"], \"c\": {\"d\": -2}}");

            Assert.Equal(JsonTuru.Nesne, j.Tur);
            Assert.Equal(1.50m, j.Al("a")!.SayiDeger);
            var b = j.Al("b")!;
            Assert.Equal(4, b.Elemanlar.Count);
            Assert.True(b.Elemanlar[0].MantiksalDeger);
            Assert.False(b.Elemanlar[1].MantiksalDeger);
            Assert.Equal(JsonTuru.Bos, b.Elemanlar[2].Tur);
            Assert.Equal("x", b.Elemanlar[3].MetinDeger);
            Assert.Equal(-2m, j.Al("c")!.Al("d")!.SayiDeger);
            Assert.Null(j.Al("yok"));
        }

        [Fact]
        public void Ozellikler_dosyadaki_sirayla_doner()
        {
            var j = JsonAyristirici.Ayristir("{\"z\":1,\"a\":2,\"m\":3}");
            Assert.Equal(new[] { "z", "a", "m" }, j.Ozellikler.Select(o => o.Key).ToArray());
        }

        [Fact]
        public void Sayilar_decimal_olarak_tam_okunur()
        {
            Assert.Equal(0.1m, JsonAyristirici.Ayristir("0.1").SayiDeger);
            Assert.Equal(1000m, JsonAyristirici.Ayristir("1e3").SayiDeger);
            Assert.Equal(-0.25m, JsonAyristirici.Ayristir("-0.25").SayiDeger);
        }

        [Fact]
        public void Metin_kacislarini_cozer()
        {
            var j = JsonAyristirici.Ayristir("\"a\\n\\t\\\"\\\\\\/\\u00e7\"");
            Assert.Equal("a\n\t\"\\/\u00e7", j.MetinDeger);
        }

        [Fact]
        public void Konum_bilgisi_satir_ve_sutun_verir()
        {
            var j = JsonAyristirici.Ayristir("{\n  \"a\": 1,\n  \"b\": 2\n}");
            Assert.Equal(1, j.Satir);
            Assert.Equal(2, j.Al("a")!.Satir);
            Assert.Equal(8, j.Al("a")!.Sutun);
            Assert.Equal(3, j.Al("b")!.Satir);
        }

        [Theory]
        [InlineData("")]
        [InlineData("{")]
        [InlineData("{\"a\":}")]
        [InlineData("{\"a\":1,}")]
        [InlineData("[1,]")]
        [InlineData("{a:1}")]
        [InlineData("01")]
        [InlineData("1.")]
        [InlineData("\"abc")]
        [InlineData("\"\\x\"")]
        [InlineData("tru")]
        [InlineData("1 2")]
        [InlineData("// yorum\n1")]
        public void Gecersiz_girdi_JsonHatasi_atar(string metin)
        {
            Assert.Throws<JsonHatasi>(() => JsonAyristirici.Ayristir(metin));
        }

        [Fact]
        public void Tekrar_eden_anahtar_hatadir_ve_satiri_soyler()
        {
            var hata = Assert.Throws<JsonHatasi>(() => JsonAyristirici.Ayristir("{\n\"a\":1,\n\"a\":2}"));
            Assert.Equal(3, hata.Satir);
            Assert.Contains("\"a\"", hata.Message);
        }

        [Fact]
        public void Hata_mesaji_satir_ve_sutun_icerir()
        {
            var hata = Assert.Throws<JsonHatasi>(() => JsonAyristirici.Ayristir("{\n  \"a\" 1\n}"));
            Assert.Equal(2, hata.Satir);
            Assert.Contains("satır 2", hata.Message);
        }

        [Fact]
        public void Cok_derin_ic_ice_gecme_reddedilir()
        {
            var metin = new string('[', 200) + new string(']', 200);
            Assert.Throws<JsonHatasi>(() => JsonAyristirici.Ayristir(metin));
        }

        [Fact]
        public void BOM_yoksayilir()
        {
            Assert.Equal(1m, JsonAyristirici.Ayristir("\uFEFF1").SayiDeger);
        }

        [Fact]
        public void Yazici_girintili_ve_sirayi_koruyarak_yazar_ve_gidis_donus_aynidir()
        {
            var kaynak = JsonDegeri.Nesne(
                ("seed", JsonDegeri.Sayi(42m)),
                ("ad", JsonDegeri.Metin("a\"b\u00e7\n")),
                ("dizi", JsonDegeri.Dizi(JsonDegeri.Sayi(1.5m), JsonDegeri.Mantiksal(true), JsonDegeri.Bos())),
                ("bos", JsonDegeri.Nesne()),
                ("bosdizi", JsonDegeri.Dizi()));

            var metin = JsonYazici.Yaz(kaynak);
            var geri = JsonAyristirici.Ayristir(metin);

            Assert.Equal(metin, JsonYazici.Yaz(geri));
            Assert.Equal("a\"b\u00e7\n", geri.Al("ad")!.MetinDeger);
            Assert.StartsWith("{\n  \"seed\": 42,\n  \"ad\":", metin);
            Assert.EndsWith("}\n", metin);
        }

        [Fact]
        public void Yazici_sayilarin_sondaki_sifirlarini_kirpar()
        {
            Assert.Equal("1.5\n", JsonYazici.Yaz(JsonDegeri.Sayi(1.50m)));
            Assert.Equal("0\n", JsonYazici.Yaz(JsonDegeri.Sayi(0.000m)));
        }
    }
}
