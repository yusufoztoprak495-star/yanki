using System.Linq;
using Xunit;
using Yanki.Core.Mind;

namespace Yanki.Core.Tests.Mind
{
    public class HafizaTests
    {
        private static Zihin Yeni() => new Zihin("Can", new Kisilik(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, 0.5m), new ZihinAyarlari
        {
            HafizaSonumuTickBasina = 0.05m,
            HafizaKaliciEsigi = 0.7m,
            HafizaUnutmaEsigi = 0.1m,
        });

        [Fact]
        public void Kayit_olay_kimligi_ve_onem_tasir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici { Tick = 4 };
            z.Uygula(u.Hafiza("Can", 42, 0.5m));

            var k = z.Hafiza(4).Single();

            Assert.Equal(42, k.OlayId);
            Assert.Equal(0.5m, k.Onem);
            Assert.Equal(4, k.Tick);
        }

        [Fact]
        public void Kalici_olan_hep_sorgulanir_gecici_olan_silikleserek_sorgudan_cikar()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Hafiza("Can", 10, 0.9m)); // kalıcı
            z.Uygula(u.Hafiza("Can", 11, 0.5m)); // 0.5 → 0.1 eşiği: tick 8

            Assert.Equal(new long[] { 10, 11 }, z.Hafiza(7).Select(h => h.OlayId).ToArray());
            Assert.Equal(new long[] { 10 }, z.Hafiza(8).Select(h => h.OlayId).ToArray());
            Assert.Equal(new long[] { 10 }, z.Hafiza(100000).Select(h => h.OlayId).ToArray());
        }

        [Fact]
        public void Unutulan_kayit_zihinden_silinmez_yalniz_sorgudan_cikar()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Hafiza("Can", 11, 0.5m));

            Assert.Empty(z.Hafiza(500));
            Assert.Single(z.HafizaKayitlari);
        }

        [Fact]
        public void Hafiza_varligi_olay_turu_fail_ve_pencereye_gore_sorulur()
        {
            var gunluk = new Yanki.Core.Events.OlayGunlugu();
            gunluk.Ekle(new Yanki.Core.Events.Olay(1, 100, 0, "yardim_etti", "oyuncu", "Can", "han",
                new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>>(), new long[0], "player"));
            var z = Yeni();
            var u = new ZihinOlayUretici { Tick = 100 };
            z.Uygula(u.Hafiza("Can", 1, 0.9m));

            Assert.True(z.HafizaVar(gunluk, "yardim_etti", "oyuncu", 168, 200));
            Assert.True(z.HafizaVar(gunluk, "yardim_etti", "oyuncu", 100, 200));
            Assert.False(z.HafizaVar(gunluk, "yardim_etti", "oyuncu", 99, 200));
            Assert.False(z.HafizaVar(gunluk, "yardim_etti", "Ali", 168, 200));
            Assert.False(z.HafizaVar(gunluk, "tehdit_etti", "oyuncu", 168, 200));
        }

        [Fact]
        public void Unutulmus_hafiza_hafiza_varda_gorunmez()
        {
            var gunluk = new Yanki.Core.Events.OlayGunlugu();
            gunluk.Ekle(new Yanki.Core.Events.Olay(1, 0, 0, "yardim_etti", "oyuncu", "Can", "han",
                new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>>(), new long[0], "player"));
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Hafiza("Can", 1, 0.5m));

            Assert.True(z.HafizaVar(gunluk, "yardim_etti", "oyuncu", 168, 5));
            Assert.False(z.HafizaVar(gunluk, "yardim_etti", "oyuncu", 168, 50));
        }
    }
}
