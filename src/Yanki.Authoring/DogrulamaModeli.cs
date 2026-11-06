using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Yanki.Authoring
{
    /// <summary>Doğrulamaya verilen bir dosya: senaryodaki adı, hata mesajlarında görünen adı ve metni.</summary>
    public sealed class DosyaGirdisi
    {
        /// <summary>Senaryo dosyasındaki liste girdisi (ör. <c>kurallar.json</c>).</summary>
        public string Ad { get; }

        /// <summary>Hata mesajlarında görünen ad (ör. dizinle birleşmiş yol).</summary>
        public string GorunenAd { get; }

        /// <summary>Dosya metni.</summary>
        public string Metin { get; }

        /// <summary>Dosya girdisi kurar.</summary>
        public DosyaGirdisi(string ad, string gorunenAd, string metin)
        {
            Ad = ad ?? throw new ArgumentNullException(nameof(ad));
            GorunenAd = gorunenAd ?? throw new ArgumentNullException(nameof(gorunenAd));
            Metin = metin ?? throw new ArgumentNullException(nameof(metin));
        }
    }

    /// <summary>
    /// <c>yanki validate</c> girdisi: yalnızca metin ve dosya adı. Dosya sistemini CLI (ve ileride demo) okur; bu derleme dokunmaz.
    /// <c>dosyalar</c> senaryonun <c>kural_dosyalari</c>, <c>eylem_dosyalari</c> ve <c>kriz_dosyalari</c> listelerindeki dosyalardır (bulunanlar).
    /// </summary>
    public sealed class DogrulamaGirdisi
    {
        /// <summary>Senaryo dosyasının görünen adı.</summary>
        public string SenaryoAdi { get; }

        /// <summary>Senaryo metni.</summary>
        public string SenaryoMetni { get; }

        /// <summary>Senaryoya bağlı dosyalar (okunabilenler).</summary>
        public IReadOnlyList<DosyaGirdisi> Dosyalar { get; }

        /// <summary>İsteğe bağlı şablon dosyası.</summary>
        public DosyaGirdisi? Sablon { get; }

        /// <summary>İsteğe bağlı oyuncu eylem menüsü (<c>oyuncu_eylemleri.json</c>).</summary>
        public DosyaGirdisi? Menu { get; }

        /// <summary>Girdi kurar.</summary>
        public DogrulamaGirdisi(string senaryoAdi, string senaryoMetni, IEnumerable<DosyaGirdisi> dosyalar, DosyaGirdisi? sablon, DosyaGirdisi? menu)
        {
            SenaryoAdi = senaryoAdi ?? throw new ArgumentNullException(nameof(senaryoAdi));
            SenaryoMetni = senaryoMetni ?? throw new ArgumentNullException(nameof(senaryoMetni));
            Dosyalar = new ReadOnlyCollection<DosyaGirdisi>(new List<DosyaGirdisi>(dosyalar ?? throw new ArgumentNullException(nameof(dosyalar))));
            Sablon = sablon;
            Menu = menu;
        }
    }

    /// <summary>Bir hata ya da uyarı: <c>dosya:satir: tur "id": mesaj</c>.</summary>
    public sealed class Bulgu
    {
        /// <summary>Görünen dosya adı.</summary>
        public string Dosya { get; }

        /// <summary>1 tabanlı satır; bilinmiyorsa 0.</summary>
        public int Satir { get; }

        /// <summary>Hata mı (değilse uyarı).</summary>
        public bool Hata { get; }

        /// <summary>Kimliğin türü: <c>kural</c>, <c>eylem</c>, <c>kriz</c>, <c>senaryo</c>, <c>sablon</c>, <c>kisilik</c>, <c>korumali</c>, <c>menu</c>.</summary>
        public string Tur { get; }

        /// <summary>İlgili kimlik (kural, eylem, şablon, özellik, varlık); yoksa boş.</summary>
        public string Kimlik { get; }

        /// <summary>Açıklama.</summary>
        public string Mesaj { get; }

        /// <summary>Bulgu kurar.</summary>
        public Bulgu(string dosya, int satir, bool hata, string tur, string kimlik, string mesaj)
        {
            Dosya = dosya ?? string.Empty;
            Satir = satir;
            Hata = hata;
            Tur = tur ?? string.Empty;
            Kimlik = kimlik ?? string.Empty;
            Mesaj = mesaj ?? string.Empty;
        }

        /// <summary><c>dosya:satir: tur "id": mesaj</c> (uyarılarda başına <c>uyarı </c> eklenir).</summary>
        public override string ToString() =>
            Dosya + ":" + Satir.ToString(CultureInfo.InvariantCulture) + ": " + (Hata ? string.Empty : "uyarı ")
            + (Kimlik.Length > 0 ? Tur + " \"" + Kimlik + "\": " : string.Empty) + Mesaj;
    }

    /// <summary>Doğrulama sonucu: bütün bulgular, dosya ve satır sırasıyla.</summary>
    public sealed class DogrulamaSonucu
    {
        /// <summary>Bulgular (önce dosya adı ordinal, sonra satır, sonra hata önce, sonra tür, kimlik, mesaj).</summary>
        public IReadOnlyList<Bulgu> Bulgular { get; }

        /// <summary>Hata sayısı.</summary>
        public int HataSayisi { get; }

        /// <summary>Uyarı sayısı.</summary>
        public int UyariSayisi { get; }

        /// <summary>Sonuç kurar ve bulguları sıralar.</summary>
        public DogrulamaSonucu(IEnumerable<Bulgu> bulgular)
        {
            var liste = new List<Bulgu>(bulgular);
            liste.Sort(Karsilastir);
            Bulgular = new ReadOnlyCollection<Bulgu>(liste);
            foreach (var b in liste)
            {
                if (b.Hata)
                {
                    HataSayisi++;
                }
                else
                {
                    UyariSayisi++;
                }
            }
        }

        private static int Karsilastir(Bulgu a, Bulgu b)
        {
            var c = string.CompareOrdinal(a.Dosya, b.Dosya);
            if (c != 0)
            {
                return c;
            }

            c = a.Satir.CompareTo(b.Satir);
            if (c != 0)
            {
                return c;
            }

            c = b.Hata.CompareTo(a.Hata);
            if (c != 0)
            {
                return c;
            }

            c = string.CompareOrdinal(a.Tur, b.Tur);
            if (c != 0)
            {
                return c;
            }

            c = string.CompareOrdinal(a.Kimlik, b.Kimlik);
            return c != 0 ? c : string.CompareOrdinal(a.Mesaj, b.Mesaj);
        }
    }
}
