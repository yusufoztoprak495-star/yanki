using System;
using System.Collections.Generic;
using System.Globalization;
using Yanki.Core.Criteria;
using Yanki.Core.Events;
using Yanki.Core.Simulation;
using Yanki.Core.World;
using Yanki.Dialogue.Gramer;

namespace Yanki.Dialogue
{
    /// <summary>
    /// Diyalog motoru (A kararı): <see cref="Simulasyon.OlayYayinlandi"/> abonesidir. Çekirdek diyalog kavramını bilmez, simülasyon adımları değişmez; motor yalnızca okur
    /// (Submit, Tick ya da zihin yazımı çağırmaz; mimari testle kilitli). Olay tick sonunda yayınlandığı için satır TICK SONU durumundan üretilir. Oyuncu eylemlerinin kuralları aynı
    /// tick'te çalıştığından satır fikir değişimini görür; NPC eyleminin kuralları sıradaki tick'te çalışır, bu yüzden NPC kaynaklı followup (cevap) bir sonraki tick sonunda üretilir (G kararı).
    /// Bekleyen followup diyalog durumundadır ve baştan oynatmayla aynen yeniden kurulur.
    /// </summary>
    public sealed class DiyalogMotoru : IDisposable
    {
        private readonly Simulasyon _sim;
        private readonly SablonDosyasi _dosya;
        private readonly DiyalogAyarlari _ayar;
        private readonly IDialogueRenderer _varsayilan;
        private readonly IDialogueRenderer? _renderer;
        private readonly TekrarDurumu _tekrar = new TekrarDurumu();
        private readonly List<DiyalogSatiri> _satirlar = new List<DiyalogSatiri>();
        private readonly List<Bekleyen> _bekleyenler = new List<Bekleyen>();
        private readonly Action<Olay> _isleyici;
        private bool _bagli;

        private sealed class Bekleyen
        {
            public Bekleyen(long vade, Niyet ana, DiyalogSatiri anaSatir, SonraTanimi sonra)
            {
                Vade = vade;
                Ana = ana;
                AnaSatir = anaSatir;
                Sonra = sonra;
            }

            public long Vade { get; }

            public Niyet Ana { get; }

            public DiyalogSatiri AnaSatir { get; }

            public SonraTanimi Sonra { get; }
        }

        private DiyalogMotoru(Simulasyon sim, SablonDosyasi dosya, DiyalogAyarlari ayar, IDialogueRenderer? renderer)
        {
            _sim = sim;
            _dosya = dosya;
            _ayar = ayar;
            _varsayilan = new SablonRenderer(dosya, sim.Seed);
            _renderer = renderer;
            _isleyici = Isle;
        }

        /// <summary>
        /// Motoru simülasyona bağlar. Olayları kaçırmamak için simülasyon İLK TICK'TEN ÖNCE (ya da baştan oynatmadan önce) bağlanmalıdır. <paramref name="renderer"/> verilirse satır
        /// metni onun çıktısıdır; <c>diyalog_hash</c> yine varsayılan renderer'dan hesaplanır.
        /// </summary>
        public static DiyalogMotoru Baglan(Simulasyon sim, SablonDosyasi dosya, DiyalogAyarlari? ayar = null, IDialogueRenderer? renderer = null)
        {
            if (sim == null)
            {
                throw new ArgumentNullException(nameof(sim));
            }

            if (dosya == null)
            {
                throw new ArgumentNullException(nameof(dosya));
            }

            var a = ayar ?? new DiyalogAyarlari();
            a.Dogrula();
            var motor = new DiyalogMotoru(sim, dosya, a, renderer) { _bagli = true };
            sim.OlayYayinlandi += motor._isleyici;
            return motor;
        }

        /// <summary>Şimdiye kadar üretilen satırlar (üretim sırasıyla).</summary>
        public IReadOnlyList<DiyalogSatiri> Satirlar => _satirlar;

        /// <summary>Bir sonraki tick sonunu bekleyen followup sayısı (diyalog durumu; replay ile yeniden kurulur).</summary>
        public int BekleyenFollowupSayisi => _bekleyenler.Count;

