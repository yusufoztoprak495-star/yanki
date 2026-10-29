using Xunit;
using Yanki.Core.World;

namespace Yanki.Core.Tests.World
{
    public class KanonikYaziciTests
    {
        [Theory]
        [InlineData("500", "500")]
        [InlineData("0.5", "0.5")]
        [InlineData("0.10", "0.1")]
        [InlineData("-20", "-20")]
        [InlineData("1.000000", "1")]
        [InlineData("123.456789", "123.456789")]
        public void Sayi_sondaki_sifirlar_kirpilir(string girdi, string beklenen)
        {
            Assert.Equal(beklenen, KanonikYazici.Sayi(decimal.Parse(girdi, System.Globalization.CultureInfo.InvariantCulture)));
        }

        // Tam orta nokta: 7. basamak 5 ve sonrası sıfır. MidpointRounding.ToEven: yarım, çift basamağa gider.
        [Theory]
        [InlineData("0.0000005", "0")]          // ...0|5 → 0 (çift)
        [InlineData("0.0000015", "0.000002")]   // ...1|5 → 2 (çift)
        [InlineData("0.0000025", "0.000002")]   // ...2|5 → 2 (çift)
        [InlineData("0.0000035", "0.000004")]   // ...3|5 → 4 (çift)
        [InlineData("1.2345675", "1.234568")]   // ...7|5 → 8 (çift)
        [InlineData("1.2345665", "1.234566")]   // ...6|5 → 6 (çift)
        [InlineData("-0.0000015", "-0.000002")]
        [InlineData("-0.0000025", "-0.000002")]
        [InlineData("-0.0000005", "0")]         // "-0" yazılmaz
        [InlineData("0.0000004", "0")]
        [InlineData("0.0000006", "0.000001")]
        public void Sayi_tam_orta_noktada_ToEven_yuvarlar(string girdi, string beklenen)
        {
            Assert.Equal(beklenen, KanonikYazici.Sayi(decimal.Parse(girdi, System.Globalization.CultureInfo.InvariantCulture)));
        }

        [Fact]
        public void Sayi_kultur_bagimsizdir()
        {
            var onceki = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                // InvariantGlobalization açık olduğu için tr-TR kurulamaz; ondalık ayracı virgül olan bir kültür kopyası kullanılır.
                var virgullu = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.InvariantCulture.Clone();
                virgullu.NumberFormat.NumberDecimalSeparator = ",";
                System.Globalization.CultureInfo.CurrentCulture = virgullu;

                Assert.Equal("0,5", 0.5m.ToString(System.Globalization.CultureInfo.CurrentCulture.NumberFormat));
                Assert.Equal("0.5", KanonikYazici.Sayi(0.5m));
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = onceki;
            }
        }
    }
}
