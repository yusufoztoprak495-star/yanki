using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Yanki.Core.Mind
{
    /// <summary>Kalıcı ilişki eksenlerinin adları (plan 5.4).</summary>
    public static class Eksenler
    {
        /// <summary>Güven ekseni.</summary>
        public const string Guven = "guven";

        /// <summary>Sevgi ekseni.</summary>
        public const string Sevgi = "sevgi";

        /// <summary>Korku ekseni.</summary>
        public const string Korku = "korku";

        /// <summary>Eksen alt sınırı.</summary>
        public const decimal Alt = -100m;

        /// <summary>Eksen üst sınırı.</summary>
        public const decimal Ust = 100m;

        /// <summary>Üç eksen, fikir dökümündeki sırayla.</summary>
        public static IReadOnlyList<string> Hepsi { get; } = new ReadOnlyCollection<string>(new[] { Guven, Sevgi, Korku });
    }

    /// <summary>Fikir bantları: <c>dusman &lt; −40 ≤ soguk &lt; −10 ≤ notr ≤ 10 &lt; yakin ≤ 40 &lt; dost</c> (plan 5.4).</summary>
    public static class FikirBandi
    {
        /// <summary>Fikir değerinin bandı.</summary>
        public static string Hesapla(decimal fikir)
        {
            if (fikir < -40m)
            {
                return "dusman";
            }

            if (fikir < -10m)
            {
                return "soguk";
            }

            if (fikir <= 10m)
            {
                return "notr";
            }

            return fikir <= 40m ? "yakin" : "dost";
        }
    }

    /// <summary>Fikre tek bir katkı: bir eksenden ya da bir modifier'dan; neden olayıyla birlikte.</summary>
    public sealed class FikirKatkisi
    {
        /// <summary><c>eksen:guven</c> ya da <c>modifier:minnet</c>.</summary>
        public string Kaynak { get; }

        /// <summary>Fikre eklenen değer (eksen için ağırlıklı, modifier için kopya çarpanı uygulanmış).</summary>
        public decimal Deger { get; }

        /// <summary>Katkıyı doğuran olay: modifier için <c>modifier_eklendi</c>, eksen için son <c>eksen_degisti</c>; hiç değişmediyse 0.</summary>
        public long NedenOlayId { get; }

        /// <summary>Modifier'ın bitmesine kalan tick; eksenlerde −1.</summary>
        public long KalanTick { get; }

        internal FikirKatkisi(string kaynak, decimal deger, long nedenOlayId, long kalanTick)
        {
            Kaynak = kaynak;
            Deger = deger;
            NedenOlayId = nedenOlayId;
            KalanTick = kalanTick;
        }
    }

    /// <summary>
    /// "Neden kızgın?" sorusunun cevabı: fikrin her katkısı, sıralı (eksenler güven/sevgi/korku, sonra modifier'lar tür ve başlangıca göre).
    /// <see cref="Toplam"/> katkıların toplamının −100..100'e kırpılmışıdır.
    /// </summary>
    public sealed class FikirDokumu
    {
        /// <summary>Fikri taşıyan NPC.</summary>
        public string Sahip { get; }

        /// <summary>Fikrin konusu.</summary>
        public string Hedef { get; }

        /// <summary>Hesabın yapıldığı tick.</summary>
        public long Tick { get; }

        /// <summary>Katkılar.</summary>
        public IReadOnlyList<FikirKatkisi> Katkilar { get; }

        /// <summary>Kırpılmış toplam fikir.</summary>
        public decimal Toplam { get; }

        internal FikirDokumu(string sahip, string hedef, long tick, IList<FikirKatkisi> katkilar)
        {
            Sahip = sahip;
            Hedef = hedef;
            Tick = tick;
            Katkilar = new ReadOnlyCollection<FikirKatkisi>(new List<FikirKatkisi>(katkilar));
            var toplam = 0m;
            foreach (var k in katkilar)
            {
                toplam += k.Deger;
            }

            Toplam = Math.Max(Eksenler.Alt, Math.Min(Eksenler.Ust, toplam));
        }
    }
}
