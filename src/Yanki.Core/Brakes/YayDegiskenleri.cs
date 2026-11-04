using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Yanki.Core.Events;
using Yanki.Core.World;

namespace Yanki.Core.Brakes
{
    /// <summary>Senaryodaki bir yay değişkeninin tanımı (<c>yay_degiskenleri</c>): başlangıç değeri ve [min, max] aralığı.</summary>
    public sealed class YayTanimi
    {
        /// <summary>Değişken adı (kimlik).</summary>
        public string Ad { get; }

        /// <summary>Başlangıç değeri.</summary>
        public decimal Baslangic { get; }

        /// <summary>Alt sınır.</summary>
        public decimal Min { get; }

        /// <summary>Üst sınır.</summary>
        public decimal Max { get; }

        /// <summary>Kurar; <c>min ≤ başlangıç ≤ max</c> olmalıdır.</summary>
        public YayTanimi(string ad, decimal baslangic, decimal min, decimal max)
        {
            if (!Kimlik.GecerliKimlikMi(ad))
            {
                throw new ArgumentException($"Yay değişkeni adı geçersiz kimlik: \"{ad}\".", nameof(ad));
            }

            if (min > max || baslangic < min || baslangic > max)
            {
                throw new ArgumentException($"Yay değişkeni \"{ad}\": min ≤ baslangic ≤ max olmalı.");
            }

            Ad = ad;
            Baslangic = baslangic;
            Min = min;
            Max = max;
        }
    }

    /// <summary>
    /// Yay değişkenleri deposu (7. hafta, ADR-013): dramatik yayı yönlendiren sayısal senaryo değişkenleri. Değer <c>yay_degisti</c>
    /// olaylarından türer (son olayın <c>deger</c> yükü); başlangıç değeri senaryodandır. <c>yay_degisti</c> algılanmaz ve kural tetiklemez
    /// (sarmal açmasın): bir zihin olayı değildir ve hiçbir tetik kuyruğuna alınmaz. Değişken yoksa kanonik metne satır girmez.
    /// </summary>
    public sealed class YayDegiskenleri
    {
        /// <summary>Olay türü.</summary>
        public const string OlayTuru = "yay_degisti";

        /// <summary>Hiç değişken yok.</summary>
        public static YayDegiskenleri Yok { get; } = new YayDegiskenleri(new YayTanimi[0]);

        private readonly SortedDictionary<string, YayTanimi> _tanimlar = new SortedDictionary<string, YayTanimi>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, decimal> _degerler = new SortedDictionary<string, decimal>(StringComparer.Ordinal);

        /// <summary>Kurar; ad tekrar edemez.</summary>
        public YayDegiskenleri(IEnumerable<YayTanimi> tanimlar)
        {
            foreach (var t in tanimlar ?? throw new ArgumentNullException(nameof(tanimlar)))
            {
                if (_tanimlar.ContainsKey(t.Ad))
                {
                    throw new ArgumentException($"Yay değişkeni tekrar ediyor: \"{t.Ad}\".", nameof(tanimlar));
                }

                _tanimlar.Add(t.Ad, t);
                _degerler.Add(t.Ad, t.Baslangic);
            }
        }

        /// <summary>Tanımlı değişkenler, ad sırasıyla.</summary>
        public IEnumerable<YayTanimi> Tanimlar => _tanimlar.Values;

        /// <summary>Değişken tanımlı mı.</summary>
        public bool Var(string ad) => ad != null && _tanimlar.ContainsKey(ad);

        /// <summary>Hiç değişken tanımlı değil mi.</summary>
        public bool Bos => _tanimlar.Count == 0;

        /// <summary>Değişkenin şimdiki değeri.</summary>
        /// <exception cref="ArgumentException">Değişken tanımlı değilse.</exception>
        public decimal Deger(string ad) =>
            ad != null && _degerler.TryGetValue(ad, out var d) ? d : throw new ArgumentException($"Yay değişkeni tanımlı değil: \"{ad}\".", nameof(ad));

        /// <summary>
        /// <paramref name="delta"/> uygulanınca değişkenin yeni değeri: aralığa kırpılır. <paramref name="kirpildi"/> kırpma olduysa doğrudur
        /// (<c>yay_siniri</c> freni).
        /// </summary>
        public decimal Sinirla(string ad, decimal delta, out bool kirpildi)
        {
            var t = _tanimlar[ad];
            var ham = _degerler[ad] + delta;
            var yeni = Math.Max(t.Min, Math.Min(t.Max, ham));
            kirpildi = yeni != ham;
            return yeni;
        }

        /// <summary>Günlüğe eklenen her olay bununla işlenir; yalnızca <c>yay_degisti</c> değeri değiştirir.</summary>
        public void Uygula(Olay olay)
        {
            if (olay.Tur != OlayTuru)
            {
                return;
            }

            var ad = olay.Deger("degisken");
            if (!_tanimlar.ContainsKey(ad))
            {
                throw new ArgumentException($"Olay #{olay.Id}: yay değişkeni tanımlı değil: \"{ad}\".");
            }

            _degerler[ad] = decimal.Parse(olay.Deger("deger"), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        }

        /// <summary><c>yay_degisti</c> olayı: yük <c>degisken</c>, <c>delta</c> (istenen), <c>deger</c> (kırpılmış yeni değer).</summary>
        public static Olay OlayYap(long id, long tick, int sira, string ad, decimal delta, decimal yeniDeger, IEnumerable<long> nedenler, string kuralId, string mekan, string? ben = null)
        {
            // `ben`: yalnızca cooldown/en_fazla/tick_basina_en_fazla tanımlı kuralın yay etkisinde yazılır; sayaç (kural, NPC) çiftini günlükten türetebilsin diye
            // (gercek olayındaki `ben` ile aynı kural; fren alanı olmayan kuralların olayları ve hash'leri değişmez).
            var yuk = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("degisken", ad),
                new KeyValuePair<string, string>("delta", KanonikYazici.Sayi(delta)),
                new KeyValuePair<string, string>("deger", KanonikYazici.Sayi(yeniDeger)),
            };
            if (!string.IsNullOrEmpty(ben))
            {
                yuk.Add(new KeyValuePair<string, string>("ben", ben!));
            }

            return new Olay(id, tick, sira, OlayTuru, string.Empty, string.Empty, mekan, yuk, nedenler, kuralId);
        }

        /// <summary>Kanonik metin: tanımlı değişken yoksa boş; varsa ad sırasıyla <c>YAY|ad|deger</c> satırları.</summary>
        public string KanonikMetin()
        {
            var sb = new StringBuilder();
            foreach (var kv in _degerler)
            {
                sb.Append("YAY|").Append(kv.Key).Append('|').Append(KanonikYazici.Sayi(kv.Value)).Append('\n');
            }

            return sb.ToString();
        }
    }
}
