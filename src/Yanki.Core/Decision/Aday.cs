using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Yanki.Core.Decision
{
    /// <summary>
    /// Karar adayı: (eylem, hedef). Hedef NPC ya da oyuncu kimliği, komşu mekan kimliği ya da hedefsiz eylemde boş metindir.
    /// </summary>
    public sealed class Aday
    {
        /// <summary>Eylem tanımı.</summary>
        public EylemTanimi Eylem { get; }

        /// <summary>Hedef varlık/mekan kimliği; hedefsiz eylemde boş.</summary>
        public string Hedef { get; }

        /// <summary><c>eylem|hedef</c>: aday kimliği ve ordinal sıralama anahtarı.</summary>
        public string Anahtar => Eylem.Id + "|" + Hedef;

        /// <summary>Aday kurar.</summary>
        public Aday(EylemTanimi eylem, string hedef)
        {
            Eylem = eylem ?? throw new ArgumentNullException(nameof(eylem));
            Hedef = hedef ?? string.Empty;
        }
    }

    /// <summary>Elenen aday ve nedeni (karar izinde görünür). Uygun hedefi olmayan eylemde <see cref="Hedef"/> boştur.</summary>
    public sealed class ElenenAday
    {
        /// <summary>Eylem tanımı.</summary>
        public EylemTanimi Eylem { get; }

        /// <summary>Elenen hedef; eylemin hiç uygun hedefi yoksa boş.</summary>
        public string Hedef { get; }

        /// <summary>İnsan okuyabilir neden.</summary>
        public string Neden { get; }

        /// <summary>Kurar.</summary>
        public ElenenAday(EylemTanimi eylem, string hedef, string neden)
        {
            Eylem = eylem ?? throw new ArgumentNullException(nameof(eylem));
            Hedef = hedef ?? string.Empty;
            Neden = neden ?? string.Empty;
        }
    }

    /// <summary>Aday üretiminin sonucu: geçerli adaylar ve elenenler, ikisi de eylem kimliği sonra hedef, ordinal sıralı.</summary>
    public sealed class AdayUretimi
    {
        /// <summary>Ön koşulları tutan adaylar.</summary>
        public IReadOnlyList<Aday> Adaylar { get; }

        /// <summary>Elenenler ve nedenleri.</summary>
        public IReadOnlyList<ElenenAday> Elenenler { get; }

        /// <summary>Kurar; listeleri sıralar.</summary>
        public AdayUretimi(IEnumerable<Aday> adaylar, IEnumerable<ElenenAday> elenenler)
        {
            var a = new List<Aday>(adaylar ?? throw new ArgumentNullException(nameof(adaylar)));
            a.Sort((x, y) => Karsilastir(x.Eylem.Id, x.Hedef, y.Eylem.Id, y.Hedef));
            var e = new List<ElenenAday>(elenenler ?? throw new ArgumentNullException(nameof(elenenler)));
            e.Sort((x, y) => Karsilastir(x.Eylem.Id, x.Hedef, y.Eylem.Id, y.Hedef));
            Adaylar = new ReadOnlyCollection<Aday>(a);
            Elenenler = new ReadOnlyCollection<ElenenAday>(e);
        }

        /// <summary>Sabit sıra: eylem kimliği, sonra hedef; ikisi de ordinal.</summary>
        public static int Karsilastir(string eylemA, string hedefA, string eylemB, string hedefB)
        {
            var c = string.CompareOrdinal(eylemA, eylemB);
            return c != 0 ? c : string.CompareOrdinal(hedefA, hedefB);
        }
    }
}
