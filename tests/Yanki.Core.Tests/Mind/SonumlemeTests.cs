using Xunit;
using Yanki.Core.Mind;

namespace Yanki.Core.Tests.Mind
{
    /// <summary>Bütün sönmeler saf fonksiyondur: (durum, simdikiTick) → değer. Oranlar <see cref="ZihinAyarlari"/>'ndan gelir.</summary>
    public class SonumlemeTests
    {
        private static readonly ZihinAyarlari Ayar = new ZihinAyarlari
        {
            DuyguSonumuTickBasina = 0.1m,
            StresSonumuTickBasina = 2m,
            KanitSonumuTickBasina = 0.01m,
            HafizaSonumuTickBasina = 0.05m,
            HafizaKaliciEsigi = 0.7m,
            HafizaUnutmaEsigi = 0.1m,
        };

        private static Sonumleme Yap() => new Sonumleme(Ayar);

        [Fact]
        public void Modifier_suresi_dolana_kadar_aktiftir_dolunca_hesaba_girmez()
        {
            var m = new FikirModifier("minnet", 20m, baslangicTick: 10, sure: 5, nedenOlayId: 1);
            var s = Yap();

            Assert.True(s.ModifierAktifMi(m, 10));
            Assert.True(s.ModifierAktifMi(m, 14));
            Assert.False(s.ModifierAktifMi(m, 15));
            Assert.False(s.ModifierAktifMi(m, 100));
        }

        [Fact]
        public void Modifier_baslamadan_once_aktif_degildir()
        {
            var m = new FikirModifier("minnet", 20m, 10, 5, 1);

            Assert.False(Yap().ModifierAktifMi(m, 9));
        }

        [Fact]
        public void Modifier_kalan_sure_hesaplanir()
        {
            var m = new FikirModifier("minnet", 20m, 10, 5, 1);

            Assert.Equal(3, Yap().ModifierKalan(m, 12));
            Assert.Equal(0, Yap().ModifierKalan(m, 20));
        }

        [Fact]
        public void Duygu_siddeti_dogrusal_soner_ve_sifirin_altina_inmez()
        {
            var d = new Duygu("ofke", 0.6m, "oyuncu", baslangicTick: 0, nedenOlayId: 1);
            var s = Yap();

            Assert.Equal(0.6m, s.DuyguSiddeti(d, 0));
            Assert.Equal(0.5m, s.DuyguSiddeti(d, 1));
            Assert.Equal(0.1m, s.DuyguSiddeti(d, 5));
            Assert.Equal(0m, s.DuyguSiddeti(d, 6));
            Assert.Equal(0m, s.DuyguSiddeti(d, 1000));
        }

        [Fact]
        public void Duygu_baslamadan_once_tam_siddettedir()
        {
            var d = new Duygu("ofke", 0.6m, "", 10, 1);

            Assert.Equal(0.6m, Yap().DuyguSiddeti(d, 3));
        }

        [Fact]
        public void Stres_soner_ve_0da_durur()
        {
            var st = new StresDurumu(50m, tick: 10);
            var s = Yap();

            Assert.Equal(50m, s.Stres(st, 10));
            Assert.Equal(44m, s.Stres(st, 13));
            Assert.Equal(0m, s.Stres(st, 35));
            Assert.Equal(0m, s.Stres(st, 10000));
        }

        [Fact]
        public void Kanit_gucu_zayiflar_ama_kanit_silinmez()
        {
            var k = new Kanit(KanitTuru.Duydu, "Ayse", tick: 0, guc: 0.5m, olayId: 1);
            var s = Yap();

            Assert.Equal(0.5m, s.KanitGucu(k, 0));
            Assert.Equal(0.3m, s.KanitGucu(k, 20));
            Assert.Equal(0m, s.KanitGucu(k, 500));
            Assert.Equal(0.5m, k.Guc);
        }

        [Fact]
        public void Kalici_hafiza_sonmez()
        {
            var h = new HafizaKaydi(olayId: 7, onem: 0.71m, tick: 0);
            var s = Yap();

            Assert.True(s.HafizaKaliciMi(h));
            Assert.Equal(0.71m, s.HafizaOnemi(h, 100000));
            Assert.True(s.HafizaAktifMi(h, 100000));
        }

        [Fact]
        public void Esikte_tam_0_7_olan_hafiza_kalici_degildir()
        {
            var h = new HafizaKaydi(7, 0.7m, 0);

            Assert.False(Yap().HafizaKaliciMi(h));
        }

        [Fact]
        public void Gecici_hafiza_silikleser_unutma_esigine_dusunce_aktif_olmaz()
        {
            var h = new HafizaKaydi(7, 0.5m, 0);
            var s = Yap();

            Assert.Equal(0.5m, s.HafizaOnemi(h, 0));
            Assert.Equal(0.25m, s.HafizaOnemi(h, 5));
            Assert.True(s.HafizaAktifMi(h, 7));
            Assert.False(s.HafizaAktifMi(h, 8));
            Assert.False(s.HafizaAktifMi(h, 9));
        }

        [Fact]
        public void Varsayilan_ayarlar_dokumante_edilen_degerlerdedir()
        {
            var v = new ZihinAyarlari();

            Assert.Equal(0.7m, v.HafizaKaliciEsigi);
            Assert.Equal(3, v.ModifierBirikmeSiniri);
            Assert.Equal(0.9m, v.ModifierKopyaCarpani);
            Assert.Equal(0.4m, v.EksenAgirligiGuven);
            Assert.Equal(0.4m, v.EksenAgirligiSevgi);
            Assert.Equal(-0.2m, v.EksenAgirligiKorku);
        }
    }
}
