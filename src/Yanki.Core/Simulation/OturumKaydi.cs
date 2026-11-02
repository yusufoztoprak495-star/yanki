using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Yanki.Core.Json;
using Yanki.Core.Rules;
using Yanki.Core.World;

namespace Yanki.Core.Simulation
{
    /// <summary>Oturum dosyasının okuma sonucu.</summary>
    public sealed class OturumOkumaSonucu
    {
        /// <summary>Kayıt; hata varsa <c>null</c>.</summary>
        public OturumKaydi? Kayit { get; }

        /// <summary>Hatalar, dosyadaki sırayla.</summary>
        public IReadOnlyList<KuralHatasi> Hatalar { get; }

        /// <summary>Hata yok mu.</summary>
        public bool Basarili => Hatalar.Count == 0 && Kayit != null;

        /// <summary>Sonuç kurar.</summary>
        public OturumOkumaSonucu(OturumKaydi? kayit, IEnumerable<KuralHatasi> hatalar)
        {
            Kayit = kayit;
            Hatalar = new ReadOnlyCollection<KuralHatasi>(new List<KuralHatasi>(hatalar));
        }
    }

    /// <summary>
    /// Oturum kaydı: seed + senaryo + oyuncu eylem günlüğü + son durum hash'i (plan 5.8, 9). Golden replay dosyası ve
    /// (7. haftada) kayıt/yükleme aynı biçimi kullanır: durum, bu girdilerin baştan oynatılmasıyla yeniden kurulur.
    /// Senaryo dosyasının adı kayıt dosyasına göre göreli bir yoldur; dosyayı çözmek çağıranın (CLI) işidir.
    /// </summary>
    public sealed class OturumKaydi
    {
        /// <summary>Desteklenen biçim sürümü.</summary>
        public const int BicimSurumu = 1;

        private static readonly string[] Anahtarlar = { "surum", "seed", "senaryo", "tick_sayisi", "eylemler", "son_hash" };

        /// <summary>Sürüm.</summary>
        public int Surum => BicimSurumu;

        /// <summary>Simülasyon seed'i.</summary>
        public ulong Seed { get; }

        /// <summary>Senaryo dosyasının yolu (kayıt dosyasına göre göreli).</summary>
        public string Senaryo { get; }

        /// <summary>Toplam tick sayısı.</summary>
        public long TickSayisi { get; }

        /// <summary>Oyuncu eylemleri, tick sırasıyla.</summary>
        public IReadOnlyList<EylemKaydi> Eylemler { get; }

        /// <summary>Oynatma sonunda beklenen durum hash'i (16 karakter küçük harfli onaltılık).</summary>
        public string SonHash { get; }

        /// <summary>Kayıt kurar.</summary>
        /// <exception cref="ArgumentException">Senaryo boşsa, tick sayısı negatifse, hash biçimi bozuksa ya da eylemler tick sırasında/aralığında değilse.</exception>
        public OturumKaydi(ulong seed, string senaryo, long tickSayisi, IEnumerable<EylemKaydi> eylemler, string sonHash)
        {
            if (string.IsNullOrEmpty(senaryo))
            {
                throw new ArgumentException("Senaryo yolu boş olamaz.", nameof(senaryo));
            }

            if (tickSayisi < 0)
            {
                throw new ArgumentException("Tick sayısı negatif olamaz.", nameof(tickSayisi));
            }

            if (!HashMi(sonHash))
            {
                throw new ArgumentException($"Son hash 16 karakterlik küçük harfli onaltılık metin olmalı: \"{sonHash}\".", nameof(sonHash));
            }

            var liste = new List<EylemKaydi>(eylemler ?? throw new ArgumentNullException(nameof(eylemler)));
            long onceki = 1;
            foreach (var e in liste)
            {
                if (e.Tick < onceki || e.Tick > tickSayisi)
                {
                    throw new ArgumentException($"Eylem tick'i 1..{tickSayisi} aralığında ve artan sırada olmalı: {e.Tick}.", nameof(eylemler));
                }

                onceki = e.Tick;
            }

            Seed = seed;
            Senaryo = senaryo;
            TickSayisi = tickSayisi;
            Eylemler = new ReadOnlyCollection<EylemKaydi>(liste);
            SonHash = sonHash;
        }

