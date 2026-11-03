using System;
using System.Collections.Generic;
using System.Text;
using Yanki.Core.Decision;
using Yanki.Core.Events;
using Yanki.Core.Rules;

namespace Yanki.Core.Brakes
{
    /// <summary>
    /// Cooldown ve tetiklenme sayaçları (7. hafta, ADR-013). Sayaçlar hafızadan okunmaz ve ayrı bir "tetiklendi" olayı yoktur:
    /// yalnızca olay günlüğünün bir katlamasıdır (<see cref="Isle"/> her olay eklenince çağrılır). Aynı günlük aynı sayaçları verir;
    /// bu yüzden yeniden kurulabilirler (boş bir nesneye bütün olaylar sırayla verilir).
    /// <para>
    /// Tetikleme günlükte şöyle tanınır. Kural: kuralın <c>KuralId</c>'li olayları; bir tetiklemenin olayları ardışık yazılır ve aynı
    /// (tick, NPC, <c>NedenOlaylar</c>) imzasını taşır (NPC: zihin olaylarında <c>npc</c>, <c>gercek</c> olaylarında <c>ben</c> yükü; sınırlı
    /// kuralın <c>gercek</c> olayına yazılır). Eylem: <c>eylem:&lt;id&gt;</c> KuralId'li, <c>Fail</c>'i dolu İLK olay (konuşmadaki ilk
    /// <c>anlatti</c>, hareketin <c>ayrildi</c>'si); bu yüzden tek karar tek sayılır.
    /// </para>
    /// <para>
    /// Kural değerlendirmesi saf kalsın diye adım içi artışlar <see cref="Gorunum"/> üzerinde tutulur (sıra: tetikleyici → kural → NPC);
    /// bu, "tek durum görüntüsü" ilkesinin belgeli istisnasıdır: değer dünya durumu değil tetiklenme kaydıdır.
    /// </para>
    /// </summary>
    public sealed class TetikSayaclari
    {
        internal sealed class Kayit
        {
            public long SonTick = long.MinValue;
            public int SonTickSayisi;
            public int Toplam;
            public long SonOlayId;

            public Kayit Kopya() => (Kayit)MemberwiseClone();

            public void Say(long tick)
            {
                Toplam++;
                if (SonTick == tick)
                {
                    SonTickSayisi++;
                }
                else
                {
                    SonTick = tick;
                    SonTickSayisi = 1;
                }
            }
        }

        private readonly SortedDictionary<string, TetikSiniri> _kurallar = new SortedDictionary<string, TetikSiniri>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, TetikSiniri> _eylemler = new SortedDictionary<string, TetikSiniri>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, Kayit> _kayitlar = new SortedDictionary<string, Kayit>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, string> _sonKuralImzasi = new SortedDictionary<string, string>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, string> _sonEylemImzasi = new SortedDictionary<string, string>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, long> _frenOlaylari = new SortedDictionary<string, long>(StringComparer.Ordinal);

        /// <summary>Sınırı tanımlı kural ve eylemler için sayaç kurar; sınırı olmayanlar izlenmez.</summary>
        public TetikSayaclari(IEnumerable<KuralTanimi> kurallar, IEnumerable<EylemTanimi> eylemler)
        {
            foreach (var k in kurallar ?? throw new ArgumentNullException(nameof(kurallar)))
            {
                if (k.TetikSiniri != null && k.TetikSiniri.Tanimli)
                {
                    _kurallar[k.Id] = k.TetikSiniri;
                }
            }

            foreach (var e in eylemler ?? throw new ArgumentNullException(nameof(eylemler)))
            {
                if (e.TetikSiniri != null && e.TetikSiniri.Tanimli)
                {
                    _eylemler[e.Id] = e.TetikSiniri;
                }
            }
        }

        /// <summary>Hiçbir kural ya da eylemin sınırı yok mu (sayaç izlemesi gerekmez).</summary>
        public bool Bos => _kurallar.Count == 0 && _eylemler.Count == 0;

        private static string KuralAnahtari(string id, string npc, SinirKapsami kapsam) =>
            kapsam == SinirKapsami.Kural ? "k|" + id : "k|" + id + "|" + npc;

        private static string KuralTickAnahtari(string id) => "kt|" + id;

        private static string EylemAnahtari(string id, string npc, string hedef, SinirKapsami kapsam) =>
            kapsam == SinirKapsami.Hedef ? "e|" + id + "|" + npc + "|" + hedef : "e|" + id + "|" + npc;

