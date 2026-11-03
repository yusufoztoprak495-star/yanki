using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Yanki.Core.Criteria;
using Yanki.Core.Mind;

namespace Yanki.Core.Decision
{
    /// <summary>Seçilen adayın girdilerine katkı veren zihin olayları ve kaynağı bulunamayan girdiler.</summary>
    internal sealed class KaynakSonucu
    {
        /// <summary>Kaynak olay kimlikleri, artan ve tekrarsız.</summary>
        public IReadOnlyList<long> Olaylar { get; }

        /// <summary>Kaynağı bulunamayan girdilerin açıklamaları, ordinal sıralı ve tekrarsız.</summary>
        public IReadOnlyList<string> Kaynaksiz { get; }

        public KaynakSonucu(IEnumerable<long> olaylar, IEnumerable<string> kaynaksiz)
        {
            Olaylar = new ReadOnlyCollection<long>(new List<long>(olaylar));
            Kaynaksiz = new ReadOnlyCollection<string>(new List<string>(kaynaksiz));
        }
    }

    /// <summary>
    /// Karar izinin <c>NedenOlaylar</c> kaynakları (ADR-011). SEÇİLEN adayın ön koşul ve ölçüt sorguları ile fikir ve ruh hali
    /// çarpanlarının okuduğu zihin girdilerinden, onlara katkı veren olayları toplar: aktif fikir modifier'ını doğuran olay, eksenin son
    /// değişim olayı, ruh halini doğuran olay, inancın kanıtlarını ekleyen olaylar, hatırlanan olay.
    /// Kaynak olayı olmayan girdi UYDURULMAZ: birikmiş eksen değeri (yalnızca son değişim bilinir) ve stres (süreli sönen, olaya bağlı
    /// tutulmuyor) <see cref="KaynakSonucu.Kaynaksiz"/> içinde raporlanır. Kişilik, senaryodan gelen sabit bir değerdir; olay gerektirmez.
    /// </summary>
    internal static class KaynakToplayici
    {
        public static KaynakSonucu Topla(KararBaglami baglam, Aday aday)
        {
            var olaylar = new SortedSet<long>();
            var kaynaksiz = new SortedSet<string>(StringComparer.Ordinal);
            var sorgu = baglam.SorguBaglami(aday.Hedef, aday.Eylem);
            var eylem = aday.Eylem;

            foreach (var kriter in eylem.OnKosullar)
            {
                Cagri(kriter.Alan, baglam, sorgu, olaylar, kaynaksiz);
            }

            foreach (var olcut in eylem.Olcutler)
            {
                Cagri(olcut.Alan, baglam, sorgu, olaylar, kaynaksiz);
            }

            if (eylem.FikirCarpani != null && aday.Hedef.Length > 0)
            {
                FikirKaynagi(baglam, aday.Hedef, null, olaylar, kaynaksiz);
            }

            if (eylem.RuhHaliCarpani != null)
            {
                foreach (var d in eylem.RuhHaliCarpani.Duygular)
                {
                    DuyguKaynagi(baglam, d.Duygu, olaylar);
                }
            }

            return new KaynakSonucu(olaylar, kaynaksiz);
        }

        private static void Cagri(SorguCagrisi cagri, KararBaglami b, SorguBaglami sorgu, SortedSet<long> olaylar, SortedSet<string> kaynaksiz)
        {
            switch (cagri.Ad)
            {
                case "fikir":
                case "fikir_bandi":
                    FikirKaynagi(b, ZihinSorguCozucusu.Varlik(cagri, 0, sorgu), null, olaylar, kaynaksiz);
                    break;
                case "eksen":
                    FikirKaynagi(b, ZihinSorguCozucusu.Varlik(cagri, 0, sorgu), "eksen:" + ZihinSorguCozucusu.Ad(cagri, 1), olaylar, kaynaksiz);
                    break;
                case "modifier_var":
                    FikirKaynagi(b, ZihinSorguCozucusu.Varlik(cagri, 0, sorgu), "modifier:" + ZihinSorguCozucusu.Ad(cagri, 1), olaylar, kaynaksiz);
                    break;
                case "duygu":
                    DuyguKaynagi(b, ZihinSorguCozucusu.Ad(cagri, 0), olaylar);
                    break;
                case "duygu_hedefi":
                    DuyguKaynagi(b, null, olaylar);
                    break;
                case "stres":
                    if (b.Zihin.Stres(b.Tick) > 0m)
                    {
                        kaynaksiz.Add("stres(): güncel stres süreli sönen bir değerdir, tek bir kaynak olaya bağlı tutulmuyor");
                    }

                    break;
                case "inanc":
                    KanitKaynagi(b, ZihinSorguCozucusu.Desen(cagri, sorgu), null, olaylar);
                    break;
                case "kaynak":
                    KanitKaynagi(b, ZihinSorguCozucusu.Desen(cagri, sorgu), ZihinSorguCozucusu.Varlik(cagri, 1, sorgu), olaylar);
                    break;
                case "ayni_mekanda":
                    // Dünya durumunu okuyan sorgu (konum! gerçekleri; gercek(...) eylemlerde zaten kullanılamaz) tek bir olaya bağlı tutulmaz; uydurulmaz, kaynaksız raporlanır.
                    kaynaksiz.Add($"{cagri.Ad}(...): dünya durumunu okur, tek bir kaynak olaya bağlı tutulmuyor");
                    break;
                case "hafiza_var":
                    HafizaKaynagi(
                        b, ZihinSorguCozucusu.Ad(cagri, 0), ZihinSorguCozucusu.Varlik(cagri, 1, sorgu), ZihinSorguCozucusu.Tam(cagri, 2), olaylar);
                    break;
            }
        }