        /// <summary>Çalışmış bir simülasyonun kaydını çıkarır.</summary>
        public static OturumKaydi Uret(Simulasyon sim, string senaryoYolu)
        {
            if (sim == null)
            {
                throw new ArgumentNullException(nameof(sim));
            }

            return new OturumKaydi(sim.Seed, senaryoYolu, sim.SimdikiTick, sim.EylemGunlugu, sim.SonHash);
        }

        /// <summary>Kaydı kanonik JSON metnine çevirir.</summary>
        public static string Yaz(OturumKaydi kayit)
        {
            if (kayit == null)
            {
                throw new ArgumentNullException(nameof(kayit));
            }

            var eylemler = new List<JsonDegeri>();
            foreach (var k in kayit.Eylemler)
            {
                var alanlar = new List<(string, JsonDegeri)>
                {
                    ("tick", JsonDegeri.Sayi(k.Tick)),
                    ("tur", JsonDegeri.Metin(k.Eylem.Tur)),
                };
                if (k.Eylem.Hedef.Length > 0)
                {
                    alanlar.Add(("hedef", JsonDegeri.Metin(k.Eylem.Hedef)));
                }

                if (k.Eylem.Yuk.Count > 0)
                {
                    var yuk = new List<(string, JsonDegeri)>();
                    foreach (var kv in k.Eylem.Yuk)
                    {
                        yuk.Add((kv.Key, JsonDegeri.Metin(kv.Value)));
                    }

                    alanlar.Add(("yuk", JsonDegeri.Nesne(yuk.ToArray())));
                }

                eylemler.Add(JsonDegeri.Nesne(alanlar.ToArray()));
            }

            return JsonYazici.Yaz(JsonDegeri.Nesne(
                ("surum", JsonDegeri.Sayi(BicimSurumu)),
                ("seed", JsonDegeri.Sayi(kayit.Seed)),
                ("senaryo", JsonDegeri.Metin(kayit.Senaryo)),
                ("tick_sayisi", JsonDegeri.Sayi(kayit.TickSayisi)),
                ("eylemler", JsonDegeri.Dizi(eylemler.ToArray())),
                ("son_hash", JsonDegeri.Metin(kayit.SonHash))));
        }

        /// <summary>Oturum dosyasını okur ve doğrular. Tüm hatalar toplanır.</summary>
        public static OturumOkumaSonucu Oku(string json, string kaynakAdi)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            var hatalar = new List<KuralHatasi>();
            void Hata(int satir, string mesaj) => hatalar.Add(new KuralHatasi(kaynakAdi, satir, string.Empty, mesaj));

            JsonDegeri kok;
            try
            {
                kok = JsonAyristirici.Ayristir(json);
            }
            catch (JsonHatasi e)
            {
                Hata(e.Satir, "JSON okunamadı: " + e.Message);
                return new OturumOkumaSonucu(null, hatalar);
            }

            if (kok.Tur != JsonTuru.Nesne)
            {
                Hata(kok.Satir, "Oturum dosyası bir JSON nesnesi olmalı");
                return new OturumOkumaSonucu(null, hatalar);
            }

            foreach (var kv in kok.Ozellikler)
            {
                if (Array.IndexOf(Anahtarlar, kv.Key) < 0)
                {
                    Hata(kv.Value.Satir, $"Bilinmeyen alan \"{kv.Key}\" ({string.Join(", ", Anahtarlar)})");
                }
            }

            var surum = kok.Al("surum");
            if (surum == null || surum.Tur != JsonTuru.Sayi || surum.SayiDeger != BicimSurumu)
            {
                Hata(surum?.Satir ?? kok.Satir, $"\"surum\" {BicimSurumu} olmalı");
            }

            var seed = TamSayi(kok, "seed", ulong.MaxValue, Hata);
            var senaryo = kok.Al("senaryo");
            if (senaryo == null || senaryo.Tur != JsonTuru.Metin || senaryo.MetinDeger.Length == 0)
            {
                Hata(senaryo?.Satir ?? kok.Satir, "\"senaryo\" boş olmayan metin olmalı");
            }

            var tickSayisi = TamSayi(kok, "tick_sayisi", long.MaxValue, Hata);
            var sonHash = kok.Al("son_hash");
            if (sonHash == null || sonHash.Tur != JsonTuru.Metin || !HashMi(sonHash.MetinDeger))
            {
                Hata(sonHash?.Satir ?? kok.Satir, "\"son_hash\" 16 karakterlik küçük harfli onaltılık metin olmalı");
            }

