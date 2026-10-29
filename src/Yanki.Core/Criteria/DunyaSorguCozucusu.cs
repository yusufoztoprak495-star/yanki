using System;
using System.Collections.Generic;
using Yanki.Core.World;

namespace Yanki.Core.Criteria
{
    /// <summary>
    /// Yalnızca dünyaya dayanan iki sorgu fonksiyonunu çözer: <c>gercek</c> ve <c>ayni_mekanda</c>
    /// (<c>konum!</c> gerçeklerinden). Zihin, hafıza ve yay tabanlı diğer 13 fonksiyon 3. haftadan sonra gelir;
    /// bu çözücüde <see cref="NotSupportedException"/> verir.
    /// </summary>
    public sealed class DunyaSorguCozucusu : ISorguCozucu
    {
        private readonly Dunya _dunya;

        /// <summary>Verilen dünya üzerinde çalışan çözücü kurar.</summary>
        public DunyaSorguCozucusu(Dunya dunya)
        {
            _dunya = dunya ?? throw new ArgumentNullException(nameof(dunya));
        }

        /// <inheritdoc />
        public SorguDegeri Coz(SorguCagrisi cagri, SorguBaglami baglam)
        {
            if (cagri == null)
            {
                throw new ArgumentNullException(nameof(cagri));
            }

            if (baglam == null)
            {
                throw new ArgumentNullException(nameof(baglam));
            }

            switch (cagri.Ad)
            {
                case "gercek":
                    return SorguDegeri.Mantiksal(GercekVarMi(cagri, baglam));
                case "ayni_mekanda":
                    return SorguDegeri.Mantiksal(AyniMekandaMi(cagri, baglam));
                default:
                    throw new NotSupportedException(
                        $"'{cagri.Ad}' fonksiyonu DunyaSorguCozucusu'nde yok (zihin/hafıza/yay tabanlı fonksiyonlar sonraki haftalarda eklenir).");
            }
        }

        private bool GercekVarMi(SorguCagrisi cagri, SorguBaglami baglam)
        {
            var onerme = ((OnermeArgumani)cagri.Argumanlar[0]).Cagri;
            var args = new List<string?>(onerme.Argumanlar.Count);
            foreach (var a in onerme.Argumanlar)
            {
                switch (a)
                {
                    case TanimlayiciArgumani t: args.Add(t.Ad); break;
                    case SayiArgumani s: args.Add(KanonikYazici.Sayi(s.Deger)); break;
                    case JokerArgumani _: args.Add(null); break;
                    case RolArgumani r: args.Add(baglam.Coz(r.Rol)); break;
                    default: throw new KriterHatasi("Önerme argümanı tanımlayıcı, sayı, joker ya da rol olmalı", a.Konum);
                }
            }

            var desen = new GercekDeseni(onerme.Ad + (onerme.TekDegerli ? "!" : string.Empty), args);
            return _dunya.Eslesen(desen).Count > 0;
        }

        private bool AyniMekandaMi(SorguCagrisi cagri, SorguBaglami baglam)
        {
            var arg = cagri.Argumanlar[0];
            string hedef;
            switch (arg)
            {
                case TanimlayiciArgumani t: hedef = t.Ad; break;
                case RolArgumani r: hedef = baglam.Coz(r.Rol); break;
                default: throw new KriterHatasi("'ayni_mekanda' bir varlık kimliği ya da rol bekler", arg.Konum);
            }

            var benimMekanim = _dunya.TekDeger("konum!", baglam.Coz(Rol.Ben));
            var onunMekani = _dunya.TekDeger("konum!", hedef);
            return benimMekanim != null && onunMekani != null && string.Equals(benimMekanim, onunMekani, StringComparison.Ordinal);
        }
    }
}
