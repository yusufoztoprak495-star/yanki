using System;
using System.Text;

namespace Yanki.Core.World
{
    /// <summary>
    /// Kendi 64 bit hash'imiz: FNV-1a, ardından SplitMix64 bitirme karıştırıcısı. Platformdan ve süreçten bağımsızdır
    /// (<c>string.GetHashCode</c> ve <c>HashCode</c> süreçten sürece değiştiği için kullanılmaz).
    /// </summary>
    public static class Hash64
    {
        private const ulong FnvOffset = 14695981039346656037UL;
        private const ulong FnvAsal = 1099511628211UL;

        /// <summary>Saf 64 bit FNV-1a.</summary>
        public static ulong Fnv1a(byte[] veri)
        {
            if (veri == null)
            {
                throw new ArgumentNullException(nameof(veri));
            }

            unchecked
            {
                var h = FnvOffset;
                for (var i = 0; i < veri.Length; i++)
                {
                    h ^= veri[i];
                    h *= FnvAsal;
                }

                return h;
            }
        }

        /// <summary>FNV-1a sonucunu SplitMix64 karıştırıcısından geçirir; kısa girdilerde bile bitler iyi dağılır.</summary>
        public static ulong Hesapla(byte[] veri) => Karistir(Fnv1a(veri));

        /// <summary>Metni UTF-8 baytlarına çevirip <see cref="Hesapla(byte[])"/> uygular.</summary>
        public static ulong Hesapla(string metin)
        {
            if (metin == null)
            {
                throw new ArgumentNullException(nameof(metin));
            }

            return Hesapla(Encoding.UTF8.GetBytes(metin));
        }

        /// <summary>64 bit değeri 16 karakterlik, küçük harfli, sıfır dolgulu onaltılık metne çevirir.</summary>
        public static string Hex(ulong deger) => deger.ToString("x16", System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>SplitMix64'ün bitirme (finalizer) adımı.</summary>
        internal static ulong Karistir(ulong z)
        {
            unchecked
            {
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
    }
}
