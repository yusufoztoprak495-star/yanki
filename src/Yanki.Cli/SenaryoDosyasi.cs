using System;
using System.Collections.Generic;
using System.IO;
using Yanki.Core.Decision;
using Yanki.Core.Mind;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;

namespace Yanki.Cli
{
    /// <summary>
    /// Senaryo, eylem ve kural dosyalarını diskten okuyup simülasyon kurar. Çekirdek dosya sistemine dokunmaz; bu iş CLI'dadır.
    /// Kural ve eylem dosyaları senaryonun <c>kural_dosyalari</c> ve <c>eylem_dosyalari</c> listelerindendir ve senaryo dosyasına göre göreli
    /// çözülür. Eylem ve kriz dosyaları kurallardan ÖNCE okunur: kurallar NPC eylemlerinin ve krizlerin olay türlerini <c>ne_zaman</c> olarak kullanabilir.
    /// </summary>
    internal static class SenaryoDosyasi
    {
        /// <summary>
        /// Senaryo girdilerinin hash'i (<c>senaryo_hash</c>): senaryo, kural, eylem ve kriz dosyalarının metinleri, senaryodaki sırayla.
        /// Dosya okunamıyorsa (ya da senaryo geçersizse) <c>null</c>; asıl hata <see cref="Yukle"/> tarafından raporlanır.
        /// </summary>
        public static string? Hash(string senaryoYolu)
        {
            if (!File.Exists(senaryoYolu))
            {
                return null;
            }

            var metin = File.ReadAllText(senaryoYolu);
            var sonuc = SenaryoYukleyici.Yukle(metin, senaryoYolu);
            if (!sonuc.Basarili)
            {
                return null;
            }

            var klasor = Path.GetDirectoryName(Path.GetFullPath(senaryoYolu))!;
            var metinler = new List<string> { metin };
            foreach (var liste in new[] { sonuc.Senaryo!.KuralDosyalari, sonuc.Senaryo.EylemDosyalari, sonuc.Senaryo.KrizDosyalari })
            {
                foreach (var ad in liste)
                {
                    var yol = Path.Combine(klasor, ad);
                    if (!File.Exists(yol))
                    {
                        return null;
                    }

                    metinler.Add(File.ReadAllText(yol));
                }
            }

            return SenaryoHashu.Hesapla(metinler);
        }

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

            var krizler = new List<KrizSatiri>();
            var krizHatali = false;
            foreach (var ad in senaryo.KrizDosyalari)
            {
                var yol = Path.Combine(klasor, ad);
                if (!File.Exists(yol))
                {
                    hata.WriteLine($"{senaryoYolu}: kriz dosyası bulunamadı: {yol}");
                    krizHatali = true;
                    continue;
                }

                // Yedek satır denetimi dosyalar birleştikten sonra yapılır (seviye başına bir yedek, hangi dosyada olursa olsun).
                var sonuc = KrizYukleyici.Yukle(File.ReadAllText(yol), yol, new StresAyarlari().Esikler.Count, yedekDenetle: false);
                if (!sonuc.Basarili)
                {
                    krizHatali = true;
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

                krizler.AddRange(sonuc.Satirlar);
            }

            if (!krizHatali && krizler.Count > 0)
            {
                foreach (var h in KrizYukleyici.YedekHatalari(krizler, new StresAyarlari().Esikler.Count, senaryoYolu))
                {
                    hata.WriteLine(h.ToString());
                    krizHatali = true;
                }
            }

            if (krizHatali)
            {
                return null;
            }

            var kuralTurleri = new List<string>(senaryo.OlayTurleri);
            kuralTurleri.AddRange(katalog.OlayTurleri);
            foreach (var k in krizler)
            {
                kuralTurleri.Add(k.OlayTuru);
            }

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
                return Simulasyon.Olustur(senaryo, kurallar, seed, eylemler: katalog.Eylemler, krizler: krizler);
            }
            catch (ArgumentException e)
            {
                hata.WriteLine($"{senaryoYolu}: {e.Message}");
                return null;
            }
        }
    }
}
