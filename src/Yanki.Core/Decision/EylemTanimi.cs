using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Yanki.Core.Criteria;
using Yanki.Core.Rules;

namespace Yanki.Core.Decision
{
    /// <summary>
    /// Eylemin hedef türü. <c>yok</c>: hedefsiz; <c>npc</c>: aynı mekandaki başka bir NPC; <c>oyuncu</c>: aynı mekandaysa oyuncu;
    /// <c>komsu_mekan</c>: bağlı komşu mekan (hareket eylemi).
    /// </summary>
    public enum EylemHedefTuru
    {
        /// <summary>Hedefsiz.</summary>
        Yok,

        /// <summary>Aynı mekandaki başka bir NPC.</summary>
        Npc,

        /// <summary>Aynı mekandaysa oyuncu.</summary>
        Oyuncu,

        /// <summary>Bağlı komşu mekan; eylem bir harekettir.</summary>
        KomsuMekan,
    }

    /// <summary><see cref="EylemHedefTuru"/> ile JSON adları arasında çeviri.</summary>
    public static class EylemHedefTurleri
    {
        /// <summary>Geçerli adlar, JSON'daki sırayla.</summary>
        public const string GecerliAdlar = "yok, npc, oyuncu, komsu_mekan";

        /// <summary>Türün JSON adı.</summary>
        public static string Ad(EylemHedefTuru tur)
        {
            switch (tur)
            {
                case EylemHedefTuru.Yok: return "yok";
                case EylemHedefTuru.Npc: return "npc";
                case EylemHedefTuru.Oyuncu: return "oyuncu";
                case EylemHedefTuru.KomsuMekan: return "komsu_mekan";
                default: throw new ArgumentOutOfRangeException(nameof(tur), tur, "Bilinmeyen hedef türü.");
            }
        }

        /// <summary>Adı türe çevirir; bilinmiyorsa <c>false</c>.</summary>
        public static bool TryCoz(string ad, out EylemHedefTuru tur)
        {
            switch (ad)
            {
                case "yok": tur = EylemHedefTuru.Yok; return true;
                case "npc": tur = EylemHedefTuru.Npc; return true;
                case "oyuncu": tur = EylemHedefTuru.Oyuncu; return true;
                case "komsu_mekan": tur = EylemHedefTuru.KomsuMekan; return true;
                default: tur = EylemHedefTuru.Yok; return false;
            }
        }
    }

    /// <summary>
    /// Çekirdeğe ait özel olay türü adları. <c>ayrildi</c> ve <c>geldi</c> hareketin iki dış olayıdır, <c>karar_verildi</c> karar izini
    /// taşır, <c>hareket</c> oyuncunun hareket eylemidir (ADR-011). Senaryo ve eylemler bu adları yeniden tanımlayamaz.
    /// </summary>
    public static class YerlesikTurler
    {
        /// <summary>Hareketin kalkış olayı (<c>Mekan</c> = kalkış).</summary>
        public const string Ayrildi = "ayrildi";

        /// <summary>Hareketin varış olayı (<c>Mekan</c> = varış).</summary>
        public const string Geldi = "geldi";

        /// <summary>Karar izini taşıyan olay; algılanmaz, kural tetiklemez, hash'e girmez.</summary>
        public const string KararVerildi = "karar_verildi";

        /// <summary>Oyuncunun hareket eylemi türü (<c>PlayerAction.Tur</c>).</summary>
        public const string OyuncuHareketi = "hareket";

