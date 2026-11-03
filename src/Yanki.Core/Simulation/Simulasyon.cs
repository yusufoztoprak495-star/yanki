using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Yanki.Core.Decision;
using Yanki.Core.Criteria;
using Yanki.Core.Events;
using Yanki.Core.Mind;
using Yanki.Core.Perception;
using Yanki.Core.Rules;
using Yanki.Core.World;
using KararIziVerisi = Yanki.Core.Decision.KararIzi;

namespace Yanki.Core.Simulation
{
    /// <summary>
    /// Simülasyonun dış API'si (plan 5.8) ve tick döngüsü (plan 6). Durumun tek kaynağı olay günlüğüdür (ADR-001): dünya ve
    /// zihinler her olay eklenir eklenmez uygulanır. <see cref="Submit"/> oyuncu eylemini bekletir, <see cref="Tick"/> ilerletir.
    /// <para>1 tick = 1 oyun saati (docs/notes/zihin.md, Karar (Yusuf)); çekirdek bunu varsaymaz, sönme oranları böyle seçilmiştir.</para>
    /// <para>
    /// Bir tick'te çalışan adımlar (plan 6): (1) bekleyen oyuncu eylemleri olay olur (oyuncu hareketi dahil: oyuncunun konumu hemen
    /// değişir), (2) algı: dış olayları görenlere kanıt ve hafıza yazılır, (3) sonuç kuralları, (4) sönüm ve süre dolumu (yazılacak durum yok,
    /// ADR-008), (5) her NPC için eylem seçimi, (6) seçimlerin olaya dönüşmesi, (7) durum hash'i ve olay yayını.
    /// </para>
    /// <para>
    /// Adım 5: bütün NPC'ler adım başındaki AYNI durumu görür; kararlar NPC kimlik sırasıyla hesaplanır ve hiçbiri yazılmadan önce
    /// hepsi hesaplanır. Aday yoksa olay yazılmaz; aday var ama seçim yoksa boş seçimli <c>karar_verildi</c> yazılır.
    /// Adım 6, sırayla: (1) <c>karar_verildi</c>, eylem olayı (hareket eyleminde <c>ayrildi</c> ve <c>geldi</c>) NPC kimlik sırasıyla;
    /// (2) bu dış olayların algısı, adım 5 başındaki konum görüntüsüyle (<c>konum!</c> henüz değişmedi); (3) eylem etkileri ve <c>konum!</c>
    /// değişimleri, en son. Böylece aynı tick'te mekandan ayrılan tanık olayı görür, aynı tick'te varan görmez ve sonuç NPC işleme
    /// sırasına bağlı olmaz.
    /// </para>
    /// <para>
    /// Kaskad (v0): kuralların ve eylem etkilerinin ürettiği olaylar aynı tick'te yeniden kural tetiklemez; bir sonraki tick'in başına
    /// devredilir. NPC eylem olayları da sonraki tick'te kurallara girer; algılayanları o tick'te adım 6'da yazılan algı olaylarından
    /// gelir (saklanır, sonra atılır) ve kurallar bunları yeniden hesaplamaz. Sonraki tick'in algı adımı bu olayları ikinci kez işlemez.
    /// Sonraki tick'in tetik kuyruğu <see cref="Olay.Id"/> artan sırayla kurulur. Zincir derinliği sınırı 7. haftadadır.
    /// </para>
    /// </summary>
    public sealed class Simulasyon
    {
        private readonly KuralMotoru _kuralMotoru;
        private readonly EylemKatalogu _eylemKatalogu;
        private readonly IActionSelector _secici;
        private readonly AlgiAyarlari _algiAyarlari;
        private readonly DedikoduAyarlari _dedikodu;
        private readonly StresAyarlari _stresAyarlari;
        private readonly IReadOnlyList<KrizSatiri> _krizSatirlari;
        private readonly List<PlayerAction> _bekleyenEylemler = new List<PlayerAction>();
        private readonly List<EylemKaydi> _eylemGunlugu = new List<EylemKaydi>();
        private readonly List<Olay> _devredilenler = new List<Olay>();
        private readonly SortedSet<string> _eylemTurleri;
        private readonly SortedDictionary<string, KararIziVerisi> _sonKararlar = new SortedDictionary<string, KararIziVerisi>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, long> _sonKararOlaylari = new SortedDictionary<string, long>(StringComparer.Ordinal);

        // Açıkça istenen gelecek kararların tam izleri: "npc@tick" anahtarı. Başka hiçbir tam iz saklanmaz (K3).
        private readonly SortedSet<string> _istenenIzler = new SortedSet<string>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, KararIziVerisi> _yakalananIzler = new SortedDictionary<string, KararIziVerisi>(StringComparer.Ordinal);

        // Adım 6'da yazılan NPC eylem/hareket olayları ve algı sonuçları: yalnızca sıradaki tick'in kural değerlendirmesinde kullanılıp atılır.
        private List<Olay> _saklananOlaylar = new List<Olay>();
        private AlgiHaritasi _saklananAlgi = new AlgiHaritasi();
        private long _tick;

        /// <summary>Senaryo.</summary>
        public SenaryoTanimi Senaryo { get; }

        /// <summary>Seed. Karar adımı <see cref="AnahtarliRng"/> ile bunu kullanır.</summary>
        public ulong Seed { get; }

        /// <summary>Dünya durumu.</summary>
        public Dunya Dunya { get; } = new Dunya();

        /// <summary>NPC zihinleri.</summary>
        public ZihinDeposu Zihinler { get; } = new ZihinDeposu();

        /// <summary>Olay günlüğü.</summary>
        public OlayGunlugu Gunluk { get; } = new OlayGunlugu();

        /// <summary>Şu anki tick; kurulumdan hemen sonra 0.</summary>
        public long SimdikiTick => _tick;

        /// <summary>Oyuncu varlığının kimliği.</summary>
        public string OyuncuId => Senaryo.OyuncuId;

        /// <summary>Son tick'in sonunda alınan durum hash'i; hiç tick işlenmediyse kurulum sonrası hash.</summary>
        public string SonHash { get; private set; }

        /// <summary>İşlenmiş oyuncu eylemleri, tick sırasıyla (golden replay ve kayıt satırları). Oyuncu hareketi de buradadır.</summary>
        public IReadOnlyList<EylemKaydi> EylemGunlugu => new ReadOnlyCollection<EylemKaydi>(_eylemGunlugu);

        /// <summary>Yüklü kurallar, kimliğe göre ordinal sıralı.</summary>
        public IReadOnlyList<KuralTanimi> Kurallar => _kuralMotoru.Kurallar;

        /// <summary>Yüklü NPC eylemleri, kimliğe göre ordinal sıralı; boşsa NPC karar adımı olay üretmez.</summary>
        public IReadOnlyList<EylemTanimi> Eylemler => _eylemKatalogu.Eylemler;

        /// <summary>
        /// Her tick'in sonunda (adım 7) o tick'te günlüğe eklenen olaylar, ekleniş sırasıyla bildirilir. Oyun buna abone olup sahneyi günceller.
        /// </summary>
        public event Action<Olay>? OlayYayinlandi;

        private Simulasyon(
            SenaryoTanimi senaryo, KuralMotoru motor, EylemKatalogu katalog, IActionSelector secici, ulong seed, AlgiAyarlari algi,
            DedikoduAyarlari dedikodu, StresAyarlari stres, IReadOnlyList<KrizSatiri> krizler)
        {
            Senaryo = senaryo;
            _kuralMotoru = motor;
            _eylemKatalogu = katalog;
            _secici = secici;
            Seed = seed;
            _algiAyarlari = algi;
            _dedikodu = dedikodu;
            _stresAyarlari = stres;
            _krizSatirlari = krizler;
            _eylemTurleri = new SortedSet<string>(senaryo.OlayTurleri, StringComparer.Ordinal);
            SonHash = string.Empty;
        }

