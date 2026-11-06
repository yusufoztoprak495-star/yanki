using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Yanki.Authoring;
using Yanki.Core.Simulation;

namespace Yanki.Cli
{
    /// <summary>
    /// <c>yanki validate</c> (9. hafta): dosyaları okur, <see cref="SenaryoDogrulayici"/>'ya metin olarak verir. Doğrulama mantığı CLI'da değil
    /// <c>Yanki.Authoring</c>'dedir (10. haftada demo da kullanabilsin diye). Çıkış kodu: 0 temiz, 1 hata var (ya da <c>--uyari-hata</c> ile uyarı var ya da kullanım hatası).
    /// </summary>
    internal static class DogrulamaKomutu
    {
        public const string VarsayilanSablon = "sablonlar.json";
        public const string VarsayilanMenu = "oyuncu_eylemleri.json";

        public static int Calistir(string[] args, TextWriter cikti, TextWriter hata)
        {
            string? yol = null;
            string? sablonYolu = null;
            var uyariHata = false;
            for (var i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--sablon":
                        if (i + 1 >= args.Length)
                        {
                            hata.WriteLine("validate: --sablon bir dosya yolu bekler.");
                            return 1;
                        }

                        sablonYolu = args[++i];
                        break;
                    case "--uyari-hata":
                        uyariHata = true;
                        break;
                    default:
                        if (args[i].StartsWith("--", StringComparison.Ordinal) || yol != null)
                        {
                            hata.WriteLine($"validate: bilinmeyen argüman: {args[i]}");
                            return 1;
                        }

                        yol = args[i];
                        break;
                }
            }

            if (yol == null)
            {
                hata.WriteLine("validate: senaryo dizini ya da scenario.json gerekli.");
                return 1;
            }

            var girdi = GirdiOku(yol, sablonYolu, hata);
            if (girdi == null)
            {
                return 1;
            }

            var sonuc = SenaryoDogrulayici.Dogrula(girdi);
            foreach (var b in sonuc.Bulgular)
            {
                cikti.WriteLine(b.ToString());
            }

            cikti.WriteLine(sonuc.HataSayisi.ToString(CultureInfo.InvariantCulture) + " hata, " + sonuc.UyariSayisi.ToString(CultureInfo.InvariantCulture) + " uyarı.");
            return sonuc.HataSayisi > 0 || (uyariHata && sonuc.UyariSayisi > 0) ? 1 : 0;
        }

        /// <summary>Senaryo dizini ya da dosyasından doğrulama girdisini okur. Okunamazsa mesajı <paramref name="hata"/>'ya yazar ve <c>null</c> döner.</summary>
        public static DogrulamaGirdisi? GirdiOku(string yol, string? sablonYolu, TextWriter hata)
        {
            string senaryoYolu;
            if (Directory.Exists(yol))
            {
                senaryoYolu = Path.Combine(yol, "scenario.json");
            }
            else
            {
                senaryoYolu = yol;
            }

            if (!File.Exists(senaryoYolu))
            {
                hata.WriteLine($"Dosya bulunamadı: {Goster(senaryoYolu)}");
                return null;
            }

            var klasor = Path.GetDirectoryName(Path.GetFullPath(senaryoYolu))!;
            var gorunenKlasor = Path.GetDirectoryName(senaryoYolu) ?? string.Empty;
            var metin = File.ReadAllText(senaryoYolu);

            var dosyalar = new List<DosyaGirdisi>();
            var yukleme = SenaryoYukleyici.Yukle(metin, Goster(senaryoYolu));
            if (yukleme.Basarili)
            {
                var s = yukleme.Senaryo!;
                var adlar = new List<string>();
                adlar.AddRange(s.EylemDosyalari);
                adlar.AddRange(s.KrizDosyalari);
                adlar.AddRange(s.KuralDosyalari);
                foreach (var ad in adlar)
                {
                    var tam = Path.Combine(klasor, ad);
                    if (File.Exists(tam))
                    {
                        dosyalar.Add(new DosyaGirdisi(ad, Goster(Path.Combine(gorunenKlasor, ad)), File.ReadAllText(tam)));
                    }
                }
            }

            DosyaGirdisi? sablon = null;
            if (sablonYolu != null)
            {
                if (!File.Exists(sablonYolu))
                {
                    hata.WriteLine($"Dosya bulunamadı: {Goster(sablonYolu)}");
                    return null;
                }

                sablon = new DosyaGirdisi(Path.GetFileName(sablonYolu), Goster(sablonYolu), File.ReadAllText(sablonYolu));
            }
            else if (File.Exists(Path.Combine(klasor, VarsayilanSablon)))
            {
                sablon = new DosyaGirdisi(VarsayilanSablon, Goster(Path.Combine(gorunenKlasor, VarsayilanSablon)), File.ReadAllText(Path.Combine(klasor, VarsayilanSablon)));
            }

            DosyaGirdisi? menu = null;
            if (File.Exists(Path.Combine(klasor, VarsayilanMenu)))
            {
                menu = new DosyaGirdisi(VarsayilanMenu, Goster(Path.Combine(gorunenKlasor, VarsayilanMenu)), File.ReadAllText(Path.Combine(klasor, VarsayilanMenu)));
            }

            return new DogrulamaGirdisi(Goster(senaryoYolu), metin, dosyalar, sablon, menu);
        }

        internal static string Goster(string yol) => yol.Replace((char)92, (char)47);
    }
}
