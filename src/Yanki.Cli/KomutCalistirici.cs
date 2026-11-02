using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Yanki.Core;
using Yanki.Core.Decision;
using Yanki.Core.Events;
using Yanki.Core.Simulation;

namespace Yanki.Cli
{
    /// <summary>
    /// Komut satırı komutları. Çıkış kodları: 0 başarılı, 1 kullanım hatası, 2 okuma/doğrulama/çalıştırma hatası,
    /// 3 replay hash'i tutmadı, 4 yeniden üretilen karar izi günlükteki kısa izle uyuşmadı. Dosya sistemi erişimi yalnızca burada ve <see cref="SenaryoDosyasi"/>'ndadır; çekirdek metin alır.
    /// </summary>
    internal static class KomutCalistirici
    {
        public const int Basarili = 0;
        public const int KullanimHatasi = 1;
        public const int DosyaHatasi = 2;
        public const int HashUyusmadi = 3;
        public const int IzUyusmadi = 4;

        private const ulong VarsayilanSeed = 42;

        public static int Calistir(string[] args, TextWriter cikti, TextWriter hata)
        {
            if (args.Length == 0)
            {
                cikti.WriteLine($"{YankiInfo.Name} {Surum()}");
                KullanimYaz(cikti);
                return Basarili;
            }

            switch (args[0])
            {
                case "--version":
                case "-v":
                    cikti.WriteLine($"yanki {Surum()}");
                    return Basarili;
                case "run":
                    return Run(args, cikti, hata);
                case "replay":
                    return Replay(args, cikti, hata);
                case "query":
                    return Query(args, cikti, hata);
                case "why":
                    return Why(args, cikti, hata);
                default:
                    hata.WriteLine($"Bilinmeyen komut: {args[0]}");
                    KullanimYaz(hata);
                    return KullanimHatasi;
            }
        }