        /// <summary>Satırların <c>diyalog_hash</c>'i.</summary>
        public string DiyalogHash() => DiyalogHashu.Hesapla(_satirlar);

        /// <summary>Simülasyondan ayrılır (olay aboneliğini kaldırır).</summary>
        public void Dispose()
        {
            if (_bagli)
            {
                _sim.OlayYayinlandi -= _isleyici;
                _bagli = false;
            }
        }

        // RNG alanı: olay kimliğini içerir (aynı tick'te aynı NPC'nin iki satırı aynı zarı çekmez) ve followup'ı ayırır. Çekirdek RNG akışına dokunmaz.
        internal static string RngAlani(Niyet niyet) =>
            "diyalog:" + niyet.OlayId.ToString(CultureInfo.InvariantCulture) + (niyet.Followup ? ":sonra" : string.Empty);

        private void Isle(Olay olay)
        {
            BekleyenleriBosalt(olay.Tick);

            var niyet = NiyetUretici.Uret(olay, _sim, _dosya, _ayar);
            if (niyet == null)
            {
                return;
            }

            var sablon = SatirUret(niyet, out var satir);
            if (satir == null || sablon == null || sablon.Sonra == null || niyet.Followup)
            {
                return;
            }

            if (_sim.Zihinler.Var(olay.Fail))
            {
                // NPC kaynaklı olayın kuralları sıradaki tick'te çalışır: cevap satırı bir sonraki tick sonunda üretilir (G kararı, seçenek b).
                _bekleyenler.Add(new Bekleyen(olay.Tick + 1, niyet, satir, sablon.Sonra));
            }
            else
            {
                FollowupUret(new Bekleyen(olay.Tick, niyet, satir, sablon.Sonra));
            }
        }

        private void BekleyenleriBosalt(long tick)
        {
            while (_bekleyenler.Count > 0 && _bekleyenler[0].Vade <= tick)
            {
                var b = _bekleyenler[0];
                _bekleyenler.RemoveAt(0);
                FollowupUret(b);
            }
        }

        private void FollowupUret(Bekleyen b)
        {
            var hedef = b.Sonra.Hedef == "dinleyen" ? b.Ana.Dinleyen : b.Sonra.Hedef;
            string? dusme = null;
            if (!_sim.Zihinler.Var(hedef))
            {
                dusme = $"hedef \"{hedef}\" bir NPC değil";
            }
            else if (string.Equals(hedef, b.Ana.Konusan, StringComparison.Ordinal))
            {
                dusme = "hedef konuşanın kendisi";
            }
            else
            {
                var a = _sim.Dunya.TekDeger("konum!", b.Ana.Konusan) ?? string.Empty;
                var h = _sim.Dunya.TekDeger("konum!", hedef) ?? string.Empty;
                if (a.Length == 0 || !string.Equals(a, h, StringComparison.Ordinal))
                {
                    dusme = $"{hedef} ile {b.Ana.Konusan} aynı mekanda değil (üretim anında denetlenir)";
                }
            }

            if (dusme != null)
            {
                b.AnaSatir.NotEkle($"followup düştü ({b.Sonra.Concept} → {hedef}): {dusme}");
                return;
            }

            var niyet = NiyetUretici.Followup(b.Ana, hedef, b.Sonra.Concept, _sim, _ayar);
            SatirUret(niyet, out var satir);
            if (satir == null)
            {
                b.AnaSatir.NotEkle($"followup üretilemedi ({b.Sonra.Concept} → {hedef}): uygun şablon yok");
            }
            else
            {
                b.AnaSatir.NotEkle($"followup: satır #{satir.No} ({hedef}, {b.Sonra.Concept})");
            }
        }

        private static string Mekan(Simulasyon sim, string kimlik) => sim.Dunya.TekDeger("konum!", kimlik) ?? string.Empty;

