using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Yanki.Core.Criteria
{
    /// <summary>Bir sorgu fonksiyonunun döndürdüğü değerin türü.</summary>
    public enum DonusTuru
    {
        /// <summary>Sayı (<see cref="decimal"/>).</summary>
        Sayi,

        /// <summary>Doğru/yanlış.</summary>
        Mantiksal,

        /// <summary>Varlık kimliği.</summary>
        Varlik,

        /// <summary>Fikir bandı: <c>dusman</c>, <c>soguk</c>, <c>notr</c>, <c>yakin</c>, <c>dost</c>.</summary>
        Bant,
    }

    /// <summary>Sözlükteki bir sorgu fonksiyonunun tanımı.</summary>
    public sealed class FonksiyonTanimi
    {
        /// <summary>ASCII fonksiyon adı.</summary>
        public string Ad { get; }

        /// <summary>Argüman sayısı.</summary>
        public int ArgumanSayisi { get; }

        /// <summary>Dönüş türü.</summary>
        public DonusTuru Donus { get; }

        /// <summary>Önerme alan argümanın indeksi; önerme almıyorsa -1.</summary>
        public int OnermeIndeksi { get; }

        internal FonksiyonTanimi(string ad, int argumanSayisi, DonusTuru donus, int onermeIndeksi = -1)
        {
            Ad = ad;
            ArgumanSayisi = argumanSayisi;
            Donus = donus;
            OnermeIndeksi = onermeIndeksi;
        }
    }

    /// <summary>
    /// Kriter dilinin sabit sorgu fonksiyonu sözlüğü (v0.1, 15 fonksiyon). Kaynak: <c>docs/criteria-language.md</c>;
    /// sözlük Yusuf'un tasarım alanıdır, değişiklik onun onayıyla yapılır.
    /// </summary>
    public static class SorguSozlugu
    {
        private static readonly FonksiyonTanimi[] Liste =
        {
            new FonksiyonTanimi("ayni_mekanda", 1, DonusTuru.Mantiksal),
            new FonksiyonTanimi("duygu", 1, DonusTuru.Sayi),
            new FonksiyonTanimi("duygu_hedefi", 0, DonusTuru.Varlik),
            new FonksiyonTanimi("eksen", 2, DonusTuru.Sayi),
            new FonksiyonTanimi("fikir", 1, DonusTuru.Sayi),
            new FonksiyonTanimi("fikir_bandi", 1, DonusTuru.Bant),
            new FonksiyonTanimi("gercek", 1, DonusTuru.Mantiksal, 0),
            new FonksiyonTanimi("hafiza_var", 3, DonusTuru.Mantiksal),
            new FonksiyonTanimi("inanc", 1, DonusTuru.Sayi, 0),
            new FonksiyonTanimi("kaynak", 2, DonusTuru.Mantiksal, 0),
            new FonksiyonTanimi("kisilik", 1, DonusTuru.Sayi),
            new FonksiyonTanimi("modifier_var", 2, DonusTuru.Mantiksal),
            new FonksiyonTanimi("stres", 0, DonusTuru.Sayi),
            new FonksiyonTanimi("yalan_soyledi", 1, DonusTuru.Mantiksal),
            new FonksiyonTanimi("yay", 1, DonusTuru.Sayi),
        };

        /// <summary>Geçerli bant değerleri.</summary>
        public static readonly IReadOnlyList<string> Bantlar =
            new ReadOnlyCollection<string>(new[] { "dusman", "soguk", "notr", "yakin", "dost" });

        /// <summary>Tüm fonksiyonlar, ada göre ordinal sıralı.</summary>
        public static IReadOnlyList<FonksiyonTanimi> Hepsi { get; } = new ReadOnlyCollection<FonksiyonTanimi>(Liste);

        /// <summary>Adı verilen fonksiyonun tanımı; yoksa <c>null</c>.</summary>
        public static FonksiyonTanimi? Bul(string ad)
        {
            foreach (var t in Liste)
            {
                if (string.Equals(t.Ad, ad, StringComparison.Ordinal))
                {
                    return t;
                }
            }

            return null;
        }

        /// <summary>
        /// Çağrıyı sözlüğe göre doğrular: bilinen fonksiyon, doğru argüman sayısı, önerme alan yerde önerme
        /// (başka yerde önerme ve joker yok), önerme içinde yalnızca tanımlayıcı/sayı/joker/rol.
        /// </summary>
        /// <exception cref="KriterHatasi">Geçersizse; hata ilgili çağrının ya da argümanın konumunu taşır.</exception>
        public static void Dogrula(SorguCagrisi cagri)
        {
            if (cagri == null)
            {
                throw new ArgumentNullException(nameof(cagri));
            }

            if (cagri.TekDegerli)
            {
                throw new KriterHatasi($"'{cagri.Ad}!' bir sorgu fonksiyonu olamaz: '!' yalnızca önermelerde (tek değerli yüklem) kullanılır", cagri.Konum);
            }

            var tanim = Bul(cagri.Ad);
            if (tanim == null)
            {
                throw new KriterHatasi($"Bilinmeyen fonksiyon: '{cagri.Ad}'", cagri.Konum);
            }

            if (cagri.Argumanlar.Count != tanim.ArgumanSayisi)
            {
                throw new KriterHatasi(
                    $"'{tanim.Ad}' {tanim.ArgumanSayisi} argüman alır, {cagri.Argumanlar.Count} verildi",
                    cagri.Konum);
            }

            for (var i = 0; i < cagri.Argumanlar.Count; i++)
            {
                var arg = cagri.Argumanlar[i];
                if (i == tanim.OnermeIndeksi)
                {
                    if (!(arg is OnermeArgumani onerme))
                    {
                        throw new KriterHatasi($"'{tanim.Ad}' fonksiyonunun {i + 1}. argümanı bir önerme olmalı (ör. yuklem(a, b))", arg.Konum);
                    }

                    var icerik = onerme.Cagri;
                    if (string.Equals(icerik.Ad, "degil", StringComparison.Ordinal) && !icerik.TekDegerli)
                    {
                        // Olumsuz önerme yalnızca zihin sorgularında (inanc, kaynak): degil(yuklem(...)) tek bir önermeyi sarar.
                        if (tanim.Ad != "inanc" && tanim.Ad != "kaynak")
                        {
                            throw new KriterHatasi($"'degil(...)' yalnızca 'inanc' ve 'kaynak' içinde kullanılabilir, '{tanim.Ad}' içinde değil", icerik.Konum);
                        }

                        if (icerik.Argumanlar.Count != 1 || !(icerik.Argumanlar[0] is OnermeArgumani sarilan))
                        {
                            throw new KriterHatasi("'degil' tek bir önerme sarmalıdır (ör. degil(konum!(Ali, han)))", icerik.Konum);
                        }

                        OnermeDogrula(sarilan.Cagri);
                    }
                    else
                    {
                        OnermeDogrula(icerik);
                    }
                }
                else if (arg is OnermeArgumani)
                {
                    throw new KriterHatasi($"'{tanim.Ad}' fonksiyonunun {i + 1}. argümanı önerme olamaz", arg.Konum);
                }
                else if (arg is JokerArgumani)
                {
                    throw new KriterHatasi("Joker (*) yalnızca önerme içinde kullanılabilir", arg.Konum);
                }
            }
        }

        private static void OnermeDogrula(SorguCagrisi onerme)
        {
            if (onerme.Argumanlar.Count == 0)
            {
                throw new KriterHatasi($"'{onerme.Ad}' önermesi en az bir argüman almalı", onerme.Konum);
            }

            foreach (var arg in onerme.Argumanlar)
            {
                if (arg is OnermeArgumani)
                {
                    throw new KriterHatasi("Önermeler iç içe olamaz", arg.Konum);
                }
            }
        }
    }
}
