using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Yanki.Core.World
{
    /// <summary>
    /// Dünyada nesnel olarak doğru bir önerme: <c>yuklem(arg1, arg2, ...)</c> (plan 5.2). Yüklem adı <c>!</c> ile
    /// biterse gerçek tek değerlidir: anahtar, son argüman hariç her şeydir; yeni değer yazılınca eskisi düşer
    /// (<c>konum!(Ali, han)</c>). Argümanlar ASCII kimlik ya da kanonik sayıdır.
    /// </summary>
    public sealed class Gercek
    {
        /// <summary>Yüklem adı; tek değerliyse sonunda <c>!</c> vardır.</summary>
        public string Yuklem { get; }

        /// <summary>Argümanlar; en az bir tane.</summary>
        public IReadOnlyList<string> Argumanlar { get; }

        /// <summary>Yüklem <c>!</c> ile işaretli mi.</summary>
        public bool TekDegerli => Yuklem[Yuklem.Length - 1] == '!';

        /// <summary>
        /// Tek değerli gerçeklerde, yeni değerin eskisini düşürdüğü anahtar: yüklem ve son argüman hariç tüm argümanlar.
        /// </summary>
        public string TekDegerAnahtari { get; }

        /// <summary>Yeni gerçek kurar.</summary>
        /// <exception cref="ArgumentException">Yüklem ya da bir argüman geçersizse; argüman listesi boşsa.</exception>
        public Gercek(string yuklem, IEnumerable<string> argumanlar)
        {
            if (!Kimlik.GecerliYuklemMi(yuklem))
            {
                throw new ArgumentException($"Geçersiz yüklem adı: \"{yuklem}\" (ASCII kimlik, istenirse sonda tek '!').", nameof(yuklem));
            }

            var liste = new List<string>(argumanlar ?? throw new ArgumentNullException(nameof(argumanlar)));
            if (liste.Count == 0)
            {
                throw new ArgumentException($"\"{yuklem}\" gerçeği en az bir argüman almalı.", nameof(argumanlar));
            }

            foreach (var a in liste)
            {
                if (!Kimlik.GecerliArgumanMi(a))
                {
                    throw new ArgumentException($"Geçersiz gerçek argümanı: \"{a}\" (ASCII kimlik ya da kanonik sayı olmalı).", nameof(argumanlar));
                }
            }

            Yuklem = yuklem;
            Argumanlar = new ReadOnlyCollection<string>(liste);
            TekDegerAnahtari = Yuklem + "(" + string.Join(",", liste.GetRange(0, liste.Count - 1));
        }

        /// <summary>Kanonik metinden gerçek okur: <c>yuklem(arg,arg)</c>. Ayraçların çevresindeki boşluklar yoksayılır.</summary>
        /// <exception cref="ArgumentException">Metin geçerli bir gerçek değilse.</exception>
        public static Gercek Ayristir(string metin)
        {
            if (metin == null)
            {
                throw new ArgumentNullException(nameof(metin));
            }

            var s = metin.Trim();
            var ac = s.IndexOf('(');
            if (ac <= 0 || s[s.Length - 1] != ')')
            {
                throw new ArgumentException($"Geçersiz gerçek: \"{metin}\" (beklenen biçim: yuklem(arg, ...)).", nameof(metin));
            }

            var yuklem = s.Substring(0, ac).Trim();
            var icerik = s.Substring(ac + 1, s.Length - ac - 2);
            var parcalar = icerik.Split(',');
            for (var i = 0; i < parcalar.Length; i++)
            {
                parcalar[i] = parcalar[i].Trim();
            }

            return new Gercek(yuklem, parcalar);
        }

        /// <summary>Kanonik metin: <c>konum!(Ali,han)</c>.</summary>
        public override string ToString() => Yuklem + "(" + string.Join(",", Argumanlar) + ")";
    }
}
