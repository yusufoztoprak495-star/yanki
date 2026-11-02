using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Yanki.Core.Criteria;
using Yanki.Core.Events;
using Yanki.Core.Mind;
using Yanki.Core.World;

namespace Yanki.Core.Rules
{
    /// <summary>
    /// Bir kuralın tetiklenebileceği olay: olayın kendisi, onu algılayan NPC'ler (ordinal sıralı) ve her algılayan için,
    /// üretilen olayların <c>NedenOlaylar</c>'ına tetikleyiciyle birlikte eklenecek algı olayları (ör. o NPC'nin <c>gordu</c> kanıtı).
    /// </summary>
    public sealed class TetikBilgisi
    {
        /// <summary>Tetikleyici olay.</summary>
        public Olay Olay { get; }

        /// <summary>Olayı algılayan NPC'ler, ordinal sıralı.</summary>
        public IReadOnlyList<string> Algilayanlar { get; }

        private readonly Func<string, IReadOnlyList<long>> _algiNedenleri;

        /// <summary>Tetik bilgisi kurar.</summary>
        public TetikBilgisi(Olay olay, IEnumerable<string> algilayanlar, Func<string, IReadOnlyList<long>>? algiNedenleri = null)
        {
            Olay = olay ?? throw new ArgumentNullException(nameof(olay));
            Algilayanlar = new ReadOnlyCollection<string>(new List<string>(algilayanlar ?? throw new ArgumentNullException(nameof(algilayanlar))));
            _algiNedenleri = algiNedenleri ?? (_ => new long[0]);
        }

        /// <summary>NPC'nin bu olayı algılamasından doğan olayların kimlikleri.</summary>
        public IReadOnlyList<long> AlgiNedenleri(string npc) => _algiNedenleri(npc);
    }

    /// <summary>
    /// Bir kuralın bir NPC için ürettiği, henüz olaya dönüşmemiş etki: hedef ve gerçek metni çözülmüştür.
    /// <see cref="KuralMotoru.OlayYap"/> bunu günlüğe eklenecek olaya çevirir.
    /// </summary>
    public sealed class UretilenEtki
    {
        /// <summary>Kuralı.</summary>
        public KuralTanimi Kural { get; }

        /// <summary>Etki.</summary>
        public KuralEtkisi Etki { get; }

        /// <summary>Kuralı değerlendiren (<c>ben</c>) NPC.</summary>
        public string Npc { get; }

        /// <summary>Tetikleyici olay.</summary>
        public Olay Tetik { get; }

        /// <summary>Çözülmüş hedef varlık kimliği (modifier, eksen, duygu); yoksa boş.</summary>
        public string Hedef { get; }

        /// <summary>Çözülmüş kanonik gerçek metni (gerçek etkisi); yoksa boş.</summary>
        public string Gercek { get; }

        /// <summary>Üretilecek olayın nedenleri: tetikleyici ve algı olayları.</summary>
        public IReadOnlyList<long> Nedenler { get; }

        internal UretilenEtki(KuralTanimi kural, KuralEtkisi etki, string npc, Olay tetik, string hedef, string gercek, IReadOnlyList<long> nedenler)
        {
            Kural = kural;
            Etki = etki;
            Npc = npc;
            Tetik = tetik;
            Hedef = hedef;
            Gercek = gercek;
            Nedenler = nedenler;
        }
    }

    /// <summary>
    /// Sonuç kuralı motoru (plan 5.6, 6. adım 3). Saf bir değerlendiricidir: durumu değiştirmez, yalnızca etki listesi döndürür;
    /// çağıran etkileri <see cref="OlayYap"/> ile olaya çevirip günlüğe ekler (etkiler doğrudan state değiştirmez).
    /// <para>
    /// Sıra sabittir: tetikleyiciler verilen sırayla, her tetikleyici için ona uyan kurallar kimliğe göre ordinal sırayla,
    /// her kural için olayı algılayan NPC'ler ordinal sırayla, bir kural bir NPC için tam bir kez değerlendirilir.
    /// Tüm değerlendirme tek bir durum görüntüsü üzerinde yapılır: adım içinde üretilen etkiler aynı adımdaki başka kurallara görünmez.
    /// </para>
    /// <para>
    /// Bir kural yalnızca tüm kriterleri tutunca tetiklenir (ADR-010); kriteri olmayan kural her tetikleyici olayda tetiklenir.
    /// <c>agirlik</c> ve <c>zorunlu</c> sonucu etkilemez (kriter puanı kural tetiklenmesinde kullanılmaz). Kuralın başvurduğu bir rol olayda
    /// tanımsızsa (ör. hedefsiz eylemde <c>olay.hedef</c>) kural o olay için atlanır.
    /// </para>
    /// </summary>
    public sealed class KuralMotoru
    {
        private readonly List<KuralTanimi> _kurallar;

