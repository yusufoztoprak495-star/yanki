using System;
using Xunit;
using Yanki.Core.Decision;

namespace Yanki.Core.Tests.Decision
{
    /// <summary>
    /// Eğriler ve elle yazılmış decimal <c>e^y</c> (plan 5.5, ADR-007). Altın değerler <c>tools/golden_ref.py</c> içindeki
    /// bağımsız Python hesabından (60 haneli <c>Decimal.exp</c>) alınmıştır; kodun kendi çıktısından kopyalanmamıştır.
    /// </summary>
    public class EgriTests
    {
        // ---- DecimalMat.Us ----

        [Theory]
        [InlineData("0", "1")]
        [InlineData("1", "2.71828182845904523536")]
        [InlineData("-1", "0.36787944117144232160")]
        [InlineData("0.5", "1.64872127070012814685")]
        [InlineData("2.5", "12.18249396070347343807")]
        [InlineData("-0.25", "0.77880078307140486825")]
        [InlineData("10", "22026.46579480671651695790")]
        [InlineData("-10", "0.0000453999297624848515355915")]
        [InlineData("30", "10686474581524.46214699046865074140")]
        [InlineData("-30", "0.0000000000000935762296884017")]
        public void Us_python_referansiyla_uyusur(string y, string beklenen)
        {
            var sonuc = DecimalMat.Us(decimal.Parse(y, System.Globalization.CultureInfo.InvariantCulture));
            var hedef = decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture);

            // Göreli hata 1e-15'ten küçük (6 ondalığa yuvarlamadan çok önce).
            Assert.True(Math.Abs(sonuc - hedef) <= hedef * 0.000000000000001m, $"Us({y}) = {sonuc}, beklenen {beklenen}");
        }

        [Fact]
        public void Us_aralik_disini_kirpilir()
        {
            Assert.Equal(DecimalMat.Us(DecimalMat.UsUstSinir), DecimalMat.Us(1000m));
            Assert.Equal(DecimalMat.Us(DecimalMat.UsAltSinir), DecimalMat.Us(-1000m));
            Assert.Equal(30m, DecimalMat.UsUstSinir);
            Assert.Equal(-30m, DecimalMat.UsAltSinir);
        }

        [Fact]
        public void Us_monoton_artar()
        {
            var onceki = DecimalMat.Us(-30m);
            for (var y = -29.5m; y <= 30m; y += 0.5m)
            {
                var simdiki = DecimalMat.Us(y);
                Assert.True(simdiki > onceki, $"Us({y}) önceki değerden büyük olmalı");
                onceki = simdiki;
            }
        }

        // ---- Eğriler ----

        [Theory]
        [InlineData("0", "0")]
        [InlineData("0.25", "0.25")]
        [InlineData("1", "1")]
        public void Dogrusal_girdiyi_aynen_verir(string x, string beklenen) =>
            Assert.Equal(Ondalik(beklenen), Egriler.Hesapla(EgriTanimi.Dogrusal(), Ondalik(x)));

        [Theory]
        [InlineData(1, "0.5", "0.5")]
        [InlineData(2, "0.5", "0.25")]
        [InlineData(3, "0.5", "0.125")]
        [InlineData(8, "0.5", "0.003906")] // 0.00390625 → 6 ondalıkta ToEven: 0.003906
        [InlineData(2, "0", "0")]
        [InlineData(8, "1", "1")]
        public void Ustel_tam_us_alir(int k, string x, string beklenen) =>
            Assert.Equal(Ondalik(beklenen), Egriler.Hesapla(EgriTanimi.Ustel(k), Ondalik(x)));

        [Theory]
        [InlineData("10", "0.5", "0", "0.006693")]
        [InlineData("10", "0.5", "0.25", "0.075858")]
        [InlineData("10", "0.5", "0.5", "0.5")]
        [InlineData("10", "0.5", "0.75", "0.924142")]
        [InlineData("10", "0.5", "1", "0.993307")]
        [InlineData("4", "0.3", "0", "0.231475")]
        [InlineData("4", "0.3", "0.1", "0.310026")]
        [InlineData("4", "0.3", "0.6", "0.768525")]
        [InlineData("4", "0.3", "1", "0.942676")]
        [InlineData("50", "0.5", "0", "0")]
        [InlineData("50", "0.5", "0.4", "0.006693")]
        [InlineData("50", "0.5", "0.6", "0.993307")]
        [InlineData("50", "0.5", "1", "1")]
        public void Lojistik_python_referansiyla_uyusur(string k, string m, string x, string beklenen) =>
            Assert.Equal(Ondalik(beklenen), Egriler.Hesapla(EgriTanimi.Lojistik(Ondalik(k), Ondalik(m)), Ondalik(x)));

        [Theory]
        [InlineData("0.5", "0.49", "0")]
        [InlineData("0.5", "0.5", "1")]
        [InlineData("0.5", "0.51", "1")]
        [InlineData("0.5", "1", "1")]
        [InlineData("0.5", "0", "0")]
        public void Basamak_esikte_ve_ustunde_bir_verir(string esik, string x, string beklenen) =>
            Assert.Equal(Ondalik(beklenen), Egriler.Hesapla(EgriTanimi.Basamak(Ondalik(esik)), Ondalik(x)));

