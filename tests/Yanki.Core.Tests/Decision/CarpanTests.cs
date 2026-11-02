using System;
using System.Collections.Generic;
using Xunit;
using Yanki.Core.Decision;
using Yanki.Core.Mind;

namespace Yanki.Core.Tests.Decision
{
    /// <summary>
    /// Kişilik/fikir/ruh hali çarpan eşlemesi (ADR-011, Yusuf'un kararı): parçalı doğrusal, nötr değer 1.0; sınırlar aşılamaz.
    /// </summary>
    public class CarpanTests
    {
        private static Kisilik K(decimal acgozluluk = 0.5m, decimal durustluk = 0.5m, decimal cesaret = 0.5m, decimal sosyallik = 0.5m, decimal kinTutma = 0.5m, decimal merhamet = 0.5m) =>
            new Kisilik(acgozluluk, durustluk, cesaret, sosyallik, kinTutma, merhamet);

        private static KisilikCarpaniTanimi Tek(string ozellik, bool ters = false) =>
            new KisilikCarpaniTanimi(new[] { new OzellikAgirligi(ozellik, 1m, ters) });

        // ---- sınır sabitleri ----

        [Fact]
        public void Sinirlar_gorev_metnindeki_degerlerdir()
        {
            Assert.Equal(0.25m, CarpanSiniri.KisilikAlt);
            Assert.Equal(2.0m, CarpanSiniri.KisilikUst);
            Assert.Equal(0.6m, CarpanSiniri.FikirAlt);
            Assert.Equal(1.5m, CarpanSiniri.FikirUst);
            Assert.Equal(0.8m, CarpanSiniri.RuhHaliAlt);
            Assert.Equal(1.25m, CarpanSiniri.RuhHaliUst);
        }

        // ---- kişilik ----

        [Theory]
        [InlineData("0", "0.25")]
        [InlineData("0.25", "0.625")]
        [InlineData("0.5", "1")]
        [InlineData("0.75", "1.5")]
        [InlineData("1", "2")]
        public void Kisilik_parcali_dogrusal_esleme(string deger, string beklenen)
        {
            var kisilik = K(acgozluluk: Ond(deger));

            Assert.Equal(Ond(beklenen), CarpanHesaplayici.Kisilik(Tek("acgozluluk"), kisilik));
        }

        [Fact]
        public void Kisilik_ters_ozellik_bir_eksi_v_kullanir()
        {
            // durustluk 0.8 → ters: 0.2 → 0.25 + 0.2/0.5*0.75 = 0.55
            Assert.Equal(0.55m, CarpanHesaplayici.Kisilik(Tek("durustluk", ters: true), K(durustluk: 0.8m)));
        }

        [Fact]
        public void Kisilik_birden_cok_ozellik_agirlikli_ortalamadir()
        {
            var tanim = new KisilikCarpaniTanimi(new[]
            {
                new OzellikAgirligi("cesaret", 1m, false),
                new OzellikAgirligi("merhamet", 3m, false),
            });

            // (1*1.0 + 3*0.0) / 4 = 0.25 → 0.625
            Assert.Equal(0.625m, CarpanHesaplayici.Kisilik(tanim, K(cesaret: 1m, merhamet: 0m)));
        }

        [Fact]
        public void Tanimsiz_carpan_bir_dir()
        {
            Assert.Equal(1m, CarpanHesaplayici.Kisilik(null, K(acgozluluk: 1m)));
            Assert.Equal(1m, CarpanHesaplayici.Fikir(null, 100m));
            Assert.Equal(1m, CarpanHesaplayici.RuhHali(null, _ => 1m));
        }

        [Fact]
        public void Kisilik_nötr_yer_tutucu_degerlerde_bir_verir()
        {
            // MiniHan'daki 0.5 yer tutucuları nötr kalır.
            foreach (var ozellik in Kisilik.Ozellikler)
            {
                Assert.Equal(1m, CarpanHesaplayici.Kisilik(Tek(ozellik), K()));
            }
        }

        // ---- fikir ----

        [Theory]
        [InlineData("-100", "0.6")]
        [InlineData("-50", "0.8")]
        [InlineData("0", "1")]
        [InlineData("50", "1.25")]
        [InlineData("100", "1.5")]
        [InlineData("250", "1.5")]
        [InlineData("-250", "0.6")]
        public void Fikir_artan_parcali_dogrusal(string fikir, string beklenen) =>
            Assert.Equal(Ond(beklenen), CarpanHesaplayici.Fikir(new FikirCarpaniTanimi(FikirYonu.Artan), Ond(fikir)));

        [Theory]
        [InlineData("100", "0.6")]
        [InlineData("-100", "1.5")]
        [InlineData("-50", "1.25")]
        [InlineData("0", "1")]
        public void Fikir_azalan_isareti_cevirir(string fikir, string beklenen) =>
            Assert.Equal(Ond(beklenen), CarpanHesaplayici.Fikir(new FikirCarpaniTanimi(FikirYonu.Azalan), Ond(fikir)));

        // ---- ruh hali ----

