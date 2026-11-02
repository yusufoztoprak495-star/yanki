using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Yanki.Core.Criteria;

namespace Yanki.Core.Rules
{
    /// <summary>Sonuç kuralının v0 etki türleri (plan 5.6). Liste bu hafta genişletilmez.</summary>
    public enum EtkiTuru
    {
        /// <summary>Süreli fikir modifier'ı ekler (<c>modifier_eklendi</c>).</summary>
        Modifier,

        /// <summary>Kalıcı ilişki ekseni değiştirir (<c>eksen_degisti</c>).</summary>
        Eksen,

        /// <summary>Ruh halini değiştirir (<c>duygu_degisti</c>).</summary>
        Duygu,

        /// <summary>Tetikleyici olayı hafızaya yazar (<c>hafiza_eklendi</c>).</summary>
        Hafiza,

        /// <summary>Dünya gerçeği ekler ya da kaldırır (<c>gercek_eklendi</c> / <c>gercek_kaldirildi</c>).</summary>
        Gercek,
    }

    /// <summary>
    /// Kuralın tek etkisi. Zihin etkileri kuralı değerlendiren NPC'ye (<c>ben</c>) uygulanır; <see cref="Hedef"/> ilişkinin
    /// karşı tarafıdır (rol adı ya da varlık kimliği). Alanların anlamı türe göre değişir; kullanılmayanlar boştur.
    /// </summary>
    public sealed class KuralEtkisi
    {
        /// <summary>Etki türü.</summary>
        public EtkiTuru Tur { get; }

        /// <summary>Modifier/eksen/duygu için hedef: <c>ben</c>, <c>oyuncu</c>, <c>olay.fail</c>, <c>olay.hedef</c> ya da varlık kimliği; diğerlerinde boş.</summary>
        public string Hedef { get; }

        /// <summary>Modifier türü, eksen adı ya da duygu türü; diğerlerinde boş.</summary>
        public string Ad { get; }

        /// <summary>Modifier değeri, eksen delta'sı, duygu şiddeti ya da hafıza önemi.</summary>
        public decimal Deger { get; }

        /// <summary>Modifier süresi (tick); diğerlerinde 0.</summary>
        public long Sure { get; }

        /// <summary>Gerçek metni; <c>{rol}</c> yer tutucuları içerebilir (<c>ben</c>, <c>oyuncu</c>, <c>olay.fail</c>, <c>olay.hedef</c>).</summary>
        public string GercekSablonu { get; }

        /// <summary>Gerçek etkisinde: true ise kaldırır, false ise ekler.</summary>
        public bool Kaldir { get; }

        /// <summary>Etkinin kaynak dosyadaki 1 tabanlı satırı; bilinmiyorsa 0.</summary>
        public int Satir { get; }

        private KuralEtkisi(EtkiTuru tur, string hedef, string ad, decimal deger, long sure, string gercek, bool kaldir, int satir)
        {
            Tur = tur;
            Hedef = hedef;
            Ad = ad;
            Deger = deger;
            Sure = sure;
            GercekSablonu = gercek;
            Kaldir = kaldir;
            Satir = satir;
        }

        /// <summary>Modifier etkisi.</summary>
        public static KuralEtkisi Modifier(string hedef, string tur, decimal deger, long sure, int satir = 0) =>
            new KuralEtkisi(EtkiTuru.Modifier, hedef, tur, deger, sure, string.Empty, false, satir);

        /// <summary>Eksen etkisi.</summary>
        public static KuralEtkisi Eksen(string hedef, string eksen, decimal delta, int satir = 0) =>
            new KuralEtkisi(EtkiTuru.Eksen, hedef, eksen, delta, 0, string.Empty, false, satir);

        /// <summary>Duygu etkisi; <paramref name="hedef"/> boş olabilir.</summary>
        public static KuralEtkisi Duygu(string tur, decimal siddet, string hedef, int satir = 0) =>
            new KuralEtkisi(EtkiTuru.Duygu, hedef, tur, siddet, 0, string.Empty, false, satir);

        /// <summary>Hafıza etkisi.</summary>
        public static KuralEtkisi Hafiza(decimal onem, int satir = 0) =>
            new KuralEtkisi(EtkiTuru.Hafiza, string.Empty, string.Empty, onem, 0, string.Empty, false, satir);

        /// <summary>Gerçek etkisi.</summary>
        public static KuralEtkisi Gercek(string sablon, bool kaldir, int satir = 0) =>
            new KuralEtkisi(EtkiTuru.Gercek, string.Empty, string.Empty, 0m, 0, sablon, kaldir, satir);
    }

    /// <summary>
    /// Sonuç kuralı (plan 5.6): <c>ne_zaman &lt;olay türü&gt;</c> + kriterler (5.9) → etkiler. Tüm kriterler tutarsa (bkz.
    /// <c>KuralMotoru</c>, ADR-010) etkiler yeni olaylar olarak kayda düşer.
    /// </summary>
    public sealed class KuralTanimi
    {
        /// <summary>Kural kimliği; üretilen olayların <c>KuralId</c>'sidir.</summary>
        public string Id { get; }

        /// <summary>Kuralı tetikleyen olay türü.</summary>
        public string NeZaman { get; }

