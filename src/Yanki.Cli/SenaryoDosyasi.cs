using System;
using System.Collections.Generic;
using System.IO;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;

namespace Yanki.Cli
{
    /// <summary>
    /// Senaryo ve kural dosyalarını diskten okuyup simülasyon kurar. Çekirdek dosya sistemine dokunmaz; bu iş CLI'dadır.
    /// Kural dosyaları senaryonun <c>kural_dosyalari</c> listesindendir ve senaryo dosyasına göre göreli çözülür.
    /// </summary>
    internal static class SenaryoDosyasi
    {
        /// <summary>Başarısızlıkta hataları <paramref name="hata"/>'ya yazar ve <c>null</c> döner.</summary>
        public static Simulasyon? Yukle(string senaryoYolu, ulong seed, TextWriter hata)
        {
            if (!File.Exists(senaryoYolu))
            {
                hata.WriteLine($"Dosya bulunamadı: {senaryoYolu}");
                return null;
            }

            var senaryoSonucu = SenaryoYukleyici.Yukle(File.ReadAllText(senaryoYolu), senaryoYolu);
            if (!senaryoSonucu.Basarili)
            {
                foreach (var h in senaryoSonucu.Hatalar)
                {
                    hata.WriteLine(h.ToString());
                }

                return null;
            }

            var senaryo = senaryoSonucu.Senaryo!;
            var klasor = Path.GetDirectoryName(Path.GetFullPath(senaryoYolu))!;
            var kurallar = new List<KuralTanimi>();
            var hatali = false;
            foreach (var ad in senaryo.KuralDosyalari)
            {
                var yol = Path.Combine(klasor, ad);
                if (!File.Exists(yol))
                {
                    hata.WriteLine($"{senaryoYolu}: kural dosyası bulunamadı: {yol}");
                    hatali = true;
                    continue;
                }

                var sonuc = KuralYukleyici.Yukle(File.ReadAllText(yol), yol, senaryo.OlayTurleri);
                if (!sonuc.Basarili)
                {
                    hatali = true;
                    foreach (var h in sonuc.Hatalar)
                    {
                        hata.WriteLine(h.ToString());
                    }

                    continue;
                }

                foreach (var u in sonuc.Uyarilar)
                {
                    hata.WriteLine("uyarı: " + u);
                }

                kurallar.AddRange(sonuc.Kurallar);
            }

            if (hatali)
            {
                return null;
            }

            try
            {
                return Simulasyon.Olustur(senaryo, kurallar, seed);
            }
            catch (ArgumentException e)
            {
                hata.WriteLine($"{senaryoYolu}: {e.Message}");
                return null;
            }
        }
    }
}
