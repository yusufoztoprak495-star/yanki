using System;
using System.Collections.Generic;
using System.IO;
using Yanki.Core.Decision;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;

namespace Yanki.Cli
{
    /// <summary>
    /// Senaryo, eylem ve kural dosyalarını diskten okuyup simülasyon kurar. Çekirdek dosya sistemine dokunmaz; bu iş CLI'dadır.
    /// Kural ve eylem dosyaları senaryonun <c>kural_dosyalari</c> ve <c>eylem_dosyalari</c> listelerindendir ve senaryo dosyasına göre göreli
    /// çözülür. Eylem dosyaları kurallardan ÖNCE okunur: kurallar NPC eylemlerinin olay türlerini <c>ne_zaman</c> olarak kullanabilir.
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

            var eylemler = new List<EylemTanimi>();
            var eylemHatali = false;
            foreach (var ad in senaryo.EylemDosyalari)
            {
                var yol = Path.Combine(klasor, ad);
                if (!File.Exists(yol))
                {
                    hata.WriteLine($"{senaryoYolu}: eylem dosyası bulunamadı: {yol}");
                    eylemHatali = true;
                    continue;
                }

                var sonuc = EylemYukleyici.Yukle(File.ReadAllText(yol), yol);
                if (!sonuc.Basarili)
                {
                    eylemHatali = true;
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

                eylemler.AddRange(sonuc.Eylemler);
            }

            if (eylemHatali)
            {
                return null;
            }

            EylemKatalogu katalog;
            try
            {
                katalog = new EylemKatalogu(eylemler);
            }
            catch (ArgumentException e)
            {
                hata.WriteLine($"{senaryoYolu}: {e.Message}");
                return null;
            }

            var kuralTurleri = new List<string>(senaryo.OlayTurleri);
            kuralTurleri.AddRange(katalog.OlayTurleri);

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

                var sonuc = KuralYukleyici.Yukle(File.ReadAllText(yol), yol, kuralTurleri);
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
                return Simulasyon.Olustur(senaryo, kurallar, seed, eylemler: katalog.Eylemler);
            }
            catch (ArgumentException e)
            {
                hata.WriteLine($"{senaryoYolu}: {e.Message}");
                return null;
            }
        }
    }
}