        /// <summary>
        /// Senaryodan simülasyon kurar: başlangıç dünyası tick 0'da <c>KuralId: senaryo</c> olaylarıyla yazılır, zihinler kurulur. Eylem seçici verilmezse varsayılan <see cref="UtilitySelector"/> (ilk 3, %70) kullanılır.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Bir kuralın <c>ne_zaman</c> türü (senaryonun oyuncu eylem türleri, çekirdek olayları ve <paramref name="eylemler"/>'in olay türleri dışında)
        /// ya da bir kural/eylemin sabit etki hedefi bu senaryoda yoksa; eylem kimliği tekrar ediyorsa.
        /// </exception>
        public static Simulasyon Olustur(
            SenaryoTanimi senaryo, IEnumerable<KuralTanimi> kurallar, ulong seed, ZihinAyarlari? zihinAyarlari = null, AlgiAyarlari? algiAyarlari = null,
            IEnumerable<EylemTanimi>? eylemler = null, IActionSelector? secici = null,
            DedikoduAyarlari? dedikoduAyarlari = null, StresAyarlari? stresAyarlari = null, IEnumerable<KrizSatiri>? krizler = null)
        {
            if (senaryo == null)
            {
                throw new ArgumentNullException(nameof(senaryo));
            }

            var motor = new KuralMotoru(kurallar ?? throw new ArgumentNullException(nameof(kurallar)));
            var katalog = new EylemKatalogu(eylemler ?? new EylemTanimi[0]);
            var dedikodu = dedikoduAyarlari ?? new DedikoduAyarlari();
            dedikodu.Dogrula();
            var stres = stresAyarlari ?? new StresAyarlari();
            stres.Dogrula();
            var krizListesi = KrizleriDenetle(senaryo, krizler, stres);
            KurallariDenetle(senaryo, motor, katalog, krizListesi);
            EylemleriDenetle(senaryo, katalog);
            var sim = new Simulasyon(senaryo, motor, katalog, secici ?? new UtilitySelector(), seed, algiAyarlari ?? new AlgiAyarlari(), dedikodu, stres, krizListesi);
            sim.Kur(zihinAyarlari);
            sim.SonHash = sim.DurumHash();
            return sim;
        }

        /// <summary>
        /// Oyuncu eylemini bekletir; bir sonraki <see cref="Tick"/>'te olaya dönüşür. Eylemler <c>Submit</c> sırasıyla işlenir.
        /// <c>hareket</c> (<see cref="YerlesikTurler.OyuncuHareketi"/>) senaryoda bildirilmeden kullanılır: hedefi oyuncunun mekanına bağlı
        /// komşu bir mekan olmalıdır ve tick başına bir kez kabul edilir.
        /// </summary>
        /// <exception cref="ArgumentException">Tür senaryoda tanımlı değilse, hedef yoksa, görünürlük bilinmiyorsa ya da hareket geçersizse.</exception>
        public void Submit(PlayerAction eylem)
        {
            if (eylem == null)
            {
                throw new ArgumentNullException(nameof(eylem));
            }

            if (eylem.Tur == YerlesikTurler.OyuncuHareketi)
            {
                HareketiDenetle(eylem);
                _bekleyenEylemler.Add(eylem);
                return;
            }

            if (eylem.Tur == YerlesikTurler.Anlatti)
            {
                AnlatimiDenetle(eylem);
                _bekleyenEylemler.Add(eylem);
                return;
            }

            if (!_eylemTurleri.Contains(eylem.Tur))
            {
                throw new ArgumentException($"Eylem türü senaryoda tanımlı değil: \"{eylem.Tur}\".", nameof(eylem));
            }

            if (eylem.Hedef.Length > 0 && !Dunya.VarlikVar(eylem.Hedef))
            {
                throw new ArgumentException($"Eylemin hedefi dünyada yok: \"{eylem.Hedef}\".", nameof(eylem));
            }

            YukuDenetle(eylem);
            _bekleyenEylemler.Add(eylem);
        }

        private static void YukuDenetle(PlayerAction eylem)
        {
            foreach (var kv in eylem.Yuk)
            {
                if (kv.Key.Length == 0 || kv.Value == null)
                {
                    throw new ArgumentException("Eylem yükünde boş anahtar ya da null değer olamaz.", nameof(eylem));
                }

                if (kv.Key == "gorunurluk" && kv.Value != "acik" && kv.Value != "sessiz" && kv.Value != "gizli")
                {
                    throw new ArgumentException($"Bilinmeyen görünürlük \"{kv.Value}\" (acik, sessiz, gizli).", nameof(eylem));
                }
            }
        }

        // Oyuncu anlatımı: hedef oyuncuyla aynı mekandaki (bekleyen harekete göre varış mekanı) bir NPC olmalı; yük yalnızca gecerli bir
        // "onerme" ve isteğe bağlı "gorunurluk" (sessiz | acik) taşır. "kok" motor tarafından yazılır, oyuncu gönderemez.
        private void AnlatimiDenetle(PlayerAction eylem)
        {
            if (eylem.Hedef.Length == 0 || Dunya.TurOf(eylem.Hedef) != VarlikTuru.Npc)
            {
                throw new ArgumentException($"Anlatımın hedefi dünyada var olan bir NPC olmalı: \"{eylem.Hedef}\".", nameof(eylem));
            }

            var konum = Dunya.TekDeger("konum!", OyuncuId) ?? string.Empty;
            foreach (var bekleyen in _bekleyenEylemler)
            {
                if (bekleyen.Tur == YerlesikTurler.OyuncuHareketi)
                {
                    konum = bekleyen.Hedef;
                }
            }

            if (!string.Equals(Dunya.TekDeger("konum!", eylem.Hedef), konum, StringComparison.Ordinal))
            {
                throw new ArgumentException($"Anlatımın hedefi oyuncuyla aynı mekanda olmalı: \"{eylem.Hedef}\" \"{konum}\" mekanında değil.", nameof(eylem));
            }

            string? onerme = null;
            foreach (var kv in eylem.Yuk)
            {
                if (kv.Key == "onerme")
                {
                    onerme = kv.Value;
                }
                else if (kv.Key == "gorunurluk")
                {
                    if (kv.Value != "sessiz" && kv.Value != "acik")
                    {
                        throw new ArgumentException($"Anlatımın görünürlüğü sessiz ya da acik olmalı: \"{kv.Value}\".", nameof(eylem));
                    }
                }
                else
                {
                    throw new ArgumentException($"Anlatım yalnızca \"onerme\" ve \"gorunurluk\" yükü alır; \"{kv.Key}\" yazılamaz.", nameof(eylem));
                }
            }

            if (string.IsNullOrWhiteSpace(onerme))
            {
                throw new ArgumentException("Anlatımın yükünde gecerli bir \"onerme\" gerekli.", nameof(eylem));
            }

            Mind.Onerme gecerli;
            try
            {
                gecerli = Mind.Onerme.Ayristir(onerme!);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException($"Anlatımın önermesi geçersiz: {e.Message}", nameof(eylem));
            }

            if (string.Equals(gecerli.Yuklem, "anlatti", StringComparison.Ordinal))
            {
                throw new ArgumentException("anlatti önermesi anlatılamaz (konuşma içeriği dedikodu konusu olmaz).", nameof(eylem));
            }
        }

