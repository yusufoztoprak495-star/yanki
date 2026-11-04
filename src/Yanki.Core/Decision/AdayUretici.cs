using System;
using System.Collections.Generic;
using Yanki.Core.Brakes;
using Yanki.Core.Criteria;
using Yanki.Core.World;

namespace Yanki.Core.Decision
{
    /// <summary>
    /// Aday üretiminin TEK noktası (ADR-011): her eylem için hedefleri bulur, ön koşulları değerlendirir (hepsi tutmalı, ADR-010),
    /// tutmayanı elenen listesine nedeniyle yazar. Yalnızca karar verenin zihni ve kriter sözlüğünün izin verdiği sorgular kullanılır.
    /// Korumalı bayrak filtresi (7. hafta) ön koşullardan önce çalışır: ihlal eden aday <c>Adaylar</c>'a girmez, nedeni <c>Elenenler</c>'de yazılır.
    /// <para>
    /// Hedef türleri: <c>yok</c> → tek aday (hedef boş); <c>npc</c> → karar verenle aynı mekandaki diğer NPC'ler;
    /// <c>oyuncu</c> → oyuncu, aynı mekandaysa; <c>komsu_mekan</c> → karar verenin mekanına bağlı komşu mekanlar.
    /// Uygun hedefi olmayan eylem tek bir elenen kaydı bırakır (hedefi boş).
    /// </para>
    /// </summary>
    public static class AdayUretici
    {
        /// <summary>Adayları üretir. Dünya ve zihinleri değiştirmez.</summary>
        public static AdayUretimi Uret(KararBaglami baglam, IEnumerable<EylemTanimi> eylemler)
        {
            if (baglam == null)
            {
                throw new ArgumentNullException(nameof(baglam));
            }

            if (eylemler == null)
            {
                throw new ArgumentNullException(nameof(eylemler));
            }

            var adaylar = new List<Aday>();
            var elenenler = new List<ElenenAday>();
            var frenler = new List<FrenBilgisi>();
            var konum = baglam.Dunya.TekDeger("konum!", baglam.Npc);

            foreach (var eylem in eylemler)
            {
                // Önbellek yalnızca bu eylem için bu çağrının içinde yaşar (bu NPC'nin bu karar adımı); hiçbir yere taşınmaz.
                var onbellek = new OnKosulOnbellegi(eylem);
                var hedefler = Hedefler(eylem, baglam, konum);
                if (hedefler.Count == 0)
                {
                    elenenler.Add(new ElenenAday(eylem, string.Empty, $"uygun hedef yok ({EylemHedefTurleri.Ad(eylem.Hedef)})"));
                    continue;
                }

                foreach (var hedef in hedefler)
                {
                    var neden = OnKosulSonucu(eylem, hedef, baglam, onbellek);

                    // Korumalı bayrak filtresi (ön koşullardan önce çalışır, aday adaylar'a hiç girmez): neden her durumda elenen listesinde ve izde
                    // görünür; fren olayı yalnızca ön koşullar (ve konuşmada konu) tutsaydı aday olacaksa yazılır.
                    var ihlal = KorumaIhlali(eylem, hedef, baglam);
                    if (ihlal != null)
                    {
                        var olacakti = neden == null && (!eylem.Konusma || KonuSec(eylem, hedef, baglam).Konular.Count > 0);
                        elenenler.Add(new ElenenAday(eylem, hedef, $"korumali: {ihlal.Value.Varlik} {ihlal.Value.Bayrak}" + (neden == null ? string.Empty : "; " + neden)));
                        if (olacakti)
                        {
                            frenler.Add(new FrenBilgisi(FrenTurleri.Korumali, eylem.Id, baglam.Npc, ihlal.Value.Varlik, konum ?? string.Empty, new long[0]));
                        }

                        continue;
                    }

                    var engel = baglam.Sayaclar?.EylemEngeli(eylem, baglam.Npc, hedef, baglam.Tick);
                    if (engel != null)
                    {
                        // Cooldown/en_fazla filtresi ön koşullardan önce çalışır ama fren olayı yalnızca ön koşullar tutsaydı (ve konuşmada konu
                        // olsaydı) aday olacak eylem için yazılır. Elenme nedeni her durumda görünür.
                        var adayOlacakti = neden == null && (!eylem.Konusma || KonuSec(eylem, hedef, baglam).Konular.Count > 0);
                        var engelNedeni = EngelNedeni(engel, eylem);
                        elenenler.Add(new ElenenAday(eylem, hedef, neden == null ? engelNedeni : engelNedeni + "; " + neden));
                        if (adayOlacakti)
                        {
                            var sonTetik = baglam.Sayaclar!.EylemSonTetiklemeOlayi(eylem, baglam.Npc, hedef);
                            frenler.Add(new FrenBilgisi(
                                engel, eylem.Id, baglam.Npc, hedef, konum ?? string.Empty, sonTetik > 0 ? new[] { sonTetik } : new long[0]));
                        }

                        continue;
                    }

                    if (neden == null)
                    {
                        if (!eylem.Konusma)
                        {
                            adaylar.Add(new Aday(eylem, hedef));
                            continue;
                        }

                        // Konuşma eylemi: anlatılacak konu (ya da yalan) yoksa aday elenir; neden izde görünür.
                        var konu = KonuSec(eylem, hedef, baglam);
                        if (konu.Konular.Count == 0)
                        {
                            elenenler.Add(new ElenenAday(eylem, hedef, KonuYokNedeni(konu)));
                        }
                        else
                        {
                            adaylar.Add(new Aday(eylem, hedef, konu));
                        }
                    }
                    else
                    {
                        elenenler.Add(new ElenenAday(eylem, hedef, neden));
                    }
                }
            }

            return new AdayUretimi(adaylar, elenenler, frenler);
        }

