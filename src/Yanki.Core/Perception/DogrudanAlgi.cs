using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Yanki.Core.Events;
using Yanki.Core.Mind;
using Yanki.Core.World;

namespace Yanki.Core.Perception
{
    /// <summary>Doğrudan algının ayarlanabilir sayıları (plan 5.3). Varsayılanlar Yusuf'a açık noktadır.</summary>
    public sealed class AlgiAyarlari
    {
        /// <summary>Doğrudan algıdan doğan <c>gordu</c> kanıtının gücü (0..1).</summary>
        public decimal GorduKanitGucu { get; init; } = 0.9m;

        /// <summary>Algılanan olay için hafızaya yazılan önem (0..1); kalıcılık eşiğinin altındaysa silikleşir.</summary>
        public decimal HafizaOnemi { get; init; } = 0.5m;
    }

    /// <summary>
    /// Doğrudan algı (plan 5.3): bir olay olduğunda görünürlük seviyesini karşılayan NPC'ler olayı algılar; her biri için
    /// <c>gordu</c> kanıtı ve hafıza kaydı olayı üretilir. Görünürlük olayın yükünde <c>gorunurluk</c> anahtarıyla tutulur:
    /// <c>acik</c> (yoksa varsayılan), <c>sessiz</c>, <c>gizli</c>. Dedikodu (<c>duydu</c> kanıtı aktarımı) 6. haftanın işidir.
    /// </summary>
    public static class DogrudanAlgi
    {
        /// <summary>Algı olaylarının <c>KuralId</c>'si.</summary>
        public const string AlgiKuralId = "algi";

        private const string GorunurlukAnahtari = "gorunurluk";

        /// <summary>
        /// Olayı algılayan NPC'ler, ordinal sıralı. <c>acik</c>: olayın mekanındaki (<c>konum!</c> gerçeğine göre) bütün NPC'ler;
        /// <c>sessiz</c>: fail ve (olayın mekanında bulunan) hedef; <c>gizli</c>: yalnızca fail. NPC olmayan fail/hedef algılamaz.
        /// </summary>
        /// <exception cref="ArgumentException">Görünürlük değeri bilinmiyorsa.</exception>
        public static IReadOnlyList<string> Algilayanlar(Dunya dunya, Olay olay)
        {
            if (dunya == null)
            {
                throw new ArgumentNullException(nameof(dunya));
            }

            if (olay == null)
            {
                throw new ArgumentNullException(nameof(olay));
            }

            var sonuc = new SortedSet<string>(StringComparer.Ordinal);
            switch (Gorunurluk(olay))
            {
                case "acik":
                    if (olay.Mekan.Length > 0)
                    {
                        foreach (var npc in dunya.Varliklar(VarlikTuru.Npc))
                        {
                            if (string.Equals(dunya.TekDeger("konum!", npc), olay.Mekan, StringComparison.Ordinal))
                            {
                                sonuc.Add(npc);
                            }
                        }
                    }

                    break;
                case "sessiz":
                    NpcIse(dunya, olay.Fail, sonuc);
                    if (olay.Mekan.Length > 0 && string.Equals(dunya.TekDeger("konum!", olay.Hedef), olay.Mekan, StringComparison.Ordinal))
                    {
                        NpcIse(dunya, olay.Hedef, sonuc);
                    }

                    break;
                default:
                    NpcIse(dunya, olay.Fail, sonuc);
                    break;
            }

            return new ReadOnlyCollection<string>(new List<string>(sonuc));
        }

        /// <summary>
        /// Olaydan algılanan önerme: <c>gercek_eklendi</c> → o gerçek; <c>gercek_kaldirildi</c> → <c>degil(gercek)</c>;
        /// diğer türler → <c>tur(fail, hedef)</c> (boş olan atlanır). Fail ve hedef ikisi de boşsa <c>null</c>.
        /// </summary>
        public static string? OnermeUret(Olay olay)
        {
            if (olay == null)
            {
                throw new ArgumentNullException(nameof(olay));
            }

            switch (olay.Tur)
            {
                case Dunya.GercekEklendi:
                    return olay.Deger("gercek");
                case Dunya.GercekKaldirildi:
                    return Onerme.OlumsuzOn + olay.Deger("gercek") + ")";
            }

            var argumanlar = new List<string>(2);
            if (olay.Fail.Length > 0)
            {
                argumanlar.Add(olay.Fail);
            }

            if (olay.Hedef.Length > 0)
            {
                argumanlar.Add(olay.Hedef);
            }

            return argumanlar.Count == 0 ? null : olay.Tur + "(" + string.Join(",", argumanlar) + ")";
        }

        /// <summary>
        /// Her algılayan için sırayla <c>kanit_eklendi</c> (<c>gordu</c>; önerme türetilemezse atlanır) ve <c>hafiza_eklendi</c> olaylarını
        /// üretir. Olaylar günlüğe EKLENMEZ; kimlik ve sıra günlüğün sonuna göre verilir, çağıran sırayla ekler ve uygular.
        /// Hepsinin <c>NedenOlaylar</c> alanında algılanan olay, <c>KuralId</c>'sinde <c>algi</c> yazar.
        /// </summary>
        public static IReadOnlyList<Olay> Uret(Dunya dunya, Olay olay, OlayGunlugu gunluk, AlgiAyarlari? ayar = null)
        {
            if (gunluk == null)
            {
                throw new ArgumentNullException(nameof(gunluk));
            }

            ayar ??= new AlgiAyarlari();
            var onerme = OnermeUret(olay);
            var neden = new[] { olay.Id };
            var id = gunluk.SonrakiId;
            var sira = gunluk.SonrakiSira(olay.Tick);
            var sonuc = new List<Olay>();

            foreach (var npc in Algilayanlar(dunya, olay))
            {
                if (onerme != null)
                {
                    sonuc.Add(ZihinOlaylari.KanitEklendi(
                        id++, olay.Tick, sira++, npc, onerme, KanitTurleri.Ad(KanitTuru.Gordu), npc, ayar.GorduKanitGucu, neden, AlgiKuralId, olay.Mekan));
                }

                sonuc.Add(ZihinOlaylari.HafizaEklendi(
                    id++, olay.Tick, sira++, npc, olay.Id, ayar.HafizaOnemi, neden, AlgiKuralId, olay.Mekan));
            }

            return new ReadOnlyCollection<Olay>(sonuc);
        }

        private static string Gorunurluk(Olay olay)
        {
            foreach (var kv in olay.Yuk)
            {
                if (string.Equals(kv.Key, GorunurlukAnahtari, StringComparison.Ordinal))
                {
                    if (kv.Value == "acik" || kv.Value == "sessiz" || kv.Value == "gizli")
                    {
                        return kv.Value;
                    }

                    throw new ArgumentException($"Olay #{olay.Id}: bilinmeyen görünürlük \"{kv.Value}\" (acik, sessiz, gizli).");
                }
            }

            return "acik";
        }

        private static void NpcIse(Dunya dunya, string kimlik, SortedSet<string> sonuc)
        {
            if (kimlik.Length > 0 && dunya.TurOf(kimlik) == VarlikTuru.Npc)
            {
                sonuc.Add(kimlik);
            }
        }
    }
}
