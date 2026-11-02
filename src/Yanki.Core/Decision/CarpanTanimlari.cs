using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Yanki.Core.Mind;
using Yanki.Core.World;

namespace Yanki.Core.Decision
{
    /// <summary>Çarpan aralıkları (plan 5.5). Kod sabitidir, eylem tanımıyla aşılamaz; tutarlılık ilkesinin sayısal karşılığıdır.</summary>
    public static class CarpanSiniri
    {
        /// <summary>Kişilik çarpanı alt sınırı.</summary>
        public const decimal KisilikAlt = 0.25m;

        /// <summary>Kişilik çarpanı üst sınırı.</summary>
        public const decimal KisilikUst = 2.0m;

        /// <summary>Fikir çarpanı alt sınırı.</summary>
        public const decimal FikirAlt = 0.6m;

        /// <summary>Fikir çarpanı üst sınırı.</summary>
        public const decimal FikirUst = 1.5m;

        /// <summary>Ruh hali çarpanı alt sınırı.</summary>
        public const decimal RuhHaliAlt = 0.8m;

        /// <summary>Ruh hali çarpanı üst sınırı.</summary>
        public const decimal RuhHaliUst = 1.25m;
    }

    /// <summary>Bir kişilik özelliğinin kişilik çarpanındaki ağırlığı. <c>ters</c>: özellik <c>1−v</c> olarak sayılır.</summary>
    public sealed class OzellikAgirligi
    {
        /// <summary>Kişilik özelliği (<see cref="Kisilik.Ozellikler"/> içinden).</summary>
        public string Ozellik { get; }

        /// <summary>Ağırlık, 0'dan büyük.</summary>
        public decimal Agirlik { get; }

        /// <summary>Ters etkili mi.</summary>
        public bool Ters { get; }

        /// <summary>Kurar.</summary>
        /// <exception cref="ArgumentException">Özellik bilinmiyorsa.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Ağırlık 0'dan büyük değilse.</exception>
        public OzellikAgirligi(string ozellik, decimal agirlik, bool ters)
        {
            if (ozellik == null || !((IList<string>)Kisilik.Ozellikler).Contains(ozellik))
            {
                throw new ArgumentException($"Bilinmeyen kişilik özelliği: \"{ozellik}\" ({string.Join(", ", Kisilik.Ozellikler)}).", nameof(ozellik));
            }

            if (agirlik <= 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(agirlik), agirlik, "Ağırlık 0'dan büyük olmalı.");
            }

