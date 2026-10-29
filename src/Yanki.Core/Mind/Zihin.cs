using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Yanki.Core.Events;
using Yanki.Core.World;

namespace Yanki.Core.Mind
{
    /// <summary>
    /// Bir NPC'nin zihni (plan 5.4): kişilik, ilişkiler, ruh hali, stres, hafıza ve kanıtlı inançlar.
    /// Durum yalnızca zihin olayları <see cref="Uygula"/> ile işlenerek ilerler (ADR-001). Süreli değerler (modifier, duygu, stres,
    /// kanıt gücü, hafıza) saklanmaz; sorgu anında tick'ten hesaplanır (ADR-008, <see cref="Sonumleme"/>).
    /// </summary>
    public sealed class Zihin
    {
        private readonly Sonumleme _sonumleme;
        private readonly SortedDictionary<string, Iliski> _iliskiler = new SortedDictionary<string, Iliski>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, InancKaydi> _inanclar = new SortedDictionary<string, InancKaydi>(StringComparer.Ordinal);
        private readonly List<HafizaKaydi> _hafiza = new List<HafizaKaydi>();
        private Duygu? _duygu;
        private Duygu? _onceki;
        private StresDurumu? _stres;

        private sealed class Iliski
        {
            public readonly SortedDictionary<string, decimal> Eksen = new SortedDictionary<string, decimal>(StringComparer.Ordinal);
            public readonly SortedDictionary<string, long> EksenOlay = new SortedDictionary<string, long>(StringComparer.Ordinal);
            public readonly List<FikirModifier> Modifierlar = new List<FikirModifier>();
        }

        private sealed class InancKaydi
        {
            public InancKaydi(Onerme onerme)
            {
                Onerme = onerme;
            }

            public Onerme Onerme { get; }

            public List<Kanit> Kanitlar { get; } = new List<Kanit>();
        }

        /// <summary>Zihnin sahibi NPC'nin kimliği.</summary>
        public string Id { get; }

        /// <summary>Kişilik; kurulduktan sonra değişmez.</summary>
        public Kisilik Kisilik { get; }

        /// <summary>Bu zihnin ayarları.</summary>
        public ZihinAyarlari Ayarlar { get; }

        /// <summary>Baskın duygu (kayıtlı hali; güncel şiddet için <see cref="DuyguSiddeti"/>); yoksa <c>null</c>.</summary>
        public Duygu? Duygu => _duygu;

        /// <summary>Baskın duygudan önceki duygu; yoksa <c>null</c>.</summary>
        public Duygu? OncekiDuygu => _onceki;

        /// <summary>Hafıza kayıtlarının tamamı (silikleşenler dahil), eklenme sırasıyla.</summary>
        public IReadOnlyList<HafizaKaydi> HafizaKayitlari => new ReadOnlyCollection<HafizaKaydi>(_hafiza);

        /// <summary>Yeni zihin kurar.</summary>
        /// <exception cref="ArgumentException">Kimlik geçersizse ya da ayarlar tutarsızsa.</exception>
        public Zihin(string id, Kisilik kisilik, ZihinAyarlari? ayarlar = null)
        {
            if (!Kimlik.GecerliKimlikMi(id))
            {
                throw new ArgumentException($"Geçersiz NPC kimliği: \"{id}\".", nameof(id));
            }

            Id = id;
            Kisilik = kisilik ?? throw new ArgumentNullException(nameof(kisilik));
            Ayarlar = ayarlar ?? new ZihinAyarlari();
            Ayarlar.Dogrula();
            _sonumleme = new Sonumleme(Ayarlar);
        }

        /// <summary>
        /// Zihin olayını uygular ve <c>true</c> döner; zihin olayı olmayan olay yoksayılır (<c>false</c>).
        /// </summary>
        /// <exception cref="ArgumentException">Olay başka bir NPC'ye aitse ya da yükü geçersizse.</exception>
        public bool Uygula(Olay olay)
        {
            if (olay == null)
            {
                throw new ArgumentNullException(nameof(olay));
            }

            if (!ZihinOlaylari.ZihinOlayiMi(olay.Tur))
            {
                return false;
            }

            var sahip = olay.Deger("npc");
            if (!string.Equals(sahip, Id, StringComparison.Ordinal))
            {
                throw new ArgumentException($"Olay #{olay.Id} \"{sahip}\" zihnine ait, \"{Id}\" zihnine uygulanamaz.");
            }

            switch (olay.Tur)
            {
                case ZihinOlaylari.ModifierEklendiTuru: ModifierEkle(olay); break;
                case ZihinOlaylari.EksenDegistiTuru: EksenDegistir(olay); break;
                case ZihinOlaylari.DuyguDegistiTuru: DuyguDegistir(olay); break;
                case ZihinOlaylari.StresDegistiTuru: StresDegistir(olay); break;
                case ZihinOlaylari.KanitEklendiTuru: KanitEkle(olay); break;
                default: HafizaEkle(olay); break;
            }

            return true;
        }

