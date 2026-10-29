using System;
using System.Collections.Generic;
using Yanki.Core.World;

namespace Yanki.Core.Mind
{
    /// <summary>
    /// İnancın konusu: bir <see cref="Gercek"/> ya da onun olumsuzu <c>degil(gercek)</c>. Kanonik yazım
    /// <c>borclu(Mehmet,Ayse,500)</c> ya da <c>degil(borclu(Mehmet,Ayse,500))</c>. <c>degil</c> adı yüklem olarak ayrılmıştır.
    /// </summary>
    public sealed class Onerme
    {
        internal const string OlumsuzOn = "degil(";

        /// <summary>Altındaki gerçek biçimli önerme.</summary>
        public Gercek Gercek { get; }

        /// <summary><c>degil(...)</c> ile mi sarılı.</summary>
        public bool Olumsuz { get; }

        /// <summary>Yüklem adı (tek değerliyse <c>!</c> dahil).</summary>
        public string Yuklem => Gercek.Yuklem;

        /// <summary>Argümanlar.</summary>
        public IReadOnlyList<string> Argumanlar => Gercek.Argumanlar;

        /// <summary>Yüklem tek değerli mi.</summary>
        public bool TekDegerli => Gercek.TekDegerli;

        /// <summary>Yeni önerme.</summary>
        public Onerme(Gercek gercek, bool olumsuz)
        {
            Gercek = gercek ?? throw new ArgumentNullException(nameof(gercek));
            Olumsuz = olumsuz;
        }

        /// <summary>Kanonik metinden önerme okur.</summary>
        /// <exception cref="ArgumentException">Metin geçerli bir önerme değilse (iç içe <c>degil</c> da geçersizdir).</exception>
        public static Onerme Ayristir(string metin)
        {
            if (metin == null)
            {
                throw new ArgumentNullException(nameof(metin));
            }

            var s = metin.Trim();
            if (s.StartsWith(OlumsuzOn, StringComparison.Ordinal) && s[s.Length - 1] == ')')
            {
                return new Onerme(Gercek.Ayristir(s.Substring(OlumsuzOn.Length, s.Length - OlumsuzOn.Length - 1)), true);
            }

            return new Onerme(Gercek.Ayristir(s), false);
        }

        /// <summary>
        /// İki önerme çelişir mi: <c>p</c> ile <c>degil(p)</c>; ya da aynı anahtarlı tek değerli (<c>!</c>) iki olumlu önermenin farklı değerleri.
        /// </summary>
        public static bool Celisir(Onerme a, Onerme b)
        {
            if (a == null)
            {
                throw new ArgumentNullException(nameof(a));
            }

            if (b == null)
            {
                throw new ArgumentNullException(nameof(b));
            }

            var ayniGercek = string.Equals(a.Gercek.ToString(), b.Gercek.ToString(), StringComparison.Ordinal);
            if (a.Olumsuz != b.Olumsuz)
            {
                return ayniGercek;
            }

            return !a.Olumsuz
                && a.TekDegerli
                && b.TekDegerli
                && !ayniGercek
                && string.Equals(a.Gercek.TekDegerAnahtari, b.Gercek.TekDegerAnahtari, StringComparison.Ordinal);
        }

        /// <summary>Kanonik yazım.</summary>
        public override string ToString() => Olumsuz ? OlumsuzOn + Gercek + ")" : Gercek.ToString();
    }

    /// <summary>Önermelerle eşleşen desen: <c>*</c> her argüman konumunda joker olabilir.</summary>
    public sealed class OnermeDeseni
    {
        private readonly GercekDeseni _desen;

        /// <summary>Desen olumsuz önermeleri mi eşler.</summary>
        public bool Olumsuz { get; }

        /// <summary>Yeni desen.</summary>
        public OnermeDeseni(GercekDeseni desen, bool olumsuz)
        {
            _desen = desen ?? throw new ArgumentNullException(nameof(desen));
            Olumsuz = olumsuz;
        }

        /// <summary>Metinden desen okur: <c>borclu(Mehmet,*,*)</c> ya da <c>degil(borclu(*,*,*))</c>.</summary>
        /// <exception cref="ArgumentException">Metin geçerli bir desen değilse.</exception>
        public static OnermeDeseni Ayristir(string metin)
        {
            if (metin == null)
            {
                throw new ArgumentNullException(nameof(metin));
            }

            var s = metin.Trim();
            var olumsuz = s.StartsWith(Onerme.OlumsuzOn, StringComparison.Ordinal) && s[s.Length - 1] == ')';
            if (olumsuz)
            {
                s = s.Substring(Onerme.OlumsuzOn.Length, s.Length - Onerme.OlumsuzOn.Length - 1);
            }

            var ac = s.IndexOf('(');
            if (ac <= 0 || s[s.Length - 1] != ')')
            {
                throw new ArgumentException($"Geçersiz önerme deseni: \"{metin}\" (beklenen biçim: yuklem(arg, *, ...)).", nameof(metin));
            }

            var parcalar = s.Substring(ac + 1, s.Length - ac - 2).Split(',');
            var argumanlar = new List<string?>(parcalar.Length);
            foreach (var p in parcalar)
            {
                var t = p.Trim();
                argumanlar.Add(t == "*" ? null : t);
            }

            return new OnermeDeseni(new GercekDeseni(s.Substring(0, ac).Trim(), argumanlar), olumsuz);
        }

        /// <summary>Önerme bu desene uyuyor mu: olumsuzluk aynı, yüklem ve argüman sayısı aynı, joker olmayan argümanlar eşit.</summary>
        public bool Uyar(Onerme onerme)
        {
            if (onerme == null)
            {
                throw new ArgumentNullException(nameof(onerme));
            }

            return onerme.Olumsuz == Olumsuz && _desen.Uyar(onerme.Gercek);
        }
    }
}
