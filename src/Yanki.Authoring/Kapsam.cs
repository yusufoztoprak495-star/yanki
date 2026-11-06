using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Yanki.Core.Brakes;
using Yanki.Core.Events;
using Yanki.Core.Json;
using Yanki.Core.Simulation;
using Yanki.Dialogue;

namespace Yanki.Authoring
{
    /// <summary>Oynanış betiği hatası: <c>dosya:satir: mesaj</c>.</summary>
    public sealed class BetikHatasi : Exception
    {
        /// <summary>Hata kurar.</summary>
        public BetikHatasi(string mesaj)
            : base(mesaj)
        {
        }
    }

    /// <summary>
    /// Sabit oyuncu betiği (<c>oynanislar/*.json</c>): ad, isteğe bağlı açıklama ve oturum kaydındaki biçimde eylem günlüğü
    /// (<c>{ "tick", "tur", "hedef", "yuk" }</c>). Betikler test senaryosudur ve Yusuf'un alanıdır.
    /// </summary>
    public sealed class Betik
    {
        /// <summary>Betik adı (rapor sütunu; <c>oyuncusuz</c> ayrılmıştır).</summary>
        public string Ad { get; }

        /// <summary>Açıklama (isteğe bağlı).</summary>
        public string Aciklama { get; }

        /// <summary>Oyuncu eylemleri, tick sırasıyla.</summary>
        public IReadOnlyList<EylemKaydi> Eylemler { get; }

        /// <summary>Betik kurar.</summary>
        public Betik(string ad, string aciklama, IEnumerable<EylemKaydi> eylemler)
        {
            Ad = ad ?? throw new ArgumentNullException(nameof(ad));
            Aciklama = aciklama ?? string.Empty;
            Eylemler = new ReadOnlyCollection<EylemKaydi>(new List<EylemKaydi>(eylemler ?? throw new ArgumentNullException(nameof(eylemler))));
        }
    }

    /// <summary>Betik metnini JSON okuyucusuyla (<see cref="JsonAyristirici"/>) okur.</summary>
    public static class BetikOkuyucu
    {
        /// <summary>Betik adı olarak ayrılmış değer (oyuncusuz koşu).</summary>
        public const string OyuncusuzAd = "oyuncusuz";