        // Niyetten satır üretir; seçilen şablonu döndürür (satır üretilemediyse null).
        private Sablon? SatirUret(Niyet niyet, out DiyalogSatiri? satir)
        {
            satir = null;
            var tick = _sim.SimdikiTick;
            var notlar = new List<string>
            {
                $"niyet: {niyet.Ad} (olay #{niyet.OlayId.ToString(CultureInfo.InvariantCulture)}, tick {niyet.Tick.ToString(CultureInfo.InvariantCulture)}{(niyet.Followup ? ", followup" : string.Empty)}); " +
                $"konuşan {niyet.Konusan}, dinleyen {niyet.Dinleyen}, ton {niyet.Ton}, fikir bandı {niyet.FikirBandi}, baskın duygu {niyet.Gerekce.BaskinDuygu}"
                + (niyet.Konu.Length > 0 ? ", konu " + niyet.Konu : string.Empty),
            };

            // 1. Niyetin şablonları ve tekrar-dışı süzgeçler (konu deseni, hafıza bağlanabilirliği).
            var onAday = new List<Sablon>();
            var hafizalar = new SortedDictionary<string, HafizaSonucu>(StringComparer.Ordinal);
            foreach (var s in _dosya.Sablonlar)
            {
                if (s.Concept != niyet.Ad)
                {
                    continue;
                }

                if (s.KonuDeseni != null)
                {
                    bool uyar;
                    try
                    {
                        uyar = niyet.Konu.Length > 0 && s.KonuDeseni.Uyar(Yanki.Core.Mind.Onerme.Ayristir(niyet.Konu));
                    }
                    catch (ArgumentException)
                    {
                        uyar = false;
                    }

                    if (!uyar)
                    {
                        notlar.Add($"şablon {s.Id}: elendi (konu_deseni uymuyor)");
                        continue;
                    }
                }

                if (s.Hafiza != null)
                {
                    var h = HafizaGondermesi.Bul(_sim, niyet, s.Hafiza);
                    if (h == null)
                    {
                        notlar.Add($"şablon {s.Id}: elendi (konuşanın hatırladığı {s.Hafiza.Tur} olayı yok: gönderme yapılamaz)");
                        continue;
                    }

                    hafizalar[s.Id] = h;
                }

                onAday.Add(s);
            }

            // 2. Tekrar filtresi seçimden ÖNCE uygulanır (D kararı).
            var gruplar = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
            foreach (var s in onAday)
            {
                if (s.Tekrar.Tur == TekrarTuru.Tekrarsiz)
                {
                    if (!gruplar.TryGetValue(s.Grup, out var uyeler))
                    {
                        uyeler = new SortedSet<string>(StringComparer.Ordinal);
                        gruplar[s.Grup] = uyeler;
                    }

                    uyeler.Add(s.Id);
                }
            }

            var adaylar = new List<Aday<Sablon>>();
            foreach (var s in onAday)
            {
                var uyeler = gruplar.TryGetValue(s.Grup, out var u) ? (ICollection<string>)u : new string[0];
                if (_tekrar.Engelli(s, niyet.Konusan, tick, uyeler))
                {
                    notlar.Add($"şablon {s.Id}: elendi (tekrar: {TekrarMetni(s.Tekrar)})");
                    continue;
                }

                adaylar.Add(new Aday<Sablon>(s.Id, s, s.Kriterler));
            }

            // 3. Tek seçim yolu Secici.EnIyiSec; kriterler HER ZAMAN konuşanın zihni üzerinden (ben = konuşan). Yuvası bağlanamayan şablon aday olamaz.
            var cozucu = _sim.SorguCozucusu();
            var baglam = new SorguBaglami(niyet.Konusan, _sim.OyuncuId, niyet.Konusan, niyet.Dinleyen);
            Sablon? secilen = null;
            string metin = string.Empty;
            string varsayilanMetin = string.Empty;
            while (adaylar.Count > 0)
            {
                var rng = AnahtarliRng.Icin(_sim.Seed, niyet.Tick, niyet.Konusan, RngAlani(niyet));
                var sonuc = Secici.EnIyiSec(adaylar, cozucu, baglam, rng);
                if (!sonuc.Bulundu)
                {
                    break;
                }

                var aday = sonuc.Secilen!;
                hafizalar.TryGetValue(aday.Id, out var hafiza);
                var girdi = GirdiKur(niyet, aday.Deger, hafiza);
                try
                {
                    varsayilanMetin = _varsayilan.Render(girdi);
                    metin = _renderer == null ? varsayilanMetin : _renderer.Render(girdi);
                }
                catch (InvalidOperationException e)
                {
                    notlar.Add($"şablon {aday.Id}: elendi (yuva bağlanamadı: {e.Message})");
                    adaylar.Remove(aday);
                    continue;
                }

                secilen = aday.Deger;
                foreach (var iz in sonuc.Izler)
                {
                    notlar.Add(IzMetni(iz, iz.AdayId == aday.Id));
                }

                if (hafiza != null)
                {
                    notlar.Add($"hafıza göndermesi: olay #{hafiza.OlayId.ToString(CultureInfo.InvariantCulture)} ({hafiza.Tur}, {hafiza.Fail}, tick {hafiza.Tick.ToString(CultureInfo.InvariantCulture)})");
                }

                break;
            }

            if (secilen == null)
            {
                return null;
            }

            _tekrar.Kaydet(secilen, niyet.Konusan, tick);
            notlar.Insert(1, $"seçilen şablon: {secilen.Id}");
            satir = new DiyalogSatiri(
                _satirlar.Count + 1, tick, niyet.OlayId, niyet.Konusan, niyet.Dinleyen, secilen.Id, metin, varsayilanMetin, niyet.Followup, notlar);
            _satirlar.Add(satir);
            return secilen;
        }

