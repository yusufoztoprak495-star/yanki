using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Yanki.Core;
using Yanki.Core.Decision;
using Yanki.Core.Events;
using Yanki.Core.Simulation;
using Yanki.Dialogue;

namespace Yanki.Cli
{
    /// <summary>
    /// Komut satırı komutları. Çıkış kodları: 0 başarılı, 1 kullanım hatası, 2 okuma/doğrulama/çalıştırma hatası,
    /// 3 replay durum hash'i (son_hash) tutmadı, 4 yeniden üretilen karar izi günlükteki kısa izle uyuşmadı, 5 günlük hash'i (gunluk_hash) tutmadı,
    /// 6 senaryo girdileri kayıttan sonra değişmiş (senaryo_hash). Dosya sistemi erişimi yalnızca burada ve <see cref="SenaryoDosyasi"/>'ndadır; çekirdek metin alır.
    /// </summary>
    internal static class KomutCalistirici
    {
        public const int Basarili = 0;
        public const int KullanimHatasi = 1;
        public const int DosyaHatasi = 2;
        public const int HashUyusmadi = 3;
        public const int IzUyusmadi = 4;
        public const int GunlukHashUyusmadi = 5;
        public const int SenaryoDegisti = 6;
        public const int SablonHashUyusmadi = 7;

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
                case "devam":
                    return Devam(args, cikti, hata);
                case "query":
                    return Query(args, cikti, hata);
                case "why":
                    return Why(args, cikti, hata);
                case "diyalog":
                    return Diyalog(args, cikti, hata);
                case "diyalog-replay":
                    return DiyalogReplay(args, cikti, hata);
                case "validate":
                    return DogrulamaKomutu.Calistir(args, cikti, hata);
                case "coverage":
                    return KapsamKomutu.Calistir(args, cikti, hata);
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
            w.WriteLine("  yanki replay <oturum.json> [--guncelle | --hash-ekle | --senaryo-hash-yenile]");
            w.WriteLine("  yanki devam <oturum.json> [--eylem tur[:hedef[:anahtar=deger,...]]] [--tick M] ... --kaydet yeni.json   (kayıttan devam: baştan oynatır, doğrular, sürdürür)");
            w.WriteLine("  yanki query <oturum.json> <npc>      (NPC durumu ve son karar izi)");
            w.WriteLine("  yanki why <oturum.json> <olayId>     (nedensellik zinciri; karar olayında adaylar ve puan bileşenleri)");
            w.WriteLine("  yanki diyalog <oturum.json> --sablon <sablon.json> [--neden <satir_no>]   (kayıttan diyalog satırları; --neden seçilen şablonu, kriterleri ve olayın why zincirini gösterir)");
            w.WriteLine("  yanki diyalog-replay <golden.json> [--guncelle]   (tests/golden/diyalog/: kayit, sablon, diyalog_hash, sablon_hash; çıkış kodu 3 diyalog_hash, 7 sablon_hash)");
            w.WriteLine("  yanki validate <senaryo_dizini | scenario.json> [--sablon <sablon.json>] [--uyari-hata]   (hata ve uyarılar dosya:satir ile; çıkış 0 temiz, 1 hata var)");
            w.WriteLine("  yanki coverage <senaryo_dizini> [--seeds N] [--tick T] [--sablon <sablon.json>]   (oynanislar/*.json betikleri x seed; Markdown rapor; çıkış 0 plan 3 kriteri tuttu, 2 tutmadı)");
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
                File.WriteAllText(tamYol, OturumKaydi.Yaz(OturumKaydi.Uret(sim, goreli, SenaryoDosyasi.Hash(senaryoYolu))));
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
                foreach (var cift in YukCiftleri(parcalar[2]))
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