        // Fikir dökümünün katkıları: süzgeç null ise hepsi, değilse yalnızca o kaynak ("eksen:guven", "modifier:minnet").
        private static void FikirKaynagi(KararBaglami b, string hedef, string? suzgec, SortedSet<long> olaylar, SortedSet<string> kaynaksiz)
        {
            foreach (var katki in b.Zihin.FikirDokumu(hedef, b.Tick).Katkilar)
            {
                if (katki.Kaynak == "kirpma" || (suzgec != null && !string.Equals(katki.Kaynak, suzgec, StringComparison.Ordinal)))
                {
                    continue;
                }

                if (katki.NedenOlayId > 0)
                {
                    olaylar.Add(katki.NedenOlayId);
                }

                if (katki.Kaynak.StartsWith("eksen:", StringComparison.Ordinal) && katki.Deger != 0m)
                {
                    var eksen = katki.Kaynak.Substring("eksen:".Length);
                    kaynaksiz.Add(
                        $"eksen({hedef}, {eksen}): birikmiş değer birden çok olaydan gelmiş olabilir; yalnızca son değişim #{katki.NedenOlayId} kaynak gösterildi");
                }
            }
        }

        // tur null ise baskın duygunun kendisi.
        private static void DuyguKaynagi(KararBaglami b, string? tur, SortedSet<long> olaylar)
        {
            var duygu = b.Zihin.Duygu;
            if (duygu == null || duygu.NedenOlayId <= 0 || b.Zihin.BaskinDuyguSiddeti(b.Tick) <= 0m)
            {
                return;
            }

            if (tur == null || string.Equals(duygu.Tur, tur, StringComparison.Ordinal))
            {
                olaylar.Add(duygu.NedenOlayId);
            }
        }

        // kisi null ise inanılan önermelerin bütün kanıtları; değilse yalnızca o kişiden gelenler.
        private static void KanitKaynagi(KararBaglami b, OnermeDeseni desen, string? kisi, SortedSet<long> olaylar)
        {
            foreach (var metin in b.Zihin.Onermeler)
            {
                if (!desen.Uyar(Onerme.Ayristir(metin)))
                {
                    continue;
                }

                if (kisi == null && !b.Zihin.Inanilan(metin, b.Tick))
                {
                    continue;
                }

                foreach (var kanit in b.Zihin.Kanitlar(metin))
                {
                    if (kisi == null || string.Equals(kanit.Kaynak, kisi, StringComparison.Ordinal))
                    {
                        olaylar.Add(kanit.OlayId);
                    }
                }
            }
        }

        // Zihin.HafizaVar ile aynı süzgeç; eşleşen hafızanın işaret ettiği (hatırlanan) olay kaynak gösterilir.
        private static void HafizaKaynagi(KararBaglami b, string olayTuru, string fail, long sonNTick, SortedSet<long> olaylar)
        {
            foreach (var h in b.Zihin.Hafiza(b.Tick))
            {
                if (!b.Gunluk.Var(h.OlayId))
                {
                    continue;
                }

                var olay = b.Gunluk[h.OlayId];
                if (string.Equals(olay.Tur, olayTuru, StringComparison.Ordinal)
                    && string.Equals(olay.Fail, fail, StringComparison.Ordinal)
                    && b.Tick - olay.Tick <= sonNTick)
                {
                    olaylar.Add(h.OlayId);
                }
            }
        }
    }
}