        // Eylemin (yerleşik hareket dahil) bu hedefte ihlal ettiği ilk korumalı bayrak; yoksa null.
        private static (string Varlik, string Bayrak)? KorumaIhlali(EylemTanimi eylem, string hedef, KararBaglami baglam)
        {
            var k = baglam.Korumali;
            if (k == null || k.Bos)
            {
                return null;
            }

            if (eylem.Hareket && k.Korunuyor(baglam.Npc, KorumaBayraklari.MekandanAyrilamaz))
            {
                return (baglam.Npc, KorumaBayraklari.MekandanAyrilamaz);
            }

            foreach (var ib in eylem.IhlalBayraklari)
            {
                var v = ib.Coz(baglam.Npc, hedef, string.Empty, string.Empty);
                if (v.Length > 0 && k.Korunuyor(v, ib.Bayrak))
                {
                    return (v, ib.Bayrak);
                }
            }

            return null;
        }

        private static string EngelNedeni(string engel, EylemTanimi eylem)
        {
            var s = eylem.TetikSiniri!;
            return engel == FrenTurleri.Cooldown
                ? $"cooldown: {s.Cooldown.ToString(System.Globalization.CultureInfo.InvariantCulture)} tick bekleme sürüyor (kapsam {(s.Kapsam == SinirKapsami.Hedef ? "hedef" : "npc")})"
                : $"en_fazla: {s.EnFazla.ToString(System.Globalization.CultureInfo.InvariantCulture)} kez tetiklendi, sınır doldu (kapsam {(s.Kapsam == SinirKapsami.Hedef ? "hedef" : "npc")})";
        }

        private static KonuSecimi KonuSec(EylemTanimi eylem, string hedef, KararBaglami baglam)
        {
            var hesap = baglam.KonuHesabi
                ?? throw new InvalidOperationException($"Eylem \"{eylem.Id}\" konuşma eylemi: karar bağlamında KonuHesabi gerekli.");
            return eylem.Aktarim == AktarimTuru.Dogru
                ? hesap.Dogru(baglam.Npc, hedef, eylem.KonuDesenleri)
                : hesap.Yalan(baglam.Npc, hedef, eylem.YalanOnermeleriniAc(baglam.Npc, hedef), baglam.Seed);
        }

        private static string KonuYokNedeni(KonuSecimi konu)
        {
            if (konu.Elenenler.Count == 0)
            {
                return "konu yok";
            }

            var parcalar = new List<string>(konu.Elenenler.Count);
            foreach (var e in konu.Elenenler)
            {
                parcalar.Add(e.Onerme + ": " + e.Neden);
            }

            return "konu yok (" + string.Join("; ", parcalar) + ")";
        }