        /// <summary>Ad çekirdeğe ayrılmış mı (dünya/zihin olayları dahil).</summary>
        public static bool AyrilmisMi(string tur)
        {
            if (tur == Ayrildi || tur == Geldi || tur == KararVerildi || tur == OyuncuHareketi)
            {
                return true;
            }

            foreach (var t in KuralYukleyici.YerlesikOlayTurleri)
            {
                if (string.Equals(t, tur, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Eylem ölçütü (consideration): kriter sözlüğünden bir sorgu, <c>[Min, Max]</c> ile 0..1'e normalize edilir, eğriden geçer.
    /// Doğru/yanlış sonuç 1/0 sayılır.
    /// </summary>
    public sealed class Olcut
    {
        /// <summary>Sorgu (kriter sözlüğünden; tek sorgu yolu).</summary>
        public SorguCagrisi Alan { get; }

        /// <summary>Normalizasyonun alt ucu.</summary>
        public decimal Min { get; }

        /// <summary>Normalizasyonun üst ucu; <see cref="Min"/>'den büyük.</summary>
        public decimal Max { get; }

        /// <summary>Eğri.</summary>
        public EgriTanimi Egri { get; }

        /// <summary>Kaynak dosyadaki 1 tabanlı satır; bilinmiyorsa 0.</summary>
        public int Satir { get; }

        /// <summary>Ölçüt kurar.</summary>
        /// <exception cref="ArgumentException"><paramref name="max"/> <paramref name="min"/>'den büyük değilse.</exception>
        public Olcut(SorguCagrisi alan, decimal min, decimal max, EgriTanimi egri, int satir = 0)
        {
            if (max <= min)
            {
                throw new ArgumentException("Ölçütte min < max olmalı.", nameof(max));
            }

            Alan = alan ?? throw new ArgumentNullException(nameof(alan));
            Min = min;
            Max = max;
            Egri = egri ?? throw new ArgumentNullException(nameof(egri));
            Satir = satir;
        }
    }

    /// <summary>
    /// Eylem tanımı (plan 5.5, ADR-011). Hareket eylemi (<see cref="EylemHedefTuru.KomsuMekan"/>) <see cref="OlayTuru"/> taşımaz ve
    /// etkisiz olabilir: hareketin kendisi etkidir. Diğer eylemlerde <see cref="OlayTuru"/> ve en az bir <see cref="Etkiler"/> öğesi vardır.
    /// </summary>
    public sealed class EylemTanimi
    {
        /// <summary>Kimlik; olayların <c>KuralId</c>'si <c>eylem:&lt;Id&gt;</c> olur.</summary>
        public string Id { get; }

        /// <summary>Eylemin dışarı görünen olayının türü; hareket eyleminde boş.</summary>
        public string OlayTuru { get; }

        /// <summary>Hedef türü.</summary>
        public EylemHedefTuru Hedef { get; }

        /// <summary>Puan tabanı, 0'dan büyük.</summary>
        public decimal Taban { get; }

        /// <summary>Ön koşullar: hepsi tutmalı (ADR-010).</summary>
        public IReadOnlyList<Kriter> OnKosullar { get; }

        /// <summary>Puanlama ölçütleri.</summary>
        public IReadOnlyList<Olcut> Olcutler { get; }

        /// <summary>Kişilik çarpanı tanımı; yoksa <c>null</c> (çarpan 1).</summary>
        public KisilikCarpaniTanimi? KisilikCarpani { get; }

        /// <summary>Fikir çarpanı tanımı; yalnızca npc/oyuncu hedefli eylemde olabilir; yoksa <c>null</c>.</summary>
        public FikirCarpaniTanimi? FikirCarpani { get; }

        /// <summary>Ruh hali çarpanı tanımı; yoksa <c>null</c>.</summary>
        public RuhHaliCarpaniTanimi? RuhHaliCarpani { get; }

        /// <summary>Eylemin çeliştiği kişilik özellikleri, ordinal sıralı. 5. hafta: yalnızca okunur ve izde görünür, stres yazılmaz.</summary>
        public IReadOnlyList<string> CelisenOzellikler { get; }

        /// <summary>Eylemin etkileri (kural etkisi biçimi); seçilen eylem için 6. adımda uygulanır.</summary>
        public IReadOnlyList<KuralEtkisi> Etkiler { get; }

        /// <summary>Kaynak dosyadaki 1 tabanlı satır; bilinmiyorsa 0.</summary>
        public int Satir { get; }

        /// <summary>Hareket eylemi mi (<c>hedef: komsu_mekan</c>).</summary>
        public bool Hareket => Hedef == EylemHedefTuru.KomsuMekan;

        /// <summary>Olay ve karar izlerindeki <c>KuralId</c>: <c>eylem:&lt;Id&gt;</c>.</summary>
        public string KuralId => KuralIdOneki + Id;

        /// <summary>Eylem kaynaklı olayların <c>KuralId</c> öneki; kural kimlikleriyle çakışmaz (kimliklerde <c>:</c> geçersizdir).</summary>
        public const string KuralIdOneki = "eylem:";

        /// <summary>Eylem tanımı kurar.</summary>
        /// <exception cref="ArgumentException">Tutarsız tanım (kimlik/olay türü/etki/fikir çarpanı kuralları).</exception>
        public EylemTanimi(
            string id,
            string olayTuru,
            EylemHedefTuru hedef,
            decimal taban,
            IEnumerable<Kriter> onKosullar,
            IEnumerable<Olcut> olcutler,
            KisilikCarpaniTanimi? kisilikCarpani,
            FikirCarpaniTanimi? fikirCarpani,
            RuhHaliCarpaniTanimi? ruhHaliCarpani,
            IEnumerable<string> celisenOzellikler,
            IEnumerable<KuralEtkisi> etkiler,
            int satir = 0)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Eylem kimliği boş olamaz.", nameof(id));
            }

            if (taban <= 0m)
            {
                throw new ArgumentException("Taban 0'dan büyük olmalı.", nameof(taban));
            }

            var etkiListesi = new List<KuralEtkisi>(etkiler ?? throw new ArgumentNullException(nameof(etkiler)));
            if (hedef == EylemHedefTuru.KomsuMekan)
            {
                if (!string.IsNullOrEmpty(olayTuru))
                {
                    throw new ArgumentException("Hareket eyleminde olay türü yazılmaz.", nameof(olayTuru));
                }
            }
            else
            {
                if (string.IsNullOrEmpty(olayTuru))
                {
                    throw new ArgumentException("Eylemin olay türü boş olamaz.", nameof(olayTuru));
                }

                if (etkiListesi.Count == 0)
                {
                    throw new ArgumentException("Eylemin en az bir etkisi olmalı.", nameof(etkiler));
                }
            }

            if (fikirCarpani != null && hedef != EylemHedefTuru.Npc && hedef != EylemHedefTuru.Oyuncu)
            {
                throw new ArgumentException("Fikir çarpanı yalnızca npc/oyuncu hedefli eylemde tanımlanabilir.", nameof(fikirCarpani));
            }

            Id = id;
            OlayTuru = olayTuru ?? string.Empty;
            Hedef = hedef;
            Taban = taban;
            OnKosullar = new ReadOnlyCollection<Kriter>(new List<Kriter>(onKosullar ?? throw new ArgumentNullException(nameof(onKosullar))));
            Olcutler = new ReadOnlyCollection<Olcut>(new List<Olcut>(olcutler ?? throw new ArgumentNullException(nameof(olcutler))));
            KisilikCarpani = kisilikCarpani;
            FikirCarpani = fikirCarpani;
            RuhHaliCarpani = ruhHaliCarpani;
            var celisen = new List<string>(celisenOzellikler ?? throw new ArgumentNullException(nameof(celisenOzellikler)));
            celisen.Sort(string.CompareOrdinal);
            CelisenOzellikler = new ReadOnlyCollection<string>(celisen);
            Etkiler = new ReadOnlyCollection<KuralEtkisi>(etkiListesi);
            Satir = satir;
        }
    }

    /// <summary>Eylem dosyasının yükleme sonucu: hata yoksa eylemler (kimliğe göre ordinal sıralı), varsa yalnızca hatalar.</summary>
    public sealed class EylemYuklemeSonucu
    {
        /// <summary>Yüklenen eylemler; hata varsa boş.</summary>
        public IReadOnlyList<EylemTanimi> Eylemler { get; }

        /// <summary>Bulunan tüm hatalar, dosyadaki sırayla (<c>dosya:satir: eylem "id": mesaj</c>).</summary>
        public IReadOnlyList<KuralHatasi> Hatalar { get; }

        /// <summary>Uyarılar (yüklemeyi engellemez).</summary>
        public IReadOnlyList<KuralHatasi> Uyarilar { get; }

        /// <summary>Hata yok mu.</summary>
        public bool Basarili => Hatalar.Count == 0;

        /// <summary>Sonuç kurar.</summary>
        public EylemYuklemeSonucu(IEnumerable<EylemTanimi> eylemler, IEnumerable<KuralHatasi> hatalar, IEnumerable<KuralHatasi>? uyarilar = null)
        {
            Eylemler = new ReadOnlyCollection<EylemTanimi>(new List<EylemTanimi>(eylemler));
            Hatalar = new ReadOnlyCollection<KuralHatasi>(new List<KuralHatasi>(hatalar));
            Uyarilar = new ReadOnlyCollection<KuralHatasi>(new List<KuralHatasi>(uyarilar ?? new KuralHatasi[0]));
        }
    }
}