        // ---- İlişki ve fikir ----

        /// <summary>Hedefe karşı kalıcı eksen değeri (−100..100); hiç değişmediyse 0.</summary>
        /// <exception cref="ArgumentException">Eksen adı bilinmiyorsa.</exception>
        public decimal Eksen(string hedef, string eksen)
        {
            EksenDenetle(eksen);
            return _iliskiler.TryGetValue(hedef, out var i) && i.Eksen.TryGetValue(eksen, out var d) ? d : 0m;
        }

        /// <summary>
        /// Hedef hakkındaki fikir: eksenlerin ağırlıklı toplamı + aktif modifier'ların toplamı, −100..100'e kırpılmış.
        /// Aynı türden kopyalar birikir: n'inci kopyanın etkisi <c>deger × çarpan^(n−1)</c>, tür başına en fazla birikme sınırı kadar.
        /// </summary>
        public decimal Fikir(string hedef, long simdikiTick) => FikirDokumu(hedef, simdikiTick).Toplam;

        /// <summary>Fikrin bandı (<c>dusman</c>, <c>soguk</c>, <c>notr</c>, <c>yakin</c>, <c>dost</c>).</summary>
        public string FikirBandi(string hedef, long simdikiTick) =>
            Yanki.Core.Mind.FikirBandi.Hesapla(Fikir(hedef, simdikiTick));

        /// <summary>Fikrin her katkısını neden olayıyla birlikte, sıralı döndürür.</summary>
        public FikirDokumu FikirDokumu(string hedef, long simdikiTick)
        {
            var katkilar = new List<FikirKatkisi>();
            _iliskiler.TryGetValue(hedef, out var iliski);

            AddEksen(katkilar, iliski, Eksenler.Guven, Ayarlar.EksenAgirligiGuven);
            AddEksen(katkilar, iliski, Eksenler.Sevgi, Ayarlar.EksenAgirligiSevgi);
            AddEksen(katkilar, iliski, Eksenler.Korku, Ayarlar.EksenAgirligiKorku);

            if (iliski != null)
            {
                var aktif = new List<FikirModifier>();
                foreach (var m in iliski.Modifierlar)
                {
                    if (_sonumleme.ModifierAktifMi(m, simdikiTick))
                    {
                        aktif.Add(m);
                    }
                }

                aktif.Sort(ModifierSirasi);

                string? sonTur = null;
                var kopya = 0;
                var carpan = 1m;
                foreach (var m in aktif)
                {
                    if (!string.Equals(m.Tur, sonTur, StringComparison.Ordinal))
                    {
                        sonTur = m.Tur;
                        kopya = 0;
                        carpan = 1m;
                    }

                    kopya++;
                    if (kopya > Ayarlar.ModifierBirikmeSiniri)
                    {
                        continue;
                    }

                    katkilar.Add(new FikirKatkisi("modifier:" + m.Tur, m.Deger * carpan, m.NedenOlayId, _sonumleme.ModifierKalan(m, simdikiTick)));
                    carpan *= Ayarlar.ModifierKopyaCarpani;
                }
            }

            return new FikirDokumu(Id, hedef, simdikiTick, katkilar);
        }

        /// <summary>Hedefe karşı bu türde aktif bir modifier var mı.</summary>
        public bool ModifierVar(string hedef, string tur, long simdikiTick)
        {
            if (!_iliskiler.TryGetValue(hedef, out var iliski))
            {
                return false;
            }

            foreach (var m in iliski.Modifierlar)
            {
                if (string.Equals(m.Tur, tur, StringComparison.Ordinal) && _sonumleme.ModifierAktifMi(m, simdikiTick))
                {
                    return true;
                }
            }

            return false;
        }

        // ---- Ruh hali ve stres ----