        /// <summary>Kural kimliğine göre ordinal sıralı kurallar.</summary>
        public IReadOnlyList<KuralTanimi> Kurallar => new ReadOnlyCollection<KuralTanimi>(_kurallar);

        /// <summary>Motor kurar; kurallar kimliğe göre sıralanır, kimlikler tekil olmalıdır.</summary>
        /// <exception cref="ArgumentException">Kural kimliği tekrar ederse.</exception>
        public KuralMotoru(IEnumerable<KuralTanimi> kurallar)
        {
            _kurallar = new List<KuralTanimi>(kurallar ?? throw new ArgumentNullException(nameof(kurallar)));
            _kurallar.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            for (var i = 1; i < _kurallar.Count; i++)
            {
                if (string.Equals(_kurallar[i - 1].Id, _kurallar[i].Id, StringComparison.Ordinal))
                {
                    throw new ArgumentException($"Kural kimliği tekrar ediyor: \"{_kurallar[i].Id}\".", nameof(kurallar));
                }
            }
        }

        /// <summary>Tetikleyicileri değerlendirir ve üretilecek etkileri sırayla döndürür.</summary>
        public IReadOnlyList<UretilenEtki> Degerlendir(
            IReadOnlyList<TetikBilgisi> tetikler, Dunya dunya, ZihinDeposu zihinler, OlayGunlugu gunluk, long tick, string oyuncuId)
        {
            if (tetikler == null)
            {
                throw new ArgumentNullException(nameof(tetikler));
            }

            var cozucu = new ZihinSorguCozucusu(dunya, zihinler, gunluk, tick);
            var yazilanGercekler = new SortedSet<string>(StringComparer.Ordinal);
            var sonuc = new List<UretilenEtki>();

            foreach (var tetik in tetikler)
            {
                foreach (var kural in _kurallar)
                {
                    if (!string.Equals(kural.NeZaman, tetik.Olay.Tur, StringComparison.Ordinal) || !RollerTanimli(kural, tetik.Olay, oyuncuId))
                    {
                        continue;
                    }

                    foreach (var npc in tetik.Algilayanlar)
                    {
                        var baglam = new SorguBaglami(npc, oyuncuId, olayFail: tetik.Olay.Fail, olayHedef: tetik.Olay.Hedef);
                        if (!KriterlerTutuyor(kural, cozucu, baglam))
                        {
                            continue;
                        }

                        var nedenler = new SortedSet<long> { tetik.Olay.Id };
                        foreach (var n in tetik.AlgiNedenleri(npc))
                        {
                            nedenler.Add(n);
                        }

                        var nedenListesi = new ReadOnlyCollection<long>(new List<long>(nedenler));
                        foreach (var etki in kural.Etkiler)
                        {
                            var uretilen = EtkiUret(kural, etki, npc, tetik.Olay, baglam, nedenListesi, dunya, yazilanGercekler);
                            if (uretilen != null)
                            {
                                sonuc.Add(uretilen);
                            }
                        }
                    }
                }
            }

            return new ReadOnlyCollection<UretilenEtki>(sonuc);
        }

