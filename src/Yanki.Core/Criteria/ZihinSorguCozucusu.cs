using System;
using System.Collections.Generic;
using Yanki.Core.Decision;
using Yanki.Core.Events;
using Yanki.Core.Mind;
using Yanki.Core.World;

namespace Yanki.Core.Criteria
{
    /// <summary>
    /// Sözlükteki zihne dayanan fonksiyonları gerçek veriye bağlar: <c>kisilik</c>, <c>eksen</c>, <c>fikir</c>, <c>fikir_bandi</c>,
    /// <c>modifier_var</c>, <c>olay_rolu</c>, <c>yalan_soyledi</c>, <c>duygu</c>, <c>duygu_hedefi</c>, <c>stres</c>, <c>inanc</c>, <c>kaynak</c>, <c>hafiza_var</c>.
    /// <c>gercek</c> ve <c>ayni_mekanda</c> dünya çözücüsüne devredilir. <c>yay</c> (7. hafta) henüz yoktur. "Ben" rolünün zihni sorgulanır; süreli değerler verilen tick'te hesaplanır (ADR-008).
    /// Çözücü tek tick için kurulur; tick ilerleyince yenisi kurulur.
    /// </summary>
    public sealed class ZihinSorguCozucusu : ISorguCozucu
    {
        private readonly DunyaSorguCozucusu _dunyaCozucusu;
        private readonly ZihinDeposu _zihinler;
        private readonly OlayGunlugu _gunluk;
        private readonly long _simdikiTick;
        private readonly DedikoduAyarlari _dedikodu;

        /// <summary>Verilen dünya, zihinler ve günlük üzerinde, <paramref name="simdikiTick"/> anı için çözücü kurar.</summary>
        public ZihinSorguCozucusu(Dunya dunya, ZihinDeposu zihinler, OlayGunlugu gunluk, long simdikiTick, DedikoduAyarlari? dedikodu = null)
        {
            _dunyaCozucusu = new DunyaSorguCozucusu(dunya ?? throw new ArgumentNullException(nameof(dunya)));
            _zihinler = zihinler ?? throw new ArgumentNullException(nameof(zihinler));
            _gunluk = gunluk ?? throw new ArgumentNullException(nameof(gunluk));
            _simdikiTick = simdikiTick;
            _dedikodu = dedikodu ?? new DedikoduAyarlari();
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
                case "ayni_mekanda":
                    return _dunyaCozucusu.Coz(cagri, baglam);
                case "olay_rolu":
                    return SorguDegeri.Mantiksal(OlayRolundeMi(Ad(cagri, 0), baglam));
                case "olay_konusu":
                    return SorguDegeri.Mantiksal(OlayKonusuMu(cagri, baglam));
                case "yay":
                    throw new NotSupportedException("'yay' yay değişkenleri gerektirir; 7. haftada eklenecek.");
            }

            var ben = _zihinler[baglam.Coz(Rol.Ben)];
            switch (cagri.Ad)
            {
                case "kisilik":
                    return SorguDegeri.Sayi(ben.Kisilik.Deger(Ad(cagri, 0)));
                case "eksen":
                    return SorguDegeri.Sayi(ben.Eksen(Varlik(cagri, 0, baglam), Ad(cagri, 1)));
                case "fikir":
                    return SorguDegeri.Sayi(ben.Fikir(Varlik(cagri, 0, baglam), _simdikiTick));
                case "fikir_bandi":
                    return SorguDegeri.Metin(ben.FikirBandi(Varlik(cagri, 0, baglam), _simdikiTick));
                case "modifier_var":
                    return SorguDegeri.Mantiksal(ben.ModifierVar(Varlik(cagri, 0, baglam), Ad(cagri, 1), _simdikiTick));
                case "duygu":
                    return SorguDegeri.Sayi(ben.DuyguSiddeti(Ad(cagri, 0), _simdikiTick));
                case "duygu_hedefi":
                    return SorguDegeri.Metin(ben.BaskinDuyguSiddeti(_simdikiTick) > 0m && ben.Duygu != null ? ben.Duygu.Hedef : string.Empty);
                case "stres":
                    return SorguDegeri.Sayi(ben.Stres(_simdikiTick));
                case "inanc":
                    return SorguDegeri.Sayi(ben.InancDerecesi(Desen(cagri, baglam), _simdikiTick));
                case "konu_puani":
                    if (baglam.KonuDesenleri == null)
                    {
                        throw new InvalidOperationException("'konu_puani' yalnızca aktarim: dogru eyleminin ön koşul/ölçütlerinde çözülür (konu havuzu eylemden gelir).");
                    }

                    return SorguDegeri.Sayi(
                        new KonuHesabi(_zihinler, _gunluk, _simdikiTick, _dedikodu)
                            .EnYuksekKonuPuani(baglam.Coz(Rol.Ben), Varlik(cagri, 0, baglam), baglam.KonuDesenleri));
                case "yalan_soyledi":
                    return SorguDegeri.Mantiksal(ben.YalanSoyledi(Varlik(cagri, 0, baglam)));
                case "kaynak":
                    return SorguDegeri.Mantiksal(ben.KaynakVar(Desen(cagri, baglam), Varlik(cagri, 1, baglam)));
                case "hafiza_var":
                    return SorguDegeri.Mantiksal(ben.HafizaVar(_gunluk, Ad(cagri, 0), Varlik(cagri, 1, baglam), Tam(cagri, 2), _simdikiTick));
                default:
                    throw new NotSupportedException($"'{cagri.Ad}' fonksiyonu ZihinSorguCozucusu'nde yok.");
            }
        }

