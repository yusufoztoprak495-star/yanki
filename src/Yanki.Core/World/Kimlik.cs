using System;
using System.Globalization;

namespace Yanki.Core.World
{
    /// <summary>
    /// Varlık kimliği, yüklem adı ve gerçek argümanı kuralları. Kimlikler ASCII'dir; bu, kanonik serileştirmenin
    /// ayraçlarla (<c>( ) , ! * |</c>) karışmamasını ve hash'in platformdan bağımsız kalmasını sağlar.
    /// Oyuncuya görünen (Türkçe karakterli) ad ileride ayrı bir alan olacaktır.
    /// </summary>
    internal static class Kimlik
    {
        /// <summary><c>[A-Za-z_][A-Za-z0-9_]*</c> biçiminde mi.</summary>
        public static bool GecerliKimlikMi(string? s)
        {
            if (string.IsNullOrEmpty(s) || !(HarfMi(s![0]) || s[0] == '_'))
            {
                return false;
            }

            for (var i = 1; i < s.Length; i++)
            {
                if (!(HarfMi(s[i]) || RakamMi(s[i]) || s[i] == '_'))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Kimlik, ya da sonuna tek bir <c>!</c> eklenmiş kimlik (tek değerli yüklem).</summary>
        public static bool GecerliYuklemMi(string? s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return false;
            }

            return s![s.Length - 1] == '!'
                ? GecerliKimlikMi(s.Substring(0, s.Length - 1))
                : GecerliKimlikMi(s);
        }

        /// <summary>Gerçek argümanı: geçerli kimlik ya da <see cref="KanonikYazici.Sayi"/> ile birebir aynı yazılmış sayı.</summary>
        public static bool GecerliArgumanMi(string? s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return false;
            }

            if (RakamMi(s![0]) || s[0] == '-')
            {
                return KanonikSayiMi(s);
            }

            return GecerliKimlikMi(s);
        }

        private static bool KanonikSayiMi(string s)
        {
            if (!decimal.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d))
            {
                return false;
            }

            return string.Equals(KanonikYazici.Sayi(d), s, StringComparison.Ordinal);
        }

        private static bool HarfMi(char c) => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');

        private static bool RakamMi(char c) => c >= '0' && c <= '9';
    }
}
