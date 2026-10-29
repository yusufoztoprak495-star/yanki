using Xunit;
using Yanki.Core.Tests.World;
using Yanki.Core.World;

namespace Yanki.Core.Tests.Determinizm
{
    /// <summary>
    /// Altın sabit testleri: beklenen değerler testin içinde sabit yazılıdır. Biri hash'e <c>GetHashCode</c> gibi kararsız bir
    /// şey, yuvarlamaya <c>AwayFromZero</c> ya da RNG'ye başka bir akış karıştırırsa bu test kırılır.
    /// Değerler C# çıktısından kopyalanmadı: aynı algoritmayı (FNV-1a 64 + SplitMix64, UTF-8, Python <c>decimal</c> ile
    /// <c>ROUND_HALF_EVEN</c>) bağımsız bir Python betiğinde uygulayıp hesaplandı.
    /// </summary>
    public class AltinSabitTests
    {
        // Beklenen kanonik metin, bağımsız betiğin de kullandığı şekilde elle yazılmıştır.
        private const string BeklenenKanonikMetin =
            "YANKI-DURUM|1\n" +
            "V|Ali|npc\n" +
            "V|Ayse|npc\n" +
            "V|Mehmet|npc\n" +
            "V|han|mekan\n" +
            "V|pazar|mekan\n" +
            "M|han|pazar\n" +
            "G|borclu(Mehmet,Ayse,500)\n" +
            "G|faiz(Mehmet,Ayse,0.000002)\n" +
            "G|konum!(Ali,han)\n" +
            "G|konum!(Ayse,han)\n" +
            "G|konum!(Mehmet,pazar)\n";

        [Fact]
        public void Sabit_dunyanin_kanonik_metni_beklenen_gibidir()
        {
            // faiz(...) argümanı 0.0000025 (tam orta nokta) → ToEven ile 0.000002 (AwayFromZero olsaydı 0.000003).
            Assert.Equal(BeklenenKanonikMetin, OlayUretici.SabitDunya().KanonikMetin());
        }

        [Fact]
        public void Sabit_dunyanin_durum_hashi_degismez()
        {
            Assert.Equal("b37a372bab89d9b4", OlayUretici.SabitDunya().DurumHash());
        }

        [Fact]
        public void Anahtarli_rng_ilk_bes_deger_degismez()
        {
            var rng = AnahtarliRng.Icin(12345UL, 7, "Ali", "karar");

            var degerler = new[] { rng.SonrakiUlong(), rng.SonrakiUlong(), rng.SonrakiUlong(), rng.SonrakiUlong(), rng.SonrakiUlong() };

            Assert.Equal(
                new[]
                {
                    0x41897ba17e68cc09UL,
                    0x94e63e30c5c5cba5UL,
                    0x310f640f487b6299UL,
                    0x0c50749f6a6a10ebUL,
                    0x9a0fd699122c67dcUL,
                },
                degerler);
        }
    }
}