            Ozellik = ozellik;
            Agirlik = agirlik;
            Ters = ters;
        }
    }

    /// <summary>Kişilik çarpanı tanımı: özelliklerin ağırlıklı ortalaması 0.25–2.0 aralığına eşlenir.</summary>
    public sealed class KisilikCarpaniTanimi
    {
        /// <summary>Özellikler, tanımdaki sırayla; boş değil, tekrarsız.</summary>
        public IReadOnlyList<OzellikAgirligi> Ozellikler { get; }

        /// <summary>Kurar.</summary>
        /// <exception cref="ArgumentException">Liste boşsa ya da bir özellik tekrar ediyorsa.</exception>
        public KisilikCarpaniTanimi(IEnumerable<OzellikAgirligi> ozellikler)
        {
            var liste = new List<OzellikAgirligi>(ozellikler ?? throw new ArgumentNullException(nameof(ozellikler)));
            if (liste.Count == 0)
            {
                throw new ArgumentException("Kişilik çarpanı en az bir özellik içermeli (çarpan istenmiyorsa tanımı hiç yazma).", nameof(ozellikler));
            }

            var gorulen = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var o in liste)
            {
                if (!gorulen.Add(o.Ozellik))
                {
                    throw new ArgumentException($"Kişilik özelliği tekrar ediyor: \"{o.Ozellik}\".", nameof(ozellikler));
                }
            }

            Ozellikler = new ReadOnlyCollection<OzellikAgirligi>(liste);
        }
    }

    /// <summary>Fikir çarpanının yönü: <c>artan</c> hedefi sevdikçe puan artar, <c>azalan</c> hedefi sevmedikçe artar.</summary>
    public enum FikirYonu
    {
        /// <summary>Fikir arttıkça çarpan artar.</summary>
        Artan,

        /// <summary>Fikir azaldıkça çarpan artar.</summary>
        Azalan,
    }

    /// <summary><see cref="FikirYonu"/> ile JSON adları arasında çeviri.</summary>
    public static class FikirYonleri
    {
        /// <summary>Yönün JSON adı.</summary>
        public static string Ad(FikirYonu yon) => yon == FikirYonu.Artan ? "artan" : "azalan";

        /// <summary>Adı yöne çevirir; bilinmiyorsa <c>false</c>.</summary>
        public static bool TryCoz(string ad, out FikirYonu yon)
        {
            switch (ad)
            {
                case "artan": yon = FikirYonu.Artan; return true;
                case "azalan": yon = FikirYonu.Azalan; return true;
                default: yon = FikirYonu.Artan; return false;
            }
        }
    }

    /// <summary>Fikir çarpanı tanımı: <c>fikir(eylem.hedef)</c> −100..100 aralığından 0.6–1.5'e eşlenir.</summary>
    public sealed class FikirCarpaniTanimi
    {
        /// <summary>Yön.</summary>
        public FikirYonu Yon { get; }

        /// <summary>Kurar.</summary>
        public FikirCarpaniTanimi(FikirYonu yon)
        {
            Yon = yon;
        }
    }

    /// <summary>Bir duygunun ruh hali çarpanındaki ağırlığı. <c>ters</c>: duygu şiddeti çarpanı azaltır.</summary>
    public sealed class DuyguAgirligi
    {
        /// <summary>Duygu türü (kimlik).</summary>
        public string Duygu { get; }

        /// <summary>Ağırlık, 0'dan büyük.</summary>
        public decimal Agirlik { get; }

        /// <summary>Ters etkili mi.</summary>
        public bool Ters { get; }

        /// <summary>Kurar.</summary>
        /// <exception cref="ArgumentException">Duygu adı geçerli kimlik değilse.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Ağırlık 0'dan büyük değilse.</exception>
        public DuyguAgirligi(string duygu, decimal agirlik, bool ters)
        {
            if (duygu == null || !Kimlik.GecerliKimlikMi(duygu))
            {
                throw new ArgumentException($"Duygu türü geçersiz kimlik: \"{duygu}\".", nameof(duygu));
            }

            if (agirlik <= 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(agirlik), agirlik, "Ağırlık 0'dan büyük olmalı.");
            }

            Duygu = duygu;
            Agirlik = agirlik;
            Ters = ters;
        }
    }

    /// <summary>Ruh hali çarpanı tanımı: duygu şiddetlerinin işaretli, ağırlıklı toplamı −1..1'e kırpılır ve 0.8–1.25'e eşlenir.</summary>
    public sealed class RuhHaliCarpaniTanimi
    {
        /// <summary>Duygular, tanımdaki sırayla; boş değil, tekrarsız.</summary>
        public IReadOnlyList<DuyguAgirligi> Duygular { get; }

        /// <summary>Kurar.</summary>
        /// <exception cref="ArgumentException">Liste boşsa ya da bir duygu tekrar ediyorsa.</exception>
        public RuhHaliCarpaniTanimi(IEnumerable<DuyguAgirligi> duygular)
        {
            var liste = new List<DuyguAgirligi>(duygular ?? throw new ArgumentNullException(nameof(duygular)));
            if (liste.Count == 0)
            {
                throw new ArgumentException("Ruh hali çarpanı en az bir duygu içermeli (çarpan istenmiyorsa tanımı hiç yazma).", nameof(duygular));
            }

            var gorulen = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var d in liste)
            {
                if (!gorulen.Add(d.Duygu))
                {
                    throw new ArgumentException($"Duygu tekrar ediyor: \"{d.Duygu}\".", nameof(duygular));
                }
            }

            Duygular = new ReadOnlyCollection<DuyguAgirligi>(liste);
        }
    }
}