        // İlk tutmayan ön koşulun nedeni; hepsi tutuyorsa null.
        private static string? OnKosulSonucu(EylemTanimi eylem, string hedef, KararBaglami baglam, OnKosulOnbellegi onbellek)
        {
            var sorguBaglami = baglam.SorguBaglami(hedef, eylem);
            for (var i = 0; i < eylem.OnKosullar.Count; i++)
            {
                var kriter = eylem.OnKosullar[i];
                bool tuttu;
                if (onbellek.Bagimsiz(i) && onbellek.Var(i))
                {
                    tuttu = onbellek.Sonuc(i);
                }
                else
                {
                    tuttu = KriterDegerlendirici.Degerlendir(kriter, baglam.Cozucu, sorguBaglami).Tuttu;
                    if (onbellek.Bagimsiz(i))
                    {
                        onbellek.Yaz(i, tuttu);
                    }
                }

                if (!tuttu)
                {
                    return $"on_kosul #{i + 1} tutmadi: {kriter}";
                }
            }

            return null;
        }

        /// <summary>
        /// Bir eylemin hedefe bağlı OLMAYAN ön koşullarının (<c>eylem.hedef</c> rolünü ne doğrudan ne önerme içinde kullanan) tek çağrılık sonucu.
        /// Sonuç hedeften bağımsız olduğu için her hedefte aynıdır; ilk gereken anda hesaplanır (kısa devre sırası ve elenme nedeni değişmez).
        /// </summary>
        private sealed class OnKosulOnbellegi
        {
            private readonly bool[] _bagimsiz;
            private readonly bool[] _var;
            private readonly bool[] _sonuc;

            public OnKosulOnbellegi(EylemTanimi eylem)
            {
                var n = eylem.OnKosullar.Count;
                _bagimsiz = new bool[n];
                _var = new bool[n];
                _sonuc = new bool[n];
                for (var i = 0; i < n; i++)
                {
                    _bagimsiz[i] = !HedefeBagli(eylem.OnKosullar[i].Alan);
                }
            }

            public bool Bagimsiz(int i) => _bagimsiz[i];

            public bool Var(int i) => _var[i];

            public bool Sonuc(int i) => _sonuc[i];

            public void Yaz(int i, bool tuttu)
            {
                _var[i] = true;
                _sonuc[i] = tuttu;
            }

            private static bool HedefeBagli(SorguCagrisi cagri)
            {
                foreach (var a in cagri.Argumanlar)
                {
                    if (a is RolArgumani r && r.Rol == Rol.EylemHedef)
                    {
                        return true;
                    }

                    if (a is OnermeArgumani o && HedefeBagli(o.Cagri))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private static List<string> Hedefler(EylemTanimi eylem, KararBaglami baglam, string? konum)
        {
            var sonuc = new List<string>();
            switch (eylem.Hedef)
            {
                case EylemHedefTuru.Yok:
                    sonuc.Add(string.Empty);
                    break;
                case EylemHedefTuru.Npc:
                    if (konum != null)
                    {
                        foreach (var npc in baglam.Dunya.Varliklar(VarlikTuru.Npc))
                        {
                            if (!string.Equals(npc, baglam.Npc, StringComparison.Ordinal)
                                && string.Equals(baglam.Dunya.TekDeger("konum!", npc), konum, StringComparison.Ordinal))
                            {
                                sonuc.Add(npc);
                            }
                        }
                    }

                    break;
                case EylemHedefTuru.Oyuncu:
                    if (konum != null && baglam.OyuncuId.Length > 0
                        && string.Equals(baglam.Dunya.TekDeger("konum!", baglam.OyuncuId), konum, StringComparison.Ordinal))
                    {
                        sonuc.Add(baglam.OyuncuId);
                    }

                    break;
                default:
                    if (konum != null)
                    {
                        sonuc.AddRange(baglam.Dunya.Komsular(konum));
                    }

                    break;
            }

            sonuc.Sort(string.CompareOrdinal);
            return sonuc;
        }
    }
}
