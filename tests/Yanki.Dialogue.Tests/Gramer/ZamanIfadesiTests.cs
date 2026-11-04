using System;
using Xunit;
using Yanki.Dialogue.Gramer;

namespace Yanki.Dialogue.Tests.Gramer
{
    /// <summary>
    /// Zaman ifadesi (8. hafta, E kararı): göreli ifade; takvim yok, senaryo şeması değişmez, oyun içi günden hesaplanır (1 tick = 1 saat, gün = 24 tick, tick 1 = gün 0'ın ilk saati).
    /// Eşikler ve ifade metinleri <see cref="DiyalogAyarlari"/>'ndadır (yer tutucu, Yusuf'un alanı).
    /// </summary>
    public class ZamanIfadesiTests
    {
        private static readonly DiyalogAyarlari Ayar = new DiyalogAyarlari();

        [Theory]
        // aynı gün: gün sınırı (tick 24 → 25) bir tick farkla bile "dün" yapar
        [InlineData(1, 1, "bugün")]
        [InlineData(1, 24, "bugün")]
        [InlineData(13, 24, "bugün")]
        [InlineData(24, 25, "dün")]
        [InlineData(1, 25, "dün")]
        [InlineData(1, 48, "dün")]
        [InlineData(1, 49, "evvelsi gün")]
        [InlineData(25, 73, "evvelsi gün")]
        [InlineData(25, 72, "dün")]
        // "N gün önce": 3 .. HaftaEsigiGun-1
        [InlineData(1, 73, "üç gün önce")]
        [InlineData(1, 97, "dört gün önce")]
        [InlineData(1, 121, "beş gün önce")]
        [InlineData(1, 145, "altı gün önce")]
        // hafta eşiği (7) ve uzun süre eşiği (30)
        [InlineData(1, 169, "geçen hafta")]
        [InlineData(1, 24 * 29 + 1, "geçen hafta")]
        [InlineData(1, 24 * 30 + 1, "çok önce")]
        [InlineData(1, 24 * 400, "çok önce")]
        public void Gun_farkina_gore_goreli_ifade_uretir_her_sinirda(long olayTick, long simdikiTick, string beklenen)
        {
            Assert.Equal(beklenen, ZamanIfadesi.Uret(olayTick, simdikiTick, Ayar));
        }

        [Fact]
        public void Esikler_ayardan_gelir_kodda_sabit_degildir()
        {
            var ayar = new DiyalogAyarlari { GunUzunlugu = 12, HaftaEsigiGun = 4, UzunSureEsigiGun = 8, DunIfadesi = "önceki gün" };

            Assert.Equal("önceki gün", ZamanIfadesi.Uret(12, 13, ayar));      // 12 tick'lik günde sınır 12 → 13
            Assert.Equal("üç gün önce", ZamanIfadesi.Uret(1, 37, ayar));        // gün 0 → gün 3
            Assert.Equal("geçen hafta", ZamanIfadesi.Uret(1, 49, ayar));        // gün 4 = hafta eşiği
            Assert.Equal("çok önce", ZamanIfadesi.Uret(1, 97, ayar));           // gün 8 = uzun süre eşiği
        }

        [Fact]
        public void Gelecekteki_olay_acik_hata_verir()
        {
            Assert.Throws<ArgumentException>(() => ZamanIfadesi.Uret(10, 5, Ayar));
        }

        [Fact]
        public void Tutarsiz_ayar_dogrulayicida_reddedilir()
        {
            Assert.Throws<ArgumentException>(() => new DiyalogAyarlari { GunUzunlugu = 0 }.Dogrula());
            Assert.Throws<ArgumentException>(() => new DiyalogAyarlari { HaftaEsigiGun = 2 }.Dogrula());
            Assert.Throws<ArgumentException>(() => new DiyalogAyarlari { HaftaEsigiGun = 10, UzunSureEsigiGun = 10 }.Dogrula());
            new DiyalogAyarlari().Dogrula();
        }
    }
}