        [Fact]
        public void Girdi_0_1_disindaysa_once_kirpilir()
        {
            foreach (var egri in Hepsi())
            {
                Assert.Equal(Egriler.Hesapla(egri, 0m), Egriler.Hesapla(egri, -5m));
                Assert.Equal(Egriler.Hesapla(egri, 1m), Egriler.Hesapla(egri, 7m));
            }
        }

        [Fact]
        public void Cikti_her_zaman_0_1_arasi_ve_6_ondaliga_yuvarlanmistir()
        {
            foreach (var egri in Hepsi())
            {
                for (var x = -0.2m; x <= 1.2m; x += 0.0173m)
                {
                    var sonuc = Egriler.Hesapla(egri, x);
                    Assert.InRange(sonuc, 0m, 1m);
                    Assert.Equal(Math.Round(sonuc, 6, MidpointRounding.ToEven), sonuc);
                }
            }
        }

        [Fact]
        public void Dogrusal_ustel_ve_lojistik_monoton_azalmaz()
        {
            foreach (var egri in new[] { EgriTanimi.Dogrusal(), EgriTanimi.Ustel(3), EgriTanimi.Lojistik(10m, 0.5m), EgriTanimi.Basamak(0.5m) })
            {
                var onceki = Egriler.Hesapla(egri, 0m);
                for (var x = 0.01m; x <= 1m; x += 0.01m)
                {
                    var simdiki = Egriler.Hesapla(egri, x);
                    Assert.True(simdiki >= onceki, $"{egri.Tur} x={x}: {simdiki} < {onceki}");
                    onceki = simdiki;
                }
            }
        }

        [Fact]
        public void Lojistik_tanimli_uc_degerlerde_tasmaz()
        {
            // Büyük k ile bile Us kırpması sayesinde taşma ve sıfıra bölme yok.
            var egri = EgriTanimi.Lojistik(EgriTanimi.LojistikEnBuyukK, 0.5m);

            Assert.Equal(0m, Egriler.Hesapla(egri, 0m));
            Assert.Equal(1m, Egriler.Hesapla(egri, 1m));
        }

        // ---- Tanım doğrulaması ----

        [Theory]
        [InlineData(0)]
        [InlineData(9)]
        [InlineData(-1)]
        public void Ustel_k_1_8_disinda_reddedilir(int k) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => EgriTanimi.Ustel(k));

        [Theory]
        [InlineData("0")]
        [InlineData("-3")]
        public void Lojistik_k_pozitif_olmali(string k) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => EgriTanimi.Lojistik(Ondalik(k), 0.5m));

        [Theory]
        [InlineData("-0.1")]
        [InlineData("1.1")]
        public void Lojistik_ve_basamak_m_0_1_icinde_olmali(string m)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EgriTanimi.Lojistik(5m, Ondalik(m)));
            Assert.Throws<ArgumentOutOfRangeException>(() => EgriTanimi.Basamak(Ondalik(m)));
        }

        [Fact]
        public void Lojistik_k_ust_siniri_asilamaz() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => EgriTanimi.Lojistik(EgriTanimi.LojistikEnBuyukK + 1m, 0.5m));

        [Theory]
        [InlineData("dogrusal", EgriTuru.Dogrusal)]
        [InlineData("ustel", EgriTuru.Ustel)]
        [InlineData("lojistik", EgriTuru.Lojistik)]
        [InlineData("basamak", EgriTuru.Basamak)]
        public void Egri_adlari_gidis_donus(string ad, EgriTuru tur)
        {
            Assert.True(EgriTurleri.TryCoz(ad, out var cozulen));
            Assert.Equal(tur, cozulen);
            Assert.Equal(ad, EgriTurleri.Ad(tur));
        }

        [Theory]
        [InlineData("")]
        [InlineData("kare")]
        [InlineData("Dogrusal")]
        public void Bilinmeyen_egri_adi_cozulmez(string ad) =>
            Assert.False(EgriTurleri.TryCoz(ad, out _));

        [Fact]
        public void Yuvarla_ToEven_6_ondalik()
        {
            Assert.Equal(0.000002m, Egriler.Yuvarla(0.0000025m));
            Assert.Equal(0.000004m, Egriler.Yuvarla(0.0000035m));
            Assert.Equal(0m, Egriler.Yuvarla(0.0000004m));
        }

        private static EgriTanimi[] Hepsi() =>
            new[] { EgriTanimi.Dogrusal(), EgriTanimi.Ustel(2), EgriTanimi.Ustel(8), EgriTanimi.Lojistik(10m, 0.5m), EgriTanimi.Lojistik(50m, 0.2m), EgriTanimi.Basamak(0.3m) };

        private static decimal Ondalik(string s) => decimal.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
    }
}
