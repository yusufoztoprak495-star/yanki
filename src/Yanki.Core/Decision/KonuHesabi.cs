using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Yanki.Core.Events;
using Yanki.Core.Mind;
using Yanki.Core.World;

namespace Yanki.Core.Decision
{
    /// <summary>
    /// Anlatılacak bir konu: önerme, puan bileşenleri ve dayanak kanıt. <see cref="Kanit"/> konuşanın o önerme için (yalanda: yalanın
    /// çeliştiği inanç için) seçilen en güçlü TEK güncel kanıtıdır; aktarım gücü, kök ve <c>NedenOlaylar</c> dayanağı hep bu kanıttan gelir.
    /// </summary>
    public sealed class KonuAdayi
    {
        /// <summary>Kanonik önerme.</summary>
        public string Onerme { get; }

        /// <summary>Önem (0..1).</summary>
        public decimal Onem { get; }

        /// <summary>Yenilik (0..1).</summary>
        public decimal Yenilik { get; }

        /// <summary>İlgi (0..1).</summary>
        public decimal Ilgi { get; }

        /// <summary><c>Onem × Yenilik × Ilgi</c>, 6 ondalığa yuvarlanmış. Yalanda 0 (yalan puanlanmaz).</summary>
        public decimal Puan { get; }

        /// <summary>Dayanak kanıt.</summary>
        public Kanit Kanit { get; }

        /// <summary>Aktarılan söylentinin kökü: doğruda kanıtın etkin kökü, yalanda konuşan.</summary>
        public string Kok { get; }

        /// <summary>Konuşanın kendi inancına göre yalan mı.</summary>
        public bool Yalan { get; }

        /// <summary>Konu kurar.</summary>
        public KonuAdayi(string onerme, decimal onem, decimal yenilik, decimal ilgi, decimal puan, Kanit kanit, string kok, bool yalan)
        {
            Onerme = onerme ?? throw new ArgumentNullException(nameof(onerme));
            Onem = onem;
            Yenilik = yenilik;
            Ilgi = ilgi;
            Puan = puan;
            Kanit = kanit ?? throw new ArgumentNullException(nameof(kanit));
            Kok = kok ?? throw new ArgumentNullException(nameof(kok));
            Yalan = yalan;
        }
    }

    /// <summary>Elenen konu ve insan okuyabilir nedeni (karar izinde görünür).</summary>
    public sealed class ElenenKonu
    {
        /// <summary>Önerme ya da yalan şablonu.</summary>
        public string Onerme { get; }

        /// <summary>Neden.</summary>
        public string Neden { get; }

        /// <summary>Kurar.</summary>
        public ElenenKonu(string onerme, string neden)
        {
            Onerme = onerme ?? throw new ArgumentNullException(nameof(onerme));
            Neden = neden ?? throw new ArgumentNullException(nameof(neden));
        }
    }

    /// <summary>Konu seçiminin sonucu: seçilen konular (puana göre azalan, eşitlikte önerme metnine göre), elenenler ve konu sınırı.</summary>
    public sealed class KonuSecimi
    {
        /// <summary>Seçilen konular.</summary>
        public IReadOnlyList<KonuAdayi> Konular { get; }

        /// <summary>Elenenler.</summary>
        public IReadOnlyList<ElenenKonu> Elenenler { get; }

        /// <summary>Bu sohbette uygulanan konu sınırı (yalanda 1).</summary>
        public int Sinir { get; }

        /// <summary>Kurar.</summary>
        public KonuSecimi(IEnumerable<KonuAdayi> konular, IEnumerable<ElenenKonu> elenenler, int sinir)
        {
            Konular = new ReadOnlyCollection<KonuAdayi>(new List<KonuAdayi>(konular));
            Elenenler = new ReadOnlyCollection<ElenenKonu>(new List<ElenenKonu>(elenenler));
            Sinir = sinir;
        }
    }

    /// <summary>
    /// Dedikodunun TEK hesap sınıfı (ADR-012): konu puanı, konu sınırı, yalan seçimi ve aktarım gücü. Karar (aday elenmesi,
    /// <c>konu_puani</c>) ve aktarım (adım 5–6) aynı sonucu kullanır. Yalnızca KONUŞANIN zihni okunur; dinleyenin zihni yalnızca
    /// <see cref="AktarimGucu"/> içinde, kararı değil yazılacak kanıtın gücünü belirlemek için okunur. Salt okunurdur ve durum hash'ine girmez.
    /// </summary>
    public sealed class KonuHesabi
    {
        private readonly ZihinDeposu _zihinler;
        private readonly OlayGunlugu _gunluk;
        private readonly long _tick;
        private readonly DedikoduAyarlari _ayar;

