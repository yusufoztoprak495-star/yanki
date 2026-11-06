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

        /// <summary>
        /// Parça parça beslenen hash (9. hafta optimizasyonu): <c>Ekle(a); Ekle(b); Bitir()</c>, <c>Hesapla(a + b)</c> ile BİREBİR aynı değeri verir ama birleştirilmiş metin ve UTF-8 bayt dizisi
        /// ayırmaz. UTF-8 dönüşümü <c>Encoding.UTF8</c> ile aynıdır (geçersiz vekil çifti U+FFFD olur; parçalar arasına düşen vekil çifti de doğru birleşir).
        /// </summary>
        public struct Akis
        {
            private ulong _h;
            private char _bekleyenYuksek;
            private bool _basladi;

            /// <summary>Metni akışa ekler.</summary>
            public void Ekle(string metin)
            {
                if (metin == null)
                {
                    throw new ArgumentNullException(nameof(metin));
                }

                if (!_basladi)
                {
                    _h = FnvOffset;
                    _basladi = true;
                }

                unchecked
                {
                    var h = _h;
                    for (var i = 0; i < metin.Length; i++)
                    {
                        var c = metin[i];
                        if (_bekleyenYuksek != '\0')
                        {
                            if (c >= '\uDC00' && c <= '\uDFFF')
                            {
                                var cp = 0x10000 + ((_bekleyenYuksek - 0xD800) << 10) + (c - 0xDC00);
                                _bekleyenYuksek = '\0';
                                h = Bayt(h, 0xF0 | (cp >> 18));
                                h = Bayt(h, 0x80 | ((cp >> 12) & 0x3F));
                                h = Bayt(h, 0x80 | ((cp >> 6) & 0x3F));
                                h = Bayt(h, 0x80 | (cp & 0x3F));
                                continue;
                            }

                            _bekleyenYuksek = '\0';
                            h = Degistirme(h);
                        }

                        if (c < 0x80)
                        {
                            h = Bayt(h, c);
                        }
                        else if (c < 0x800)
                        {
                            h = Bayt(h, 0xC0 | (c >> 6));
                            h = Bayt(h, 0x80 | (c & 0x3F));
                        }
                        else if (c >= '\uD800' && c <= '\uDBFF')
                        {
                            _bekleyenYuksek = c;
                        }
                        else if (c >= '\uDC00' && c <= '\uDFFF')
                        {
                            h = Degistirme(h);
                        }
                        else
                        {
                            h = Bayt(h, 0xE0 | (c >> 12));
                            h = Bayt(h, 0x80 | ((c >> 6) & 0x3F));
                            h = Bayt(h, 0x80 | (c & 0x3F));
                        }
                    }

                    _h = h;
                }
            }

            /// <summary>Akışı bitirir ve <see cref="Hesapla(string)"/> ile aynı değeri döndürür.</summary>
            public ulong Bitir()
            {
                if (!_basladi)
                {
                    _h = FnvOffset;
                    _basladi = true;
                }

                if (_bekleyenYuksek != '\0')
                {
                    _bekleyenYuksek = '\0';
                    _h = Degistirme(_h);
                }

                return Karistir(_h);
            }

            private static ulong Bayt(ulong h, int b)
            {
                unchecked
                {
                    return (h ^ (byte)b) * FnvAsal;
                }
            }

            // Geçersiz vekil: U+FFFD = EF BF BD.
            private static ulong Degistirme(ulong h) => Bayt(Bayt(Bayt(h, 0xEF), 0xBF), 0xBD);
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
