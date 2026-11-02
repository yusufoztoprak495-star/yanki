using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yanki.Core.Decision;
using Yanki.Core.Events;
using Yanki.Core.Simulation;

namespace Yanki.Core.Tests.Simulation
{
    /// <summary>
    /// Test seçicisi: (npc, tick) için önceden belirlenmiş <c>eylem|hedef</c> adayını seçer; <c>null</c> dönerse seçim yapmaz.
    /// Tick döngüsünün mekaniğini (sıra, eş zamanlı algı, hareket) <see cref="UtilitySelector"/> kararlarından bağımsız sınamak içindir.
    /// Seçilecek aday listede yoksa test hatasıdır ve açık mesajla patlar.
    /// </summary>
    internal sealed class SabitSecici : IActionSelector
    {
        private readonly Func<string, long, string?> _secim;

        public SabitSecici(Func<string, long, string?> secim)
        {
            _secim = secim;
        }

        public List<string> Cagrilar { get; } = new List<string>();

        public SecimSonucu Sec(KararBaglami baglam, IReadOnlyList<Aday> adaylar, IReadOnlyList<ElenenAday> elenenler)
        {
            Cagrilar.Add(baglam.Npc + "@" + baglam.Tick);
            var anahtar = _secim(baglam.Npc, baglam.Tick);
            if (anahtar == null)
            {
                return new SecimSonucu(null, new KararIzi(baglam.Npc, baglam.Tick, new SecimAyarlari(), new AdayIzi[0], new string[0], null, null, new string[0], new long[0], new string[0]));
            }

            var aday = adaylar.FirstOrDefault(a => a.Anahtar == anahtar);
            if (aday == null)
            {
                throw new InvalidOperationException(
                    $"{baglam.Npc} tick {baglam.Tick}: '{anahtar}' adayı yok. Adaylar: [{string.Join(", ", adaylar.Select(a => a.Anahtar))}]. Elenenler: [{string.Join("; ", elenenler.Select(e => e.Eylem.Id + "|" + e.Hedef + ": " + e.Neden))}]");
            }

            return new SecimSonucu(aday, KararIzi.Basit(baglam, aday));
        }
    }

    /// <summary>Olay günlüğü karşılaştırmaları için yardımcılar.</summary>
    internal static class GunlukImzasi
    {
        /// <summary>Günlüğün tüm olaylarının kanonik, sıralı özeti: iki çalıştırmanın günlükleri aynıysa metinleri aynıdır.</summary>
        public static string Al(Simulasyon sim)
        {
            var sb = new StringBuilder();
            foreach (var o in sim.Gunluk.Olaylar)
            {
                sb.Append(o.Id).Append('|').Append(o.Tick).Append('|').Append(o.Sira).Append('|').Append(o.Tur).Append('|')
                    .Append(o.Fail).Append('|').Append(o.Hedef).Append('|').Append(o.Mekan).Append('|');
                foreach (var kv in o.Yuk)
                {
                    sb.Append(kv.Key).Append('=').Append(kv.Value).Append(';');
                }

                sb.Append('|').Append(string.Join(",", o.NedenOlaylar)).Append('|').Append(o.KuralId).Append('\n');
            }

            return sb.ToString();
        }

        public static IEnumerable<Olay> Turde(Simulasyon sim, string tur) => sim.Gunluk.Olaylar.Where(o => o.Tur == tur);
    }
}