        private void HareketiDenetle(PlayerAction eylem)
        {
            if (eylem.Hedef.Length == 0 || Dunya.TurOf(eylem.Hedef) != VarlikTuru.Mekan)
            {
                throw new ArgumentException($"Hareketin hedefi dünyada var olan bir mekan olmalı: \"{eylem.Hedef}\".", nameof(eylem));
            }

            foreach (var bekleyen in _bekleyenEylemler)
            {
                if (bekleyen.Tur == YerlesikTurler.OyuncuHareketi)
                {
                    throw new ArgumentException("Bir tick'te yalnızca bir hareket kabul edilir.", nameof(eylem));
                }
            }

            foreach (var kv in eylem.Yuk)
            {
                if (kv.Key != "gorunurluk")
                {
                    throw new ArgumentException($"Hareket yalnızca \"gorunurluk\" yükü alır; \"{kv.Key}\" yazılamaz.", nameof(eylem));
                }
            }

            YukuDenetle(eylem);

            var konum = Dunya.TekDeger("konum!", OyuncuId) ?? string.Empty;
            if (!Dunya.BagliMi(konum, eylem.Hedef))
            {
                throw new ArgumentException($"\"{eylem.Hedef}\" mekanı oyuncunun mekanına (\"{konum}\") bağlı değil; yalnızca bağlı komşu mekana gidilir.", nameof(eylem));
            }
        }

        /// <summary>Simülasyonu <paramref name="n"/> adım ilerletir.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> negatifse.</exception>
        public void Tick(int n)
        {
            if (n < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(n), n, "Tick sayısı negatif olamaz.");
            }

            for (var i = 0; i < n; i++)
            {
                TekTick();
            }
        }

        /// <summary>Olayın nedensellik zinciri: ona doğrudan ya da dolaylı yol açan olaylar, Id'si azalan sırada (plan 5.8).</summary>
        /// <exception cref="ArgumentOutOfRangeException">Olay günlükte yoksa.</exception>
        public IReadOnlyList<Olay> Neden(long olayId) => Gunluk.Neden(olayId);

        /// <summary>NPC'nin son karar izi (bütün adaylar, puan bileşenleri, seçim); NPC henüz karar vermediyse ya da NPC değilse <c>null</c>.</summary>
        public KararIziVerisi? KararIzi(string npc) => npc != null && _sonKararlar.TryGetValue(npc, out var iz) ? iz : null;

        /// <summary>NPC'nin son <c>karar_verildi</c> olayı; yoksa <c>null</c>.</summary>
        public Olay? SonKararOlayi(string npc) => npc != null && _sonKararOlaylari.TryGetValue(npc, out var id) ? Gunluk[id] : null;

        /// <summary>
        /// Geçmiş bir kararın izi: günlükteki KISA iz (K3). Simülasyon geçmişe gidemez ve geçmiş tam izleri tutmaz; tam iz için oturumu
        /// replay ile o karara kadar oynatan <c>KararIziYenidenUretici</c> kullanılır (gizli önbellek yoktur).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Olay günlükte yoksa.</exception>
        /// <exception cref="ArgumentException">Olay bir karar_verildi olayı değilse.</exception>
        public KisaIz GecmisKararKisaIzi(long kararOlayId) => KisaIz.Oku(Gunluk[kararOlayId]);