        private static string Surum() =>
            typeof(YankiInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "bilinmiyor";

        private static void KullanimYaz(TextWriter w)
        {
            w.WriteLine("Kullanım:");
            w.WriteLine("  yanki run <senaryo.json> [--seed N] [--eylem tur[:hedef[:anahtar=deger,...]]] [--tick N] ... [--kaydet oturum.json] [--hepsi]");
            w.WriteLine("      oyuncu hareketi: --eylem hareket:<komsu_mekan>");
            w.WriteLine("  yanki replay <oturum.json> [--guncelle]");
            w.WriteLine("  yanki query <oturum.json> <npc>      (NPC durumu ve son karar izi)");
            w.WriteLine("  yanki why <oturum.json> <olayId>     (nedensellik zinciri; karar olayında adaylar ve puan bileşenleri)");
            w.WriteLine("  yanki --version");
        }

        // ---- run ----

        private static int Run(string[] args, TextWriter cikti, TextWriter hata)
        {
            if (args.Length < 2 || args[1].StartsWith("--", StringComparison.Ordinal))
            {
                hata.WriteLine("run: senaryo dosyası gerekli.");
                KullanimYaz(hata);
                return KullanimHatasi;
            }

            var senaryoYolu = args[1];
            var seed = VarsayilanSeed;
            string? kaydet = null;
            var hepsi = false;
            var adimlar = new List<KeyValuePair<string, string>>();

            for (var i = 2; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--seed":
                        if (i + 1 >= args.Length || !ulong.TryParse(args[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out seed))
                        {
                            hata.WriteLine("--seed negatif olmayan bir tam sayı ister.");
                            return KullanimHatasi;
                        }

                        i++;
                        break;
                    case "--eylem":
                    case "--tick":
                        if (i + 1 >= args.Length)
                        {
                            hata.WriteLine($"{args[i]} bir değer ister.");
                            return KullanimHatasi;
                        }

                        if (args[i] == "--tick" && (!int.TryParse(args[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out var n) || n < 1))
                        {
                            hata.WriteLine("--tick 1 ya da daha büyük bir tam sayı ister.");
                            return KullanimHatasi;
                        }

                        adimlar.Add(new KeyValuePair<string, string>(args[i], args[i + 1]));
                        i++;
                        break;
                    case "--kaydet":
                        if (i + 1 >= args.Length)
                        {
                            hata.WriteLine("--kaydet bir dosya yolu ister.");
                            return KullanimHatasi;
                        }

                        kaydet = args[++i];
                        break;
                    case "--hepsi":
                        hepsi = true;
                        break;
                    default:
                        hata.WriteLine($"run: bilinmeyen seçenek {args[i]}");
                        return KullanimHatasi;
                }
            }

            if (adimlar.Count > 0 && adimlar[adimlar.Count - 1].Key == "--eylem")
            {
                hata.WriteLine("Son --eylem'den sonra işlenmesi için bir --tick gerekli.");
                return KullanimHatasi;
            }

            var sim = SenaryoDosyasi.Yukle(senaryoYolu, seed, hata);
            if (sim == null)
            {
                return DosyaHatasi;
            }

            try
            {
                foreach (var adim in adimlar)
                {
                    if (adim.Key == "--eylem")
                    {
                        sim.Submit(EylemAyristir(adim.Value));
                    }
                    else
                    {
                        sim.Tick(int.Parse(adim.Value, NumberStyles.None, CultureInfo.InvariantCulture));
                    }
                }
            }
            catch (ArgumentException e)
            {
                hata.WriteLine("Eylem reddedildi: " + e.Message);
                return DosyaHatasi;
            }

            cikti.WriteLine($"senaryo: {sim.Senaryo.Ad} (seed {seed.ToString(CultureInfo.InvariantCulture)})");
            foreach (var olay in sim.Gunluk.Olaylar)
            {
                if (hepsi || olay.Tick > 0)
                {
                    cikti.WriteLine(NedenYazici.Ozet(olay));
                }
            }

            cikti.WriteLine($"tick: {sim.SimdikiTick.ToString(CultureInfo.InvariantCulture)}");
            cikti.WriteLine($"durum hash: {sim.SonHash}");

            if (kaydet != null)
            {
                var tamYol = Path.GetFullPath(kaydet);
                var goreli = Path.GetRelativePath(Path.GetDirectoryName(tamYol)!, Path.GetFullPath(senaryoYolu)).Replace('\\', '/');
                File.WriteAllText(tamYol, OturumKaydi.Yaz(OturumKaydi.Uret(sim, goreli)));
                cikti.WriteLine($"oturum kaydedildi: {kaydet}");
            }

            return Basarili;
        }

        // tur[:hedef[:anahtar=deger,anahtar=deger]]
        private static PlayerAction EylemAyristir(string metin)
        {
            var parcalar = metin.Split(new[] { ':' }, 3);
            var yuk = new List<KeyValuePair<string, string>>();
            if (parcalar.Length == 3 && parcalar[2].Length > 0)
            {
                foreach (var cift in parcalar[2].Split(','))
                {
                    var i = cift.IndexOf('=');
                    if (i <= 0)
                    {
                        throw new ArgumentException($"Yük \"anahtar=deger\" biçiminde olmalı: \"{cift}\".");
                    }

                    yuk.Add(new KeyValuePair<string, string>(cift.Substring(0, i), cift.Substring(i + 1)));
                }
            }

            return new PlayerAction(parcalar[0], parcalar.Length > 1 ? parcalar[1] : string.Empty, yuk);
        }

        // ---- replay / query / why için ortak yükleme ----

        private sealed class YuklenenOturum
        {
            public YuklenenOturum(string yol, string senaryoYolu, OturumKaydi kayit, Simulasyon sim, OynatmaSonucu sonuc)
            {
                Yol = yol;
                SenaryoYolu = senaryoYolu;
                Kayit = kayit;
                Sim = sim;
                Sonuc = sonuc;
            }

            public string Yol { get; }

            public string SenaryoYolu { get; }

            public OturumKaydi Kayit { get; }

            public Simulasyon Sim { get; }

            public OynatmaSonucu Sonuc { get; }
        }

        private static YuklenenOturum? OturumuOynat(string yol, TextWriter hata)
        {
            if (!File.Exists(yol))
            {
                hata.WriteLine($"Dosya bulunamadı: {yol}");
                return null;
            }

            var okuma = OturumKaydi.Oku(File.ReadAllText(yol), yol);
            if (!okuma.Basarili)
            {
                foreach (var h in okuma.Hatalar)
                {
                    hata.WriteLine(h.ToString());
                }

                return null;
            }

            var kayit = okuma.Kayit!;
            var senaryoYolu = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(yol))!, kayit.Senaryo);
            var sim = SenaryoDosyasi.Yukle(senaryoYolu, kayit.Seed, hata);
            if (sim == null)
            {
                return null;
            }

            try
            {
                return new YuklenenOturum(yol, senaryoYolu, kayit, sim, OturumOynatici.Oynat(sim, kayit));
            }
            catch (ArgumentException e)
            {
                hata.WriteLine($"{yol}: oturum oynatılamadı: {e.Message}");
                return null;
            }
        }

        // ---- replay ----