        // Tick içi önbellek: yalnızca arama (sıralı gezinti yok); durum hash'ine girmez, hesap saf olduğundan sonucu değiştirmez.
        private readonly Dictionary<string, KonuSecimi> _onbellek = new Dictionary<string, KonuSecimi>(StringComparer.Ordinal);

        /// <summary>Hesabı <paramref name="tick"/> anı için kurar.</summary>
        public KonuHesabi(ZihinDeposu zihinler, OlayGunlugu gunluk, long tick, DedikoduAyarlari? ayar = null)
        {
            _zihinler = zihinler ?? throw new ArgumentNullException(nameof(zihinler));
            _gunluk = gunluk ?? throw new ArgumentNullException(nameof(gunluk));
            _tick = tick;
            _ayar = ayar ?? new DedikoduAyarlari();
            _ayar.Dogrula();
        }

        /// <summary>
        /// Bu sohbette en çok kaç konu konuşulur: <c>KonuSinirTaban + ⌊sosyallik × KonuSinirSosyallik⌋</c>, konuşanın dinleyene
        /// fikir bandı <c>yakin</c> ya da <c>dost</c> ise +1 (plan 5.3).
        /// </summary>
        public int KonuSiniri(string konusan, string dinleyen)
        {
            var zihin = _zihinler[konusan];
            var n = _ayar.KonuSinirTaban + (int)decimal.Floor(zihin.Kisilik.Deger("sosyallik") * _ayar.KonuSinirSosyallik);
            var bant = zihin.FikirBandi(dinleyen, _tick);
            return bant == "yakin" || bant == "dost" ? n + 1 : n;
        }

        /// <summary>
        /// Doğru anlatılabilecek konular: konuşanın inandığı, ham güveni eşikte ya da üstünde olan, <paramref name="desenler"/>'den birine uyan
        /// (<c>anlatti</c> önermeleri hiçbir zaman), bu dinleyene daha önce anlatılmamış, dinleyenden gelmemiş ve kökü dinleyen olmayan önermeler.
        /// Puan eşiğin altındakiler elenir; kalanlar puana göre azalan sıralanır ve konu sınırı kadarı seçilir.
        /// </summary>
        public KonuSecimi Dogru(string konusan, string dinleyen, IEnumerable<OnermeDeseni> desenler)
        {
            if (desenler == null)
            {
                throw new ArgumentNullException(nameof(desenler));
            }

            var desenListesi = new List<OnermeDeseni>(desenler);
            var anahtar = "d|" + konusan + "|" + dinleyen + "|" + string.Join(";", desenListesi);
            if (_onbellek.TryGetValue(anahtar, out var onceki))
            {
                return onceki;
            }

            var zihin = _zihinler[konusan];
            var konular = new List<KonuAdayi>();
            var elenenler = new List<ElenenKonu>();
            List<Onerme>? inanilanlar = null;

            foreach (var metin in zihin.Onermeler)
            {
                var onerme = Onerme.Ayristir(metin);
                if (string.Equals(onerme.Yuklem, "anlatti", StringComparison.Ordinal) || !DesenlerdenBiriUyar(desenListesi, onerme))
                {
                    continue;
                }

                if (!zihin.Inanilan(metin, _tick))
                {
                    elenenler.Add(new ElenenKonu(metin, "konusan buna inanmiyor"));
                    continue;
                }

                var guven = zihin.Guven(metin, _tick);
                if (guven < _ayar.AnlatmaGuveniEsigi)
                {
                    elenenler.Add(new ElenenKonu(metin, "guven " + Metin(guven) + " < " + Metin(_ayar.AnlatmaGuveniEsigi)));
                    continue;
                }

                if (zihin.AktarimVar(metin, dinleyen))
                {
                    elenenler.Add(new ElenenKonu(metin, "bu kisiye daha once anlatildi"));
                    continue;
                }

                if (DinleyenKaynakVeyaKok(zihin, metin, dinleyen))
                {
                    elenenler.Add(new ElenenKonu(metin, "kanit dinleyenden geldi ya da kok dinleyen"));
                    continue;
                }

                var kanit = zihin.EnGucluKanit(metin, _tick);
                if (kanit == null)
                {
                    continue;
                }

                inanilanlar ??= Inanilanlar(zihin);
                var onem = Onem(zihin, kanit);
                var yenilik = Yenilik(kanit);
                var ilgi = Ilgi(konusan, dinleyen, onerme, inanilanlar);
                var puan = Math.Round(onem * yenilik * ilgi, 6, MidpointRounding.ToEven);
                if (puan < _ayar.KonuEsigi)
                {
                    elenenler.Add(new ElenenKonu(metin, "konu puani " + Metin(puan) + " < " + Metin(_ayar.KonuEsigi)));
                    continue;
                }

                konular.Add(new KonuAdayi(metin, onem, yenilik, ilgi, puan, kanit, zihin.EtkinKok(kanit), false));
            }

            konular.Sort((a, b) =>
            {
                var c = b.Puan.CompareTo(a.Puan);
                return c != 0 ? c : string.CompareOrdinal(a.Onerme, b.Onerme);
            });
            var sinir = KonuSiniri(konusan, dinleyen);
            if (konular.Count > sinir)
            {
                konular.RemoveRange(sinir, konular.Count - sinir);
            }

            var sonuc = new KonuSecimi(konular, elenenler, sinir);
            _onbellek[anahtar] = sonuc;
            return sonuc;
        }

