using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Yanki.Core.Mind;
using Yanki.Core.Rules;

namespace Yanki.Core.Simulation
{
    /// <summary>Senaryodaki bir NPC: kimlik, başlangıç mekanı ve kişilik.</summary>
    public sealed class NpcTanimi
    {
        /// <summary>NPC kimliği (ASCII).</summary>
        public string Id { get; }

        /// <summary>Başlangıç mekanı.</summary>
        public string Konum { get; }

        /// <summary>Kişilik.</summary>
        public Kisilik Kisilik { get; }

        /// <summary>NPC tanımı kurar.</summary>
        public NpcTanimi(string id, string konum, Kisilik kisilik)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Konum = konum ?? throw new ArgumentNullException(nameof(konum));
            Kisilik = kisilik ?? throw new ArgumentNullException(nameof(kisilik));
        }
    }

    /// <summary>
    /// Başlangıç dünyası: mekanlar ve bağlantıları, oyuncu, NPC'ler, başlangıç gerçekleri, oyuncu eylem türleri ve kural dosyaları.
    /// Dosya okuma çekirdeğin işi değildir: <see cref="KuralDosyalari"/> ve <see cref="EylemDosyalari"/> yalnızca adlardır, okuyucu (CLI) çözer.
    /// </summary>
    public sealed class SenaryoTanimi
    {
        /// <summary>Senaryonun adı.</summary>
        public string Ad { get; }

        /// <summary>Mekan kimlikleri, dosyadaki sırayla.</summary>
        public IReadOnlyList<string> Mekanlar { get; }

        /// <summary>Mekan bağlantıları (yönsüz çiftler).</summary>
        public IReadOnlyList<KeyValuePair<string, string>> Baglantilar { get; }

        /// <summary>Oyuncu varlığının kimliği.</summary>
        public string OyuncuId { get; }

        /// <summary>Oyuncunun başlangıç mekanı.</summary>
        public string OyuncuKonumu { get; }

        /// <summary>NPC'ler, kimliğe göre ordinal sıralı.</summary>
        public IReadOnlyList<NpcTanimi> Npcler { get; }

        /// <summary>Başlangıç gerçekleri (kanonik metin), dosyadaki sırayla.</summary>
        public IReadOnlyList<string> Gercekler { get; }

        /// <summary>Oyuncunun gönderebileceği eylem (olay) türleri, ordinal sıralı.</summary>
        public IReadOnlyList<string> OlayTurleri { get; }

        /// <summary>Kural dosyalarının adları (senaryo dosyasına göre göreli).</summary>
        public IReadOnlyList<string> KuralDosyalari { get; }

        /// <summary>
        /// NPC eylem dosyalarının adları (senaryo dosyasına göre göreli). Bu liste boşsa NPC karar adımı hiçbir olay üretmez (ADR-011).
        /// </summary>
        public IReadOnlyList<string> EylemDosyalari { get; }

        /// <summary>Senaryo kurar. Tutarlılık denetimi <c>SenaryoYukleyici</c>'dadır.</summary>
        public SenaryoTanimi(
            string ad,
            IEnumerable<string> mekanlar,
            IEnumerable<KeyValuePair<string, string>> baglantilar,
            string oyuncuId,
            string oyuncuKonumu,
            IEnumerable<NpcTanimi> npcler,
            IEnumerable<string> gercekler,
            IEnumerable<string> olayTurleri,
            IEnumerable<string> kuralDosyalari,
            IEnumerable<string>? eylemDosyalari = null)
        {
            Ad = ad ?? string.Empty;
            Mekanlar = new ReadOnlyCollection<string>(new List<string>(mekanlar));
            Baglantilar = new ReadOnlyCollection<KeyValuePair<string, string>>(new List<KeyValuePair<string, string>>(baglantilar));
            OyuncuId = oyuncuId;
            OyuncuKonumu = oyuncuKonumu;
            var n = new List<NpcTanimi>(npcler);
            n.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            Npcler = new ReadOnlyCollection<NpcTanimi>(n);
            Gercekler = new ReadOnlyCollection<string>(new List<string>(gercekler));
            var t = new List<string>(olayTurleri);
            t.Sort(string.CompareOrdinal);
            OlayTurleri = new ReadOnlyCollection<string>(t);
            KuralDosyalari = new ReadOnlyCollection<string>(new List<string>(kuralDosyalari));
            EylemDosyalari = new ReadOnlyCollection<string>(new List<string>(eylemDosyalari ?? new string[0]));
        }
    }

    /// <summary>Senaryo dosyasının yükleme sonucu.</summary>
    public sealed class SenaryoYuklemeSonucu
    {
        /// <summary>Senaryo; hata varsa <c>null</c>.</summary>
        public SenaryoTanimi? Senaryo { get; }

        /// <summary>Hatalar, dosyadaki sırayla.</summary>
        public IReadOnlyList<KuralHatasi> Hatalar { get; }

        /// <summary>Hata yok mu.</summary>
        public bool Basarili => Hatalar.Count == 0 && Senaryo != null;

        /// <summary>Sonuç kurar.</summary>
        public SenaryoYuklemeSonucu(SenaryoTanimi? senaryo, IEnumerable<KuralHatasi> hatalar)
        {
            Senaryo = senaryo;
            Hatalar = new ReadOnlyCollection<KuralHatasi>(new List<KuralHatasi>(hatalar));
        }
    }

    /// <summary>
    /// Oyuncu eylemi (plan 5.8 <c>PlayerAction</c>): senaryonun tanımladığı bir tür, isteğe bağlı hedef ve yük.
    /// Yükte <c>gorunurluk</c> (<c>acik</c>, <c>sessiz</c>, <c>gizli</c>) olabilir; yoksa <c>acik</c>.
    /// </summary>
    public sealed class PlayerAction
    {
        /// <summary>Eylem (olay) türü.</summary>
        public string Tur { get; }

        /// <summary>Eylemin yöneldiği varlık; yoksa boş.</summary>
        public string Hedef { get; }

        /// <summary>Anahtar-değer yükü, anahtara göre ordinal sıralı.</summary>
        public IReadOnlyList<KeyValuePair<string, string>> Yuk { get; }

        /// <summary>Eylem kurar.</summary>
        /// <exception cref="ArgumentException">Tür boşsa ya da yük anahtarı tekrar ediyorsa.</exception>
        public PlayerAction(string tur, string hedef = "", IEnumerable<KeyValuePair<string, string>>? yuk = null)
        {
            if (string.IsNullOrEmpty(tur))
            {
                throw new ArgumentException("Eylem türü boş olamaz.", nameof(tur));
            }

            Tur = tur;
            Hedef = hedef ?? string.Empty;
            var liste = new List<KeyValuePair<string, string>>(yuk ?? new KeyValuePair<string, string>[0]);
            liste.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            for (var i = 1; i < liste.Count; i++)
            {
                if (string.Equals(liste[i - 1].Key, liste[i].Key, StringComparison.Ordinal))
                {
                    throw new ArgumentException($"Yükte \"{liste[i].Key}\" anahtarı birden fazla kez geçiyor.", nameof(yuk));
                }
            }

            Yuk = new ReadOnlyCollection<KeyValuePair<string, string>>(liste);
        }
    }

    /// <summary>Günlüğe işlenen oyuncu eylemi: hangi tick'te işlendiği ve eylemin kendisi. Kayıt ve golden replay dosyasının satırıdır.</summary>
    public sealed class EylemKaydi
    {
        /// <summary>Eylemin olaya dönüştüğü tick.</summary>
        public long Tick { get; }

        /// <summary>Eylem.</summary>
        public PlayerAction Eylem { get; }

        /// <summary>Kayıt kurar.</summary>
        public EylemKaydi(long tick, PlayerAction eylem)
        {
            Tick = tick;
            Eylem = eylem ?? throw new ArgumentNullException(nameof(eylem));
        }
    }
}