        /// <summary>
        /// Bu NPC'nin <paramref name="tick"/>'teki kararının TAM izinin yakalanmasını ister; <see cref="YakalananIz"/> ile okunur.
        /// Yalnızca henüz işlenmemiş (gelecek) tick'ler istenebilir: geçmiş tam izler tutulmaz, replay ile yeniden üretilir.
        /// Yakalama durum hash'ini ve olay günlüğünü değiştirmez; yalnızca istenen (NPC, tick) çiftinin izi saklanır.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="tick"/> işlenmişse.</exception>
        /// <exception cref="ArgumentException">NPC yoksa.</exception>
        public void TamIziIste(string npc, long tick)
        {
            if (npc == null || !Zihinler.Var(npc))
            {
                throw new ArgumentException($"\"{npc}\" bir NPC değil.", nameof(npc));
            }

            if (tick <= _tick)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tick), tick, $"Tick {tick} zaten işlendi (şu an {_tick}): geçmiş kararların tam izi tutulmaz; oturumu replay ile yeniden üret (KararIziYenidenUretici).");
            }

            _istenenIzler.Add(YakalamaAnahtari(npc, tick));
        }

        /// <summary><see cref="TamIziIste"/> ile istenmiş kararın tam izi; istenmediyse ya da karar henüz verilmediyse <c>null</c>.</summary>
        public KararIziVerisi? YakalananIz(string npc, long tick) =>
            npc != null && _yakalananIzler.TryGetValue(YakalamaAnahtari(npc, tick), out var iz) ? iz : null;

        /// <summary>
        /// Bellekte tutulan tam iz sayısı: NPC başına en fazla bir SON iz ve açıkça istenip yakalananlar. Zamanla büyümez.
        /// </summary>
        public int TutulanTamIzSayisi => _sonKararlar.Count + _yakalananIzler.Count;

        private static string YakalamaAnahtari(string npc, long tick) =>
            npc + "@" + tick.ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>Şu anki dünya ve zihin durumunun hash'i (16 karakter onaltılık).</summary>
        public string DurumHash() => DurumHashu.Hesapla(Dunya, Zihinler);

        // ---- Tick döngüsü ----

        private void TekTick()
        {
            _tick++;
            var baslangic = Gunluk.Sayi;

            // Önceki tick'in sakladıkları: yalnızca bu tick'in kural değerlendirmesinde kullanılır.
            var saklananOlaylar = _saklananOlaylar;
            var saklananAlgi = _saklananAlgi;
            _saklananOlaylar = new List<Olay>();
            _saklananAlgi = new AlgiHaritasi();
            var saklananKimlikler = new SortedSet<long>();
            foreach (var o in saklananOlaylar)
            {
                saklananKimlikler.Add(o.Id);
            }

            // Bu tick'in işleyeceği olaylar, Olay.Id artan sırayla: önceki tick'in kural çıktıları ve eylem etkileri ile NPC eylem/hareket
            // olayları; sonra bu tick'in oyuncu eylemleri (kimlikleri daha büyüktür).
            var kuyruk = new List<Olay>(_devredilenler);
            kuyruk.AddRange(saklananOlaylar);
            kuyruk.Sort((a, b) => a.Id.CompareTo(b.Id));
            _devredilenler.Clear();

            // 1. Bekleyen oyuncu eylemleri olay olarak kayda eklenir (KuralId: player). Oyuncu hareketi de burada işlenir.
            OyuncuEylemleri(kuyruk);

            // 2. Algı: kimin neyi gördüğü hesaplanır, kanıt ve hafıza yazılır. Saklanan olaylar zaten algılanmıştı (önceki tick'in 6. adımı).
            var algi = Algi(kuyruk, saklananKimlikler);
            algi.Birlestir(saklananAlgi);
            OyuncuAnlatimiKanitlari(kuyruk, saklananKimlikler);

            // 3. Sonuç kuralları. Çıktılar sıradaki tick'e devredilir (v0 kaskad kuralı).
            SonucKurallari(kuyruk, algi, saklananKimlikler);

            // 4. Süresi dolan modifier'lar, ruh hali, stres ve hafıza sönümü: değerler saklanmaz, sorgu anında tick'ten hesaplanır
            // (ADR-008), yani yazılacak durum yoktur. Kriz tespiti de burada: sönmüş güncel stres bir eşiğe ulaştıysa (stres ≥ eşik) o NPC için
            // en üst uygun seviyede bir kriz satırı seçilir; kriz tick başına olay yazarak izlenmez, adım 6 yolundan olay olarak geçer.
            var krizler = KrizTespiti();

            // 5. NPC karar adımı (IActionSelector) ve 6. seçilen eylemlerin (ve krizlerin) olaya dönüşmesi.
            NpcAdimi(krizler);

            // 7. Durum hash'i alınır, olaylar dışarıya yayınlanır.
            Yayin(baslangic);
        }

        private void OyuncuEylemleri(List<Olay> kuyruk)
        {
            foreach (var eylem in _bekleyenEylemler)
            {
                if (eylem.Tur == YerlesikTurler.OyuncuHareketi)
                {
                    OyuncuHareketi(eylem, kuyruk);
                }
                else if (eylem.Tur == YerlesikTurler.Anlatti)
                {
                    OyuncuAnlatimi(eylem, kuyruk);
                }
                else
                {
                    // Önceki bir hareket oyuncunun konumunu değiştirdiyse eylem varış mekanında olur.
                    var mekan = Dunya.TekDeger("konum!", OyuncuId) ?? string.Empty;
                    var olay = new Olay(
                        Gunluk.SonrakiId, _tick, Gunluk.SonrakiSira(_tick), eylem.Tur, OyuncuId, eylem.Hedef, mekan, eylem.Yuk, new long[0], "player");
                    Ekle(olay);
                    kuyruk.Add(olay);
                }

                _eylemGunlugu.Add(new EylemKaydi(_tick, eylem));
            }

            _bekleyenEylemler.Clear();
        }

        // Oyuncu anlatımı: dışarıya npc anlatımıyla aynı biçimde (anlatti: onerme, kok, gorunurluk) çıkar. Dinleyene duydu kanıtı adım 2'de,
        // algıdan sonra yazılır. Oyuncunun zihni yoktur: aktarım/yalan kaydı tutulmaz; kök oyuncudur.
        private void OyuncuAnlatimi(PlayerAction eylem, List<Olay> kuyruk)
        {
            var mekan = Dunya.TekDeger("konum!", OyuncuId) ?? string.Empty;
            var onerme = string.Empty;
            var gorunurluk = "sessiz";
            foreach (var kv in eylem.Yuk)
            {
                if (kv.Key == "onerme")
                {
                    onerme = Mind.Onerme.Ayristir(kv.Value).ToString();
                }
                else if (kv.Key == "gorunurluk")
                {
                    gorunurluk = kv.Value;
                }
            }

            var yuk = new[]
            {
                new KeyValuePair<string, string>("gorunurluk", gorunurluk),
                new KeyValuePair<string, string>("kok", OyuncuId),
                new KeyValuePair<string, string>("onerme", onerme),
            };
            var olay = new Olay(Gunluk.SonrakiId, _tick, Gunluk.SonrakiSira(_tick), YerlesikTurler.Anlatti, OyuncuId, eylem.Hedef, mekan, yuk, new long[0], "player");
            Ekle(olay);
            kuyruk.Add(olay);
        }

        // Adım 2'nin devamı: oyuncunun anlattığı önermeyi dinleyen NPC'ye duydu kanıtı olarak yazar (güç: oyuncu kanıt gücü × güven çarpanı).
        // Eşik altı güçte kanıt yazılmaz (söylenti ölür). Saklanan (önceki tick'ten gelen) NPC anlatımları 6.3'te zaten yazılmıştı.
        private void OyuncuAnlatimiKanitlari(List<Olay> kuyruk, SortedSet<long> saklananKimlikler)
        {
            var hesap = new KonuHesabi(Zihinler, Gunluk, _tick, _dedikodu);
            foreach (var olay in kuyruk)
            {
                if (olay.Tur != YerlesikTurler.Anlatti || saklananKimlikler.Contains(olay.Id) || !string.Equals(olay.Fail, OyuncuId, StringComparison.Ordinal)
                    || !Zihinler.Var(olay.Hedef))
                {
                    continue;
                }

                var guc = hesap.AktarimGucu(_dedikodu.OyuncuKanitGucu, OyuncuId, olay.Hedef);
                if (guc < _dedikodu.GucEsigi)
                {
                    continue;
                }

                Ekle(Mind.ZihinOlaylari.KanitEklendi(
                    Gunluk.SonrakiId, _tick, Gunluk.SonrakiSira(_tick), olay.Hedef, olay.Deger("onerme"), "duydu", OyuncuId, guc,
                    new[] { olay.Id }, "player", olay.Mekan, OyuncuId));
            }
        }

        // Hareket: ayrildi + geldi (dış olaylar, algılanır) ve oyuncunun konum! gerçeği ayrı olayla hemen değişir.
        private void OyuncuHareketi(PlayerAction eylem, List<Olay> kuyruk)
        {
            var nereden = Dunya.TekDeger("konum!", OyuncuId) ?? string.Empty;
            var nereye = eylem.Hedef;
            if (!Dunya.BagliMi(nereden, nereye))
            {
                throw new InvalidOperationException($"Oyuncu hareketi geçersiz: \"{nereden}\" ile \"{nereye}\" bağlı değil.");
            }

            var ayrildi = HareketOlayi(YerlesikTurler.Ayrildi, OyuncuId, nereye, nereden, nereye, eylem.Yuk, new long[0], "player");
            Ekle(ayrildi);
            var geldi = HareketOlayi(YerlesikTurler.Geldi, OyuncuId, nereden, nereye, nereden, eylem.Yuk, new long[0], "player");
            Ekle(geldi);
            kuyruk.Add(ayrildi);
            kuyruk.Add(geldi);
            KonumuYaz(OyuncuId, nereye, geldi.Id, "player");
        }

        // karsi: Yuk'taki karsi_mekan (ayrildi için varış, geldi için kalkış); hedef: Olay.Hedef (aynı karşı mekan).
        private Olay HareketOlayi(
            string tur, string fail, string hedef, string mekan, string karsi, IEnumerable<KeyValuePair<string, string>> ekYuk,
            IEnumerable<long> nedenler, string kuralId)
        {
            var yuk = new List<KeyValuePair<string, string>>(ekYuk) { new KeyValuePair<string, string>("karsi_mekan", karsi) };
            return new Olay(Gunluk.SonrakiId, _tick, Gunluk.SonrakiSira(_tick), tur, fail, hedef, mekan, yuk, nedenler, kuralId);
        }

        private Olay KonumuYaz(string varlik, string mekan, long nedenOlayi, string kuralId)
        {
            var olay = new Olay(
                Gunluk.SonrakiId, _tick, Gunluk.SonrakiSira(_tick), Dunya.GercekEklendi, string.Empty, string.Empty, mekan,
                new[] { new KeyValuePair<string, string>("gercek", Gercek.Ayristir("konum!(" + varlik + "," + mekan + ")").ToString()) },
                new[] { nedenOlayi }, kuralId);

            // konum! olayları algılanmaz ve kural tetiklemez: hareketin dış görünümü ayrildi/geldi olaylarıdır.
            Ekle(olay);
            return olay;
        }

        // Dış olayları (zihin olayı olmayanları) algılatır; olay → (npc → algı olayları) haritasını döndürür.
        // `atla`: zaten algılanmış (önceki tick'in 6. adımında) olaylar.
        private AlgiHaritasi Algi(List<Olay> olaylar, SortedSet<long> atla)
        {
            var harita = new AlgiHaritasi();
            foreach (var olay in olaylar)
            {
                if (ZihinOlaylari.ZihinOlayiMi(olay.Tur) || olay.Tur == YerlesikTurler.KararVerildi || atla.Contains(olay.Id))
                {
                    continue;
                }

                foreach (var algi in DogrudanAlgi.Uret(Dunya, olay, Gunluk, _algiAyarlari, _tick))
                {
                    Ekle(algi);
                    harita.Ekle(olay.Id, algi.Deger("npc"), algi.Id);
                }
            }

            return harita;
        }

        private void SonucKurallari(List<Olay> kuyruk, AlgiHaritasi algi, SortedSet<long> saklananKimlikler)
        {
            var tetikler = new List<TetikBilgisi>(kuyruk.Count);
            foreach (var olay in kuyruk)
            {
                List<string> kimler;
                if (ZihinOlaylari.ZihinOlayiMi(olay.Tur))
                {
                    kimler = new List<string> { olay.Deger("npc") };
                }
                else if (saklananKimlikler.Contains(olay.Id))
                {
                    // Algılayanlar adım 6'da yazılan algı olaylarından gelir; o andan sonraki konum değişimleri bunu etkilemez.
                    kimler = new List<string>(algi.Algilayanlar(olay.Id));
                }
                else
                {
                    kimler = new List<string>(DogrudanAlgi.Algilayanlar(Dunya, olay));
                }

                var kimlik = olay.Id;
                tetikler.Add(new TetikBilgisi(olay, kimler, npc => new List<long>(algi.Nedenler(kimlik, npc))));
            }

            var etkiler = _kuralMotoru.Degerlendir(tetikler, Dunya, Zihinler, Gunluk, _tick, OyuncuId);
            foreach (var etki in etkiler)
            {
                var olay = KuralMotoru.OlayYap(etki, Gunluk.SonrakiId, _tick, Gunluk.SonrakiSira(_tick));
                Ekle(olay);
                _devredilenler.Add(olay);
            }
        }

        private static void SonumVeSureDolumu()
        {
        }

        // ---- Adım 5 ve 6: NPC kararları ----

        // Konuşma eyleminin planı: adım 5'te (aynı durum görüntüsünden) konular ve aktarım güçleri belirlenir; 6.1–6.3'te yazılır.
        private sealed class PlanlananKonu
        {
            public PlanlananKonu(KonuAdayi konu, decimal guc)
            {
                Konu = konu;
                Guc = guc;
            }

            public KonuAdayi Konu { get; }

            // Dinleyene yazılacak duydu kanıtının gücü (adım 5 görüntüsünden); eşik altındaysa kanıt yazılmaz.
            public decimal Guc { get; }

            public Olay? Anlatti { get; set; }
        }

        private sealed class NpcSecimi
        {
            public NpcSecimi(string npc, string konum, SecimSonucu sonuc, List<PlanlananKonu>? konular = null)
            {
                Npc = npc;
                Konum = konum;
                Sonuc = sonuc;
                Konular = konular;
            }

            // Seçilen eylem konuşma eylemiyse planlanan konular; değilse null.
            public List<PlanlananKonu>? Konular { get; }

            // Seçilen eylemin dış olayı (hareketlerde geldi, konuşmada ilk anlatti); stres olayının nedeni.
            public Olay? EylemOlayi { get; set; }

            public string Npc { get; }

            // Adım 5 başındaki konum görüntüsü.
            public string Konum { get; }

            public SecimSonucu Sonuc { get; }
        }

        // Adım 4'te seçilmiş bir kriz: o NPC bu tick eylem seçmez; olay adım 6'da seçilmiş eylem gibi yazılır.
        private sealed class KrizUygulamasi
        {
            public KrizUygulamasi(string npc, KrizSatiri satir, string hedef, string konum)
            {
                Npc = npc;
                Satir = satir;
                Hedef = hedef;
                Konum = konum;
            }

            public string Npc { get; }

            public KrizSatiri Satir { get; }

            // Boş: hedefsiz.
            public string Hedef { get; }

            // Adım 4–5 başındaki konum görüntüsü.
            public string Konum { get; }

            public Olay? Olay { get; set; }
        }

        private SortedDictionary<string, KrizUygulamasi> KrizTespiti()
        {
            var sonuc = new SortedDictionary<string, KrizUygulamasi>(StringComparer.Ordinal);
            if (_krizSatirlari.Count == 0)
            {
                return sonuc;
            }

            var cozucu = new Criteria.ZihinSorguCozucusu(Dunya, Zihinler, Gunluk, _tick, _dedikodu);
            foreach (var npc in Senaryo.Npcler)
            {
                var stres = Zihinler[npc.Id].Stres(_tick);
                var seviye = 0;
                for (var i = 0; i < _stresAyarlari.Esikler.Count; i++)
                {
                    if (stres >= _stresAyarlari.Esikler[i])
                    {
                        seviye = i + 1;
                    }
                }

                if (seviye == 0)
                {
                    continue;
                }

                var konum = Dunya.TekDeger("konum!", npc.Id) ?? string.Empty;
                var adaylar = new List<Criteria.Aday<KrizSatiri>>();
                var hedefler = new SortedDictionary<string, string>(StringComparer.Ordinal);
                foreach (var satir in _krizSatirlari)
                {
                    if (satir.Seviye != seviye)
                    {
                        continue;
                    }

                    var hedef = KrizHedefiBul(npc.Id, satir.Hedef, konum);
                    if (hedef == null)
                    {
                        continue;   // hedefi aynı mekanda olmayan satır elenir
                    }

                    hedefler[satir.Id] = hedef;
                    adaylar.Add(new Criteria.Aday<KrizSatiri>(satir.Id, satir, satir.Kriterler));
                }

                if (adaylar.Count == 0)
                {
                    continue;
                }

                var rng = AnahtarliRng.Icin(Seed, _tick, npc.Id, "kriz");
                var secim = Criteria.Secici.EnIyiSec(
                    adaylar, cozucu, a => new Criteria.SorguBaglami(npc.Id, OyuncuId, eylemHedef: hedefler[a.Id]), rng);
                if (secim.Secilen != null)
                {
                    sonuc[npc.Id] = new KrizUygulamasi(npc.Id, secim.Secilen.Deger, hedefler[secim.Secilen.Id], konum);
                }
            }

            return sonuc;
        }

        // Kriz hedefi: yok → boş; yalan_kurbani → EN SON yalan söylenen kişi (aynı mekandaysa); en_yakin/en_soguk → aynı mekandaki (oyuncu dahil)
        // en yüksek/en düşük fikirli kişi, eşitlikte kimlik ordinal. Bulunamazsa null (satır elenir).
        private string? KrizHedefiBul(string npc, KrizHedefTuru tur, string konum)
        {
            if (tur == KrizHedefTuru.Yok)
            {
                return string.Empty;
            }

            var zihin = Zihinler[npc];
            if (tur == KrizHedefTuru.YalanKurbani)
            {
                AktarimKaydi? son = null;
                foreach (var a in zihin.Aktarimlar)
                {
                    if (a.Yalan && (son == null || a.Tick > son.Tick || (a.Tick == son.Tick && a.OlayId > son.OlayId)))
                    {
                        son = a;
                    }
                }

                return son != null && string.Equals(Dunya.TekDeger("konum!", son.Kime), konum, StringComparison.Ordinal) ? son.Kime : null;
            }

            var adaylar = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var diger in Dunya.Varliklar(VarlikTuru.Npc))
            {
                if (!string.Equals(diger, npc, StringComparison.Ordinal) && string.Equals(Dunya.TekDeger("konum!", diger), konum, StringComparison.Ordinal))
                {
                    adaylar.Add(diger);
                }
            }

            if (OyuncuId.Length > 0 && string.Equals(Dunya.TekDeger("konum!", OyuncuId), konum, StringComparison.Ordinal))
            {
                adaylar.Add(OyuncuId);
            }

            string? en = null;
            var enFikir = 0m;
            foreach (var kisi in adaylar)
            {
                var fikir = zihin.Fikir(kisi, _tick);
                var daha = en == null || (tur == KrizHedefTuru.EnYakin ? fikir > enFikir : fikir < enFikir);
                if (daha)
                {
                    en = kisi;
                    enFikir = fikir;
                }
            }

            return en;
        }

        // Krizin NedenOlaylar'ı: bu NPC'nin en yeni 5 stres ARTIŞ olayı (günlükte bulunamayanlar uydurulmaz).
        private List<long> KrizNedenleri(string npc)
        {
            var sonuc = new List<long>(5);
            for (var id = Gunluk.Sayi; id >= 1 && sonuc.Count < 5; id--)
            {
                var o = Gunluk[id];
                if (o.Tur == Mind.ZihinOlaylari.StresDegistiTuru && string.Equals(o.Deger("npc"), npc, StringComparison.Ordinal)
                    && decimal.Parse(o.Deger("delta"), System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture) > 0m)
                {
                    sonuc.Add(o.Id);
                }
            }

            return sonuc;
        }

        private void NpcAdimi(SortedDictionary<string, KrizUygulamasi> krizler)
        {
            if (_eylemKatalogu.Eylemler.Count == 0 && krizler.Count == 0)
            {
                return;
            }

            // Adım 5: hiçbir şey yazılmadan önce bütün kararlar aynı durumdan hesaplanır (NPC kimlik sırasıyla).
            var konuHesabi = new KonuHesabi(Zihinler, Gunluk, _tick, _dedikodu);
            var cozucu = new Criteria.ZihinSorguCozucusu(Dunya, Zihinler, Gunluk, _tick, _dedikodu, konuHesabi);
            var secimler = new List<NpcSecimi>();
            foreach (var npc in Senaryo.Npcler)
            {
                if (_eylemKatalogu.Eylemler.Count == 0 || krizler.ContainsKey(npc.Id))
                {
                    continue;   // eylem yok ya da krizdeki NPC bu tick eylem seçmez (karar_verildi yazılmaz)
                }

                var baglam = new KararBaglami(npc.Id, OyuncuId, Seed, _tick, Dunya, Zihinler[npc.Id], Gunluk, cozucu, konuHesabi);
                var uretim = AdayUretici.Uret(baglam, _eylemKatalogu.Eylemler);
                if (uretim.Adaylar.Count == 0)
                {
                    continue;
                }

                var sonuc = _secici.Sec(baglam, uretim.Adaylar, uretim.Elenenler);
                Aday? uretilenAday = null;
                if (sonuc.Secilen != null)
                {
                    // Özel bir seçici (GOAP/BT adaptörü) yalnızca ön koşulları tutan adaylardan seçebilir; hedef/hareket doğrulaması buna dayanır.
                    var adaylarda = false;
                    foreach (var a in uretim.Adaylar)
                    {
                        if (string.Equals(a.Anahtar, sonuc.Secilen.Anahtar, StringComparison.Ordinal))
                        {
                            adaylarda = true;
                            uretilenAday = a;
                            break;
                        }
                    }

                    if (!adaylarda)
                    {
                        throw new InvalidOperationException(
                            $"Seçici {npc.Id} (tick {_tick}) için aday listesinde olmayan bir seçim döndürdü: \"{sonuc.Secilen.Anahtar}\" (seçici yalnızca verilen adaylardan seçebilir).");
                    }
                }

                secimler.Add(new NpcSecimi(npc.Id, Dunya.TekDeger("konum!", npc.Id) ?? string.Empty, sonuc, KonusmaPlani(npc.Id, uretilenAday, konuHesabi)));
            }

            // Adım 6.1: karar_verildi, eylem ve hareket olayları.
            var disOlaylar = new List<Olay>();
            var konusmalar = new List<NpcSecimi>();

            // Adım 6.1 (kriz): kriz olayı, seçilmiş eylem gibi yazılır; algısı 6.2'de adım 5 başındaki konum görüntüsüyle olur.
            foreach (var kv in krizler)
            {
                var k = kv.Value;
                var olay = new Olay(
                    Gunluk.SonrakiId, _tick, Gunluk.SonrakiSira(_tick), k.Satir.OlayTuru, k.Npc, k.Hedef, k.Konum,
                    new[] { new KeyValuePair<string, string>("seviye", k.Satir.Seviye.ToString(System.Globalization.CultureInfo.InvariantCulture)) },
                    KrizNedenleri(k.Npc), k.Satir.KuralId);
                Ekle(olay);
                disOlaylar.Add(olay);
                k.Olay = olay;
            }

            var etkiBekleyenler = new List<KeyValuePair<NpcSecimi, Olay>>();
            var konumDegisimleri = new List<KeyValuePair<NpcSecimi, Olay>>();
            foreach (var s in secimler)
            {
                var karar = KararOlayi.Yap(Gunluk.SonrakiId, _tick, Gunluk.SonrakiSira(_tick), s.Sonuc.Iz, s.Konum);
                Ekle(karar);
                _sonKararlar[s.Npc] = s.Sonuc.Iz;
                _sonKararOlaylari[s.Npc] = karar.Id;
                var yakalamaAnahtari = YakalamaAnahtari(s.Npc, _tick);
                if (_istenenIzler.Contains(yakalamaAnahtari))
                {
                    _yakalananIzler[yakalamaAnahtari] = s.Sonuc.Iz;
                }

                var aday = s.Sonuc.Secilen;
                if (aday == null)
                {
                    continue;
                }

                var eylem = aday.Eylem;
                Olay tetik;
                if (eylem.Hareket)
                {
                    if (!Dunya.BagliMi(s.Konum, aday.Hedef))
                    {
                        throw new InvalidOperationException($"Seçici geçersiz hareket seçti: {s.Npc} \"{s.Konum}\" → \"{aday.Hedef}\" (bağlı değil).");
                    }

                    var ayrildi = HareketOlayi(YerlesikTurler.Ayrildi, s.Npc, aday.Hedef, s.Konum, aday.Hedef, new KeyValuePair<string, string>[0], new[] { karar.Id }, eylem.KuralId);
                    Ekle(ayrildi);
                    var geldi = HareketOlayi(YerlesikTurler.Geldi, s.Npc, s.Konum, aday.Hedef, s.Konum, new KeyValuePair<string, string>[0], new[] { karar.Id }, eylem.KuralId);
                    Ekle(geldi);
                    disOlaylar.Add(ayrildi);
                    disOlaylar.Add(geldi);
                    konumDegisimleri.Add(new KeyValuePair<NpcSecimi, Olay>(s, geldi));
                    tetik = geldi;
                }
                else if (s.Konular != null)
                {
                    // Konuşma: her konu ayrı bir anlatti olayıdır (kurallar konu bazında çalışır). Nedenler: karar olayı ve dayanak kanıt olayı.
                    // Doğru anlatım da yalan da aynı biçimde yazılır; yalan bilgisi yalnızca 6.3'teki aktarım kaydındadır.
                    Olay? ilk = null;
                    foreach (var pk in s.Konular)
                    {
                        var yuk = new[]
                        {
                            new KeyValuePair<string, string>("gorunurluk", eylem.Gorunurluk),
                            new KeyValuePair<string, string>("kok", pk.Konu.Kok),
                            new KeyValuePair<string, string>("onerme", pk.Konu.Onerme),
                        };
                        var anlatti = new Olay(
                            Gunluk.SonrakiId, _tick, Gunluk.SonrakiSira(_tick), YerlesikTurler.Anlatti, s.Npc, aday.Hedef, s.Konum, yuk,
                            new[] { karar.Id, pk.Konu.Kanit.OlayId }, eylem.KuralId);
                        Ekle(anlatti);
                        disOlaylar.Add(anlatti);
                        pk.Anlatti = anlatti;
                        ilk ??= anlatti;
                    }

                    tetik = ilk!;
                    konusmalar.Add(s);
                }
                else
                {
                    tetik = new Olay(
                        Gunluk.SonrakiId, _tick, Gunluk.SonrakiSira(_tick), eylem.OlayTuru, s.Npc, aday.Hedef, s.Konum,
                        new KeyValuePair<string, string>[0], new[] { karar.Id }, eylem.KuralId);
                    Ekle(tetik);
                    disOlaylar.Add(tetik);
                }

                s.EylemOlayi = tetik;
                if (eylem.Etkiler.Count > 0)
                {
                    etkiBekleyenler.Add(new KeyValuePair<NpcSecimi, Olay>(s, tetik));
                }
            }

            // Adım 6.2: bu dış olayların algısı. Dünya hâlâ adım 5 başındaki konum görüntüsünü taşır (konum! 6.3'te değişir).
            var algi = Algi(disOlaylar, new SortedSet<long>());
            foreach (var o in disOlaylar)
            {
                _saklananOlaylar.Add(o);
            }

            _saklananAlgi = algi;

            // Adım 6.3: gecikmeli eylem etkileri, sonra konum! değişimleri.
            var yazilanGercekler = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var kv in etkiBekleyenler)
            {
                EylemEtkileriniYaz(kv.Key, kv.Value, yazilanGercekler);
            }

            foreach (var s in konusmalar)
            {
                KonusmayiYaz(s);
            }

            // Stres: kişiliğe aykırı eylem seçildiyse stres_tabani × Σ(çelişen özellik) kadar artar; yalnızca seçilen eylem için.
            foreach (var s in secimler)
            {
                StresYaz(s);
            }

            // Kriz: satır etkileri ve stres düşüşü (nedeni kriz olayı). Satır etkileri sonuç kurallarına sıradaki tick'te girer.
            foreach (var kv in krizler)
            {
                KrizEtkileriniYaz(kv.Value, yazilanGercekler);
            }

            foreach (var kv in konumDegisimleri)
            {
                KonumuYaz(kv.Key.Npc, kv.Key.Sonuc.Secilen!.Hedef, kv.Value.Id, kv.Key.Sonuc.Secilen!.Eylem.KuralId);
            }
        }

        // Adım 5: seçilen aday konuşma eylemiyse konular aday aşamasında bir kez hesaplanmış olarak gelir; aktarım güçleri aynı durum
        // görüntüsünden (dinleyenin güveni dahil) şimdi sabitlenir, böylece adım 6.3'teki yazım sırası sonucu etkilemez.
        private List<PlanlananKonu>? KonusmaPlani(string konusan, Aday? aday, KonuHesabi hesap)
        {
            if (aday == null || !aday.Eylem.Konusma)
            {
                return null;
            }

            var konu = aday.Konu ?? throw new InvalidOperationException($"Konuşma adayı \"{aday.Anahtar}\" konu seçimi taşımıyor (AdayUretici'den gelmeli).");
            var plan = new List<PlanlananKonu>(konu.Konular.Count);
            foreach (var k in konu.Konular)
            {
                var guc = hesap.AktarimGucu(Zihinler[konusan].KanitGucu(k.Kanit, _tick), konusan, aday.Hedef);
                plan.Add(new PlanlananKonu(k, guc));
            }

            return plan;
        }

        // Adım 6.3: seçilen eylemin çelişen özellikleri varsa stres artışı. Olay: stres_degisti, KuralId eylem:<id>, nedeni eylem olayı.
        // Stres olayı algılanmaz ve kural tetiklemez (zihin olayıdır, kuyruğa girmez); kriz bir sonraki tick'in 4. adımında denetlenir.
        private void StresYaz(NpcSecimi s)
        {
            var aday = s.Sonuc.Secilen;
            if (aday == null || s.EylemOlayi == null)
            {
                return;
            }

            var artis = aday.Eylem.StresArtisi(Zihinler[s.Npc].Kisilik);
            if (artis <= 0m)
            {
                return;
            }

            Ekle(Mind.ZihinOlaylari.StresDegisti(
                Gunluk.SonrakiId, _tick, Gunluk.SonrakiSira(_tick), s.Npc, artis, new[] { s.EylemOlayi.Id }, aday.Eylem.KuralId, s.Konum));
        }

        // Adım 6.3: dinleyene duydu kanıtı (kök, güç; eşik altındaysa söylenti ölür, dinleyen oyuncuysa zihni yoktur) ve konuşana
        // aktarım kaydı. Kayıt konuşanın zihnindedir, algılanmaz ve kural tetiklemez (yalan bilgisi yalnızca burada, yalan bayrağıyla).
        private void KonusmayiYaz(NpcSecimi s)
        {
            var aday = s.Sonuc.Secilen!;
            foreach (var pk in s.Konular!)
            {
                var anlatti = pk.Anlatti!;
                if (Zihinler.Var(aday.Hedef) && pk.Guc >= _dedikodu.GucEsigi)
                {
                    Ekle(Mind.ZihinOlaylari.KanitEklendi(
                        Gunluk.SonrakiId, _tick, Gunluk.SonrakiSira(_tick), aday.Hedef, pk.Konu.Onerme, "duydu", s.Npc, pk.Guc,
                        new[] { anlatti.Id }, aday.Eylem.KuralId, s.Konum, pk.Konu.Kok));
                }

                Ekle(Mind.ZihinOlaylari.AktarimKaydedildi(
                    Gunluk.SonrakiId, _tick, Gunluk.SonrakiSira(_tick), s.Npc, aday.Hedef, pk.Konu.Onerme, pk.Konu.Yalan,
                    new[] { anlatti.Id }, aday.Eylem.KuralId, s.Konum));
            }
        }

        private void KrizEtkileriniYaz(KrizUygulamasi k, SortedSet<string> yazilanGercekler)
        {
            if (k.Satir.Etkiler.Count > 0)
            {
                EtkileriYaz(k.Npc, k.Satir.KuralId, k.Satir.OlayTuru, k.Satir.Etkiler, k.Olay!, k.Hedef, yazilanGercekler);
            }

            var dusus = _stresAyarlari.Dususler[k.Satir.Seviye - 1];
            Ekle(Mind.ZihinOlaylari.StresDegisti(
                Gunluk.SonrakiId, _tick, Gunluk.SonrakiSira(_tick), k.Npc, -dusus, new[] { k.Olay!.Id }, k.Satir.KuralId, k.Konum));
        }

        // Eylem etkileri mevcut kural etkisi biçimiyle ve kural motorunun etki üreticisiyle yazılır; KuralId eylem:<id>, neden eylem olayı.
        private void EylemEtkileriniYaz(NpcSecimi secim, Olay tetik, SortedSet<string> yazilanGercekler)
        {
            var aday = secim.Sonuc.Secilen!;
            EtkileriYaz(secim.Npc, aday.Eylem.KuralId, tetik.Tur, aday.Eylem.Etkiler, tetik, aday.Hedef, yazilanGercekler);
        }

        // Eylem ya da kriz satırı etkileri: mevcut kural etkisi biçimi ve etki üreticisi; KuralId eylem:<id> / kriz:<id>, neden tetikleyen olay.
        private void EtkileriYaz(
            string npc, string kuralId, string tetikTuru, IReadOnlyList<KuralEtkisi> etkiler, Olay tetik, string hedef, SortedSet<string> yazilanGercekler)
        {
            var sanalKural = new KuralTanimi(kuralId, tetikTuru, new Criteria.Kriter[0], etkiler, new Criteria.Rol[0]);
            var baglam = new Criteria.SorguBaglami(npc, OyuncuId, eylemHedef: hedef);
            var nedenler = new[] { tetik.Id };
            foreach (var etki in etkiler)
            {
                var uretilen = KuralMotoru.EtkiUret(sanalKural, etki, npc, tetik, baglam, nedenler, Dunya, yazilanGercekler);
                if (uretilen == null)
                {
                    continue;
                }

                var olay = KuralMotoru.OlayYap(uretilen, Gunluk.SonrakiId, _tick, Gunluk.SonrakiSira(_tick));
                Ekle(olay);
                _devredilenler.Add(olay);
            }
        }

        private void Yayin(int baslangic)
        {
            SonHash = DurumHash();
            var yayinlayici = OlayYayinlandi;
            if (yayinlayici == null)
            {
                return;
            }

            for (var i = baslangic; i < Gunluk.Sayi; i++)
            {
                yayinlayici(Gunluk.Olaylar[i]);
            }
        }

        // ---- Kurulum ----

        private void Kur(ZihinAyarlari? zihinAyarlari)
        {
            var s = Senaryo;
            foreach (var m in s.Mekanlar)
            {
                VarlikEkle(m, "mekan");
            }

            VarlikEkle(s.OyuncuId, "oyuncu");
            foreach (var n in s.Npcler)
            {
                VarlikEkle(n.Id, "npc");
                Zihinler.Ekle(new Zihin(n.Id, n.Kisilik, zihinAyarlari));
            }

            foreach (var b in s.Baglantilar)
            {
                SenaryoOlayi(Dunya.MekanBaglandi, new KeyValuePair<string, string>("a", b.Key), new KeyValuePair<string, string>("b", b.Value));
            }

            GercekEkle("konum!(" + s.OyuncuId + "," + s.OyuncuKonumu + ")");
            foreach (var n in s.Npcler)
            {
                GercekEkle("konum!(" + n.Id + "," + n.Konum + ")");
            }

            foreach (var g in s.Gercekler)
            {
                GercekEkle(g);
            }
        }

        private void VarlikEkle(string id, string tur) =>
            SenaryoOlayi(Dunya.VarlikEklendi, new KeyValuePair<string, string>("id", id), new KeyValuePair<string, string>("tur", tur));

        private void GercekEkle(string metin) =>
            SenaryoOlayi(Dunya.GercekEklendi, new KeyValuePair<string, string>("gercek", Gercek.Ayristir(metin).ToString()));

        private void SenaryoOlayi(string tur, params KeyValuePair<string, string>[] yuk) =>
            Ekle(new Olay(Gunluk.SonrakiId, 0, Gunluk.SonrakiSira(0), tur, string.Empty, string.Empty, string.Empty, yuk, new long[0], "senaryo"));

        // Olayı günlüğe ekler ve dünya ile zihinlere uygular.
        private void Ekle(Olay olay)
        {
            Gunluk.Ekle(olay);
            if (!Dunya.Uygula(olay))
            {
                Zihinler.Uygula(olay);
            }
        }

        // Kriz tablosunu doğrular: seviyeler eşik sayısını aşmamalı ve her seviyenin kriterli olmayan bir yedek satırı olmalı.
        private static IReadOnlyList<KrizSatiri> KrizleriDenetle(SenaryoTanimi senaryo, IEnumerable<KrizSatiri>? krizler, StresAyarlari stres)
        {
            var liste = new List<KrizSatiri>(krizler ?? new KrizSatiri[0]);
            liste.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            if (liste.Count == 0)
            {
                return liste;
            }

            for (var i = 1; i < liste.Count; i++)
            {
                if (string.Equals(liste[i - 1].Id, liste[i].Id, StringComparison.Ordinal))
                {
                    throw new ArgumentException($"Kriz satırı kimliği tekrar ediyor: \"{liste[i].Id}\".", nameof(krizler));
                }
            }

            foreach (var s in liste)
            {
                if (s.Seviye > stres.Esikler.Count)
                {
                    throw new ArgumentException($"Kriz \"{s.Id}\": seviye {s.Seviye}, ama yalnızca {stres.Esikler.Count} kriz eşiği var.", nameof(krizler));
                }
            }

            var yedek = KrizYukleyici.YedekHatalari(liste, stres.Esikler.Count, senaryo.Ad);
            if (yedek.Count > 0)
            {
                throw new ArgumentException(string.Join(" ", System.Linq.Enumerable.Select(yedek, h => h.Mesaj)), nameof(krizler));
            }

            var varliklar = new SortedSet<string>(StringComparer.Ordinal) { senaryo.OyuncuId };
            foreach (var n in senaryo.Npcler)
            {
                varliklar.Add(n.Id);
            }

            foreach (var s in liste)
            {
                foreach (var etki in s.Etkiler)
                {
                    if (etki.Hedef.Length > 0 && !RolAdiMi(etki.Hedef) && !varliklar.Contains(etki.Hedef))
                    {
                        throw new ArgumentException($"Kriz \"{s.Id}\": etki hedefi \"{etki.Hedef}\" bu senaryoda yok (NPC ya da oyuncu olmalı).", nameof(krizler));
                    }
                }
            }

            return liste;
        }

        private static void KurallariDenetle(SenaryoTanimi senaryo, KuralMotoru motor, EylemKatalogu katalog, IReadOnlyList<KrizSatiri> krizler)
        {
            var turler = new SortedSet<string>(KuralYukleyici.YerlesikOlayTurleri, StringComparer.Ordinal);
            foreach (var t in senaryo.OlayTurleri)
            {
                turler.Add(t);
            }

            foreach (var t in katalog.OlayTurleri)
            {
                turler.Add(t);
            }

            foreach (var k in krizler)
            {
                turler.Add(k.OlayTuru);
            }

            var varliklar = new SortedSet<string>(StringComparer.Ordinal) { senaryo.OyuncuId };
            foreach (var n in senaryo.Npcler)
            {
                varliklar.Add(n.Id);
            }

            foreach (var kural in motor.Kurallar)
            {
                if (!turler.Contains(kural.NeZaman))
                {
                    throw new ArgumentException($"Kural \"{kural.Id}\": bilinmeyen olay türü \"{kural.NeZaman}\".", nameof(motor));
                }

                foreach (var etki in kural.Etkiler)
                {
                    if (etki.Hedef.Length > 0 && !RolAdiMi(etki.Hedef) && !varliklar.Contains(etki.Hedef))
                    {
                        throw new ArgumentException($"Kural \"{kural.Id}\": etki hedefi \"{etki.Hedef}\" bu senaryoda yok (NPC ya da oyuncu olmalı).", nameof(motor));
                    }
                }
            }
        }

        // Eylem etkilerinin sabit hedefleri (rol olmayanlar) senaryoda var olmalı.
        private static void EylemleriDenetle(SenaryoTanimi senaryo, EylemKatalogu katalog)
        {
            var varliklar = new SortedSet<string>(StringComparer.Ordinal) { senaryo.OyuncuId };
            foreach (var n in senaryo.Npcler)
            {
                varliklar.Add(n.Id);
            }

            foreach (var eylem in katalog.Eylemler)
            {
                foreach (var etki in eylem.Etkiler)
                {
                    if (etki.Hedef.Length > 0 && !RolAdiMi(etki.Hedef) && !varliklar.Contains(etki.Hedef))
                    {
                        throw new ArgumentException(
                            $"Eylem \"{eylem.Id}\": etki hedefi \"{etki.Hedef}\" bu senaryoda yok (NPC ya da oyuncu olmalı).", nameof(katalog));
                    }
                }
            }
        }

        private static bool RolAdiMi(string ad) => Criteria.RolAdlari.TryCoz(ad, out _);
    }
}