        [Fact]
        public void Ruh_hali_isaretli_toplam_esleme()
        {
            var korkuArtan = new RuhHaliCarpaniTanimi(new[] { new DuyguAgirligi("korku", 1m, false) });
            var korkuAzalan = new RuhHaliCarpaniTanimi(new[] { new DuyguAgirligi("korku", 1m, true) });

            Assert.Equal(1m, CarpanHesaplayici.RuhHali(korkuArtan, _ => 0m));
            Assert.Equal(1.125m, CarpanHesaplayici.RuhHali(korkuArtan, _ => 0.5m));
            Assert.Equal(1.25m, CarpanHesaplayici.RuhHali(korkuArtan, _ => 1m));
            Assert.Equal(0.8m, CarpanHesaplayici.RuhHali(korkuAzalan, _ => 1m));
            Assert.Equal(0.9m, CarpanHesaplayici.RuhHali(korkuAzalan, _ => 0.5m));
        }

        [Fact]
        public void Ruh_hali_toplami_once_eksi_bir_arti_bire_kirpilir()
        {
            var tanim = new RuhHaliCarpaniTanimi(new[]
            {
                new DuyguAgirligi("korku", 5m, false),
                new DuyguAgirligi("ofke", 5m, false),
            });

            Assert.Equal(1.25m, CarpanHesaplayici.RuhHali(tanim, _ => 1m));
        }

        [Fact]
        public void Ruh_hali_karisik_yonde_toplanir()
        {
            var tanim = new RuhHaliCarpaniTanimi(new[]
            {
                new DuyguAgirligi("ofke", 1m, false),
                new DuyguAgirligi("korku", 1m, true),
            });
            var siddetler = new Dictionary<string, decimal> { ["ofke"] = 0m, ["korku"] = 0.5m };

            // toplam = +0*1 − 0.5*1 = −0.5 → 1 − 0.5*0.2 = 0.9
            Assert.Equal(0.9m, CarpanHesaplayici.RuhHali(tanim, ad => siddetler[ad]));
        }

        // ---- sınırlar en uç girdilerde aşılamaz ----

        [Fact]
        public void Hicbir_uc_girdi_siniri_asmaz()
        {
            foreach (var ozellik in Kisilik.Ozellikler)
            {
                foreach (var ters in new[] { false, true })
                {
                    foreach (var v in new[] { 0m, 1m })
                    {
                        var c = CarpanHesaplayici.Kisilik(
                            new KisilikCarpaniTanimi(new[] { new OzellikAgirligi(ozellik, 100m, ters) }),
                            K(acgozluluk: v, durustluk: v, cesaret: v, sosyallik: v, kinTutma: v, merhamet: v));
                        Assert.InRange(c, CarpanSiniri.KisilikAlt, CarpanSiniri.KisilikUst);
                    }
                }
            }

            foreach (var yon in new[] { FikirYonu.Artan, FikirYonu.Azalan })
            {
                foreach (var f in new[] { decimal.MinValue, -1000m, -100m, 0m, 100m, 1000m, decimal.MaxValue })
                {
                    Assert.InRange(CarpanHesaplayici.Fikir(new FikirCarpaniTanimi(yon), f), CarpanSiniri.FikirAlt, CarpanSiniri.FikirUst);
                }
            }

            foreach (var ters in new[] { false, true })
            {
                foreach (var s in new[] { -5m, 0m, 1m, 5m, decimal.MaxValue / 1000m })
                {
                    var tanim = new RuhHaliCarpaniTanimi(new[] { new DuyguAgirligi("korku", 1000000m, ters) });
                    Assert.InRange(CarpanHesaplayici.RuhHali(tanim, _ => s), CarpanSiniri.RuhHaliAlt, CarpanSiniri.RuhHaliUst);
                }
            }
        }

        // ---- tanım doğrulaması ----

        [Theory]
        [InlineData("0")]
        [InlineData("-1")]
        public void Agirlik_sifirdan_buyuk_olmali(string agirlik)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new OzellikAgirligi("cesaret", Ond(agirlik), false));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DuyguAgirligi("korku", Ond(agirlik), false));
        }

        [Fact]
        public void Bilinmeyen_kisilik_ozelligi_reddedilir() =>
            Assert.Throws<ArgumentException>(() => new OzellikAgirligi("sakinlik", 1m, false));

        [Fact]
        public void Bos_ya_da_tekrarli_liste_reddedilir()
        {
            Assert.Throws<ArgumentException>(() => new KisilikCarpaniTanimi(new OzellikAgirligi[0]));
            Assert.Throws<ArgumentException>(() => new KisilikCarpaniTanimi(new[]
            {
                new OzellikAgirligi("cesaret", 1m, false), new OzellikAgirligi("cesaret", 2m, true),
            }));
            Assert.Throws<ArgumentException>(() => new RuhHaliCarpaniTanimi(new DuyguAgirligi[0]));
            Assert.Throws<ArgumentException>(() => new RuhHaliCarpaniTanimi(new[]
            {
                new DuyguAgirligi("korku", 1m, false), new DuyguAgirligi("korku", 1m, true),
            }));
        }

        [Theory]
        [InlineData("artan", FikirYonu.Artan)]
        [InlineData("azalan", FikirYonu.Azalan)]
        public void Fikir_yonu_adlari(string ad, FikirYonu yon)
        {
            Assert.True(FikirYonleri.TryCoz(ad, out var cozulen));
            Assert.Equal(yon, cozulen);
            Assert.Equal(ad, FikirYonleri.Ad(yon));
            Assert.False(FikirYonleri.TryCoz("yukari", out _));
        }

        private static decimal Ond(string s) => decimal.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
    }
}