        /// <summary>Kriterler; boş olabilir (olay türü yeter).</summary>
        public IReadOnlyList<Kriter> Kriterler { get; }

        /// <summary>Etkiler; en az bir tane.</summary>
        public IReadOnlyList<KuralEtkisi> Etkiler { get; }

        /// <summary>Kriter ve etkilerin başvurduğu roller, <see cref="Rol"/> sırasıyla tekrarsız; olayda tanımsız bir rol varsa kural o olay için atlanır.</summary>
        public IReadOnlyList<Rol> GerekliRoller { get; }

        /// <summary>Kaynak dosyadaki 1 tabanlı satır; bilinmiyorsa 0.</summary>
        public int Satir { get; }

        /// <summary>Kural kurar.</summary>
        /// <exception cref="ArgumentException">Kimlik ya da olay türü boşsa, etki yoksa.</exception>
        public KuralTanimi(
            string id, string neZaman, IEnumerable<Kriter> kriterler, IEnumerable<KuralEtkisi> etkiler, IEnumerable<Rol> gerekliRoller, int satir = 0)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Kural kimliği boş olamaz.", nameof(id));
            }

            if (string.IsNullOrEmpty(neZaman))
            {
                throw new ArgumentException("ne_zaman olay türü boş olamaz.", nameof(neZaman));
            }

            Id = id;
            NeZaman = neZaman;
            Kriterler = new ReadOnlyCollection<Kriter>(new List<Kriter>(kriterler ?? throw new ArgumentNullException(nameof(kriterler))));
            var e = new List<KuralEtkisi>(etkiler ?? throw new ArgumentNullException(nameof(etkiler)));
            if (e.Count == 0)
            {
                throw new ArgumentException("Kuralın en az bir etkisi olmalı.", nameof(etkiler));
            }

            Etkiler = new ReadOnlyCollection<KuralEtkisi>(e);
            var roller = new List<Rol>(gerekliRoller ?? throw new ArgumentNullException(nameof(gerekliRoller)));
            roller.Sort();
            GerekliRoller = new ReadOnlyCollection<Rol>(roller);
            Satir = satir;
        }
    }

    /// <summary>Kural dosyasındaki bir doğrulama hatası; kaynak dosya, satır ve kural kimliğini taşır.</summary>
    public sealed class KuralHatasi
    {
        /// <summary>Kaynak adı (dosya yolu).</summary>
        public string Kaynak { get; }

        /// <summary>1 tabanlı satır; bilinmiyorsa 0.</summary>
        public int Satir { get; }

        /// <summary>Hatanın ait olduğu kural; kural dışı hatalarda boş.</summary>
        public string KuralId { get; }

        /// <summary>Hata açıklaması.</summary>
        public string Mesaj { get; }

        /// <summary>Kimliğin türü: <c>kural</c> (varsayılan) ya da <c>eylem</c>.</summary>
        public string Etiket { get; }

        /// <summary>Hata kurar.</summary>
        public KuralHatasi(string kaynak, int satir, string kuralId, string mesaj, string etiket = "kural")
        {
            Kaynak = kaynak ?? string.Empty;
            Satir = satir;
            KuralId = kuralId ?? string.Empty;
            Mesaj = mesaj ?? string.Empty;
            Etiket = etiket ?? "kural";
        }

        /// <summary><c>dosya:satır: kural "id": mesaj</c> (eylemlerde <c>eylem "id"</c>).</summary>
        public override string ToString() =>
            Kaynak + ":" + Satir.ToString(System.Globalization.CultureInfo.InvariantCulture) + ": "
            + (KuralId.Length > 0 ? Etiket + " \"" + KuralId + "\": " : string.Empty) + Mesaj;
    }

    /// <summary>Kural dosyasının yükleme sonucu: hata yoksa kurallar (kimliğe göre ordinal sıralı), varsa yalnızca hatalar.</summary>
    public sealed class KuralYuklemeSonucu
    {
        /// <summary>Yüklenen kurallar; hata varsa boş.</summary>
        public IReadOnlyList<KuralTanimi> Kurallar { get; }

        /// <summary>Bulunan tüm hatalar, dosyadaki sırayla.</summary>
        public IReadOnlyList<KuralHatasi> Hatalar { get; }

        /// <summary>Uyarılar (yüklemeyi engellemez), dosyadaki sırayla; ör. sonucu etkilemeyen <c>agirlik</c> kullanımı.</summary>
        public IReadOnlyList<KuralHatasi> Uyarilar { get; }

        /// <summary>Hata yok mu.</summary>
        public bool Basarili => Hatalar.Count == 0;

        /// <summary>Sonuç kurar.</summary>
        public KuralYuklemeSonucu(IEnumerable<KuralTanimi> kurallar, IEnumerable<KuralHatasi> hatalar, IEnumerable<KuralHatasi>? uyarilar = null)
        {
            Kurallar = new ReadOnlyCollection<KuralTanimi>(new List<KuralTanimi>(kurallar));
            Hatalar = new ReadOnlyCollection<KuralHatasi>(new List<KuralHatasi>(hatalar));
            Uyarilar = new ReadOnlyCollection<KuralHatasi>(new List<KuralHatasi>(uyarilar ?? new KuralHatasi[0]));
        }
    }
}