        /// <summary>Konuşanın bu dinleyene anlatabileceği en yüksek konu puanı (<c>konu_puani</c>); konu yoksa 0.</summary>
        public decimal EnYuksekKonuPuani(string konusan, string dinleyen, IEnumerable<OnermeDeseni> desenler)
        {
            var en = 0m;
            foreach (var konu in Dogru(konusan, dinleyen, desenler).Konular)
            {
                en = Math.Max(en, konu.Puan);
            }

            return en;
        }

        /// <summary>
        /// Yalan: yazar şablonlarından, konuşanın İNANDIĞI bir önermeyle çelişen ve bu dinleyene daha önce söylenmemiş olanlar;
        /// dayanak, çeliştiği inancın en güçlü tek kanıtıdır (yoksa şablon elenir). Birden çok uygun yalan varsa
        /// <c>AnahtarliRng.Icin(seed, tick, konusan, "yalan")</c> seçer. Yalan tek konudur; yalanın kökü konuşandır.
        /// </summary>
        public KonuSecimi Yalan(string konusan, string dinleyen, IEnumerable<string> onermeler, ulong seed)
        {
            if (onermeler == null)
            {
                throw new ArgumentNullException(nameof(onermeler));
            }

            var onermeListesi = new List<string>(onermeler);
            var anahtar = "y|" + konusan + "|" + dinleyen + "|" + seed.ToString(CultureInfo.InvariantCulture) + "|" + string.Join(";", onermeListesi);
            if (_onbellek.TryGetValue(anahtar, out var onceki))
            {
                return onceki;
            }

            var zihin = _zihinler[konusan];
            var sablonlar = new SortedSet<string>(StringComparer.Ordinal);
            var elenenler = new List<ElenenKonu>();
            foreach (var ham in onermeListesi)
            {
                try
                {
                    sablonlar.Add(Onerme.Ayristir(ham).ToString());
                }
                catch (ArgumentException)
                {
                    elenenler.Add(new ElenenKonu(ham, "gecersiz yalan onermesi"));
                }
            }

            var adaylar = new List<KonuAdayi>();
            foreach (var metin in sablonlar)
            {
                var yalan = Onerme.Ayristir(metin);
                if (zihin.AktarimVar(metin, dinleyen))
                {
                    elenenler.Add(new ElenenKonu(metin, "bu kisiye daha once soylendi"));
                    continue;
                }

                if (zihin.Inanilan(metin, _tick))
                {
                    elenenler.Add(new ElenenKonu(metin, "konusan buna inaniyor, yalan degil"));
                    continue;
                }

                Kanit? dayanak = null;
                foreach (var q in zihin.Onermeler)
                {
                    if (!Onerme.Celisir(yalan, Onerme.Ayristir(q)) || !zihin.Inanilan(q, _tick))
                    {
                        continue;
                    }

                    var aday = zihin.EnGucluKanit(q, _tick);
                    if (aday != null && (dayanak == null || Daha(zihin, aday, dayanak)))
                    {
                        dayanak = aday;
                    }
                }

                if (dayanak == null)
                {
                    elenenler.Add(new ElenenKonu(metin, "celistigi inanc yok (dayanak kanit bulunamadi)"));
                    continue;
                }

                adaylar.Add(new KonuAdayi(metin, 0m, 0m, 0m, 0m, dayanak, konusan, true));
            }

            if (adaylar.Count > 1)
            {
                var rng = AnahtarliRng.Icin(seed, _tick, konusan, "yalan");
                var secilen = adaylar[rng.Aralik(0, adaylar.Count)];
                adaylar.Clear();
                adaylar.Add(secilen);
            }

            var yalanSonuc = new KonuSecimi(adaylar, elenenler, 1);
            _onbellek[anahtar] = yalanSonuc;
            return yalanSonuc;
        }