        private Kayit? Al(string anahtar) => _kayitlar.TryGetValue(anahtar, out var k) ? k : null;

        /// <summary>Günlüğe eklenen her olay bununla işlenir (yalnızca sınırlı kural/eylem olayları ve fren olayları iz bırakır).</summary>
        public void Isle(Olay olay)
        {
            if (olay == null)
            {
                throw new ArgumentNullException(nameof(olay));
            }

            if (olay.Tur == FrenOlayi.Tur)
            {
                _frenOlaylari[FrenAnahtari(olay.KuralId.Substring("fren:".Length), Yuk(olay, "kimlik"), Yuk(olay, "npc"), Yuk(olay, "hedef"))] = olay.Id;
                return;
            }

            if (olay.KuralId.StartsWith("eylem:", StringComparison.Ordinal))
            {
                var id = olay.KuralId.Substring("eylem:".Length);
                if (_eylemler.TryGetValue(id, out var siniri) && olay.Fail.Length > 0)
                {
                    var imza = olay.Tick.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + olay.Fail;
                    if (!_sonEylemImzasi.TryGetValue(id, out var onceki) || onceki != imza)
                    {
                        _sonEylemImzasi[id] = imza;
                        Say(EylemAnahtari(id, olay.Fail, olay.Hedef, siniri.Kapsam), olay);
                    }
                }

                return;
            }

            if (_kurallar.TryGetValue(olay.KuralId, out var kuralSiniri))
            {
                var npc = Yuk(olay, "npc");
                if (npc.Length == 0)
                {
                    npc = Yuk(olay, "ben");
                }

                var imza = olay.Tick.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + npc + "|" + string.Join(",", olay.NedenOlaylar);
                if (!_sonKuralImzasi.TryGetValue(olay.KuralId, out var onceki) || onceki != imza)
                {
                    _sonKuralImzasi[olay.KuralId] = imza;
                    Say(KuralAnahtari(olay.KuralId, npc, kuralSiniri.Kapsam), olay);
                    if (kuralSiniri.TickBasinaEnFazla > 0)
                    {
                        Say(KuralTickAnahtari(olay.KuralId), olay);   // tick içi sayaç: kapsamdan bağımsız, kanonik metne girmez
                    }
                }
            }
        }

