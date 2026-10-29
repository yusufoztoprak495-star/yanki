using System;
using System.Linq;
using Xunit;
using Yanki.Core.Events;
using static Yanki.Core.Tests.Events.EventTestYardimcilari;

namespace Yanki.Core.Tests.Events
{
    public class OlayGunluguTests
    {
        [Fact]
        public void Id_birden_baslamali_ve_ardisik_olmali()
        {
            var gunluk = new OlayGunlugu();

            var hata = Assert.Throws<InvalidOperationException>(() => gunluk.Ekle(Yap(2, 0, 0)));
            Assert.Contains("Id", hata.Message);

            gunluk.Ekle(Yap(1, 0, 0));
            Assert.Throws<InvalidOperationException>(() => gunluk.Ekle(Yap(3, 0, 1)));
            gunluk.Ekle(Yap(2, 0, 1));

            Assert.Equal(2, gunluk.Sayi);
            Assert.Equal(3, gunluk.SonrakiId);
        }

        [Fact]
        public void Tick_geriye_gidemez()
        {
            var gunluk = new OlayGunlugu();
            gunluk.Ekle(Yap(1, 5, 0));

            var hata = Assert.Throws<InvalidOperationException>(() => gunluk.Ekle(Yap(2, 4, 0)));
            Assert.Contains("Tick", hata.Message);
        }

        [Fact]
        public void Sira_yeni_tickte_sifir_ayni_tickte_bir_artar()
        {
            var gunluk = new OlayGunlugu();

            Assert.Throws<InvalidOperationException>(() => gunluk.Ekle(Yap(1, 0, 1)));
            gunluk.Ekle(Yap(1, 0, 0));
            Assert.Throws<InvalidOperationException>(() => gunluk.Ekle(Yap(2, 0, 0)));
            Assert.Throws<InvalidOperationException>(() => gunluk.Ekle(Yap(2, 0, 2)));
            gunluk.Ekle(Yap(2, 0, 1));
            Assert.Throws<InvalidOperationException>(() => gunluk.Ekle(Yap(3, 1, 1)));
            gunluk.Ekle(Yap(3, 1, 0));

            Assert.Equal(1, gunluk.SonrakiSira(1));
            Assert.Equal(0, gunluk.SonrakiSira(2));
            Assert.Equal(0, gunluk.SonrakiSira(7));
        }

        [Fact]
        public void Nedeni_gunlukte_olmayan_olay_reddedilir()
        {
            var gunluk = new OlayGunlugu();
            gunluk.Ekle(Yap(1, 0, 0));

            var hata = Assert.Throws<InvalidOperationException>(() => gunluk.Ekle(Yap(2, 0, 1, 99)));
            Assert.Contains("99", hata.Message);
        }

        [Fact]
        public void Neden_kendisinden_once_olmali()
        {
            var gunluk = new OlayGunlugu();
            gunluk.Ekle(Yap(1, 0, 0));

            // Kendisini ya da sonraki bir olayı neden göstermek günlükte "zaten var" koşulunu sağlamaz.
            Assert.Throws<InvalidOperationException>(() => gunluk.Ekle(Yap(2, 0, 1, 2)));
            Assert.Throws<InvalidOperationException>(() => gunluk.Ekle(Yap(2, 0, 1, 3)));
        }

        [Fact]
        public void Indeksleyici_ve_Var_calisir()
        {
            var gunluk = new OlayGunlugu();
            gunluk.Ekle(Yap(1, 0, 0));

            Assert.True(gunluk.Var(1));
            Assert.False(gunluk.Var(0));
            Assert.False(gunluk.Var(2));
            Assert.Equal(1, gunluk[1].Id);
            Assert.Throws<ArgumentOutOfRangeException>(() => gunluk[2]);
        }

        [Fact]
        public void Neden_zinciri_Id_azalan_sirada_ve_tekrarsiz_doner()
        {
            // Elmas: 4 <- {2,3}, 2 <- 1, 3 <- 1
            var gunluk = new OlayGunlugu();
            gunluk.Ekle(Yap(1, 0, 0));
            gunluk.Ekle(Yap(2, 0, 1, 1));
            gunluk.Ekle(Yap(3, 0, 2, 1));
            gunluk.Ekle(Yap(4, 1, 0, 2, 3));

            var zincir = gunluk.Neden(4).Select(o => o.Id).ToArray();

            Assert.Equal(new long[] { 3, 2, 1 }, zincir);
        }

        [Fact]
        public void Neden_nedensiz_olay_icin_bos_doner()
        {
            var gunluk = new OlayGunlugu();
            gunluk.Ekle(Yap(1, 0, 0));

            Assert.Empty(gunluk.Neden(1));
        }

        [Fact]
        public void Neden_bilinmeyen_id_hata_verir()
        {
            var gunluk = new OlayGunlugu();

            Assert.Throws<ArgumentOutOfRangeException>(() => gunluk.Neden(1));
        }

        [Fact]
        public void Olaylar_listesi_disaridan_degistirilemez()
        {
            var gunluk = new OlayGunlugu();
            gunluk.Ekle(Yap(1, 0, 0));

            Assert.Throws<NotSupportedException>(() =>
                ((System.Collections.Generic.IList<Olay>)gunluk.Olaylar).Add(Yap(2, 0, 1)));
        }
    }
}
