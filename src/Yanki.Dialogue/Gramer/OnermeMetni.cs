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
        public static string Uret(string onerme, SablonDosyasi dosya)
        {
            var o = Onerme.Ayristir(onerme);
            if (!dosya.OnermeMetinleri.TryGetValue(o.Yuklem, out var kayit))
            {
                throw new InvalidOperationException($"\"{o.Yuklem}\" yüklemi için onerme_metinleri cümlesi yok (tanımsız yüklem).");
            }

            var sb = new System.Text.StringBuilder();
            foreach (var parca in o.Olumsuz ? kayit.Olumsuz : kayit.Olumlu)
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
                var gorunen = dosya.Adlar.TryGetValue(ham, out var ad) ? ad : new AdKaydi(ham, true, false);
                if (parca.Ek == null)
                {
                    sb.Append(gorunen.Ad);
                }
                else
                {
                    try
                    {
                        sb.Append(TurkceEkler.Uygula(gorunen.Ad, gorunen.OzelAd, parca.Ek, gorunen.Ince));
                    }
                    catch (ArgumentException e)
                    {
                        throw new InvalidOperationException($"\"{onerme}\": {e.Message}");
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>Görünen ad kaydı: tabloda varsa o, yoksa kimliğin kendisi (özel ad varsayılır).</summary>
        public static AdKaydi GorunenAd(string kimlik, SablonDosyasi dosya) =>
            dosya.Adlar.TryGetValue(kimlik, out var ad) ? ad : new AdKaydi(kimlik, true, false);

    }
}