        /// <summary>Verilen türdeki duygu baskınsa güncel şiddeti, değilse 0.</summary>
        public decimal DuyguSiddeti(string tur, long simdikiTick) =>
            _duygu != null && string.Equals(_duygu.Tur, tur, StringComparison.Ordinal)
                ? _sonumleme.DuyguSiddeti(_duygu, simdikiTick)
                : 0m;

        /// <summary>Baskın duygunun güncel şiddeti; duygu yoksa 0.</summary>
        public decimal BaskinDuyguSiddeti(long simdikiTick) =>
            _duygu != null ? _sonumleme.DuyguSiddeti(_duygu, simdikiTick) : 0m;

        /// <summary>Stresin güncel değeri (0..100); söner.</summary>
        public decimal Stres(long simdikiTick) =>
            _stres != null ? _sonumleme.Stres(_stres, simdikiTick) : 0m;

        // ---- İnançlar ----

        /// <summary>Önermeye ham güven: <c>1 − Π(1 − güncel kanıt gücü)</c>; kanıt yoksa 0. Çelişkiye bakmaz.</summary>
        public decimal Guven(string onerme, long simdikiTick) =>
            _inanclar.TryGetValue(Onerme.Ayristir(onerme).ToString(), out var kayit) ? Guven(kayit, simdikiTick) : 0m;

        /// <summary>
        /// Önerme inanılan mı: güveni 0'dan büyük ve çelişen hiçbir önermeden zayıf değil. Çelişkide birikmiş güveni yüksek olan kazanır;
        /// eşitlikte en yeni kanıtı olan; o da eşitse hiçbiri inanılmaz.
        /// </summary>
        public bool Inanilan(string onerme, long simdikiTick) =>
            _inanclar.TryGetValue(Onerme.Ayristir(onerme).ToString(), out var kayit) && Inanilan(kayit, simdikiTick);

        /// <summary>Desene uyan inanılan önermelerin en yüksek güveni; hiçbiri yoksa 0.</summary>
        public decimal InancDerecesi(string desen, long simdikiTick) => InancDerecesi(OnermeDeseni.Ayristir(desen), simdikiTick);

        /// <summary>Desene uyan inanılan önermelerin en yüksek güveni; hiçbiri yoksa 0.</summary>
        public decimal InancDerecesi(OnermeDeseni desen, long simdikiTick)
        {
            var en = 0m;
            foreach (var kayit in _inanclar.Values)
            {
                if (desen.Uyar(kayit.Onerme) && Inanilan(kayit, simdikiTick))
                {
                    en = Math.Max(en, Guven(kayit, simdikiTick));
                }
            }

            return en;
        }

        /// <summary>Tam bir önermenin tüm kanıtları (silikleşenler dahil), eklenme sırasıyla.</summary>
        public IReadOnlyList<Kanit> Kanitlar(string onerme) =>
            _inanclar.TryGetValue(Onerme.Ayristir(onerme).ToString(), out var kayit)
                ? new ReadOnlyCollection<Kanit>(new List<Kanit>(kayit.Kanitlar))
                : new ReadOnlyCollection<Kanit>(new List<Kanit>());

        /// <summary>
        /// "Bunu kimden duydu?": desene uyan önermelerin <c>duydu</c> kanıtları; (tick, önerme, kaynak, olay) sıralı.
        /// </summary>
        public IReadOnlyList<Kanit> KimdenDuydu(string desen) => KimdenDuydu(OnermeDeseni.Ayristir(desen));

        /// <summary><see cref="KimdenDuydu(string)"/>, hazır desenle.</summary>
        public IReadOnlyList<Kanit> KimdenDuydu(OnermeDeseni desen)
        {
            var bulunan = new List<KeyValuePair<string, Kanit>>();
            foreach (var kv in _inanclar)
            {
                if (!desen.Uyar(kv.Value.Onerme))
                {
                    continue;
                }

                foreach (var k in kv.Value.Kanitlar)
                {
                    if (k.Tur == KanitTuru.Duydu)
                    {
                        bulunan.Add(new KeyValuePair<string, Kanit>(kv.Key, k));
                    }
                }
            }

            bulunan.Sort((a, b) =>
            {
                var c = a.Value.Tick.CompareTo(b.Value.Tick);
                if (c != 0)
                {
                    return c;
                }

                c = string.CompareOrdinal(a.Key, b.Key);
                if (c != 0)
                {
                    return c;
                }

                c = string.CompareOrdinal(a.Value.Kaynak, b.Value.Kaynak);
                return c != 0 ? c : a.Value.OlayId.CompareTo(b.Value.OlayId);
            });

            var sonuc = new List<Kanit>(bulunan.Count);
            foreach (var kv in bulunan)
            {
                sonuc.Add(kv.Value);
            }

            return new ReadOnlyCollection<Kanit>(sonuc);
        }