        private void Say(string anahtar, Olay olay)
        {
            if (!_kayitlar.TryGetValue(anahtar, out var k))
            {
                k = new Kayit();
                _kayitlar.Add(anahtar, k);
            }

            k.Say(olay.Tick);
            k.SonOlayId = olay.Id;
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

        private static string FrenAnahtari(string tur, string kimlik, string npc, string hedef) => tur + "|" + kimlik + "|" + npc + "|" + hedef;

        // ---- Eylem sorguları (adım 5; yazmaz) ----

        /// <summary>
        /// Eylemin bu NPC ve hedef için kapalı olup olmadığı: <see cref="FrenTurleri.Cooldown"/>, <see cref="FrenTurleri.EnFazla"/> ya da <c>null</c> (açık).
        /// Sayaçlar önceki adımların olaylarından gelir; aynı NPC aynı adımda yalnızca bir eylem seçtiği için adım içi artış gerekmez.
        /// </summary>
        public string? EylemEngeli(EylemTanimi eylem, string npc, string hedef, long tick)
        {
            if (!_eylemler.TryGetValue(eylem.Id, out var s))
            {
                return null;
            }

            var k = Al(EylemAnahtari(eylem.Id, npc, hedef, s.Kapsam));
            if (k == null)
            {
                return null;
            }

            if (s.Cooldown > 0 && tick - k.SonTick < s.Cooldown)
            {
                return FrenTurleri.Cooldown;
            }

            return s.EnFazla > 0 && k.Toplam >= s.EnFazla ? FrenTurleri.EnFazla : null;
        }

        /// <summary>Eylemin bu kapsamdaki son tetiklemesinin ilk olayı (fren olayının nedeni); yoksa 0.</summary>
        public long EylemSonTetiklemeOlayi(EylemTanimi eylem, string npc, string hedef)
        {
            var kapsam = _eylemler.TryGetValue(eylem.Id, out var s) ? s.Kapsam : SinirKapsami.Npc;
            return Al(EylemAnahtari(eylem.Id, npc, hedef, kapsam))?.SonOlayId ?? 0;
        }

        /// <summary>
        /// Aday freni için yeni olay yazılmalı mı: aynı (fren, eylem, NPC, hedef) için son tetiklemeden sonra bu fren olayı zaten yazıldıysa hayır.
        /// Engel kalkıp (cooldown bitip) yeni tetiklemeyle yeniden başlarsa tekrar evet; <c>en_fazla</c> hiç kalkmadığı için bir kez yazılır.
        /// </summary>
        public bool AdayFreniYazilmali(FrenBilgisi fren, EylemTanimi eylem)
        {
            var sonFren = _frenOlaylari.TryGetValue(FrenAnahtari(fren.Tur, fren.Kimlik, fren.Npc, fren.Hedef), out var id) ? id : 0;
            return sonFren == 0 || sonFren < EylemSonTetiklemeOlayi(eylem, fren.Npc, fren.Hedef);
        }

        /// <summary>Kural değerlendirmesi için adım içi artışları taşıyan görünüm (sayaçları değiştirmez).</summary>
        public Gorunum YeniGorunum() => new Gorunum(this);

        /// <summary>Kanonik metin: yalnızca cooldown ya da en_fazla tanımlı kural/eylemlerin sayaçları, ordinal sıralı; hiçbiri yoksa boş.</summary>
        public string KanonikMetin()
        {
            var sb = new StringBuilder();
            foreach (var kv in _kayitlar)
            {
                if (SinirliAnahtar(kv.Key))
                {
                    sb.Append("SAYAC|").Append(kv.Key).Append('|')
                        .Append(kv.Value.SonTick.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('|')
                        .Append(kv.Value.Toplam.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
                }
            }

            return sb.ToString();
        }

        private bool SinirliAnahtar(string anahtar)
        {
            var parcalar = anahtar.Split('|');
            var tablo = parcalar[0] == "k" ? _kurallar : _eylemler;
            return tablo.TryGetValue(parcalar[1], out var s) && s.KanonikMetneGirer;
        }

        /// <summary>Bir <c>DegerlendirFrenli</c> çağrısının adım içi artışları; kayıtlar ilk dokunuşta temel sayaçtan kopyalanır.</summary>
        public sealed class Gorunum
        {
            private readonly TetikSayaclari _temel;
            private readonly SortedDictionary<string, Kayit> _yerel = new SortedDictionary<string, Kayit>(StringComparer.Ordinal);

            internal Gorunum(TetikSayaclari temel)
            {
                _temel = temel;
            }

            private Kayit? Oku(string anahtar) => _yerel.TryGetValue(anahtar, out var k) ? k : _temel.Al(anahtar);

            /// <summary>Kuralın bu NPC için kapalı olduğu fren türü (cooldown, en_fazla, tick_siniri) ya da <c>null</c>.</summary>
            public string? Engel(KuralTanimi kural, string npc, long tick)
            {
                var s = kural.TetikSiniri;
                if (s == null || !s.Tanimli)
                {
                    return null;
                }

                var k = Oku(KuralAnahtari(kural.Id, npc, s.Kapsam));
                if (k != null && s.Cooldown > 0 && tick - k.SonTick < s.Cooldown)
                {
                    return FrenTurleri.Cooldown;
                }

                if (k != null && s.EnFazla > 0 && k.Toplam >= s.EnFazla)
                {
                    return FrenTurleri.EnFazla;
                }

                var t = s.TickBasinaEnFazla > 0 ? Oku(KuralTickAnahtari(kural.Id)) : null;
                return t != null && t.SonTick == tick && t.SonTickSayisi >= s.TickBasinaEnFazla ? FrenTurleri.TickSiniri : null;
            }

            private void YerelSay(string anahtar, long tick)
            {
                if (!_yerel.TryGetValue(anahtar, out var k))
                {
                    k = _temel.Al(anahtar)?.Kopya() ?? new Kayit();
                    _yerel[anahtar] = k;
                }

                k.Say(tick);
            }

            /// <summary>Kuralın bu NPC için tetiklendiğini adım içinde hemen kaydeder.</summary>
            public void Kaydet(KuralTanimi kural, string npc, long tick)
            {
                var s = kural.TetikSiniri;
                if (s == null || !s.Tanimli)
                {
                    return;
                }

                YerelSay(KuralAnahtari(kural.Id, npc, s.Kapsam), tick);
                if (s.TickBasinaEnFazla > 0)
                {
                    YerelSay(KuralTickAnahtari(kural.Id), tick);
                }
            }
        }
    }
}