        /// <summary>Üretilen etkiyi günlüğe eklenecek olaya çevirir. Olay tetikleyicinin mekanında geçer.</summary>
        public static Olay OlayYap(UretilenEtki e, long id, long tick, int sira)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            var kuralId = e.Kural.Id;
            var mekan = e.Tetik.Mekan;
            switch (e.Etki.Tur)
            {
                case EtkiTuru.Modifier:
                    return ZihinOlaylari.ModifierEklendi(id, tick, sira, e.Npc, e.Hedef, e.Etki.Ad, e.Etki.Deger, e.Etki.Sure, e.Nedenler, kuralId, mekan);
                case EtkiTuru.Eksen:
                    return ZihinOlaylari.EksenDegisti(id, tick, sira, e.Npc, e.Hedef, e.Etki.Ad, e.Etki.Deger, e.Nedenler, kuralId, mekan);
                case EtkiTuru.Duygu:
                    return ZihinOlaylari.DuyguDegisti(id, tick, sira, e.Npc, e.Etki.Ad, e.Etki.Deger, e.Hedef, e.Nedenler, kuralId, mekan);
                case EtkiTuru.Hafiza:
                    return ZihinOlaylari.HafizaEklendi(id, tick, sira, e.Npc, e.Tetik.Id, e.Etki.Deger, e.Nedenler, kuralId, mekan);
                default:
                    return new Olay(
                        id, tick, sira,
                        e.Etki.Kaldir ? Dunya.GercekKaldirildi : Dunya.GercekEklendi,
                        string.Empty, string.Empty, mekan,
                        new[] { new KeyValuePair<string, string>("gercek", e.Gercek) },
                        e.Nedenler, kuralId);
            }
        }

        private static bool RollerTanimli(KuralTanimi kural, Olay olay, string oyuncuId)
        {
            foreach (var rol in kural.GerekliRoller)
            {
                switch (rol)
                {
                    case Rol.Oyuncu when oyuncuId.Length == 0:
                    case Rol.OlayFail when olay.Fail.Length == 0:
                    case Rol.OlayHedef when olay.Hedef.Length == 0:
                        return false;
                }
            }

            return true;
        }

        private static bool KriterlerTutuyor(KuralTanimi kural, ISorguCozucu cozucu, SorguBaglami baglam)
        {
            if (kural.Kriterler.Count == 0)
            {
                return true;
            }

            foreach (var kriter in kural.Kriterler)
            {
                if (!KriterDegerlendirici.Degerlendir(kriter, cozucu, baglam).Tuttu)
                {
                    return false;
                }
            }

            return true;
        }

        internal static UretilenEtki? EtkiUret(
            KuralTanimi kural, KuralEtkisi etki, string npc, Olay tetik, SorguBaglami baglam,
            IReadOnlyList<long> nedenler, Dunya dunya, SortedSet<string> yazilanGercekler)
        {
            switch (etki.Tur)
            {
                case EtkiTuru.Gercek:
                {
                    var g = Gercek.Ayristir(GercekSablonu.Doldur(etki.GercekSablonu, rol => baglam.Coz(rol), RolKumesi.Hepsi));
                    var metin = g.ToString();
                    var varMi = dunya.Var(g);

                    // Değişiklik yapmayan etki (zaten var olanı eklemek, olmayanı kaldırmak) ve aynı adımda zaten üretilen etki atlanır.
                    if (etki.Kaldir ? !varMi : varMi)
                    {
                        return null;
                    }

                    if (!yazilanGercekler.Add((etki.Kaldir ? "-" : "+") + metin))
                    {
                        return null;
                    }

                    return new UretilenEtki(kural, etki, npc, tetik, string.Empty, metin, nedenler);
                }

                case EtkiTuru.Hafiza:
                    return new UretilenEtki(kural, etki, npc, tetik, string.Empty, string.Empty, nedenler);
                default:
                {
                    var hedef = etki.Hedef.Length == 0 ? string.Empty : HedefCoz(etki.Hedef, baglam);

                    // Modifier, eksen ve duygu hedefi bir kişi (NPC ya da oyuncu) olmalı. Hedef bir mekan ya da eşya ise (ör. Hedefi mekan olan
                    // bir oyuncu eylemi, ayrildi/geldi olayları) etki üretilmez: zihne mekan adına ilişki kaydı yazılıp durum bozulmaz.
                    // 7. hafta: bu atlama için fren olayı yazılacak.
                    if (hedef.Length > 0 && !KisiMi(dunya, hedef))
                    {
                        return null;
                    }

                    return new UretilenEtki(kural, etki, npc, tetik, hedef, string.Empty, nedenler);
                }
            }
        }

        private static bool KisiMi(Dunya dunya, string kimlik)
        {
            var tur = dunya.TurOf(kimlik);
            return tur == VarlikTuru.Npc || tur == VarlikTuru.Oyuncu;
        }

        private static string HedefCoz(string hedef, SorguBaglami baglam) =>
            RolAdlari.TryCoz(hedef, out var rol) ? baglam.Coz(rol) : hedef;
    }
}
