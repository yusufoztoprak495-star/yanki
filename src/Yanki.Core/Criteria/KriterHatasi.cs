using System;

namespace Yanki.Core.Criteria
{
    /// <summary>
    /// Kriter ifadesi ya da kriter tanımı geçersizse fırlatılır. Mesaj, biliniyorsa kaynak metindeki
    /// karakter indeksini ("konum N") içerir.
    /// </summary>
    public sealed class KriterHatasi : Exception
    {
        /// <summary>Hatanın kaynak ifadedeki 0 tabanlı karakter indeksi; bilinmiyorsa -1.</summary>
        public int Konum { get; }

        /// <summary>Konumu bilinen bir hata kurar.</summary>
        public KriterHatasi(string mesaj, int konum)
            : base(konum >= 0 ? $"{mesaj} (konum {konum})" : mesaj)
        {
            Konum = konum;
        }

        /// <summary>Konumu olmayan bir hata kurar.</summary>
        public KriterHatasi(string mesaj)
            : this(mesaj, -1)
        {
        }
    }
}
