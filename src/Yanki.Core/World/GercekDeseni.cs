using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Yanki.Core.World
{
    /// <summary>
    /// Gerçeklerle eşleşen desen: <c>null</c> argüman joker (<c>*</c>) demektir.
    /// <c>borclu(Mehmet, *, *)</c> = "Mehmet'in herhangi birine borçlu olduğu önerme".
    /// </summary>
    public sealed class GercekDeseni
    {
        /// <summary>Yüklem adı (tek değerliyse <c>!</c> dahil).</summary>
        public string Yuklem { get; }

        /// <summary>Argümanlar; <c>null</c> = joker.</summary>
        public IReadOnlyList<string?> Argumanlar { get; }

        /// <summary>Yeni desen kurar.</summary>
        /// <exception cref="ArgumentException">Yüklem ya da joker olmayan bir argüman geçersizse.</exception>
        public GercekDeseni(string yuklem, IEnumerable<string?> argumanlar)
        {
            if (!Kimlik.GecerliYuklemMi(yuklem))
            {
                throw new ArgumentException($"Geçersiz yüklem adı: \"{yuklem}\".", nameof(yuklem));
            }

            var liste = new List<string?>(argumanlar ?? throw new ArgumentNullException(nameof(argumanlar)));
            foreach (var a in liste)
            {
                if (a != null && !Kimlik.GecerliArgumanMi(a))
                {
                    throw new ArgumentException($"Geçersiz desen argümanı: \"{a}\".", nameof(argumanlar));
                }
            }

            Yuklem = yuklem;
            Argumanlar = new ReadOnlyCollection<string?>(liste);
        }

        /// <summary>Gerçek bu desene uyuyor mu: aynı yüklem, aynı argüman sayısı, joker olmayanlar eşit.</summary>
        public bool Uyar(Gercek gercek)
        {
            if (gercek == null)
            {
                throw new ArgumentNullException(nameof(gercek));
            }

            if (!string.Equals(Yuklem, gercek.Yuklem, StringComparison.Ordinal) || Argumanlar.Count != gercek.Argumanlar.Count)
            {
                return false;
            }

            for (var i = 0; i < Argumanlar.Count; i++)
            {
                if (Argumanlar[i] != null && !string.Equals(Argumanlar[i], gercek.Argumanlar[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