        // olay_konusu: yalnızca sonuç kuralı bağlamında; mevcut olay anlatti ise, "ben" olayın hedefi (içeriği duyan dinleyen) ise ve
        // anlatılan önerme desene uyuyorsa doğru. Konuşan ve tanık için her zaman yanlıştır (kulak misafirliği yok).
        private static bool OlayKonusuMu(SorguCagrisi cagri, SorguBaglami baglam)
        {
            if (baglam.OlayOnermesi.Length == 0 || !baglam.Tanimli(Rol.OlayHedef))
            {
                return false;
            }

            if (!string.Equals(baglam.Coz(Rol.Ben), baglam.Coz(Rol.OlayHedef), StringComparison.Ordinal))
            {
                return false;
            }

            return Desen(cagri, baglam).Uyar(Onerme.Ayristir(baglam.OlayOnermesi));
        }

        // "Ben" mevcut olayda ne: fail (olayı yapan), hedef (olayın yöneldiği) ya da tanık (olayı algılayan ama ikisi de olmayan).
        // Olay bağlamı (olay.fail) yoksa hiçbir rol doğru değildir.
        private static bool OlayRolundeMi(string rol, SorguBaglami baglam)
        {
            if (!baglam.Tanimli(Rol.OlayFail))
            {
                return false;
            }

            var ben = baglam.Coz(Rol.Ben);
            var fail = string.Equals(ben, baglam.Coz(Rol.OlayFail), StringComparison.Ordinal);
            var hedef = baglam.Tanimli(Rol.OlayHedef) && string.Equals(ben, baglam.Coz(Rol.OlayHedef), StringComparison.Ordinal);
            switch (rol)
            {
                case "fail": return fail;
                case "hedef": return hedef;
                default: return !fail && !hedef;
            }
        }

        internal static string Ad(SorguCagrisi cagri, int indeks)
        {
            var arg = cagri.Argumanlar[indeks];
            if (arg is TanimlayiciArgumani t)
            {
                return t.Ad;
            }

            throw new KriterHatasi($"'{cagri.Ad}' fonksiyonunun {indeks + 1}. argümanı bir ad olmalı", arg.Konum);
        }

        internal static string Varlik(SorguCagrisi cagri, int indeks, SorguBaglami baglam)
        {
            var arg = cagri.Argumanlar[indeks];
            switch (arg)
            {
                case TanimlayiciArgumani t: return t.Ad;
                case RolArgumani r: return baglam.Coz(r.Rol);
                default: throw new KriterHatasi($"'{cagri.Ad}' fonksiyonunun {indeks + 1}. argümanı bir varlık kimliği ya da rol olmalı", arg.Konum);
            }
        }

        internal static long Tam(SorguCagrisi cagri, int indeks)
        {
            var arg = cagri.Argumanlar[indeks];
            if (arg is SayiArgumani s && s.Deger == decimal.Truncate(s.Deger))
            {
                return (long)s.Deger;
            }

            throw new KriterHatasi($"'{cagri.Ad}' fonksiyonunun {indeks + 1}. argümanı tam sayı olmalı", arg.Konum);
        }

        internal static OnermeDeseni Desen(SorguCagrisi cagri, SorguBaglami baglam)
        {
            var onerme = ((OnermeArgumani)cagri.Argumanlar[0]).Cagri;
            var olumsuz = false;
            if (onerme.Ad == "degil" && !onerme.TekDegerli)
            {
                olumsuz = true;
                onerme = ((OnermeArgumani)onerme.Argumanlar[0]).Cagri;
            }

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

            return new OnermeDeseni(new GercekDeseni(onerme.Ad + (onerme.TekDegerli ? "!" : string.Empty), args), olumsuz);
        }
    }
}
