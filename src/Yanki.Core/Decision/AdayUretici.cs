using System;
using System.Collections.Generic;
using Yanki.Core.Criteria;
using Yanki.Core.World;

namespace Yanki.Core.Decision
{
    /// <summary>
    /// Aday üretiminin TEK noktası (ADR-011): her eylem için hedefleri bulur, ön koşulları değerlendirir (hepsi tutmalı, ADR-010),
    /// tutmayanı elenen listesine nedeniyle yazar. Yalnızca karar verenin zihni ve kriter sözlüğünün izin verdiği sorgular kullanılır.
    /// 7. haftadaki korumalı bayrak filtresi tam burada, ön koşul değerlendirmesinin yanına takılacak (bu hafta yazılmadı).
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
            var konum = baglam.Dunya.TekDeger("konum!", baglam.Npc);

            foreach (var eylem in eylemler)
            {
                var hedefler = Hedefler(eylem, baglam, konum);
                if (hedefler.Count == 0)
                {
                    elenenler.Add(new ElenenAday(eylem, string.Empty, $"uygun hedef yok ({EylemHedefTurleri.Ad(eylem.Hedef)})"));
                    continue;
                }

                foreach (var hedef in hedefler)
                {
                    var neden = OnKosulSonucu(eylem, hedef, baglam);
                    if (neden == null)
                    {
                        adaylar.Add(new Aday(eylem, hedef));
                    }
                    else
                    {
                        elenenler.Add(new ElenenAday(eylem, hedef, neden));
                    }
                }
            }

            return new AdayUretimi(adaylar, elenenler);
        }

        // İlk tutmayan ön koşulun nedeni; hepsi tutuyorsa null.
        private static string? OnKosulSonucu(EylemTanimi eylem, string hedef, KararBaglami baglam)
        {
            var sorguBaglami = baglam.SorguBaglami(hedef);
            for (var i = 0; i < eylem.OnKosullar.Count; i++)
            {
                var kriter = eylem.OnKosullar[i];
                if (!KriterDegerlendirici.Degerlendir(kriter, baglam.Cozucu, sorguBaglami).Tuttu)
                {
                    return $"on_kosul #{i + 1} tutmadi: {kriter}";
                }
            }

            return null;
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