            var eylemler = new List<EylemKaydi>();
            var dizi = kok.Al("eylemler");
            if (dizi == null || dizi.Tur != JsonTuru.Dizi)
            {
                Hata(dizi?.Satir ?? kok.Satir, "\"eylemler\" bir dizi olmalı");
            }
            else
            {
                long onceki = 1;
                foreach (var e in dizi.Elemanlar)
                {
                    var kayit = EylemOku(e, Hata);
                    if (kayit == null)
                    {
                        continue;
                    }

                    if (kayit.Tick < onceki)
                    {
                        Hata(e.Satir, $"Eylemler tick sırasında olmalı: {kayit.Tick} < {onceki}");
                    }
                    else if (tickSayisi.HasValue && kayit.Tick > (long)tickSayisi.Value)
                    {
                        Hata(e.Satir, $"Eylemin tick'i ({kayit.Tick}) tick_sayisi ({tickSayisi.Value}) değerini aşıyor");
                    }

                    onceki = Math.Max(onceki, kayit.Tick);
                    eylemler.Add(kayit);
                }
            }

            if (hatalar.Count > 0)
            {
                return new OturumOkumaSonucu(null, hatalar);
            }

            return new OturumOkumaSonucu(
                new OturumKaydi(seed!.Value, senaryo!.MetinDeger, (long)tickSayisi!.Value, eylemler, sonHash!.MetinDeger),
                hatalar);
        }

        private static ulong? TamSayi(JsonDegeri kok, string anahtar, ulong ust, Action<int, string> hata)
        {
            var d = kok.Al(anahtar);
            if (d == null || d.Tur != JsonTuru.Sayi || d.SayiDeger < 0m || d.SayiDeger != decimal.Truncate(d.SayiDeger) || d.SayiDeger > ust)
            {
                hata(d?.Satir ?? kok.Satir, $"\"{anahtar}\" negatif olmayan bir tam sayı olmalı");
                return null;
            }

            return (ulong)d.SayiDeger;
        }

        private static EylemKaydi? EylemOku(JsonDegeri e, Action<int, string> hata)
        {
            if (e.Tur != JsonTuru.Nesne)
            {
                hata(e.Satir, "Eylem bir nesne olmalı (tick, tur, hedef, yuk)");
                return null;
            }

            var tamam = true;
            foreach (var kv in e.Ozellikler)
            {
                if (kv.Key != "tick" && kv.Key != "tur" && kv.Key != "hedef" && kv.Key != "yuk")
                {
                    hata(kv.Value.Satir, $"Eylemde bilinmeyen alan \"{kv.Key}\" (tick, tur, hedef, yuk)");
                    tamam = false;
                }
            }

            var tick = e.Al("tick");
            if (tick == null || tick.Tur != JsonTuru.Sayi || tick.SayiDeger < 1m || tick.SayiDeger != decimal.Truncate(tick.SayiDeger))
            {
                hata(tick?.Satir ?? e.Satir, "Eylemde \"tick\" 1 ya da daha büyük tam sayı olmalı");
                tamam = false;
            }

            var tur = e.Al("tur");
            if (tur == null || tur.Tur != JsonTuru.Metin || !Kimlik.GecerliKimlikMi(tur.MetinDeger))
            {
                hata(tur?.Satir ?? e.Satir, "Eylemde \"tur\" geçerli bir kimlik olmalı");
                tamam = false;
            }

            var hedefDugumu = e.Al("hedef");
            if (hedefDugumu != null && (hedefDugumu.Tur != JsonTuru.Metin || !Kimlik.GecerliKimlikMi(hedefDugumu.MetinDeger)))
            {
                hata(hedefDugumu.Satir, "Eylemde \"hedef\" geçerli bir kimlik olmalı");
                tamam = false;
            }

            var yuk = new List<KeyValuePair<string, string>>();
            var yukDugumu = e.Al("yuk");
            if (yukDugumu != null)
            {
                if (yukDugumu.Tur != JsonTuru.Nesne)
                {
                    hata(yukDugumu.Satir, "Eylemde \"yuk\" bir nesne olmalı");
                    tamam = false;
                }
                else
                {
                    foreach (var kv in yukDugumu.Ozellikler)
                    {
                        if (kv.Value.Tur != JsonTuru.Metin)
                        {
                            hata(kv.Value.Satir, $"Yükteki \"{kv.Key}\" değeri metin olmalı");
                            tamam = false;
                        }
                        else
                        {
                            yuk.Add(new KeyValuePair<string, string>(kv.Key, kv.Value.MetinDeger));
                        }
                    }
                }
            }

            return tamam
                ? new EylemKaydi((long)tick!.SayiDeger, new PlayerAction(tur!.MetinDeger, hedefDugumu?.MetinDeger ?? string.Empty, yuk))
                : null;
        }

