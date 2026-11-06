using System;
using System.Text;
using Xunit;
using Yanki.Core.World;

namespace Yanki.Core.Tests.World
{
    /// <summary>9. hafta (DURAK 3 optimizasyonu): akışlı hash, birleştirilmiş metnin hash'iyle BİREBİR aynı olmalı (altın hash'ler değişmez).</summary>
    public class Hash64AkisTests
    {
        private static ulong Akis(params string[] parcalar)
        {
            var a = new Hash64.Akis();
            foreach (var p in parcalar)
            {
                a.Ekle(p);
            }

            return a.Bitir();
        }

        [Theory]
        [InlineData("")]
        [InlineData("a")]
        [InlineData("ZIHIN|1\nÇğışöü ıİ şŞ")]
        [InlineData("emoji 😀 sonu")]
        public void Tek_parca_Hesaplaya_esittir(string metin) => Assert.Equal(Hash64.Hesapla(metin), Akis(metin));

        [Fact]
        public void Bos_akis_bos_metnin_hashidir() => Assert.Equal(Hash64.Hesapla(string.Empty), new Hash64.Akis().Bitir());

        [Fact]
        public void Vekil_cifti_parcalar_arasina_dusse_de_birlesik_metinle_ayni()
        {
            var birlesik = "x😀y";
            Assert.Equal(Hash64.Hesapla(birlesik), Akis("x\uD83D", "\uDE00y"));
            Assert.Equal(Hash64.Hesapla(birlesik), Akis("x", "😀", "y"));
        }

        [Fact]
        public void Yalniz_vekiller_Encoding_UTF8_gibi_U_FFFD_olur()
        {
            foreach (var m in new[] { "\uD83D", "\uDE00", "a\uD83Db", "\uDE00\uD83D", "\uD83D😀" })
            {
                Assert.Equal(Hash64.Hesapla(m), Akis(m));
                Assert.Equal(Hash64.Hesapla(m), Akis(m.Substring(0, m.Length / 2), m.Substring(m.Length / 2)));
            }
        }

        [Fact]
        public void Rastgele_metinlerde_ve_rastgele_bolmede_birlesik_metinle_ayni()
        {
            var alfabe = "abcçğıöşü AZ09|\n😀😀xyz".ToCharArray();
            // Sabit tohumlu basit LCG (paket ve System.Random yok): 2000 deterministik örnek.
            ulong durum = 88172645463325252UL;
            int Sonraki(int ust)
            {
                unchecked
                {
                    durum = (durum * 6364136223846793005UL) + 1442695040888963407UL;
                }

                return (int)((durum >> 33) % (ulong)ust);
            }

            for (var n = 0; n < 2000; n++)
            {
                var uzunluk = Sonraki(41);
                var sb = new StringBuilder();
                for (var k = 0; k < uzunluk; k++)
                {
                    sb.Append(alfabe[Sonraki(alfabe.Length)]);
                }

                var metin = sb.ToString();
                var kes = Sonraki(metin.Length + 1);
                Assert.Equal(Hash64.Hesapla(metin), Akis(metin.Substring(0, kes), metin.Substring(kes)));
                Assert.Equal(Hash64.Hesapla(Encoding.UTF8.GetBytes(metin)), Akis(metin));
            }
        }
    }
}
