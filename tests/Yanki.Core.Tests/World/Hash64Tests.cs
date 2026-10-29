using System.Text;
using Xunit;
using Yanki.Core.World;

namespace Yanki.Core.Tests.World
{
    public class Hash64Tests
    {
        [Theory]
        [InlineData("", "cbf29ce484222325")]
        [InlineData("a", "af63dc4c8601ec8c")]
        [InlineData("foobar", "85944171f73967e8")]
        public void Fnv1a_bilinen_vektorler(string girdi, string beklenenHex)
        {
            var sonuc = Hash64.Fnv1a(Encoding.UTF8.GetBytes(girdi));

            Assert.Equal(beklenenHex, Hash64.Hex(sonuc));
        }

        [Fact]
        public void Hex_16_karakter_kucuk_harf_sifir_dolgulu()
        {
            Assert.Equal("0000000000000000", Hash64.Hex(0UL));
            Assert.Equal("00000000000000ff", Hash64.Hex(255UL));
            Assert.Equal("ffffffffffffffff", Hash64.Hex(ulong.MaxValue));
        }

        [Fact]
        public void Hesapla_ayni_girdi_ayni_sonuc_farkli_girdi_farkli_sonuc()
        {
            var a = Hash64.Hesapla(Encoding.UTF8.GetBytes("Ali"));
            var b = Hash64.Hesapla(Encoding.UTF8.GetBytes("Ali"));
            var c = Hash64.Hesapla(Encoding.UTF8.GetBytes("Alj"));

            Assert.Equal(a, b);
            Assert.NotEqual(a, c);
            Assert.NotEqual(Hash64.Fnv1a(Encoding.UTF8.GetBytes("Ali")), a);
        }
    }
}