        /// <summary>
        /// Dinleyene yazılacak <c>duydu</c> kanıtının gücü: <c>kanıt_gücü × min(Üst, Taban + Eğim × güven)</c>; güven dinleyenin konuşana
        /// duyduğu <c>guven</c> eksenidir (dinleyenin zihni yoksa, örn. oyuncu, 0). Sonuç kanıt gücünü geçemez.
        /// </summary>
        public decimal AktarimGucu(decimal kanitGucu, string konusan, string dinleyen)
        {
            var guven = _zihinler.Var(dinleyen) ? _zihinler[dinleyen].Eksen(konusan, Eksenler.Guven) : 0m;
            var carpan = Math.Min(_ayar.GuvenCarpaniUst, _ayar.GuvenCarpaniTaban + _ayar.GuvenCarpaniEgim * guven);
            return Math.Min(kanitGucu, Math.Round(kanitGucu * carpan, 6, MidpointRounding.ToEven));
        }

        // ---- yardımcılar ----

        // aday mevcuttan "daha güçlü" mü: güncel güç büyükse; eşitlikte küçük olay kimliği.
        private bool Daha(Zihin zihin, Kanit aday, Kanit mevcut)
        {
            var a = zihin.KanitGucu(aday, _tick);
            var m = zihin.KanitGucu(mevcut, _tick);
            return a > m || (a == m && aday.OlayId < mevcut.OlayId);
        }

        private bool DesenlerdenBiriUyar(List<OnermeDeseni> desenler, Onerme onerme)
        {
            foreach (var d in desenler)
            {
                if (d.Uyar(onerme))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool DinleyenKaynakVeyaKok(Zihin zihin, string onerme, string dinleyen)
        {
            foreach (var k in zihin.Kanitlar(onerme))
            {
                if (string.Equals(k.Kaynak, dinleyen, StringComparison.Ordinal)
                    || string.Equals(zihin.EtkinKok(k), dinleyen, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        // Önem (M4, tek adım): dayanak kanıt olayının doğrudan nedenlerinden hafıza kaydı olan ilkinin önemi; yoksa varsayılan.
        private decimal Onem(Zihin zihin, Kanit kanit)
        {
            if (_gunluk.Var(kanit.OlayId))
            {
                foreach (var neden in _gunluk[kanit.OlayId].NedenOlaylar)
                {
                    var onem = zihin.HafizaOnemi(neden, _tick);
                    if (onem != null)
                    {
                        return onem.Value;
                    }
                }
            }

            return _ayar.VarsayilanOnem;
        }

        private decimal Yenilik(Kanit kanit)
        {
            var gecen = Math.Max(0L, _tick - kanit.Tick);
            return Math.Max(0m, 1m - (decimal)gecen / _ayar.YenilikPenceresi);
        }

        // İlgi (tek adım bağ): dinleyen önermede geçiyorsa 1; konuşanın inandığı başka bir önerme dinleyeni içerip bu önermeyle bir
        // varlık paylaşıyorsa 0.7; konuşan önermede geçiyorsa 0.5; yoksa 0.25. Sayı argümanları varlık sayılmaz.
        private decimal Ilgi(string konusan, string dinleyen, Onerme onerme, List<Onerme> inanilanlar)
        {
            if (Icerir(onerme, dinleyen))
            {
                return _ayar.IlgiDinleyen;
            }

            foreach (var q in inanilanlar)
            {
                if (ReferenceEquals(q, onerme) || string.Equals(q.ToString(), onerme.ToString(), StringComparison.Ordinal))
                {
                    continue;
                }

                if (Icerir(q, dinleyen) && Paylasir(q, onerme))
                {
                    return _ayar.IlgiBagli;
                }
            }

            return Icerir(onerme, konusan) ? _ayar.IlgiKonusan : _ayar.IlgiYok;
        }

        private List<Onerme> Inanilanlar(Zihin zihin)
        {
            var sonuc = new List<Onerme>();
            foreach (var metin in zihin.Onermeler)
            {
                var o = Onerme.Ayristir(metin);
                if (!string.Equals(o.Yuklem, "anlatti", StringComparison.Ordinal) && zihin.Inanilan(metin, _tick))
                {
                    sonuc.Add(o);
                }
            }

            return sonuc;
        }

        private static bool Icerir(Onerme onerme, string varlik)
        {
            foreach (var a in onerme.Argumanlar)
            {
                if (string.Equals(a, varlik, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Paylasir(Onerme a, Onerme b)
        {
            foreach (var x in a.Argumanlar)
            {
                if (Sayi(x))
                {
                    continue;
                }

                if (Icerir(b, x))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Sayi(string arguman) =>
            decimal.TryParse(arguman, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _);

        private static string Metin(decimal d) => d.ToString("0.######", CultureInfo.InvariantCulture);
    }
}