        private static int Replay(string[] args, TextWriter cikti, TextWriter hata)
        {
            var guncelle = false;
            string? yol = null;
            for (var i = 1; i < args.Length; i++)
            {
                if (args[i] == "--guncelle")
                {
                    guncelle = true;
                }
                else if (args[i].StartsWith("--", StringComparison.Ordinal) || yol != null)
                {
                    hata.WriteLine($"replay: beklenmeyen argüman {args[i]}");
                    return KullanimHatasi;
                }
                else
                {
                    yol = args[i];
                }
            }

            if (yol == null)
            {
                hata.WriteLine("replay: oturum dosyası gerekli.");
                KullanimYaz(hata);
                return KullanimHatasi;
            }

            var oturum = OturumuOynat(yol, hata);
            if (oturum == null)
            {
                return DosyaHatasi;
            }

            var sonuc = oturum.Sonuc;
            if (sonuc.HashTutuyor)
            {
                cikti.WriteLine(guncelle
                    ? $"hash değişmedi, dosya güncellenmedi: {sonuc.GercekHash}"
                    : $"hash tutuyor: {sonuc.GercekHash}");
                return Basarili;
            }

            if (!guncelle)
            {
                cikti.WriteLine("hash TUTMUYOR");
                cikti.WriteLine($"  beklenen: {sonuc.BeklenenHash}");
                cikti.WriteLine($"  gerçek:   {sonuc.GercekHash}");
                cikti.WriteLine("Davranış bilerek değiştiyse: yanki replay " + yol + " --guncelle (PR açıklamasına gerekçeyi yaz).");
                return HashUyusmadi;
            }

            var yeni = new OturumKaydi(oturum.Kayit.Seed, oturum.Kayit.Senaryo, oturum.Kayit.TickSayisi, oturum.Kayit.Eylemler, sonuc.GercekHash);
            File.WriteAllText(yol, OturumKaydi.Yaz(yeni));
            cikti.WriteLine($"hash güncellendi: {sonuc.BeklenenHash} → {sonuc.GercekHash}");
            return Basarili;
        }

        // ---- query ----

        private static int Query(string[] args, TextWriter cikti, TextWriter hata)
        {
            if (args.Length != 3)
            {
                hata.WriteLine("query: oturum dosyası ve NPC kimliği gerekli.");
                KullanimYaz(hata);
                return KullanimHatasi;
            }

            var oturum = OturumuOynat(args[1], hata);
            if (oturum == null)
            {
                return DosyaHatasi;
            }

            if (!oturum.Sim.Zihinler.Var(args[2]))
            {
                hata.WriteLine($"\"{args[2]}\" bir NPC değil. NPC'ler: {string.Join(", ", oturum.Sim.Zihinler.Kimlikler)}");
                return DosyaHatasi;
            }

            NpcDurumu.Yaz(oturum.Sim, args[2], cikti);
            if (!oturum.Sonuc.HashTutuyor)
            {
                cikti.WriteLine($"UYARI: oturumdaki son_hash ({oturum.Sonuc.BeklenenHash}) oynatma sonucuyla ({oturum.Sonuc.GercekHash}) tutmuyor.");
            }

            return Basarili;
        }

        // ---- why ----

        private static int Why(string[] args, TextWriter cikti, TextWriter hata)
        {
            if (args.Length != 3)
            {
                hata.WriteLine("why: oturum dosyası ve olay kimliği gerekli.");
                KullanimYaz(hata);
                return KullanimHatasi;
            }

            if (!long.TryParse(args[2].TrimStart('#'), NumberStyles.None, CultureInfo.InvariantCulture, out var olayId))
            {
                hata.WriteLine($"Olay kimliği tam sayı olmalı: {args[2]}");
                return KullanimHatasi;
            }

            var oturum = OturumuOynat(args[1], hata);
            if (oturum == null)
            {
                return DosyaHatasi;
            }

            if (!oturum.Sim.Gunluk.Var(olayId))
            {
                hata.WriteLine($"Günlükte #{olayId.ToString(CultureInfo.InvariantCulture)} kimlikli olay yok (olay sayısı: {oturum.Sim.Gunluk.Sayi.ToString(CultureInfo.InvariantCulture)}).");
                return DosyaHatasi;
            }

            // Günlük karar_verildi olaylarında yalnızca KISA izi taşır (K3). Zincirdeki her karar için tam iz, oturum o karara kadar
            // replay ile yeniden oynatılarak üretilir ve günlükteki kısa izle doğrulanır; uyuşmazlık sessizce geçmez.
            var sim = oturum.Sim;
            var kararlar = new List<Olay>();
            if (sim.Gunluk[olayId].Tur == YerlesikTurler.KararVerildi)
            {
                kararlar.Add(sim.Gunluk[olayId]);
            }

            foreach (var neden in sim.Neden(olayId))
            {
                if (neden.Tur == YerlesikTurler.KararVerildi)
                {
                    kararlar.Add(neden);
                }
            }

            IReadOnlyDictionary<long, KararIzi>? tamIzler = null;
            if (kararlar.Count > 0)
            {
                try
                {
                    tamIzler = KararIziYenidenUretici.UretCoklu(
                        () => SenaryoDosyasi.Yukle(oturum.SenaryoYolu, oturum.Kayit.Seed, hata)
                            ?? throw new InvalidOperationException("Senaryo yeniden yüklenemedi: " + oturum.SenaryoYolu),
                        oturum.Kayit,
                        kararlar);
                }
                catch (KararIziUyusmazligiException e)
                {
                    hata.WriteLine(e.Message);
                    return IzUyusmadi;
                }
            }

            foreach (var satir in NedenYazici.Yaz(sim.Gunluk, olayId, tamIzler))
            {
                cikti.WriteLine(satir);
            }

            return Basarili;
        }
    }
}
