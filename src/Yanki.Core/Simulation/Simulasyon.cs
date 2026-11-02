using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Yanki.Core.Decision;
using Yanki.Core.Events;
using Yanki.Core.Mind;
using Yanki.Core.Perception;
using Yanki.Core.Rules;
using Yanki.Core.World;

namespace Yanki.Core.Simulation
{
    /// <summary>
    /// Simülasyonun dış API'si (plan 5.8) ve tick döngüsü v0 (plan 6). Durumun tek kaynağı olay günlüğüdür (ADR-001): dünya ve
    /// zihinler her olay eklenir eklenmez uygulanır. <see cref="Submit"/> oyuncu eylemini bekletir, <see cref="Tick"/> ilerletir.
    /// <para>1 tick = 1 oyun saati (docs/notes/zihin.md, Karar (Yusuf)); çekirdek bunu varsaymaz, sönme oranları böyle seçilmiştir.</para>
    /// <para>
    /// Bir tick'te çalışan adımlar (plan 6): (1) bekleyen oyuncu eylemleri olay olur, (2) algı: dış olayları görenlere kanıt ve hafıza
    /// yazılır, (3) sonuç kuralları, (4) sönüm ve süre dolumu, (7) durum hash'i ve olay yayını. 5. ve 6. adımlar (karar ve
    /// eylem seçimi) sonraki haftalara aittir; ilgili yerler <see cref="TekTick"/> içinde işaretlidir.
    /// </para>
    /// <para>
    /// Kaskad (v0): kuralların ürettiği olaylar aynı tick'te yeniden kural tetiklemez; bir sonraki tick'in başına devredilir
    /// (algı ve kurallar o tick'te onlar için de çalışır). Zincir derinliği sınırı 7. haftadadır.
    /// </para>
    /// </summary>
    public sealed class Simulasyon
    {
        private readonly KuralMotoru _kuralMotoru;
        private readonly EylemKatalogu _eylemKatalogu;
        private readonly AlgiAyarlari _algiAyarlari;
        private readonly List<PlayerAction> _bekleyenEylemler = new List<PlayerAction>();
        private readonly List<EylemKaydi> _eylemGunlugu = new List<EylemKaydi>();
        private readonly List<Olay> _devredilenler = new List<Olay>();
        private readonly SortedSet<string> _eylemTurleri;
        private long _tick;

        /// <summary>Senaryo.</summary>
        public SenaryoTanimi Senaryo { get; }

        /// <summary>Seed. Karar adımı (5. hafta) <see cref="AnahtarliRng"/> ile bunu kullanacak.</summary>
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

        /// <summary>İşlenmiş oyuncu eylemleri, tick sırasıyla (golden replay ve kayıt satırları).</summary>
        public IReadOnlyList<EylemKaydi> EylemGunlugu => new ReadOnlyCollection<EylemKaydi>(_eylemGunlugu);

        /// <summary>Yüklü kurallar, kimliğe göre ordinal sıralı.</summary>
        public IReadOnlyList<KuralTanimi> Kurallar => _kuralMotoru.Kurallar;

        /// <summary>Yüklü NPC eylemleri, kimliğe göre ordinal sıralı; boşsa NPC karar adımı olay üretmez.</summary>
        public IReadOnlyList<EylemTanimi> Eylemler => _eylemKatalogu.Eylemler;

        /// <summary>
        /// Her tick'in sonunda (adım 7) o tick'te günlüğe eklenen olaylar, ekleniş sırasıyla bildirilir. Oyun buna abone olup sahneyi günceller.
        /// </summary>
        public event Action<Olay>? OlayYayinlandi;

        private Simulasyon(SenaryoTanimi senaryo, KuralMotoru motor, EylemKatalogu katalog, ulong seed, AlgiAyarlari algi)
        {
            Senaryo = senaryo;
            _kuralMotoru = motor;
            _eylemKatalogu = katalog;
            Seed = seed;
            _algiAyarlari = algi;
            _eylemTurleri = new SortedSet<string>(senaryo.OlayTurleri, StringComparer.Ordinal);
            SonHash = string.Empty;
        }

        /// <summary>
        /// Senaryodan simülasyon kurar: başlangıç dünyası tick 0'da <c>KuralId: senaryo</c> olaylarıyla yazılır, zihinler kurulur.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Bir kuralın <c>ne_zaman</c> türü (senaryonun oyuncu eylem türleri, çekirdek olayları ve <paramref name="eylemler"/>'in olay türleri dışında)
        /// ya da bir kural/eylemin sabit etki hedefi bu senaryoda yoksa; eylem kimliği tekrar ediyorsa.
        /// </exception>
        public static Simulasyon Olustur(
            SenaryoTanimi senaryo, IEnumerable<KuralTanimi> kurallar, ulong seed, ZihinAyarlari? zihinAyarlari = null, AlgiAyarlari? algiAyarlari = null,
            IEnumerable<EylemTanimi>? eylemler = null)
        {
            if (senaryo == null)
            {
                throw new ArgumentNullException(nameof(senaryo));
            }

            var motor = new KuralMotoru(kurallar ?? throw new ArgumentNullException(nameof(kurallar)));
            var katalog = new EylemKatalogu(eylemler ?? new EylemTanimi[0]);
            KurallariDenetle(senaryo, motor, katalog);
            EylemleriDenetle(senaryo, katalog);
            var sim = new Simulasyon(senaryo, motor, katalog, seed, algiAyarlari ?? new AlgiAyarlari());
            sim.Kur(zihinAyarlari);
            sim.SonHash = sim.DurumHash();
            return sim;
        }

