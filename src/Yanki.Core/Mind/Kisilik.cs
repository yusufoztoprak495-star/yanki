using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Yanki.Core.Mind
{
    /// <summary>
    /// Kişilik: altı özellik, her biri 0..1 (plan 5.4). Senaryodan bir kez okunur ve hiçbir olayla değişmez (v0.1'de kayma yok).
    /// </summary>
    public sealed class Kisilik
    {
        private static readonly string[] OzellikListesi =
        {
            "acgozluluk", "cesaret", "durustluk", "kin_tutma", "merhamet", "sosyallik",
        };

        private readonly decimal _acgozluluk;
        private readonly decimal _durustluk;
        private readonly decimal _cesaret;
        private readonly decimal _sosyallik;
        private readonly decimal _kinTutma;
        private readonly decimal _merhamet;

        /// <summary>Özellik adları (ASCII), ordinal sıralı.</summary>
        public static IReadOnlyList<string> Ozellikler { get; } = new ReadOnlyCollection<string>(OzellikListesi);

        /// <summary>Kişilik kurar.</summary>
        /// <exception cref="ArgumentOutOfRangeException">Bir özellik 0..1 dışındaysa.</exception>
        public Kisilik(decimal acgozluluk, decimal durustluk, decimal cesaret, decimal sosyallik, decimal kinTutma, decimal merhamet)
        {
            _acgozluluk = Denetle(acgozluluk, nameof(acgozluluk));
            _durustluk = Denetle(durustluk, nameof(durustluk));
            _cesaret = Denetle(cesaret, nameof(cesaret));
            _sosyallik = Denetle(sosyallik, nameof(sosyallik));
            _kinTutma = Denetle(kinTutma, nameof(kinTutma));
            _merhamet = Denetle(merhamet, nameof(merhamet));
        }

        /// <summary>Özelliğin değeri (0..1).</summary>
        /// <exception cref="ArgumentException">Özellik adı bilinmiyorsa.</exception>
        public decimal Deger(string ozellik)
        {
            switch (ozellik)
            {
                case "acgozluluk": return _acgozluluk;
                case "durustluk": return _durustluk;
                case "cesaret": return _cesaret;
                case "sosyallik": return _sosyallik;
                case "kin_tutma": return _kinTutma;
                case "merhamet": return _merhamet;
                default:
                    throw new ArgumentException($"Bilinmeyen kişilik özelliği: \"{ozellik}\" (acgozluluk, durustluk, cesaret, sosyallik, kin_tutma, merhamet).", nameof(ozellik));
            }
        }

        private static decimal Denetle(decimal deger, string ad)
        {
            if (deger < 0m || deger > 1m)
            {
                throw new ArgumentOutOfRangeException(ad, deger, "Kişilik özelliği 0..1 aralığında olmalı.");
            }

            return deger;
        }
    }
}
