using System;
using System.Collections.Generic;
using Yanki.Core.Mind;

namespace Yanki.Dialogue.Gramer
{
    /// <summary>
    /// Bir önermeyi (<c>borclu(Mehmet,Ayse,500)</c>) <c>onerme_metinleri</c> tablosundan cümleye çevirir (F kararı). Argüman kimlikleri <c>adlar</c> tablosuyla görünen ada döner
    /// (Ayse → Ayşe); tabloda olmayan kimlik olduğu gibi kullanılır. Sayısal argümana ek getirilemez. Tanımsız yüklem ya da eksik argüman açık hata verir: yuva bağlanamaz,
    /// şablon aday olamaz (yanlış cümle sessizce üretilmez).
    /// </summary>
    internal static class OnermeMetni
    {
        public static string Uret(string onerme, SablonDosyasi dosya, string konusan, string dinleyen)
        {
            var o = Onerme.Ayristir(onerme);
            if (!dosya.OnermeMetinleri.TryGetValue(o.Yuklem, out var kayit))
            {
                throw new InvalidOperationException($"\"{o.Yuklem}\" yüklemi için onerme_metinleri cümlesi yok (tanımsız yüklem).");
            }

            // Kişi zamirleri (DURAK 2): konuşan "ben", dinleyen "sen". Özne konumundaki kişi için yazarın elle yazdığı ben/sen varyantı kullanılır (eklemli yüklem: borçluyum);
            // özne olmayan konumda ekli zamir (bana, seni, benim). Özne bildirilmemiş konumda ek'siz kişi zamiri sessizce yanlış cümle üretirdi: açık hata.
            string? kisiVaryanti = null;
            foreach (var p in kayit.Ozneler)
            {
                if (p >= o.Argumanlar.Count)
                {
                    continue;
                }

                var k = Kisi(o.Argumanlar[p], konusan, dinleyen);
                if (k == null)
                {
                    continue;
                }

                if (kisiVaryanti != null)
                {
                    throw new InvalidOperationException($"\"{onerme}\": konuşan ve dinleyen birlikte özne konumunda; bu kombinasyon için varyant tanımlanmaz (iki kişiyi birlikte anan cümleyi elle yaz).");
                }

                kisiVaryanti = k + ":" + p.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            IReadOnlyList<OnermeParcasi> parcalar;
            if (kisiVaryanti != null)
            {
                var v = kayit.Varyantlar[kisiVaryanti];
                parcalar = o.Olumsuz ? v.Olumsuz : v.Olumlu;
            }
            else
            {
                parcalar = o.Olumsuz ? kayit.Olumsuz : kayit.Olumlu;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var parca in parcalar)
            {
                if (parca.Indeks < 0)
                {
                    sb.Append(parca.Sabit);
                    continue;
                }

                if (parca.Indeks >= o.Argumanlar.Count)
                {
                    throw new InvalidOperationException($"{{{parca.Indeks}}} için argüman yok: \"{onerme}\" {o.Argumanlar.Count} argümanlı.");
                }

                var ham = o.Argumanlar[parca.Indeks];
                var kisi = System.Linq.Enumerable.Contains(kayit.Sayilar, parca.Indeks) ? null : Kisi(ham, konusan, dinleyen);
                if (kisi != null)
                {
                    if (parca.Ek == null)
                    {
                        throw new InvalidOperationException(
                            $"\"{onerme}\": {ham} burada {kisi} (kişi zamiri) ama {{{parca.Indeks}}} ek'siz ve \"ozne\" listesinde değil; yüklem kişiye göre çekimlenmeli (ozne + ben/sen varyantı yaz).");
                    }

                    try
                    {
                        sb.Append(TurkceEkler.Zamir(kisi, parca.Ek));
                    }
                    catch (ArgumentException e)
                    {
                        throw new InvalidOperationException($"\"{onerme}\": {e.Message}");
                    }

                    continue;
                }

                var gorunen = dosya.Adlar.TryGetValue(ham, out var ad) ? ad : new AdKaydi(ham, true, false);
                if (parca.Ek == null)
                {
                    sb.Append(gorunen.Ad);
                }
                else
                {
                    try
                    {
                        sb.Append(TurkceEkler.Uygula(gorunen.Ad, gorunen.OzelAd, parca.Ek, gorunen.Ince, gorunen.Yumusak));
                    }
                    catch (ArgumentException e)
                    {
                        throw new InvalidOperationException($"\"{onerme}\": {e.Message}");
                    }
                }
            }

            return sb.ToString();
        }

        private static string? Kisi(string kimlik, string konusan, string dinleyen)
        {
            if (kimlik.Length == 0)
            {
                return null;
            }

            if (string.Equals(kimlik, konusan, StringComparison.Ordinal))
            {
                return "ben";
            }

            return string.Equals(kimlik, dinleyen, StringComparison.Ordinal) ? "sen" : null;
        }

        /// <summary>Görünen ad kaydı: tabloda varsa o, yoksa kimliğin kendisi (özel ad varsayılır).</summary>
        public static AdKaydi GorunenAd(string kimlik, SablonDosyasi dosya) =>
            dosya.Adlar.TryGetValue(kimlik, out var ad) ? ad : new AdKaydi(kimlik, true, false);

    }
}
