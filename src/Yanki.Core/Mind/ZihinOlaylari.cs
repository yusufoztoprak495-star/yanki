using System;
using System.Collections.Generic;
using Yanki.Core.Events;
using Yanki.Core.World;

namespace Yanki.Core.Mind
{
    /// <summary>
    /// Zihin olayları (ADR-001, ADR-008): zihinde olan her değişiklik bir olaydır ve <c>NedenOlaylar</c> ile <c>KuralId</c> taşır.
    /// Sahip NPC her zaman yükteki <c>npc</c> anahtarındadır; <c>Fail</c>/<c>Hedef</c> alanları kullanılmaz.
    /// Sayılar <see cref="KanonikYazici.Sayi"/> ile yazılır (6 basamağa yuvarlanır).
    /// </summary>
    public static class ZihinOlaylari
    {
        /// <summary>Yük: <c>npc</c>, <c>hedef</c>, <c>tur</c>, <c>deger</c>, <c>sure</c>. Başlangıç, olayın tick'idir.</summary>
        public const string ModifierEklendiTuru = "modifier_eklendi";

        /// <summary>Yük: <c>npc</c>, <c>hedef</c>, <c>eksen</c>, <c>delta</c> (eklenir, sonuç −100..100'e kırpılır).</summary>
        public const string EksenDegistiTuru = "eksen_degisti";

        /// <summary>Yük: <c>npc</c>, <c>tur</c>, <c>siddet</c> (0..1), <c>hedef</c> (boş olabilir). Yalnızca daha şiddetliyse baskın olur.</summary>
        public const string DuyguDegistiTuru = "duygu_degisti";

        /// <summary>Yük: <c>npc</c>, <c>delta</c> (sönmüş güncel stresin üzerine eklenir, sonuç 0..100'e kırpılır).</summary>
        public const string StresDegistiTuru = "stres_degisti";

        /// <summary>Yük: <c>npc</c>, <c>onerme</c>, <c>tur</c> (gordu|duydu|cikarim), <c>kaynak</c>, <c>guc</c> (0..1).</summary>
        public const string KanitEklendiTuru = "kanit_eklendi";

        /// <summary>Yük: <c>npc</c>, <c>olay</c> (hatırlanan olayın kimliği), <c>onem</c> (0..1).</summary>
        public const string HafizaEklendiTuru = "hafiza_eklendi";

        /// <summary>Tür bir zihin olayı mı.</summary>
        public static bool ZihinOlayiMi(string tur)
        {
            switch (tur)
            {
                case ModifierEklendiTuru:
                case EksenDegistiTuru:
                case DuyguDegistiTuru:
                case StresDegistiTuru:
                case KanitEklendiTuru:
                case HafizaEklendiTuru:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary><c>modifier_eklendi</c> olayı kurar.</summary>
        public static Olay ModifierEklendi(
            long id, long tick, int sira, string npc, string hedef, string tur, decimal deger, long sure,
            IEnumerable<long> nedenOlaylar, string kuralId, string mekan = "") =>
            Yap(id, tick, sira, ModifierEklendiTuru, nedenOlaylar, kuralId, mekan,
                Y("npc", npc), Y("hedef", hedef), Y("tur", tur), Y("deger", KanonikYazici.Sayi(deger)), Y("sure", Uzun(sure)));

        /// <summary><c>eksen_degisti</c> olayı kurar.</summary>
        public static Olay EksenDegisti(
            long id, long tick, int sira, string npc, string hedef, string eksen, decimal delta,
            IEnumerable<long> nedenOlaylar, string kuralId, string mekan = "") =>
            Yap(id, tick, sira, EksenDegistiTuru, nedenOlaylar, kuralId, mekan,
                Y("npc", npc), Y("hedef", hedef), Y("eksen", eksen), Y("delta", KanonikYazici.Sayi(delta)));

        /// <summary><c>duygu_degisti</c> olayı kurar.</summary>
        public static Olay DuyguDegisti(
            long id, long tick, int sira, string npc, string tur, decimal siddet, string hedef,
            IEnumerable<long> nedenOlaylar, string kuralId, string mekan = "") =>
            Yap(id, tick, sira, DuyguDegistiTuru, nedenOlaylar, kuralId, mekan,
                Y("npc", npc), Y("tur", tur), Y("siddet", KanonikYazici.Sayi(siddet)), Y("hedef", hedef ?? string.Empty));

        /// <summary><c>stres_degisti</c> olayı kurar.</summary>
        public static Olay StresDegisti(
            long id, long tick, int sira, string npc, decimal delta,
            IEnumerable<long> nedenOlaylar, string kuralId, string mekan = "") =>
            Yap(id, tick, sira, StresDegistiTuru, nedenOlaylar, kuralId, mekan,
                Y("npc", npc), Y("delta", KanonikYazici.Sayi(delta)));

        /// <summary><c>kanit_eklendi</c> olayı kurar.</summary>
        public static Olay KanitEklendi(
            long id, long tick, int sira, string npc, string onerme, string tur, string kaynak, decimal guc,
            IEnumerable<long> nedenOlaylar, string kuralId, string mekan = "") =>
            Yap(id, tick, sira, KanitEklendiTuru, nedenOlaylar, kuralId, mekan,
                Y("npc", npc), Y("onerme", onerme), Y("tur", tur), Y("kaynak", kaynak), Y("guc", KanonikYazici.Sayi(guc)));

        /// <summary><c>hafiza_eklendi</c> olayı kurar.</summary>
        public static Olay HafizaEklendi(
            long id, long tick, int sira, string npc, long olayId, decimal onem,
            IEnumerable<long> nedenOlaylar, string kuralId, string mekan = "") =>
            Yap(id, tick, sira, HafizaEklendiTuru, nedenOlaylar, kuralId, mekan,
                Y("npc", npc), Y("olay", Uzun(olayId)), Y("onem", KanonikYazici.Sayi(onem)));

        internal static decimal Sayi(Olay olay, string anahtar)
        {
            var metin = olay.Deger(anahtar);
            if (!decimal.TryParse(metin, System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowDecimalPoint,
                System.Globalization.CultureInfo.InvariantCulture, out var sonuc))
            {
                throw new ArgumentException($"Olay #{olay.Id} ({olay.Tur}) yükündeki \"{anahtar}\" sayı değil: \"{metin}\".");
            }

            return sonuc;
        }

        internal static long Tam(Olay olay, string anahtar)
        {
            var metin = olay.Deger(anahtar);
            if (!long.TryParse(metin, System.Globalization.NumberStyles.AllowLeadingSign,
                System.Globalization.CultureInfo.InvariantCulture, out var sonuc))
            {
                throw new ArgumentException($"Olay #{olay.Id} ({olay.Tur}) yükündeki \"{anahtar}\" tam sayı değil: \"{metin}\".");
            }

            return sonuc;
        }

        private static string Uzun(long deger) => deger.ToString(System.Globalization.CultureInfo.InvariantCulture);

        private static KeyValuePair<string, string> Y(string anahtar, string deger) =>
            new KeyValuePair<string, string>(anahtar, deger);

        private static Olay Yap(
            long id, long tick, int sira, string tur, IEnumerable<long> nedenOlaylar, string kuralId, string mekan,
            params KeyValuePair<string, string>[] yuk) =>
            new Olay(id, tick, sira, tur, string.Empty, string.Empty, mekan, yuk, nedenOlaylar, kuralId);
    }
}
