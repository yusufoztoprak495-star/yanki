using System;
using System.Linq;
using Xunit;
using Yanki.Core.Mind;

namespace Yanki.Core.Tests.Mind
{
    public class IliskiTests
    {
        private static Zihin Yeni(ZihinAyarlari? ayar = null) =>
            new Zihin("Can", new Kisilik(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, 0.5m), ayar);

        [Fact]
        public void Eksen_degisimi_delta_olarak_uygulanir_ve_baslangic_sifirdir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();

            Assert.Equal(0m, z.Eksen("oyuncu", "guven"));
            z.Uygula(u.Eksen("Can", "oyuncu", "guven", 30m));
            z.Uygula(u.Eksen("Can", "oyuncu", "guven", -10m));

            Assert.Equal(20m, z.Eksen("oyuncu", "guven"));
            Assert.Equal(0m, z.Eksen("oyuncu", "sevgi"));
            Assert.Equal(0m, z.Eksen("Ayse", "guven"));
        }

        [Theory]
        [InlineData(150, 100)]
        [InlineData(-150, -100)]
        public void Eksen_sinir_disina_tasmaz_kirpilir(int delta, int beklenen)
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();

            z.Uygula(u.Eksen("Can", "oyuncu", "korku", delta));

            Assert.Equal(beklenen, z.Eksen("oyuncu", "korku"));
        }

        [Fact]
        public void Kirpma_sonrasi_ters_delta_kirpilmis_degerden_calisir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();

            z.Uygula(u.Eksen("Can", "oyuncu", "guven", 500m));
            z.Uygula(u.Eksen("Can", "oyuncu", "guven", -10m));

            Assert.Equal(90m, z.Eksen("oyuncu", "guven"));
        }

        [Fact]
        public void Bilinmeyen_eksen_reddedilir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();

            Assert.Throws<ArgumentException>(() => z.Uygula(u.Eksen("Can", "oyuncu", "nefret", 5m)));
        }

        [Fact]
        public void Fikir_eksenlerin_agirlikli_toplamidir()
        {
            // varsayılan ağırlıklar: güven 0.4, sevgi 0.4, korku -0.2
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Eksen("Can", "oyuncu", "guven", 50m));
            z.Uygula(u.Eksen("Can", "oyuncu", "sevgi", 25m));
            z.Uygula(u.Eksen("Can", "oyuncu", "korku", 10m));

            Assert.Equal(28m, z.Fikir("oyuncu", 0));
        }

        [Fact]
        public void Eksen_agirliklari_ayarlanabilir()
        {
            var z = Yeni(new ZihinAyarlari { EksenAgirligiGuven = 1m, EksenAgirligiSevgi = 0m, EksenAgirligiKorku = 0m });
            var u = new ZihinOlayUretici();
            z.Uygula(u.Eksen("Can", "oyuncu", "guven", 50m));
            z.Uygula(u.Eksen("Can", "oyuncu", "sevgi", 99m));

            Assert.Equal(50m, z.Fikir("oyuncu", 0));
        }

        [Fact]
        public void Modifier_fikre_eklenir_ve_suresi_dolunca_hesaba_girmez()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici { Tick = 10 };
            z.Uygula(u.Modifier("Can", "oyuncu", "minnet", 20m, sure: 5));

            Assert.Equal(0m, z.Fikir("oyuncu", 9));
            Assert.Equal(20m, z.Fikir("oyuncu", 10));
            Assert.Equal(20m, z.Fikir("oyuncu", 14));
            Assert.Equal(0m, z.Fikir("oyuncu", 15));
            Assert.False(z.ModifierVar("oyuncu", "minnet", 15));
            Assert.True(z.ModifierVar("oyuncu", "minnet", 14));
        }

        [Fact]
        public void Ayni_turden_kopyalar_0_9un_kuvvetiyle_azalarak_birikir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Modifier("Can", "oyuncu", "minnet", 20m, 100));
            z.Uygula(u.Modifier("Can", "oyuncu", "minnet", 20m, 100));
            z.Uygula(u.Modifier("Can", "oyuncu", "minnet", 20m, 100));

            // 20 + 20*0.9 + 20*0.81 = 54.2
            Assert.Equal(54.2m, z.Fikir("oyuncu", 0));
        }

        [Fact]
        public void Negatif_modifier_da_ayni_carpanla_birikir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Modifier("Can", "oyuncu", "hakaret_etti", -15m, 100));
            z.Uygula(u.Modifier("Can", "oyuncu", "hakaret_etti", -15m, 100));

            Assert.Equal(-28.5m, z.Fikir("oyuncu", 0));
        }

        [Fact]
        public void Birikme_siniri_asilinca_fazla_kopyalar_sayilmaz()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            for (var i = 0; i < 5; i++)
            {
                z.Uygula(u.Modifier("Can", "oyuncu", "minnet", 20m, 100));
            }

            Assert.Equal(54.2m, z.Fikir("oyuncu", 0));
        }

        [Fact]
        public void Birikme_siniri_ayarlanabilir()
        {
            var z = Yeni(new ZihinAyarlari { ModifierBirikmeSiniri = 2 });
            var u = new ZihinOlayUretici();
            for (var i = 0; i < 4; i++)
            {
                z.Uygula(u.Modifier("Can", "oyuncu", "minnet", 20m, 100));
            }

            Assert.Equal(38m, z.Fikir("oyuncu", 0));
        }

        [Fact]
        public void Sinirdan_fazla_kopya_eskisi_dolunca_devreye_girer()
        {
            var z = Yeni(new ZihinAyarlari { ModifierBirikmeSiniri = 1 });
            var u = new ZihinOlayUretici { Tick = 0 };
            z.Uygula(u.Modifier("Can", "oyuncu", "minnet", 20m, sure: 10));
            u.Tick = 5;
            z.Uygula(u.Modifier("Can", "oyuncu", "minnet", 20m, sure: 100));

            Assert.Equal(20m, z.Fikir("oyuncu", 6));
            Assert.Equal(20m, z.Fikir("oyuncu", 12));
        }

        [Fact]
        public void Farkli_turler_ve_farkli_hedefler_birbirini_etkilemez()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Modifier("Can", "oyuncu", "minnet", 20m, 100));
            z.Uygula(u.Modifier("Can", "oyuncu", "hakaret_etti", -15m, 100));
            z.Uygula(u.Modifier("Can", "Ayse", "minnet", 20m, 100));

            Assert.Equal(5m, z.Fikir("oyuncu", 0));
            Assert.Equal(20m, z.Fikir("Ayse", 0));
        }

        [Fact]
        public void Fikir_eksi_100_ile_100_arasinda_kirpilir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            for (var i = 0; i < 3; i++)
            {
                z.Uygula(u.Modifier("Can", "oyuncu", "buyuk" + i, 90m, 100));
            }

            Assert.Equal(100m, z.Fikir("oyuncu", 0));
        }

        [Theory]
        [InlineData(-100, "dusman")]
        [InlineData(-40.01, "dusman")]
        [InlineData(-40, "soguk")]
        [InlineData(-10.01, "soguk")]
        [InlineData(-10, "notr")]
        [InlineData(0, "notr")]
        [InlineData(10, "notr")]
        [InlineData(10.01, "yakin")]
        [InlineData(40, "yakin")]
        [InlineData(40.01, "dost")]
        [InlineData(100, "dost")]
        public void Bant_esikleri_plandaki_gibidir(double fikir, string bant)
        {
            Assert.Equal(bant, FikirBandi.Hesapla((decimal)fikir));
        }

        [Fact]
        public void Zihin_fikir_bandini_fikirden_verir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Modifier("Can", "oyuncu", "aileme_iftira", -22m, 100));

            Assert.Equal("soguk", z.FikirBandi("oyuncu", 0));
            Assert.Equal("notr", z.FikirBandi("Ayse", 0));
        }

        [Fact]
        public void Fikir_dokumu_her_katkiyi_neden_olayiyla_sirali_verir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Eksen("Can", "oyuncu", "guven", -50m)); // olay 1
            u.Tick = 3;
            z.Uygula(u.Modifier("Can", "oyuncu", "aileme_iftira", -22m, sure: 18)); // olay 2
            u.Tick = 4;
            z.Uygula(u.Modifier("Can", "oyuncu", "minnet", 20m, sure: 50)); // olay 3

            var dokum = z.FikirDokumu("oyuncu", 10);

            Assert.Equal("Can", dokum.Sahip);
            Assert.Equal("oyuncu", dokum.Hedef);
            Assert.Equal(
                new[] { "eksen:guven", "eksen:sevgi", "eksen:korku", "modifier:aileme_iftira", "modifier:minnet" },
                dokum.Katkilar.Select(k => k.Kaynak).ToArray());
            Assert.Equal(new[] { -20m, 0m, 0m, -22m, 20m }, dokum.Katkilar.Select(k => k.Deger).ToArray());
            Assert.Equal(new long[] { 1, 0, 0, 2, 3 }, dokum.Katkilar.Select(k => k.NedenOlayId).ToArray());
            Assert.Equal(-22m, dokum.Toplam);
            Assert.Equal(z.Fikir("oyuncu", 10), dokum.Toplam);
        }

        [Fact]
        public void Fikir_dokumu_modifierin_kalan_suresini_verir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici { Tick = 3 };
            z.Uygula(u.Modifier("Can", "oyuncu", "aileme_iftira", -22m, sure: 18));

            var dokum = z.FikirDokumu("oyuncu", 6);

            Assert.Equal(15, dokum.Katkilar.Single(x => x.Kaynak == "modifier:aileme_iftira").KalanTick);
            Assert.Equal(-1, dokum.Katkilar.First().KalanTick);
        }

        [Fact]
        public void Fikir_dokumu_suresi_dolan_modifieri_icermez_ve_birikme_carpanini_yansitir()
        {
            var z = Yeni();
            var u = new ZihinOlayUretici();
            z.Uygula(u.Modifier("Can", "oyuncu", "minnet", 20m, sure: 5));
            z.Uygula(u.Modifier("Can", "oyuncu", "minnet", 20m, sure: 100));

            var yeni = z.FikirDokumu("oyuncu", 0).Katkilar.Where(k => k.Kaynak == "modifier:minnet").Select(k => k.Deger).ToArray();
            var eski = z.FikirDokumu("oyuncu", 50).Katkilar.Where(k => k.Kaynak == "modifier:minnet").Select(k => k.Deger).ToArray();

            Assert.Equal(new[] { 20m, 18m }, yeni);
            Assert.Equal(new[] { 20m }, eski);
        }
    }
}