        /// <summary>
        /// Oyuncu eylemini bekletir; bir sonraki <see cref="Tick"/>'te olaya dönüşür.
        /// </summary>
        /// <exception cref="ArgumentException">Tür senaryoda tanımlı değilse, hedef yoksa ya da görünürlük bilinmiyorsa.</exception>
        public void Submit(PlayerAction eylem)
        {
            if (eylem == null)
            {
                throw new ArgumentNullException(nameof(eylem));
            }

            if (!_eylemTurleri.Contains(eylem.Tur))
            {
                throw new ArgumentException($"Eylem türü senaryoda tanımlı değil: \"{eylem.Tur}\".", nameof(eylem));
            }

            if (eylem.Hedef.Length > 0 && !Dunya.VarlikVar(eylem.Hedef))
            {
                throw new ArgumentException($"Eylemin hedefi dünyada yok: \"{eylem.Hedef}\".", nameof(eylem));
            }

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

            _bekleyenEylemler.Add(eylem);
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

        /// <summary>Şu anki dünya ve zihin durumunun hash'i (16 karakter onaltılık).</summary>
        public string DurumHash() => DurumHashu.Hesapla(Dunya, Zihinler);

        // ---- Tick döngüsü ----

        private void TekTick()
        {
            _tick++;
            var baslangic = Gunluk.Sayi;

            // Bu tick'in işleyeceği olaylar: önceki tick'in kural çıktıları, sonra bu tick'in oyuncu eylemleri.
            var kuyruk = new List<Olay>(_devredilenler);
            _devredilenler.Clear();

            // 1. Bekleyen oyuncu eylemleri olay olarak kayda eklenir (KuralId: player).
            OyuncuEylemleri(kuyruk);

            // 2. Algı: kimin neyi gördüğü hesaplanır, kanıt ve hafıza yazılır.
            var algiNedenleri = Algi(kuyruk);

            // 3. Sonuç kuralları. Çıktılar sıradaki tick'e devredilir (v0 kaskad kuralı).
            SonucKurallari(kuyruk, algiNedenleri);

            // 4. Süresi dolan modifier'lar, ruh hali, stres ve hafıza sönümü: değerler saklanmaz, sorgu anında tick'ten hesaplanır
            // (ADR-008), yani yazılacak durum yoktur. Stres krizi olayı üretilmez (6. hafta).
            SonumVeSureDolumu();

            // 5. NPC karar adımı (IActionSelector) ve 6. seçilen eylemlerin olaya dönüşüp 2. adıma geri beslenmesi:
            // 5. ve 6. haftanın işi. Bu iki adım 4 ile 7 arasına girecek; olaylar _devredilenler kuyruğuna eklenebilir.

            // 7. Durum hash'i alınır, olaylar dışarıya yayınlanır.
            Yayin(baslangic);
        }

        private void OyuncuEylemleri(List<Olay> kuyruk)
        {
            var mekan = Dunya.TekDeger("konum!", OyuncuId) ?? string.Empty;
            foreach (var eylem in _bekleyenEylemler)
            {
                var olay = new Olay(
                    Gunluk.SonrakiId, _tick, Gunluk.SonrakiSira(_tick), eylem.Tur, OyuncuId, eylem.Hedef, mekan, eylem.Yuk, new long[0], "player");
                Ekle(olay);
                _eylemGunlugu.Add(new EylemKaydi(_tick, eylem));
                kuyruk.Add(olay);
            }

            _bekleyenEylemler.Clear();
        }

        // Dış olayları (zihin olayı olmayanları) algılatır; (olay, npc) → algı olaylarının kimlikleri haritasını döndürür.
        private SortedDictionary<string, List<long>> Algi(List<Olay> kuyruk)
        {
            var harita = new SortedDictionary<string, List<long>>(StringComparer.Ordinal);
            foreach (var olay in kuyruk)
            {
                if (ZihinOlaylari.ZihinOlayiMi(olay.Tur))
                {
                    continue;
                }

                foreach (var algi in DogrudanAlgi.Uret(Dunya, olay, Gunluk, _algiAyarlari, _tick))
                {
                    Ekle(algi);
                    var anahtar = HaritaAnahtari(olay.Id, algi.Deger("npc"));
                    if (!harita.TryGetValue(anahtar, out var liste))
                    {
                        liste = new List<long>();
                        harita.Add(anahtar, liste);
                    }

                    liste.Add(algi.Id);
                }
            }

            return harita;
        }

        private void SonucKurallari(List<Olay> kuyruk, SortedDictionary<string, List<long>> algiNedenleri)
        {
            var tetikler = new List<TetikBilgisi>(kuyruk.Count);
            foreach (var olay in kuyruk)
            {
                var kimler = ZihinOlaylari.ZihinOlayiMi(olay.Tur)
                    ? new List<string> { olay.Deger("npc") }
                    : new List<string>(DogrudanAlgi.Algilayanlar(Dunya, olay));
                var kimlik = olay.Id;
                tetikler.Add(new TetikBilgisi(olay, kimler, npc =>
                    algiNedenleri.TryGetValue(HaritaAnahtari(kimlik, npc), out var liste) ? liste : new List<long>()));
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

        private static void KurallariDenetle(SenaryoTanimi senaryo, KuralMotoru motor, EylemKatalogu katalog)
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

        private static string HaritaAnahtari(long olayId, string npc) =>
            olayId.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + npc;
    }
}
