using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Yanki.Core.Decision
{
    /// <summary>
    /// Bir senaryonun bütün eylem dosyalarından gelen eylemlerin birleşik kataloğu: kimliğe göre ordinal sıralı, kimlikler tekil.
    /// Dosya okuma çekirdeğin işi değildir; katalog yüklenmiş <see cref="EylemTanimi"/> nesneleri alır.
    /// </summary>
    public sealed class EylemKatalogu
    {
        private readonly SortedDictionary<string, EylemTanimi> _kimlikle = new SortedDictionary<string, EylemTanimi>(StringComparer.Ordinal);

        /// <summary>Eylemler, kimliğe göre ordinal sıralı.</summary>
        public IReadOnlyList<EylemTanimi> Eylemler { get; }

        /// <summary>
        /// Eylemlerin dışarı görünen olay türleri, tekrarsız ve ordinal sıralı. Hareket eylemleri tür taşımadığı için dahil değildir.
        /// Sonuç kurallarının <c>ne_zaman</c> alanı bu türleri kabul eder.
        /// </summary>
        public IReadOnlyList<string> OlayTurleri { get; }

        /// <summary>Katalog kurar.</summary>
        /// <exception cref="ArgumentException">Bir eylem kimliği tekrar ediyorsa.</exception>
        public EylemKatalogu(IEnumerable<EylemTanimi> eylemler)
        {
            if (eylemler == null)
            {
                throw new ArgumentNullException(nameof(eylemler));
            }

            var turler = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var e in eylemler)
            {
                if (_kimlikle.ContainsKey(e.Id))
                {
                    throw new ArgumentException($"Eylem kimliği tekrar ediyor: \"{e.Id}\".", nameof(eylemler));
                }

                _kimlikle.Add(e.Id, e);
                if (e.OlayTuru.Length > 0)
                {
                    turler.Add(e.OlayTuru);
                }
            }

            Eylemler = new ReadOnlyCollection<EylemTanimi>(new List<EylemTanimi>(_kimlikle.Values));
            OlayTurleri = new ReadOnlyCollection<string>(new List<string>(turler));
        }

        /// <summary>Kimlikle eylemi bulur; yoksa <c>null</c>.</summary>
        public EylemTanimi? Bul(string id) => _kimlikle.TryGetValue(id, out var e) ? e : null;
    }
}