        /// <summary>Betiği okur ve doğrular.</summary>
        /// <exception cref="BetikHatasi">Bozuk betikte (<c>dosya:satir: mesaj</c>).</exception>
        public static Betik Oku(string json, string kaynakAdi)
        {
            JsonDegeri kok;
            try
            {
                kok = JsonAyristirici.Ayristir(json);
            }
            catch (JsonHatasi e)
            {
                throw new BetikHatasi(kaynakAdi + ":1: " + e.Message);
            }

            Exception Hata(int satir, string mesaj) => new BetikHatasi(kaynakAdi + ":" + satir.ToString(CultureInfo.InvariantCulture) + ": " + mesaj);
            if (kok.Tur != JsonTuru.Nesne)
            {
                throw Hata(kok.Satir, "betik bir nesne olmalı: { \"ad\", \"eylemler\": [ { tick, tur, hedef, yuk } ] }");
            }

            foreach (var kv in kok.Ozellikler)
            {
                if (kv.Key != "ad" && kv.Key != "aciklama" && kv.Key != "eylemler")
                {
                    throw Hata(kv.Value.Satir, "bilinmeyen alan \"" + kv.Key + "\" (ad, aciklama, eylemler)");
                }
            }

            var ad = kok.Al("ad");
            if (ad == null || ad.Tur != JsonTuru.Metin || ad.MetinDeger.Length == 0 || ad.MetinDeger == OyuncusuzAd)
            {
                throw Hata(ad?.Satir ?? kok.Satir, "\"ad\" gerekli (boş olamaz, \"" + OyuncusuzAd + "\" ayrılmıştır)");
            }

            var aciklama = kok.Al("aciklama");
            var liste = kok.Al("eylemler");
            if (liste == null || liste.Tur != JsonTuru.Dizi)
            {
                throw Hata(kok.Satir, "\"eylemler\" bir dizi olmalı");
            }

            var eylemler = new List<EylemKaydi>();
            long onceki = 1;
            foreach (var e in liste.Elemanlar)
            {
                if (e.Tur != JsonTuru.Nesne)
                {
                    throw Hata(e.Satir, "eylem bir nesne olmalı (tick, tur, hedef, yuk)");
                }

                foreach (var kv in e.Ozellikler)
                {
                    if (kv.Key != "tick" && kv.Key != "tur" && kv.Key != "hedef" && kv.Key != "yuk")
                    {
                        throw Hata(kv.Value.Satir, "eylemde bilinmeyen alan \"" + kv.Key + "\" (tick, tur, hedef, yuk)");
                    }
                }

                var tick = e.Al("tick");
                if (tick == null || tick.Tur != JsonTuru.Sayi || tick.SayiDeger < 1m || tick.SayiDeger != decimal.Truncate(tick.SayiDeger))
                {
                    throw Hata(tick?.Satir ?? e.Satir, "eylemde \"tick\" 1 ya da daha büyük tam sayı olmalı");
                }

                if ((long)tick.SayiDeger < onceki)
                {
                    throw Hata(tick.Satir, "eylemler tick sırasında olmalı");
                }

                onceki = (long)tick.SayiDeger;
                var tur = e.Al("tur");
                if (tur == null || tur.Tur != JsonTuru.Metin || tur.MetinDeger.Length == 0)
                {
                    throw Hata(tur?.Satir ?? e.Satir, "eylemde \"tur\" gerekli");
                }

                var hedef = e.Al("hedef");
                if (hedef != null && hedef.Tur != JsonTuru.Metin)
                {
                    throw Hata(hedef.Satir, "eylemde \"hedef\" metin olmalı");
                }

                var yuk = new List<KeyValuePair<string, string>>();
                var yukDugumu = e.Al("yuk");
                if (yukDugumu != null)
                {
                    if (yukDugumu.Tur != JsonTuru.Nesne)
                    {
                        throw Hata(yukDugumu.Satir, "eylemde \"yuk\" bir nesne olmalı");
                    }

                    foreach (var kv in yukDugumu.Ozellikler)
                    {
                        if (kv.Value.Tur != JsonTuru.Metin)
                        {
                            throw Hata(kv.Value.Satir, "yükteki \"" + kv.Key + "\" değeri metin olmalı");
                        }

                        yuk.Add(new KeyValuePair<string, string>(kv.Key, kv.Value.MetinDeger));
                    }
                }

                eylemler.Add(new EylemKaydi((long)tick.SayiDeger, new PlayerAction(tur.MetinDeger, hedef?.MetinDeger ?? string.Empty, yuk)));
            }

            return new Betik(ad.MetinDeger, aciklama != null && aciklama.Tur == JsonTuru.Metin ? aciklama.MetinDeger : string.Empty, eylemler);
        }
    }

    /// <summary>Kapsama eşikleri ve varsayılanlar (ayar sınıfı; plan 3: hiçbir kural tetiklenmelerin %30'undan fazlasını almaz).</summary>
    public sealed class KapsamAyarlari
    {
        /// <summary>Bir oyun içi hafta (plan 10), 1 tick = 1 saat.</summary>
        public const long VarsayilanTick = 168;

        /// <summary>Varsayılan seed sayısı (plan 3: 20 seed'lik toplu koşu).</summary>
        public const int VarsayilanSeedSayisi = 20;

        /// <summary>Bir kuralın payı bunu aşarsa işaretlenir (0.30 = %30).</summary>
        public decimal PayEsigi { get; set; } = 0.30m;
    }

    /// <summary>Girdi doğrulamadan geçemedi (coverage hatasız yüklenmiş senaryo ister).</summary>
    public sealed class KapsamGirisHatasi : Exception
    {
        /// <summary>Doğrulama bulguları.</summary>
        public IReadOnlyList<Bulgu> Bulgular { get; }

        /// <summary>Hata kurar.</summary>
        public KapsamGirisHatasi(IReadOnlyList<Bulgu> bulgular)
            : base("Senaryo doğrulamadan geçemedi: " + bulgular.Count.ToString(CultureInfo.InvariantCulture) + " bulgu.")
        {
            Bulgular = bulgular;
        }
    }

    /// <summary>Bir kimliğin (kural, eylem, kriz satırı, fren türü, şablon) koşulardaki sayısı.</summary>
    public sealed class KapsamSatiri
    {
        /// <summary>Kimlik.</summary>
        public string Id { get; }

        /// <summary>Betikli koşuların (betik × seed) toplamı; oyuncusuz koşu hariç.</summary>
        public int Toplam { get; }

