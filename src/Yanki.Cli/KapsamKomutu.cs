using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Yanki.Authoring;

namespace Yanki.Cli
{
    /// <summary>
    /// <c>yanki coverage</c> (9. hafta): dizindeki <c>oynanislar/*.json</c> betiklerini okur, <see cref="KapsamHesabi"/> ile koşar, Markdown raporu yazar.
    /// Çıkış kodu: 0 plan 3 kriteri tuttu, 2 tutmadı (hiç tetiklenmeyen kural ya da payı eşiği aşan kural), 1 girdi/kullanım hatası.
    /// </summary>
    internal static class KapsamKomutu
    {
        public const int KriterTutmadi = 2;

        public static int Calistir(string[] args, TextWriter cikti, TextWriter hata)
        {
            string? yol = null;
            string? sablonYolu = null;
            var seeds = KapsamAyarlari.VarsayilanSeedSayisi;
            var tick = KapsamAyarlari.VarsayilanTick;
            for (var i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--seeds":
                    case "--tick":
                        if (i + 1 >= args.Length || !long.TryParse(args[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out var n) || n < 1 || n > int.MaxValue)
                        {
                            hata.WriteLine($"coverage: {args[i]} pozitif bir tam sayı bekler.");
                            return 1;
                        }

                        if (args[i] == "--seeds")
                        {
                            seeds = (int)n;
                        }
                        else
                        {
                            tick = n;
                        }

                        i++;
                        break;
                    case "--sablon":
                        if (i + 1 >= args.Length)
                        {
                            hata.WriteLine("coverage: --sablon bir dosya yolu bekler.");
                            return 1;
                        }

                        sablonYolu = args[++i];
                        break;
                    default:
                        if (args[i].StartsWith("--", StringComparison.Ordinal) || yol != null)
                        {
                            hata.WriteLine($"coverage: bilinmeyen argüman: {args[i]}");
                            return 1;
                        }

                        yol = args[i];
                        break;
                }
            }

            if (yol == null)
            {
                hata.WriteLine("coverage: senaryo dizini gerekli.");
                return 1;
            }

            if (!Directory.Exists(yol))
            {
                hata.WriteLine($"Dizin bulunamadı: {DogrulamaKomutu.Goster(yol)}");
                return 1;
            }

            var girdi = DogrulamaKomutu.GirdiOku(yol, sablonYolu, hata);
            if (girdi == null)
            {
                return 1;
            }

            var betikler = new List<Betik>();
            var betikDizini = Path.Combine(yol, "oynanislar");
            if (Directory.Exists(betikDizini))
            {
                var dosyalar = new List<string>(Directory.GetFiles(betikDizini, "*.json"));
                dosyalar.Sort(StringComparer.Ordinal);
                foreach (var d in dosyalar)
                {
                    try
                    {
                        betikler.Add(BetikOkuyucu.Oku(File.ReadAllText(d), DogrulamaKomutu.Goster(d)));
                    }
                    catch (BetikHatasi e)
                    {
                        hata.WriteLine(e.Message);
                        return 1;
                    }
                }
            }

            try
            {
                var sonuc = KapsamHesabi.Hesapla(girdi, betikler, seeds, tick, new KapsamAyarlari());
                cikti.Write(sonuc.Markdown);
                return sonuc.Basarili ? 0 : KriterTutmadi;
            }
            catch (KapsamGirisHatasi e)
            {
                foreach (var b in e.Bulgular)
                {
                    hata.WriteLine(b.ToString());
                }

                hata.WriteLine("coverage: senaryo hatalı, önce `yanki validate` çalıştırın.");
                return 1;
            }
            catch (BetikHatasi e)
            {
                hata.WriteLine(e.Message);
                return 1;
            }
        }
    }
}
