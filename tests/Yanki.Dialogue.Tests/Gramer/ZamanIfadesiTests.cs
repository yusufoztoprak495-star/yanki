using System;
using Xunit;
using Yanki.Dialogue.Gramer;

namespace Yanki.Dialogue.Tests.Gramer
{
    /// <summary>
    /// Zaman ifadesi (E kararı, DURAK 2 kararı 4): göreli; takvim yok, geçen süre değil OYUN GÜNÜ FARKI kullanılır: gün no = floor(tick / 24), 1 tick = 1 saat.
    /// 0 bugün, 1 dün, 2 evvelsi gün, 3–6 yazıyla "üç gün önce", 7–29 geçen hafta, 30+ uzun zaman önce. Eşikler ve metinler <see cref="DiyalogAyarlari"/>'ndadır.
    /// </summary>
    public class ZamanIfadesiTests
    {
        private static readonly DiyalogAyarlari Ayar = new DiyalogAyarlari();

        [Theory]
        // gün no = tick / 24: tick 23 hâlâ gün 0, tick 24 gün 1; gün sınırı bir tick farkla bile "dün" yapar
        [InlineData(0, 0, "bugün")]
        [InlineData(1, 1, "bugün")]
        [InlineData(1, 23, "bugün")]
        [InlineData(5, 20, "bugün")]
        [InlineData(23, 24, "dün")]
        [InlineData(1, 24, "dün")]
        [InlineData(1, 47, "dün")]
        [InlineData(24, 47, "bugün")]
        [InlineData(1, 48, "evvelsi gün")]
        [InlineData(24, 71, "dün")]
        [InlineData(24, 72, "evvelsi gün")]
        // "N gün önce": 3 .. 6 (yazıyla)
        [InlineData(1, 72, "üç gün önce")]
        [InlineData(1, 96, "dört gün önce")]
        [InlineData(1, 120, "beş gün önce")]
        [InlineData(1, 143, "beş gün önce")]
        [InlineData(1, 144, "altı gün önce")]
        // hafta eşiği (7) ve uzun süre eşiği (30)
        [InlineData(1, 167, "altı gün önce")]
        [InlineData(1, 168, "geçen hafta")]
        [InlineData(1, 24 * 30 - 1, "geçen hafta")]
        [InlineData(1, 24 * 30, "uzun zaman önce")]
        [InlineData(1, 24 * 400, "uzun zaman önce")]
        public void Gun_farkina_gore_goreli_ifade_uretir_her_sinirda(long olayTick, long simdikiTick, string beklenen)
        {
            Assert.Equal(beklenen, ZamanIfadesi.Uret(olayTick, simdikiTick, Ayar));
        }

        [Fact]
        public void Esikler_ayardan_gelir_kodda_sabit_degildir()
        {
            var ayar = new DiyalogAyarlari { GunUzunlugu = 12, HaftaEsigiGun = 4, UzunSureEsigiGun = 8, DunIfadesi = "önceki gün" };

            Assert.Equal("önceki gün", ZamanIfadesi.Uret(11, 12, ayar));      // 12 tick'lik günde sınır 11 → 12
            Assert.Equal("üç gün önce", ZamanIfadesi.Uret(0, 36, ayar));        // gün 0 → gün 3
            Assert.Equal("geçen hafta", ZamanIfadesi.Uret(0, 48, ayar));        // gün 4 = hafta eşiği
            Assert.Equal("uzun zaman önce", ZamanIfadesi.Uret(0, 96, ayar));    // gün 8 = uzun süre eşiği
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