        /// <summary>Oyuncusuz koşuların toplamı (bilgi amaçlı).</summary>
        public int OyuncusuzToplam { get; }

        /// <summary>Betik başına toplam, betik adına göre ordinal sıralı.</summary>
        public IReadOnlyList<KeyValuePair<string, int>> BetikBasina { get; }

        /// <summary>Satır kurar.</summary>
        public KapsamSatiri(string id, int toplam, int oyuncusuzToplam, IReadOnlyList<KeyValuePair<string, int>> betikBasina)
        {
            Id = id;
            Toplam = toplam;
            OyuncusuzToplam = oyuncusuzToplam;
            BetikBasina = betikBasina;
        }
    }

    /// <summary>Coverage sonucu: sayılar, işaretler ve Markdown rapor.</summary>
    public sealed class KapsamSonucu
    {
        /// <summary>Sonuç kuralları (kimliğe göre ordinal sıralı).</summary>
        public IReadOnlyList<KapsamSatiri> Kurallar { get; }

        /// <summary>NPC eylemleri (seçilme sayısı).</summary>
        public IReadOnlyList<KapsamSatiri> Eylemler { get; }

        /// <summary>Kriz satırları.</summary>
        public IReadOnlyList<KapsamSatiri> Krizler { get; }

        /// <summary>Fren türleri (yalnızca üretilenler).</summary>
        public IReadOnlyList<KapsamSatiri> Frenler { get; }

        /// <summary>Şablonlar (yalnızca diyalog açıksa).</summary>
        public IReadOnlyList<KapsamSatiri> Sablonlar { get; }

        /// <summary>Betikli koşularda hiç tetiklenmeyen kurallar.</summary>
        public IReadOnlyList<string> HicTetiklenmeyenKurallar { get; }

        /// <summary>Payı eşiği aşan kurallar.</summary>
        public IReadOnlyList<string> PayiAsanKurallar { get; }

        /// <summary>Hiç seçilmeyen şablonlar (bilgi amaçlı; çıkış koduna girmez).</summary>
        public IReadOnlyList<string> HicSecilmeyenSablonlar { get; }

        /// <summary>Plan 3 kriteri tuttu mu (her kural en az bir kez, hiçbiri eşiği aşmadan).</summary>
        public bool Basarili => HicTetiklenmeyenKurallar.Count == 0 && PayiAsanKurallar.Count == 0;

        /// <summary>Markdown rapor (yalnızca LF).</summary>
        public string Markdown { get; }

        internal KapsamSonucu(
            IReadOnlyList<KapsamSatiri> kurallar, IReadOnlyList<KapsamSatiri> eylemler, IReadOnlyList<KapsamSatiri> krizler, IReadOnlyList<KapsamSatiri> frenler,
            IReadOnlyList<KapsamSatiri> sablonlar, IReadOnlyList<string> hic, IReadOnlyList<string> pay, IReadOnlyList<string> hicSablon, string markdown)
        {
            Kurallar = kurallar;
            Eylemler = eylemler;
            Krizler = krizler;
            Frenler = frenler;
            Sablonlar = sablonlar;
            HicTetiklenmeyenKurallar = hic;
            PayiAsanKurallar = pay;
            HicSecilmeyenSablonlar = hicSablon;
            Markdown = markdown;
        }
    }

    /// <summary>
    /// <c>yanki coverage</c> (9. hafta, K kararı): senaryoyu her betik × seed için koşar, ayrıca oyuncusuz koşar. Sayım YALNIZCA olay günlüğünden ve diyalog satırlarından türer
    /// (KuralId, olay türü, şablon kimliği); simülasyona sayaç ya da yeni olay eklenmez. Bir "kural tetiklenmesi", motorun sayaçlarındaki gibi
    /// (kural, tick, npc, tetikleyici nedenler) imzasıdır. Payda yalnızca sonuç kuralı tetiklenmeleridir (algı, eylem, kriz, fren ve oyuncu eylemleri hariç).
    /// </summary>
    public static class KapsamHesabi
    {
        private sealed class Sayac
        {
            public readonly SortedDictionary<string, int> Kural = new SortedDictionary<string, int>(StringComparer.Ordinal);
            public readonly SortedDictionary<string, int> Eylem = new SortedDictionary<string, int>(StringComparer.Ordinal);
            public readonly SortedDictionary<string, int> Kriz = new SortedDictionary<string, int>(StringComparer.Ordinal);
            public readonly SortedDictionary<string, int> Fren = new SortedDictionary<string, int>(StringComparer.Ordinal);
            public readonly SortedDictionary<string, int> Sablon = new SortedDictionary<string, int>(StringComparer.Ordinal);
        }