        /// <summary>Desene uyan bir önerme için bu kişiden gelen herhangi bir (türü fark etmez) kanıt var mı.</summary>
        public bool KaynakVar(string desen, string kisi) => KaynakVar(OnermeDeseni.Ayristir(desen), kisi);

        /// <summary><see cref="KaynakVar(string, string)"/>, hazır desenle.</summary>
        public bool KaynakVar(OnermeDeseni desen, string kisi)
        {
            foreach (var kayit in _inanclar.Values)
            {
                if (!desen.Uyar(kayit.Onerme))
                {
                    continue;
                }

                foreach (var k in kayit.Kanitlar)
                {
                    if (string.Equals(k.Kaynak, kisi, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // ---- Hafıza ----

        /// <summary>Bu tick'te sorgulanabilen hafıza kayıtları (kalıcılar ve henüz silikleşmemişler), olay kimliğine göre sıralı.</summary>
        public IReadOnlyList<HafizaKaydi> Hafiza(long simdikiTick)
        {
            var sonuc = new List<HafizaKaydi>();
            foreach (var h in _hafiza)
            {
                if (_sonumleme.HafizaAktifMi(h, simdikiTick))
                {
                    sonuc.Add(h);
                }
            }

            sonuc.Sort((a, b) => a.OlayId.CompareTo(b.OlayId));
            return new ReadOnlyCollection<HafizaKaydi>(sonuc);
        }

        /// <summary>
        /// Son <paramref name="sonNTick"/> tick içinde, <paramref name="fail"/> tarafından yapılmış <paramref name="olayTuru"/> türünde
        /// bir olayı hâlâ hatırlıyor mu. Olay bilgisi günlükten okunur (günlükten hiçbir şey silinmez).
        /// </summary>
        public bool HafizaVar(OlayGunlugu gunluk, string olayTuru, string fail, long sonNTick, long simdikiTick)
        {
            if (gunluk == null)
            {
                throw new ArgumentNullException(nameof(gunluk));
            }

            foreach (var h in Hafiza(simdikiTick))
            {
                if (!gunluk.Var(h.OlayId))
                {
                    continue;
                }

                var olay = gunluk[h.OlayId];
                if (string.Equals(olay.Tur, olayTuru, StringComparison.Ordinal)
                    && string.Equals(olay.Fail, fail, StringComparison.Ordinal)
                    && simdikiTick - olay.Tick <= sonNTick)
                {
                    return true;
                }
            }

            return false;
        }

        // ---- Durum hash'i ----

        /// <summary>
        /// Zihnin kanonik metni (durum hash'ine girer). Ham durumu yazar, tick'e bağlı hesaplanmış değeri yazmaz;
        /// böylece sorgu tick'i hash'i etkilemez. Satırlar: <c>Z</c> kimlik, <c>P</c> kişilik, <c>E</c> eksen, <c>M</c> modifier,
        /// <c>D</c>/<c>O</c> duygu/önceki duygu, <c>S</c> stres, <c>B</c> kanıt, <c>H</c> hafıza.
        /// </summary>
        public string KanonikMetin()
        {
            var sb = new StringBuilder();
            sb.Append("Z|").Append(Id).Append('\n');
            foreach (var ozellik in Kisilik.Ozellikler)
            {
                sb.Append("P|").Append(ozellik).Append('|').Append(KanonikYazici.Sayi(Kisilik.Deger(ozellik))).Append('\n');
            }

            foreach (var kv in _iliskiler)
            {
                foreach (var e in kv.Value.Eksen)
                {
                    if (e.Value != 0m)
                    {
                        sb.Append("E|").Append(kv.Key).Append('|').Append(e.Key).Append('|').Append(KanonikYazici.Sayi(e.Value)).Append('\n');
                    }
                }

                var modifierlar = new List<FikirModifier>(kv.Value.Modifierlar);
                modifierlar.Sort(ModifierSirasi);
                foreach (var m in modifierlar)
                {
                    sb.Append("M|").Append(kv.Key).Append('|').Append(m.Tur).Append('|').Append(KanonikYazici.Sayi(m.Deger))
                        .Append('|').Append(Uzun(m.BaslangicTick)).Append('|').Append(Uzun(m.Sure)).Append('|').Append(Uzun(m.NedenOlayId)).Append('\n');
                }
            }

            DuyguSatiri(sb, "D", _duygu);
            DuyguSatiri(sb, "O", _onceki);
            if (_stres != null)
            {
                sb.Append("S|").Append(KanonikYazici.Sayi(_stres.Deger)).Append('|').Append(Uzun(_stres.Tick)).Append('\n');
            }

            foreach (var kv in _inanclar)
            {
                var kanitlar = new List<Kanit>(kv.Value.Kanitlar);
                kanitlar.Sort((a, b) =>
                {
                    var c = a.Tick.CompareTo(b.Tick);
                    return c != 0 ? c : a.OlayId.CompareTo(b.OlayId);
                });
                foreach (var k in kanitlar)
                {
                    sb.Append("B|").Append(kv.Key).Append('|').Append(KanitTurleri.Ad(k.Tur)).Append('|').Append(k.Kaynak)
                        .Append('|').Append(Uzun(k.Tick)).Append('|').Append(KanonikYazici.Sayi(k.Guc)).Append('|').Append(Uzun(k.OlayId)).Append('\n');
                }
            }

            var hafiza = new List<HafizaKaydi>(_hafiza);
            hafiza.Sort((a, b) => a.OlayId.CompareTo(b.OlayId));
            foreach (var h in hafiza)
            {
                sb.Append("H|").Append(Uzun(h.OlayId)).Append('|').Append(KanonikYazici.Sayi(h.Onem)).Append('|').Append(Uzun(h.Tick)).Append('\n');
            }

            return sb.ToString();
        }

        // ---- Olay uygulama ----

        private void ModifierEkle(Olay olay)
        {
            var hedef = HedefDenetle(olay.Deger("hedef"));
            var tur = KimlikDenetle(olay.Deger("tur"), "modifier türü");
            var sure = ZihinOlaylari.Tam(olay, "sure");
            if (sure <= 0)
            {
                throw new ArgumentException($"Olay #{olay.Id}: modifier süresi pozitif olmalı.");
            }

            IliskiAl(hedef).Modifierlar.Add(new FikirModifier(tur, ZihinOlaylari.Sayi(olay, "deger"), olay.Tick, sure, olay.Id));
        }

        private void EksenDegistir(Olay olay)
        {
            var hedef = HedefDenetle(olay.Deger("hedef"));
            var eksen = olay.Deger("eksen");
            EksenDenetle(eksen);
            var iliski = IliskiAl(hedef);
            iliski.Eksen.TryGetValue(eksen, out var eski);
            var yeni = eski + ZihinOlaylari.Sayi(olay, "delta");
            iliski.Eksen[eksen] = Math.Max(Eksenler.Alt, Math.Min(Eksenler.Ust, yeni));
            iliski.EksenOlay[eksen] = olay.Id;
        }

        private void DuyguDegistir(Olay olay)
        {
            var tur = KimlikDenetle(olay.Deger("tur"), "duygu türü");
            var siddet = ZihinOlaylari.Sayi(olay, "siddet");
            if (siddet < 0m || siddet > 1m)
            {
                throw new ArgumentOutOfRangeException("siddet", siddet, $"Olay #{olay.Id}: duygu şiddeti 0..1 olmalı.");
            }

            var hedef = olay.Deger("hedef");
            if (hedef.Length > 0)
            {
                HedefDenetle(hedef);
            }

            var yeni = new Duygu(tur, siddet, hedef, olay.Tick, olay.Id);
            if (siddet > BaskinDuyguSiddeti(olay.Tick))
            {
                _onceki = _duygu;
                _duygu = yeni;
            }
        }

        private void StresDegistir(Olay olay)
        {
            var yeni = Stres(olay.Tick) + ZihinOlaylari.Sayi(olay, "delta");
            _stres = new StresDurumu(Math.Max(0m, Math.Min(100m, yeni)), olay.Tick);
        }

        private void KanitEkle(Olay olay)
        {
            var onerme = Onerme.Ayristir(olay.Deger("onerme"));
            var tur = KanitTurleri.Coz(olay.Deger("tur"));
            var kaynak = KimlikDenetle(olay.Deger("kaynak"), "kanıt kaynağı");
            var guc = ZihinOlaylari.Sayi(olay, "guc");
            if (guc < 0m || guc > 1m)
            {
                throw new ArgumentOutOfRangeException("guc", guc, $"Olay #{olay.Id}: kanıt gücü 0..1 olmalı.");
            }

            var anahtar = onerme.ToString();
            if (!_inanclar.TryGetValue(anahtar, out var kayit))
            {
                kayit = new InancKaydi(onerme);
                _inanclar.Add(anahtar, kayit);
            }

            kayit.Kanitlar.Add(new Kanit(tur, kaynak, olay.Tick, guc, olay.Id));
        }

        private void HafizaEkle(Olay olay)
        {
            var onem = ZihinOlaylari.Sayi(olay, "onem");
            if (onem < 0m || onem > 1m)
            {
                throw new ArgumentOutOfRangeException("onem", onem, $"Olay #{olay.Id}: hafıza önemi 0..1 olmalı.");
            }

            _hafiza.Add(new HafizaKaydi(ZihinOlaylari.Tam(olay, "olay"), onem, olay.Tick));
        }

        // ---- Yardımcılar ----

        private void AddEksen(List<FikirKatkisi> katkilar, Iliski? iliski, string eksen, decimal agirlik)
        {
            var deger = 0m;
            long olay = 0;
            if (iliski != null)
            {
                iliski.Eksen.TryGetValue(eksen, out deger);
                iliski.EksenOlay.TryGetValue(eksen, out olay);
            }

            katkilar.Add(new FikirKatkisi("eksen:" + eksen, deger * agirlik, olay, -1));
        }

        private decimal Guven(InancKaydi kayit, long simdikiTick)
        {
            var carpim = 1m;
            foreach (var k in kayit.Kanitlar)
            {
                carpim *= 1m - _sonumleme.KanitGucu(k, simdikiTick);
            }

            return 1m - carpim;
        }

        private bool Inanilan(InancKaydi kayit, long simdikiTick)
        {
            var guven = Guven(kayit, simdikiTick);
            if (guven <= 0m)
            {
                return false;
            }

            foreach (var diger in _inanclar.Values)
            {
                if (ReferenceEquals(diger, kayit) || !Onerme.Celisir(kayit.Onerme, diger.Onerme))
                {
                    continue;
                }

                var digerGuven = Guven(diger, simdikiTick);
                if (digerGuven <= 0m)
                {
                    continue;
                }

                var c = guven.CompareTo(digerGuven);
                if (c == 0)
                {
                    c = SonKanitTicki(kayit).CompareTo(SonKanitTicki(diger));
                }

                if (c <= 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static long SonKanitTicki(InancKaydi kayit)
        {
            var en = long.MinValue;
            foreach (var k in kayit.Kanitlar)
            {
                en = Math.Max(en, k.Tick);
            }

            return en;
        }

        private static int ModifierSirasi(FikirModifier a, FikirModifier b)
        {
            var c = string.CompareOrdinal(a.Tur, b.Tur);
            if (c != 0)
            {
                return c;
            }

            c = a.BaslangicTick.CompareTo(b.BaslangicTick);
            return c != 0 ? c : a.NedenOlayId.CompareTo(b.NedenOlayId);
        }

        private Iliski IliskiAl(string hedef)
        {
            if (!_iliskiler.TryGetValue(hedef, out var iliski))
            {
                iliski = new Iliski();
                _iliskiler.Add(hedef, iliski);
            }

            return iliski;
        }

        private static void EksenDenetle(string eksen)
        {
            if (eksen != Eksenler.Guven && eksen != Eksenler.Sevgi && eksen != Eksenler.Korku)
            {
                throw new ArgumentException($"Bilinmeyen eksen: \"{eksen}\" (guven, sevgi, korku).");
            }
        }

        private static string HedefDenetle(string hedef) => KimlikDenetle(hedef, "hedef");

        private static string KimlikDenetle(string deger, string ne)
        {
            if (!Kimlik.GecerliKimlikMi(deger))
            {
                throw new ArgumentException($"Geçersiz {ne}: \"{deger}\" (ASCII kimlik olmalı).");
            }

            return deger;
        }

        private static void DuyguSatiri(StringBuilder sb, string onek, Duygu? d)
        {
            if (d != null)
            {
                sb.Append(onek).Append('|').Append(d.Tur).Append('|').Append(KanonikYazici.Sayi(d.Siddet)).Append('|').Append(d.Hedef)
                    .Append('|').Append(Uzun(d.BaslangicTick)).Append('|').Append(Uzun(d.NedenOlayId)).Append('\n');
            }
        }

        private static string Uzun(long deger) => deger.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