        // Yük çiftleri virgülle ayrılır, ama parantez içindeki virgüller (örn. onerme=borclu(Mehmet,Ayse,500)) ayraç değildir.
        private static IEnumerable<string> YukCiftleri(string yuk)
        {
            var derinlik = 0;
            var bas = 0;
            for (var i = 0; i < yuk.Length; i++)
            {
                if (yuk[i] == '(')
                {
                    derinlik++;
                }
                else if (yuk[i] == ')')
                {
                    derinlik--;
                }
                else if (yuk[i] == ',' && derinlik == 0)
                {
                    yield return yuk.Substring(bas, i - bas);
                    bas = i + 1;
                }
            }

            yield return yuk.Substring(bas);
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

        // simHazir: simülasyon kurulduktan ve baştan oynatmadan ÖNCE çağrılır (diyalog motoru olayları kaçırmasın diye abone olur).
        private static YuklenenOturum? OturumuOynat(string yol, TextWriter hata, out int cikis, bool senaryoHashDenetle = true, Action<Simulasyon>? simHazir = null)
        {
            cikis = DosyaHatasi;
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

            simHazir?.Invoke(sim);
            try
            {
                return new YuklenenOturum(yol, senaryoYolu, kayit, sim, OturumOynatici.Oynat(sim, kayit, senaryoHashDenetle ? SenaryoDosyasi.Hash(senaryoYolu) : null));
            }
            catch (SenaryoDegistiHatasi e)
            {
                hata.WriteLine($"{yol}: {e.Message}");
                cikis = SenaryoDegisti;
                return null;
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
            var hashEkle = false;
            var senaryoHashYenile = false;
            string? yol = null;
            for (var i = 1; i < args.Length; i++)
            {
                if (args[i] == "--guncelle")
                {
                    guncelle = true;
                }
                else if (args[i] == "--hash-ekle")
                {
                    hashEkle = true;
                }
                else if (args[i] == "--senaryo-hash-yenile")
                {
                    senaryoHashYenile = true;
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

            if (senaryoHashYenile && (guncelle || hashEkle))
            {
                hata.WriteLine("replay: --senaryo-hash-yenile, --guncelle ya da --hash-ekle ile birlikte kullanılamaz.");
                return KullanimHatasi;
            }

            // --guncelle, --hash-ekle ve --senaryo-hash-yenile senaryo değişikliğini bilerek kabul eder; yalnızca düz replay senaryo_hash'i denetler.
            var oturum = OturumuOynat(yol, hata, out var cikis, senaryoHashDenetle: !(guncelle || hashEkle || senaryoHashYenile));
            if (oturum == null)
            {
                return cikis;
            }

            var sonuc = oturum.Sonuc;
            if (senaryoHashYenile)
            {
                return SenaryoHashYenile(yol, oturum, cikti, hata);
            }

            if (guncelle && hashEkle)
            {
                hata.WriteLine("replay: --guncelle ve --hash-ekle birlikte kullanılamaz.");
                return KullanimHatasi;
            }

            if (!sonuc.HashTutuyor)
            {
                if (!guncelle)
                {
                    cikti.WriteLine("hash TUTMUYOR");
                    cikti.WriteLine($"  beklenen: {sonuc.BeklenenHash}");
                    cikti.WriteLine($"  gerçek:   {sonuc.GercekHash}");
                    cikti.WriteLine("Davranış bilerek değiştiyse: yanki replay " + yol + " --guncelle (PR açıklamasına gerekçeyi yaz).");
                    return HashUyusmadi;
                }

                var yeni = new OturumKaydi(
                    oturum.Kayit.Seed, oturum.Kayit.Senaryo, oturum.Kayit.TickSayisi, oturum.Kayit.Eylemler, sonuc.GercekHash,
                    oturum.Kayit.Surum >= 2 ? sonuc.GercekGunlukHash : null,
                    oturum.Kayit.Surum >= 2 ? SenaryoDosyasi.Hash(oturum.SenaryoYolu) : null,
                    oturum.Kayit.Surum >= 2 ? oturum.Kayit.BekleyenEylemler : null);
                File.WriteAllText(yol, OturumKaydi.Yaz(yeni));
                cikti.WriteLine($"hash güncellendi: {sonuc.BeklenenHash} → {sonuc.GercekHash}");
                return Basarili;
            }

            if (!sonuc.GunlukHashTutuyor)
            {
                if (!guncelle)
                {
                    cikti.WriteLine("günlük hash'i TUTMUYOR (durum hash'i tutuyor: karar izi, seçici ya da fren olayları değişmiş olabilir)");
                    cikti.WriteLine($"  beklenen: {sonuc.BeklenenGunlukHash}");
                    cikti.WriteLine($"  gerçek:   {sonuc.GercekGunlukHash}");
                    cikti.WriteLine("Davranış bilerek değiştiyse: yanki replay " + yol + " --guncelle (PR açıklamasına gerekçeyi yaz).");
                    return GunlukHashUyusmadi;
                }

                var yeni = new OturumKaydi(
                    oturum.Kayit.Seed, oturum.Kayit.Senaryo, oturum.Kayit.TickSayisi, oturum.Kayit.Eylemler, sonuc.GercekHash,
                    sonuc.GercekGunlukHash, SenaryoDosyasi.Hash(oturum.SenaryoYolu), oturum.Kayit.BekleyenEylemler);
                File.WriteAllText(yol, OturumKaydi.Yaz(yeni));
                cikti.WriteLine($"günlük hash'i güncellendi: {sonuc.BeklenenGunlukHash} → {sonuc.GercekGunlukHash}");
                return Basarili;
            }

            if (hashEkle)
            {
                // Yalnızca eksik gunluk_hash / senaryo_hash eklenir; son_hash'e dokunulmaz (tutuyorsa buraya gelinir).
                if (oturum.Kayit.GunlukHash != null && oturum.Kayit.SenaryoHash != null)
                {
                    cikti.WriteLine($"hash tutuyor, dosyada gunluk_hash ve senaryo_hash zaten var: {sonuc.GercekHash}");
                    return Basarili;
                }

                var eklenmis = new OturumKaydi(
                    oturum.Kayit.Seed, oturum.Kayit.Senaryo, oturum.Kayit.TickSayisi, oturum.Kayit.Eylemler, oturum.Kayit.SonHash,
                    sonuc.GercekGunlukHash, SenaryoDosyasi.Hash(oturum.SenaryoYolu), oturum.Kayit.BekleyenEylemler);
                File.WriteAllText(yol, OturumKaydi.Yaz(eklenmis));
                cikti.WriteLine($"gunluk_hash ve senaryo_hash eklendi (son_hash değişmedi: {sonuc.GercekHash}), günlük {sonuc.GercekGunlukHash}");
                return Basarili;
            }

            cikti.WriteLine(guncelle
                ? $"hash değişmedi, dosya güncellenmedi: {sonuc.GercekHash}"
                : $"hash tutuyor: {sonuc.GercekHash}");
            return Basarili;
        }

        // replay --senaryo-hash-yenile: senaryo dosyaları kayıttan sonra değişti ama davranış (son_hash ve gunluk_hash) aynıysa
        // (yorum, boşluk, davranışı değiştirmeyen düzenleme) yalnızca senaryo_hash'i yazar. Davranış değiştiyse reddeder; o durum --guncelle işidir.
        private static int SenaryoHashYenile(string yol, YuklenenOturum oturum, TextWriter cikti, TextWriter hata)
        {
            var kayit = oturum.Kayit;
            var sonuc = oturum.Sonuc;
            if (kayit.SenaryoHash == null)
            {
                hata.WriteLine($"{yol}: kayıtta senaryo_hash yok (sürüm 1); önce --hash-ekle kullanın.");
                return KullanimHatasi;
            }

            if (!sonuc.HashTutuyor)
            {
                cikti.WriteLine("son_hash TUTMUYOR: davranış değişmiş, senaryo_hash yenilenmedi.");
                cikti.WriteLine($"  beklenen: {sonuc.BeklenenHash}");
                cikti.WriteLine($"  gerçek:   {sonuc.GercekHash}");
                cikti.WriteLine("Davranış bilerek değiştiyse: yanki replay " + yol + " --guncelle (PR açıklamasına gerekçeyi yaz).");
                return HashUyusmadi;
            }

            if (!sonuc.GunlukHashTutuyor)
            {
                cikti.WriteLine("günlük hash'i TUTMUYOR: davranış değişmiş, senaryo_hash yenilenmedi.");
                cikti.WriteLine($"  beklenen: {sonuc.BeklenenGunlukHash}");
                cikti.WriteLine($"  gerçek:   {sonuc.GercekGunlukHash}");
                cikti.WriteLine("Davranış bilerek değiştiyse: yanki replay " + yol + " --guncelle (PR açıklamasına gerekçeyi yaz).");
                return GunlukHashUyusmadi;
            }

            var yeniHash = SenaryoDosyasi.Hash(oturum.SenaryoYolu);
            if (yeniHash == kayit.SenaryoHash)
            {
                cikti.WriteLine($"senaryo_hash zaten güncel: {yeniHash}");
                return Basarili;
            }

            var yeni = new OturumKaydi(
                kayit.Seed, kayit.Senaryo, kayit.TickSayisi, kayit.Eylemler, kayit.SonHash,
                kayit.GunlukHash, yeniHash, kayit.BekleyenEylemler);
            File.WriteAllText(yol, OturumKaydi.Yaz(yeni));
            cikti.WriteLine($"senaryo_hash yenilendi: {kayit.SenaryoHash} → {yeniHash} (son_hash ve gunluk_hash değişmedi)");
            return Basarili;
        }

        // ---- devam ----

        // yanki devam <oturum.json> [--eylem ...] [--tick M] ... --kaydet yeni.json : kaydı baştan oynatır ve doğrular, bekleyen eylemleri geri bekletir,
        // yeni eylem/tick adımlarını sürdürür ve yeni kaydı yazar (N tick koş, kaydet, yükle, M tick koş = N+M tick kesintisiz koş).
        private static int Devam(string[] args, TextWriter cikti, TextWriter hata)
        {
            if (args.Length < 2 || args[1].StartsWith("--", StringComparison.Ordinal))
            {
                hata.WriteLine("devam: oturum dosyası gerekli.");
                KullanimYaz(hata);
                return KullanimHatasi;
            }

            string? kaydet = null;
            var adimlar = new List<KeyValuePair<string, string>>();
            for (var i = 2; i < args.Length; i++)
            {
                switch (args[i])
                {
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
                    default:
                        hata.WriteLine($"devam: bilinmeyen seçenek {args[i]}");
                        return KullanimHatasi;
                }
            }

            if (kaydet == null)
            {
                hata.WriteLine("devam: --kaydet <yeni.json> gerekli (devam sonucu yeni bir kayda yazılır; eski kayıt değişmez).");
                return KullanimHatasi;
            }

            var oturum = OturumuOynat(args[1], hata, out var cikis);
            if (oturum == null)
            {
                return cikis;
            }

            if (!oturum.Sonuc.HashTutuyor)
            {
                hata.WriteLine($"devam: kayıt oynatılınca durum hash'i tutmuyor (beklenen {oturum.Sonuc.BeklenenHash}, gerçek {oturum.Sonuc.GercekHash}); devam edilmedi.");
                return HashUyusmadi;
            }

            if (!oturum.Sonuc.GunlukHashTutuyor)
            {
                hata.WriteLine($"devam: kayıt oynatılınca günlük hash'i tutmuyor (beklenen {oturum.Sonuc.BeklenenGunlukHash}, gerçek {oturum.Sonuc.GercekGunlukHash}); devam edilmedi.");
                return GunlukHashUyusmadi;
            }

            var sim = oturum.Sim;
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

            var tamYol = Path.GetFullPath(kaydet);
            var goreli = Path.GetRelativePath(Path.GetDirectoryName(tamYol)!, Path.GetFullPath(oturum.SenaryoYolu)).Replace('\\', '/');
            File.WriteAllText(tamYol, OturumKaydi.Yaz(OturumKaydi.Uret(sim, goreli, SenaryoDosyasi.Hash(oturum.SenaryoYolu))));
            cikti.WriteLine($"tick: {sim.SimdikiTick.ToString(CultureInfo.InvariantCulture)}");
            cikti.WriteLine($"durum hash: {sim.SonHash}");
            cikti.WriteLine($"oturum kaydedildi: {kaydet}");
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

            var oturum = OturumuOynat(args[1], hata, out var cikis);
            if (oturum == null)
            {
                return cikis;
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

            var oturum = OturumuOynat(args[1], hata, out var cikis);
            if (oturum == null)
            {
                return cikis;
            }

            return NedenZinciriYaz(oturum, olayId, cikti, hata);
        }

        // `why` ile `diyalog --neden` ortak zincir yazıcısı (aynı dili konuşurlar).
        private static int NedenZinciriYaz(YuklenenOturum oturum, long olayId, TextWriter cikti, TextWriter hata)
        {
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

        // ---- diyalog (8. hafta) ----

        // yanki diyalog <oturum.json> --sablon <sablon.json> [--neden <satir_no>]
        // Şablon dosyasını CLI okur (çekirdek ve Yanki.Dialogue dosya sistemine dokunmaz). Oturum baştan oynatılırken diyalog motoru olay yayınına abone olur; satırlar
        // tick sonu durumundan üretilir. Normal satır çıktısı KuralId vermez; --neden çıktısı olayın why zincirini de içerir (geliştirici bilgisi, ADR-012 istisnası).
        private static int Diyalog(string[] args, TextWriter cikti, TextWriter hata)
        {
            if (args.Length < 2 || args[1].StartsWith("--", StringComparison.Ordinal))
            {
                hata.WriteLine("diyalog: oturum dosyası gerekli.");
                KullanimYaz(hata);
                return KullanimHatasi;
            }

            string? sablonYolu = null;
            int? nedenNo = null;
            for (var i = 2; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--sablon":
                        if (i + 1 >= args.Length)
                        {
                            hata.WriteLine("--sablon bir dosya yolu ister.");
                            return KullanimHatasi;
                        }

                        sablonYolu = args[++i];
                        break;
                    case "--neden":
                        if (i + 1 >= args.Length || !int.TryParse(args[i + 1].TrimStart('#'), NumberStyles.None, CultureInfo.InvariantCulture, out var no) || no < 1)
                        {
                            hata.WriteLine("--neden 1'den başlayan bir satır numarası ister.");
                            return KullanimHatasi;
                        }

                        nedenNo = no;
                        i++;
                        break;
                    default:
                        hata.WriteLine($"Bilinmeyen seçenek: {args[i]}");
                        KullanimYaz(hata);
                        return KullanimHatasi;
                }
            }

            if (sablonYolu == null)
            {
                hata.WriteLine("diyalog: --sablon <dosya> gerekli.");
                KullanimYaz(hata);
                return KullanimHatasi;
            }

            if (!File.Exists(sablonYolu))
            {
                hata.WriteLine($"Dosya bulunamadı: {sablonYolu}");
                return DosyaHatasi;
            }

            var yukleme = SablonYukleyici.Yukle(File.ReadAllText(sablonYolu), sablonYolu);
            if (!yukleme.Basarili)
            {
                foreach (var h in yukleme.Hatalar)
                {
                    hata.WriteLine(h);
                }

                return DosyaHatasi;
            }

            foreach (var u in yukleme.Uyarilar)
            {
                hata.WriteLine("uyarı: " + u);
            }

            DiyalogMotoru? motor = null;
            var oturum = OturumuOynat(args[1], hata, out var cikis, true, sim => motor = DiyalogMotoru.Baglan(sim, yukleme.Dosya!));
            if (oturum == null || motor == null)
            {
                return oturum == null ? cikis : DosyaHatasi;
            }

            using (motor)
            {
                if (!oturum.Sonuc.HashTutuyor)
                {
                    hata.WriteLine($"{args[1]}: son_hash tutmuyor (beklenen {oturum.Sonuc.BeklenenHash}, gerçek {oturum.Sonuc.GercekHash}); diyalog satırları güvenilir değil.");
                    return HashUyusmadi;
                }

                if (!oturum.Sonuc.GunlukHashTutuyor)
                {
                    hata.WriteLine($"{args[1]}: gunluk_hash tutmuyor (beklenen {oturum.Sonuc.BeklenenGunlukHash}, gerçek {oturum.Sonuc.GercekGunlukHash}); diyalog satırları güvenilir değil.");
                    return GunlukHashUyusmadi;
                }

                if (nedenNo.HasValue)
                {
                    if (nedenNo.Value > motor.Satirlar.Count)
                    {
                        hata.WriteLine($"Satır #{nedenNo.Value.ToString(CultureInfo.InvariantCulture)} yok (satır sayısı: {motor.Satirlar.Count.ToString(CultureInfo.InvariantCulture)}).");
                        return DosyaHatasi;
                    }

                    var satir = motor.Satirlar[nedenNo.Value - 1];
                    cikti.WriteLine(SatirMetni(satir));
                    cikti.WriteLine("-- neden (diyalog) --");
                    foreach (var n in satir.NedenSatirlari)
                    {
                        cikti.WriteLine("  " + n);
                    }

                    cikti.WriteLine($"-- olayın why zinciri (yanki why {args[1]} {satir.OlayId.ToString(CultureInfo.InvariantCulture)}) --");
                    return NedenZinciriYaz(oturum, satir.OlayId, cikti, hata);
                }

                cikti.WriteLine($"sablon_hash: {yukleme.Dosya!.SablonHash}");
                cikti.WriteLine($"diyalog_hash: {motor.DiyalogHash()}");
                cikti.WriteLine($"satir: {motor.Satirlar.Count.ToString(CultureInfo.InvariantCulture)}" + (motor.BekleyenFollowupSayisi > 0 ? $" (bekleyen followup: {motor.BekleyenFollowupSayisi.ToString(CultureInfo.InvariantCulture)})" : string.Empty));
                foreach (var s in motor.Satirlar)
                {
                    cikti.WriteLine(SatirMetni(s));
                }

                return Basarili;
            }
        }

        // yanki diyalog-replay <golden.json> [--guncelle]: diyalog golden'ı AYRI biçimdedir (mevcut dört golden dosyasına alan eklenmez). Kayıt ve şablon yolları golden dosyasına göre göreli.
        private static int DiyalogReplay(string[] args, TextWriter cikti, TextWriter hata)
        {
            var guncelle = args.Length == 3 && args[2] == "--guncelle";
            if (args.Length < 2 || args[1].StartsWith("--", StringComparison.Ordinal) || args.Length > 3 || (args.Length == 3 && !guncelle))
            {
                hata.WriteLine("diyalog-replay: golden dosyası gerekli (isteğe bağlı --guncelle).");
                KullanimYaz(hata);
                return KullanimHatasi;
            }

            var goldenYolu = args[1];
            if (!File.Exists(goldenYolu))
            {
                hata.WriteLine($"Dosya bulunamadı: {goldenYolu}");
                return DosyaHatasi;
            }

            Yanki.Core.Json.JsonDegeri kok;
            try
            {
                kok = Yanki.Core.Json.JsonAyristirici.Ayristir(File.ReadAllText(goldenYolu));
            }
            catch (Yanki.Core.Json.JsonHatasi e)
            {
                hata.WriteLine($"{goldenYolu}:{e.Satir.ToString(CultureInfo.InvariantCulture)}: JSON okunamadı: {e.Message}");
                return DosyaHatasi;
            }

            var kayitAdi = kok.Tur == Yanki.Core.Json.JsonTuru.Nesne ? kok.Al("kayit") : null;
            var sablonAdi = kok.Tur == Yanki.Core.Json.JsonTuru.Nesne ? kok.Al("sablon") : null;
            if (kayitAdi == null || sablonAdi == null || kayitAdi.Tur != Yanki.Core.Json.JsonTuru.Metin || sablonAdi.Tur != Yanki.Core.Json.JsonTuru.Metin)
            {
                hata.WriteLine($"{goldenYolu}: \"kayit\" ve \"sablon\" (golden dosyasına göre göreli yollar) gerekli.");
                return DosyaHatasi;
            }

            var dizin = Path.GetDirectoryName(Path.GetFullPath(goldenYolu))!;
            var kayitYolu = Path.Combine(dizin, kayitAdi.MetinDeger);
            var sablonYolu = Path.Combine(dizin, sablonAdi.MetinDeger);
            if (!File.Exists(sablonYolu))
            {
                hata.WriteLine($"Dosya bulunamadı: {sablonYolu}");
                return DosyaHatasi;
            }

            var yukleme = SablonYukleyici.Yukle(File.ReadAllText(sablonYolu), sablonYolu);
            if (!yukleme.Basarili)
            {
                foreach (var h in yukleme.Hatalar)
                {
                    hata.WriteLine(h);
                }

                return DosyaHatasi;
            }

            foreach (var u in yukleme.Uyarilar)
            {
                hata.WriteLine("uyarı: " + u);
            }

            DiyalogMotoru? motor = null;
            var oturum = OturumuOynat(kayitYolu, hata, out var cikis, true, sim => motor = DiyalogMotoru.Baglan(sim, yukleme.Dosya!));
            if (oturum == null || motor == null)
            {
                return oturum == null ? cikis : DosyaHatasi;
            }

            using (motor)
            {
                if (!oturum.Sonuc.HashTutuyor)
                {
                    hata.WriteLine($"{kayitYolu}: son_hash tutmuyor (beklenen {oturum.Sonuc.BeklenenHash}, gerçek {oturum.Sonuc.GercekHash}).");
                    return HashUyusmadi;
                }

                if (!oturum.Sonuc.GunlukHashTutuyor)
                {
                    hata.WriteLine($"{kayitYolu}: gunluk_hash tutmuyor.");
                    return GunlukHashUyusmadi;
                }

                var diyalogHash = motor.DiyalogHash();
                var sablonHash = yukleme.Dosya!.SablonHash;
                if (guncelle)
                {
                    File.WriteAllText(
                        goldenYolu,
                        "{\n  \"surum\": 1,\n  \"kayit\": " + Yanki.Core.Json.JsonYazici.YazKisa(Yanki.Core.Json.JsonDegeri.Metin(kayitAdi.MetinDeger))
                        + ",\n  \"sablon\": " + Yanki.Core.Json.JsonYazici.YazKisa(Yanki.Core.Json.JsonDegeri.Metin(sablonAdi.MetinDeger))
                        + ",\n  \"diyalog_hash\": \"" + diyalogHash + "\",\n  \"sablon_hash\": \"" + sablonHash + "\"\n}\n");
                    cikti.WriteLine($"diyalog golden güncellendi: diyalog_hash {diyalogHash}, sablon_hash {sablonHash}");
                    return Basarili;
                }

                var beklenenDiyalog = kok.Al("diyalog_hash");
                var beklenenSablon = kok.Al("sablon_hash");
                if (beklenenDiyalog == null || beklenenSablon == null || beklenenDiyalog.Tur != Yanki.Core.Json.JsonTuru.Metin || beklenenSablon.Tur != Yanki.Core.Json.JsonTuru.Metin)
                {
                    hata.WriteLine($"{goldenYolu}: \"diyalog_hash\" ve \"sablon_hash\" yok (yazmak için --guncelle).");
                    return DosyaHatasi;
                }

                if (!string.Equals(beklenenSablon.MetinDeger, sablonHash, StringComparison.Ordinal))
                {
                    hata.WriteLine($"{goldenYolu}: sablon_hash tutmuyor (beklenen {beklenenSablon.MetinDeger}, gerçek {sablonHash}); şablon dosyası değişmiş.");
                    return SablonHashUyusmadi;
                }

                if (!string.Equals(beklenenDiyalog.MetinDeger, diyalogHash, StringComparison.Ordinal))
                {
                    hata.WriteLine($"{goldenYolu}: diyalog_hash tutmuyor (beklenen {beklenenDiyalog.MetinDeger}, gerçek {diyalogHash}).");
                    return HashUyusmadi;
                }

                cikti.WriteLine($"diyalog hash tutuyor: {diyalogHash} (sablon_hash {sablonHash})");
                return Basarili;
            }
        }

        private static string SatirMetni(DiyalogSatiri s) =>
            $"#{s.No.ToString(CultureInfo.InvariantCulture)} [t{s.Tick.ToString(CultureInfo.InvariantCulture)}] {s.Konusan} → {(s.Dinleyen.Length == 0 ? "(" + s.Mekan + ")" : s.Dinleyen)}: {s.Metin}" + (s.Followup ? "  (cevap)" : string.Empty);
    }
}
