using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Yanki.Core.Events;

namespace Yanki.Core.Simulation
{
    /// <summary>
    /// "Neden?" zincirini okunur satırlara çevirir (<c>yanki why</c>): önce olayın kendisi, altında girintili olarak
    /// <see cref="Olay.NedenOlaylar"/> ve onların nedenleri, kök nedene (oyuncu eylemi) kadar. Birden fazla yoldan
    /// erişilen olay yalnızca ilk görüldüğü yerde açılır; sonrakilerde <c>(yukarıda)</c> yazar.
    /// </summary>
    public static class NedenYazici
    {
        private const string Girinti = "   ";

        /// <summary>Zinciri satır satır döndürür.</summary>
        /// <exception cref="ArgumentOutOfRangeException">Olay günlükte yoksa.</exception>
        public static IReadOnlyList<string> Yaz(OlayGunlugu gunluk, long olayId)
        {
            if (gunluk == null)
            {
                throw new ArgumentNullException(nameof(gunluk));
            }

            var satirlar = new List<string>();
            var gorulen = new SortedSet<long>();
            Ac(gunluk, gunluk[olayId], 0, satirlar, gorulen);
            return new ReadOnlyCollection<string>(satirlar);
        }

        /// <summary>Tek olayın okunur özeti: <c>#12 modifier_eklendi (tick 3, kural yardim_minneti) ...</c>.</summary>
        public static string Ozet(Olay olay)
        {
            if (olay == null)
            {
                throw new ArgumentNullException(nameof(olay));
            }

            var sb = new StringBuilder();
            sb.Append('#').Append(olay.Id.ToString(CultureInfo.InvariantCulture)).Append(' ').Append(olay.Tur)
                .Append(" (tick ").Append(olay.Tick.ToString(CultureInfo.InvariantCulture))
                .Append(", kural ").Append(olay.KuralId).Append(')');

            if (olay.Fail.Length > 0 || olay.Hedef.Length > 0)
            {
                sb.Append(' ').Append(olay.Fail.Length > 0 ? olay.Fail : "?");
                if (olay.Hedef.Length > 0)
                {
                    sb.Append(" → ").Append(olay.Hedef);
                }
            }

            if (olay.Mekan.Length > 0)
            {
                sb.Append(" @").Append(olay.Mekan);
            }

            foreach (var kv in olay.Yuk)
            {
                sb.Append(' ').Append(kv.Key).Append('=').Append(kv.Value);
            }

            return sb.ToString();
        }

        private static void Ac(OlayGunlugu gunluk, Olay olay, int derinlik, List<string> satirlar, SortedSet<long> gorulen)
        {
            var onek = derinlik == 0 ? string.Empty : Tekrarla(Girinti, derinlik - 1) + "← ";
            if (!gorulen.Add(olay.Id))
            {
                satirlar.Add(onek + "#" + olay.Id.ToString(CultureInfo.InvariantCulture) + " " + olay.Tur + " (yukarıda)");
                return;
            }

            satirlar.Add(onek + Ozet(olay));
            foreach (var nedenId in olay.NedenOlaylar)
            {
                Ac(gunluk, gunluk[nedenId], derinlik + 1, satirlar, gorulen);
            }
        }

        private static string Tekrarla(string s, int n)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < n; i++)
            {
                sb.Append(s);
            }

            return sb.ToString();
        }
    }
}