        /// <summary>Senaryoyu doğrulayıp yükler; her betik × seed (1..seedSayisi) ve oyuncusuz koşuyu <paramref name="tick"/> tick koşar.</summary>
        /// <exception cref="KapsamGirisHatasi">Senaryo hatalıysa.</exception>
        /// <exception cref="BetikHatasi">Bir betik eylemi simülasyonda geçersizse.</exception>
        public static KapsamSonucu Hesapla(DogrulamaGirdisi girdi, IReadOnlyList<Betik> betikler, int seedSayisi, long tick, KapsamAyarlari ayar)
        {
            if (girdi == null)
            {
                throw new ArgumentNullException(nameof(girdi));
            }

            if (seedSayisi < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(seedSayisi), "En az 1 seed gerekli.");
            }

            if (tick < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(tick), "En az 1 tick gerekli.");
            }

            var dogrulama = SenaryoDogrulayici.Yukle(girdi, out var yuklenen);
            if (yuklenen == null)
            {
                throw new KapsamGirisHatasi(dogrulama.Bulgular);
            }

            var sirali = new List<Betik>(betikler);
            sirali.Sort((a, b) => string.CompareOrdinal(a.Ad, b.Ad));
            for (var i = 1; i < sirali.Count; i++)
            {
                if (sirali[i].Ad == sirali[i - 1].Ad)
                {
                    throw new BetikHatasi("betik adı tekrar ediyor: " + sirali[i].Ad);
                }
            }