        private static string TekrarMetni(TekrarKurali t)
        {
            switch (t.Tur)
            {
                case TekrarTuru.BirKez: return "bir_kez";
                case TekrarTuru.Bekleme: return "tekrar_bekleme " + t.Bekleme.ToString(CultureInfo.InvariantCulture);
                default: return "tekrarsiz";
            }
        }

        private static string IzMetni(EslesmeIzi iz, bool secilen)
        {
            var tutan = new List<string>();
            var tutmayan = new List<string>();
            foreach (var k in iz.Sonuclar)
            {
                (k.Tuttu ? tutan : tutmayan).Add(k.Kriter.ToString() + (k.Kriter.Zorunlu ? " [zorunlu]" : string.Empty));
            }

            return $"şablon {iz.AdayId}: " + (iz.Elendi ? "elendi (zorunlu kriter tutmadı)" : "puan " + iz.Puan.ToString(CultureInfo.InvariantCulture))
                + (secilen ? " SEÇİLDİ" : string.Empty)
                + "; tutan: " + (tutan.Count == 0 ? "-" : string.Join(", ", tutan)) + "; tutmayan: " + (tutmayan.Count == 0 ? "-" : string.Join(", ", tutmayan));
        }

        private RenderGirdisi GirdiKur(Niyet niyet, Sablon sablon, HafizaSonucu? hafiza)
        {
            var gercekler = new List<SecilmisGercek>();
            void AdEkle(string yuva, string kimlik)
            {
                var ad = OnermeMetni.GorunenAd(kimlik, _dosya);
                gercekler.Add(new SecilmisGercek(yuva, ad.Ad, true, ad.OzelAd));
            }

            AdEkle("konusan", niyet.Konusan);
            AdEkle("dinleyen", niyet.Dinleyen);
            AdEkle("oyuncu", _sim.OyuncuId);
            if (niyet.Konu.Length > 0)
            {
                try
                {
                    gercekler.Add(new SecilmisGercek("konu", OnermeMetni.Uret(niyet.Konu, _dosya), false, false));
                }
                catch (InvalidOperationException)
                {
                    // konu cümlesi kurulamadı: #konu# kullanan şablonun yuvası bağlanamaz, şablon aday olamaz.
                }
            }

            if (hafiza != null)
            {
                gercekler.Add(new SecilmisGercek("hafiza.zaman", ZamanIfadesi.Uret(hafiza.Tick, _sim.SimdikiTick, _ayar), false, false));
                AdEkle("hafiza.fail", hafiza.Fail);
            }

            return new RenderGirdisi(niyet, sablon.Metin, gercekler);
        }
    }
}