        private static bool HashMi(string? s)
        {
            if (s == null || s.Length != 16)
            {
                return false;
            }

            foreach (var c in s)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>Bir oturumun oynatılma sonucu.</summary>
    public sealed class OynatmaSonucu
    {
        /// <summary>Kayıtlı beklenen hash.</summary>
        public string BeklenenHash { get; }

        /// <summary>Oynatma sonunda hesaplanan hash.</summary>
        public string GercekHash { get; }

        /// <summary>İkisi aynı mı.</summary>
        public bool HashTutuyor => string.Equals(BeklenenHash, GercekHash, StringComparison.Ordinal);

        /// <summary>Sonuç kurar.</summary>
        public OynatmaSonucu(string beklenenHash, string gercekHash)
        {
            BeklenenHash = beklenenHash;
            GercekHash = gercekHash;
        }
    }

    /// <summary>
    /// Oturum kaydını baştan oynatır: her tick için o tick'in eylemlerini gönderir, sonra tick'i ilerletir; sonunda hash'i
    /// kayıttakiyle karşılaştırır. Golden replay bunu kullanır.
    /// </summary>
    public static class OturumOynatici
    {
        /// <summary>Kaydı, yeni kurulmuş (tick 0) bir simülasyon üzerinde oynatır.</summary>
        /// <exception cref="ArgumentException">Simülasyonun seed'i kayıttakinden farklıysa.</exception>
        /// <exception cref="InvalidOperationException">Simülasyon tick 0'dan ilerlemişse ya da eylem bekliyorsa.</exception>
        public static OynatmaSonucu Oynat(Simulasyon sim, OturumKaydi kayit)
        {
            if (kayit == null)
            {
                throw new ArgumentNullException(nameof(kayit));
            }

            TickeKadarOynat(sim, kayit, kayit.TickSayisi);
            return new OynatmaSonucu(kayit.SonHash, sim.DurumHash());
        }

        /// <summary>
        /// Kaydı yeni kurulmuş (tick 0) simülasyon üzerinde yalnızca <paramref name="tick"/>'e kadar oynatır. Geçmiş bir kararın tam izini
        /// yeniden üretmek için kullanılır (<c>KararIziYenidenUretici</c>); oynatma deterministiktir.
        /// </summary>
        /// <exception cref="ArgumentException">Simülasyonun seed'i kayıttakinden farklıysa.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="tick"/> 0'dan küçükse ya da kaydın tick sayısından büyükse.</exception>
        /// <exception cref="InvalidOperationException">Simülasyon tick 0'dan ilerlemişse ya da eylem bekliyorsa.</exception>
        public static void TickeKadarOynat(Simulasyon sim, OturumKaydi kayit, long tick)
        {
            if (sim == null)
            {
                throw new ArgumentNullException(nameof(sim));
            }

            if (kayit == null)
            {
                throw new ArgumentNullException(nameof(kayit));
            }

            if (sim.Seed != kayit.Seed)
            {
                throw new ArgumentException(
                    $"Simülasyonun seed'i ({sim.Seed.ToString(CultureInfo.InvariantCulture)}) kayıttakinden ({kayit.Seed.ToString(CultureInfo.InvariantCulture)}) farklı.", nameof(sim));
            }

            if (sim.SimdikiTick != 0 || sim.EylemGunlugu.Count != 0)
            {
                throw new InvalidOperationException("Oynatma yeni kurulmuş (tick 0) bir simülasyon gerektirir.");
            }

            if (tick < 0 || tick > kayit.TickSayisi)
            {
                throw new ArgumentOutOfRangeException(nameof(tick), tick, $"Tick 0..{kayit.TickSayisi.ToString(CultureInfo.InvariantCulture)} aralığında olmalı.");
            }

            var sira = 0;
            for (long t = 1; t <= tick; t++)
            {
                while (sira < kayit.Eylemler.Count && kayit.Eylemler[sira].Tick == t)
                {
                    sim.Submit(kayit.Eylemler[sira].Eylem);
                    sira++;
                }

                sim.Tick(1);
            }
        }
    }
}