            var kuralKimlikleri = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var k in yuklenen.Kurallar)
            {
                kuralKimlikleri.Add(k.Id);
            }

            var eylemKimlikleri = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var e in yuklenen.Eylemler)
            {
                eylemKimlikleri.Add(e.Id);
            }

            var krizKimlikleri = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var k in yuklenen.Krizler)
            {
                krizKimlikleri.Add(k.Id);
            }

            var sablonKimlikleri = new SortedSet<string>(StringComparer.Ordinal);
            if (yuklenen.Sablon != null)
            {
                foreach (var s in yuklenen.Sablon.Sablonlar)
                {
                    sablonKimlikleri.Add(s.Id);
                }
            }

            var betikSayaclari = new SortedDictionary<string, Sayac>(StringComparer.Ordinal);
            foreach (var b in sirali)
            {
                betikSayaclari[b.Ad] = new Sayac();
            }

            var oyuncusuz = new Sayac();
            var pasif = new Betik(BetikOkuyucu.OyuncusuzAd, string.Empty, new EylemKaydi[0]);
            for (ulong seed = 1; seed <= (ulong)seedSayisi; seed++)
            {
                foreach (var b in sirali)
                {
                    Kos(yuklenen, b, seed, tick, betikSayaclari[b.Ad], kuralKimlikleri, eylemKimlikleri, krizKimlikleri);
                }

                Kos(yuklenen, pasif, seed, tick, oyuncusuz, kuralKimlikleri, eylemKimlikleri, krizKimlikleri);
            }

            var betikAdlari = new List<string>(betikSayaclari.Keys);
            var kurallar = Satirlar(kuralKimlikleri, betikAdlari, betikSayaclari, oyuncusuz, s => s.Kural);
            var eylemler = Satirlar(eylemKimlikleri, betikAdlari, betikSayaclari, oyuncusuz, s => s.Eylem);
            var krizler = Satirlar(krizKimlikleri, betikAdlari, betikSayaclari, oyuncusuz, s => s.Kriz);
            var frenKimlikleri = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var s in betikSayaclari.Values)
            {
                frenKimlikleri.UnionWith(s.Fren.Keys);
            }

            frenKimlikleri.UnionWith(oyuncusuz.Fren.Keys);
            var frenler = Satirlar(frenKimlikleri, betikAdlari, betikSayaclari, oyuncusuz, s => s.Fren);
            var sablonlar = Satirlar(sablonKimlikleri, betikAdlari, betikSayaclari, oyuncusuz, s => s.Sablon);

            var hic = new List<string>();
            var asan = new List<string>();
            var toplamTetik = 0;
            foreach (var k in kurallar)
            {
                toplamTetik += k.Toplam;
            }

            foreach (var k in kurallar)
            {
                if (k.Toplam == 0)
                {
                    hic.Add(k.Id);
                }
                else if (toplamTetik > 0 && (decimal)k.Toplam / toplamTetik > ayar.PayEsigi)
                {
                    asan.Add(k.Id);
                }
            }

            var hicSablon = new List<string>();
            foreach (var s in sablonlar)
            {
                if (s.Toplam == 0)
                {
                    hicSablon.Add(s.Id);
                }
            }

            var md = Rapor.Yaz(yuklenen.Senaryo.Ad, seedSayisi, tick, betikAdlari, kurallar, eylemler, krizler, frenler, sablonlar, toplamTetik, hic, asan, hicSablon, ayar, yuklenen.Sablon != null);
            return new KapsamSonucu(kurallar, eylemler, krizler, frenler, sablonlar, hic, asan, hicSablon, md);
        }

        /// <summary>Test ve ölçüm için: tek betik ve seed'i koşup simülasyonu döndürür (diyalog bağlanmaz).</summary>
        public static Simulasyon TestIcinKos(DogrulamaGirdisi girdi, Betik betik, ulong seed, long tick)
        {
            SenaryoDogrulayici.Yukle(girdi, out var yuklenen);
            if (yuklenen == null)
            {
                throw new KapsamGirisHatasi(SenaryoDogrulayici.Dogrula(girdi).Bulgular);
            }

            var sim = yuklenen.SimulasyonKur(seed);
            Oyna(sim, betik, tick);
            return sim;
        }

        private static IReadOnlyList<KapsamSatiri> Satirlar(
            SortedSet<string> kimlikler, List<string> betikAdlari, SortedDictionary<string, Sayac> betikSayaclari, Sayac oyuncusuz, Func<Sayac, SortedDictionary<string, int>> sec)
        {
            var sonuc = new List<KapsamSatiri>();
            foreach (var id in kimlikler)
            {
                var toplam = 0;
                var betikBasina = new List<KeyValuePair<string, int>>();
                foreach (var ad in betikAdlari)
                {
                    sec(betikSayaclari[ad]).TryGetValue(id, out var n);
                    toplam += n;
                    betikBasina.Add(new KeyValuePair<string, int>(ad, n));
                }

                sec(oyuncusuz).TryGetValue(id, out var o);
                sonuc.Add(new KapsamSatiri(id, toplam, o, new ReadOnlyCollection<KeyValuePair<string, int>>(betikBasina)));
            }

            return new ReadOnlyCollection<KapsamSatiri>(sonuc);
        }

        private static void Oyna(Simulasyon sim, Betik betik, long tick)
        {
            var sira = 0;
            for (long t = 1; t <= tick; t++)
            {
                while (sira < betik.Eylemler.Count && betik.Eylemler[sira].Tick == t)
                {
                    try
                    {
                        sim.Submit(betik.Eylemler[sira].Eylem);
                    }
                    catch (ArgumentException e)
                    {
                        throw new BetikHatasi("betik \"" + betik.Ad + "\" tick " + t.ToString(CultureInfo.InvariantCulture) + ": " + e.Message);
                    }

                    sira++;
                }

                sim.Tick(1);
            }
        }

        private static void Kos(
            YuklenenSenaryo yuklenen, Betik betik, ulong seed, long tick, Sayac sayac,
            SortedSet<string> kuralKimlikleri, SortedSet<string> eylemKimlikleri, SortedSet<string> krizKimlikleri)
        {
            var sim = yuklenen.SimulasyonKur(seed);
            DiyalogMotoru? motor = yuklenen.Sablon != null ? DiyalogMotoru.Baglan(sim, yuklenen.Sablon) : null;
            try
            {
                Oyna(sim, betik, tick);
                Say(sim.Gunluk.Olaylar, sayac, kuralKimlikleri, eylemKimlikleri, krizKimlikleri);
                if (motor != null)
                {
                    foreach (var satir in motor.Satirlar)
                    {
                        Artir(sayac.Sablon, satir.SablonId);
                    }
                }
            }
            finally
            {
                motor?.Dispose();
            }
        }

        /// <summary>Günlükten sayım: yalnızca olaylar (KuralId, tür, yük); simülasyon durumuna bakmaz.</summary>
        private static void Say(
            IReadOnlyList<Olay> olaylar, Sayac sayac, SortedSet<string> kuralKimlikleri, SortedSet<string> eylemKimlikleri, SortedSet<string> krizKimlikleri)
        {
            var kuralImzalari = new SortedSet<string>(StringComparer.Ordinal);
            var eylemImzalari = new SortedSet<string>(StringComparer.Ordinal);
            var krizImzalari = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var o in olaylar)
            {
                if (o.Tur == FrenOlayi.Tur)
                {
                    if (o.KuralId.StartsWith("fren:", StringComparison.Ordinal))
                    {
                        Artir(sayac.Fren, o.KuralId.Substring(5));
                    }

                    continue;
                }

                var tick = o.Tick.ToString(CultureInfo.InvariantCulture);
                if (o.KuralId.StartsWith("eylem:", StringComparison.Ordinal))
                {
                    var id = o.KuralId.Substring(6);
                    if (eylemKimlikleri.Contains(id) && o.Fail.Length > 0 && eylemImzalari.Add(id + "|" + tick + "|" + o.Fail))
                    {
                        Artir(sayac.Eylem, id);
                    }

                    continue;
                }

                if (o.KuralId.StartsWith("kriz:", StringComparison.Ordinal))
                {
                    var id = o.KuralId.Substring(5);
                    if (krizKimlikleri.Contains(id) && krizImzalari.Add(id + "|" + tick + "|" + o.Fail))
                    {
                        Artir(sayac.Kriz, id);
                    }

                    continue;
                }

                if (kuralKimlikleri.Contains(o.KuralId))
                {
                    var npc = Yuk(o, "npc");
                    if (npc.Length == 0)
                    {
                        npc = Yuk(o, "ben");
                    }

                    if (kuralImzalari.Add(o.KuralId + "|" + tick + "|" + npc + "|" + string.Join(",", o.NedenOlaylar)))
                    {
                        Artir(sayac.Kural, o.KuralId);
                    }
                }
            }
        }

        private static string Yuk(Olay olay, string anahtar)
        {
            foreach (var kv in olay.Yuk)
            {
                if (string.Equals(kv.Key, anahtar, StringComparison.Ordinal))
                {
                    return kv.Value;
                }
            }

            return string.Empty;
        }

        private static void Artir(SortedDictionary<string, int> sozluk, string anahtar)
        {
            sozluk.TryGetValue(anahtar, out var n);
            sozluk[anahtar] = n + 1;
        }

        private static class Rapor
        {
            public static string Yaz(
                string ad, int seedSayisi, long tick, List<string> betikler, IReadOnlyList<KapsamSatiri> kurallar, IReadOnlyList<KapsamSatiri> eylemler, IReadOnlyList<KapsamSatiri> krizler,
                IReadOnlyList<KapsamSatiri> frenler, IReadOnlyList<KapsamSatiri> sablonlar, int toplamTetik, List<string> hic, List<string> asan, List<string> hicSablon,
                KapsamAyarlari ayar, bool diyalogAcik)
            {
                var sb = new StringBuilder();
                var esikYuzde = Yuzde(ayar.PayEsigi * 100m, 0);
                sb.Append("# Kapsama raporu: ").Append(ad).Append('\n').Append('\n');
                sb.Append("- Seed sayısı: ").Append(seedSayisi.ToString(CultureInfo.InvariantCulture)).Append(" (1..").Append(seedSayisi.ToString(CultureInfo.InvariantCulture)).Append(")\n");
                sb.Append("- Tick: ").Append(tick.ToString(CultureInfo.InvariantCulture)).Append('\n');
                sb.Append("- Betikler: ").Append(betikler.Count == 0 ? "(yok)" : string.Join(", ", betikler)).Append(" (ayrıca oyuncusuz koşu, bilgi amaçlı)\n");
                sb.Append("- Pay eşiği: %").Append(esikYuzde).Append(" (payda: betikli koşulardaki sonuç kuralı tetiklenmeleri, ").Append(toplamTetik.ToString(CultureInfo.InvariantCulture)).Append(")\n");
                sb.Append("- Plan 3 kriteri: ").Append(hic.Count == 0 && asan.Count == 0 ? "TUTTU" : "TUTMADI").Append('\n').Append('\n');

                sb.Append("## Sonuç kuralları\n\n");
                Baslik(sb, betikler, "tetiklenme", "pay");
                foreach (var k in kurallar)
                {
                    var isaret = k.Toplam == 0 ? "hiç tetiklenmedi" : (toplamTetik > 0 && (decimal)k.Toplam / toplamTetik > ayar.PayEsigi ? "pay %" + esikYuzde + "'u aşıyor" : string.Empty);
                    Satir(sb, k, toplamTetik > 0 ? Yuzde(k.Toplam * 100m / toplamTetik, 1) : "0.0", isaret);
                }

                sb.Append('\n').Append("## Eylemler (seçilme)\n\n");
                Baslik(sb, betikler, "seçilme", string.Empty);
                foreach (var e in eylemler)
                {
                    Satir(sb, e, null, e.Toplam == 0 && e.OyuncusuzToplam == 0 ? "hiç seçilmedi" : string.Empty);
                }

                sb.Append('\n').Append("## Kriz satırları\n\n");
                Baslik(sb, betikler, "tetiklenme", string.Empty);
                foreach (var k in krizler)
                {
                    Satir(sb, k, null, k.Toplam == 0 && k.OyuncusuzToplam == 0 ? "hiç tetiklenmedi" : string.Empty);
                }

                sb.Append('\n').Append("## Frenler (tür başına olay)\n\n");
                Baslik(sb, betikler, "olay", string.Empty);
                foreach (var f in frenler)
                {
                    Satir(sb, f, null, string.Empty);
                }

                if (diyalogAcik)
                {
                    sb.Append('\n').Append("## Şablonlar (seçilme)\n\n");
                    Baslik(sb, betikler, "seçilme", string.Empty);
                    foreach (var s in sablonlar)
                    {
                        Satir(sb, s, null, s.Toplam == 0 && s.OyuncusuzToplam == 0 ? "hiç seçilmedi" : string.Empty);
                    }
                }

                sb.Append('\n').Append("## İşaretler\n\n");
                sb.Append("- Hiç tetiklenmeyen kurallar: ").Append(hic.Count == 0 ? "yok" : string.Join(", ", hic)).Append('\n');
                sb.Append("- Payı %").Append(esikYuzde).Append("'u aşan kurallar: ").Append(asan.Count == 0 ? "yok" : string.Join(", ", asan)).Append('\n');
                if (diyalogAcik)
                {
                    sb.Append("- Hiç seçilmeyen şablonlar: ").Append(hicSablon.Count == 0 ? "yok" : string.Join(", ", hicSablon)).Append('\n');
                }

                return sb.ToString();
            }

            private static void Baslik(StringBuilder sb, List<string> betikler, string sayiAdi, string payAdi)
            {
                sb.Append("| kimlik | ").Append(sayiAdi).Append(' ');
                if (payAdi.Length > 0)
                {
                    sb.Append("| ").Append(payAdi).Append(" (%) ");
                }

                foreach (var b in betikler)
                {
                    sb.Append("| ").Append(b).Append(' ');
                }

                sb.Append("| oyuncusuz | işaret |\n");
                sb.Append("|---|---:|");
                if (payAdi.Length > 0)
                {
                    sb.Append("---:|");
                }

                foreach (var unused in betikler)
                {
                    sb.Append("---:|");
                }

                sb.Append("---:|---|\n");
            }

            private static void Satir(StringBuilder sb, KapsamSatiri s, string? pay, string isaret)
            {
                sb.Append("| ").Append(s.Id).Append(" | ").Append(s.Toplam.ToString(CultureInfo.InvariantCulture)).Append(' ');
                if (pay != null)
                {
                    sb.Append("| ").Append(pay).Append(' ');
                }

                foreach (var b in s.BetikBasina)
                {
                    sb.Append("| ").Append(b.Value.ToString(CultureInfo.InvariantCulture)).Append(' ');
                }

                sb.Append("| ").Append(s.OyuncusuzToplam.ToString(CultureInfo.InvariantCulture)).Append(" | ").Append(isaret).Append(" |\n");
            }

            private static string Yuzde(decimal deger, int basamak) =>
                Math.Round(deger, basamak, MidpointRounding.AwayFromZero).ToString(basamak == 0 ? "0" : "0." + new string('0', basamak), CultureInfo.InvariantCulture);
        }
    }
}
