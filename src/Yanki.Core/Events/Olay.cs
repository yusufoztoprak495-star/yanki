using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Yanki.Core.Events
{
    /// <summary>
    /// Olay günlüğündeki değişmez kayıt (plan 5.1). Bir olay "ne oldu"yu, "kim yaptı"yı ve
    /// "hangi olaylar buna yol açtı"yı (<see cref="NedenOlaylar"/>) taşır.
    /// </summary>
    public sealed class Olay
    {
        /// <summary>Olayın günlükteki sıra numarası; 1'den başlar.</summary>
        public long Id { get; }

        /// <summary>Olayın gerçekleştiği simülasyon tick'i.</summary>
        public long Tick { get; }

        /// <summary>Olayın tick içindeki sırası; her tick'te 0'dan başlar.</summary>
        public int Sira { get; }

        /// <summary>Olay türü (ör. <c>gercek_eklendi</c>). Boş olamaz.</summary>
        public string Tur { get; }

        /// <summary>Eylemi yapan varlığın kimliği; yoksa boş metin.</summary>
        public string Fail { get; }

        /// <summary>Eylemin yöneldiği varlığın kimliği; yoksa boş metin.</summary>
        public string Hedef { get; }

        /// <summary>Olayın gerçekleştiği mekanın kimliği; yoksa boş metin.</summary>
        public string Mekan { get; }

        /// <summary>Olaya özgü anahtar-değer yükü; anahtara göre ordinal sıralıdır, anahtarlar tekildir.</summary>
        public IReadOnlyList<KeyValuePair<string, string>> Yuk { get; }

        /// <summary>Bu olaya yol açan olayların kimlikleri; artan sıralıdır, tekrar içermez.</summary>
        public IReadOnlyList<long> NedenOlaylar { get; }

        /// <summary>Olayı üreten kuralın ya da eylem tanımının kimliği; oyuncu eylemlerinde <c>player</c>.</summary>
        public string KuralId { get; }

        /// <summary>
        /// Yeni bir olay kurar. Girdi koleksiyonları kopyalanır; sonradan değişmeleri olayı etkilemez.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="tur"/> ya da <paramref name="kuralId"/> boşsa; yük anahtarları ya da neden kimlikleri tekrar ediyorsa.</exception>
        public Olay(
            long id,
            long tick,
            int sira,
            string tur,
            string fail,
            string hedef,
            string mekan,
            IEnumerable<KeyValuePair<string, string>> yuk,
            IEnumerable<long> nedenOlaylar,
            string kuralId)
        {
            if (string.IsNullOrEmpty(tur))
            {
                throw new ArgumentException("Olay türü (Tur) boş olamaz.", nameof(tur));
            }

            if (string.IsNullOrEmpty(kuralId))
            {
                throw new ArgumentException("KuralId boş olamaz (oyuncu eylemleri için \"player\" kullanılır).", nameof(kuralId));
            }

            Id = id;
            Tick = tick;
            Sira = sira;
            Tur = tur;
            Fail = fail ?? string.Empty;
            Hedef = hedef ?? string.Empty;
            Mekan = mekan ?? string.Empty;
            KuralId = kuralId;
            Yuk = YukuSirala(yuk);
            NedenOlaylar = NedenleriSirala(nedenOlaylar);
        }

        /// <summary>Verilen yük anahtarının değerini döndürür.</summary>
        /// <exception cref="InvalidOperationException">Anahtar yükte yoksa.</exception>
        public string Deger(string yukAnahtari)
        {
            foreach (var oge in Yuk)
            {
                if (string.Equals(oge.Key, yukAnahtari, StringComparison.Ordinal))
                {
                    return oge.Value;
                }
            }

            throw new InvalidOperationException($"Olay #{Id} ({Tur}) yükünde \"{yukAnahtari}\" anahtarı yok.");
        }

        private static IReadOnlyList<KeyValuePair<string, string>> YukuSirala(IEnumerable<KeyValuePair<string, string>> yuk)
        {
            var dizi = new List<KeyValuePair<string, string>>(yuk ?? throw new ArgumentNullException(nameof(yuk)));
            dizi.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));

            for (var i = 0; i < dizi.Count; i++)
            {
                if (dizi[i].Key == null || dizi[i].Value == null)
                {
                    throw new ArgumentException("Yük anahtarı ve değeri null olamaz.", nameof(yuk));
                }

                if (i > 0 && string.Equals(dizi[i - 1].Key, dizi[i].Key, StringComparison.Ordinal))
                {
                    throw new ArgumentException($"Yükte \"{dizi[i].Key}\" anahtarı birden fazla kez geçiyor.", nameof(yuk));
                }
            }

            return new ReadOnlyCollection<KeyValuePair<string, string>>(dizi);
        }

        private static IReadOnlyList<long> NedenleriSirala(IEnumerable<long> nedenler)
        {
            var dizi = new List<long>(nedenler ?? throw new ArgumentNullException(nameof(nedenler)));
            dizi.Sort();

            for (var i = 1; i < dizi.Count; i++)
            {
                if (dizi[i - 1] == dizi[i])
                {
                    throw new ArgumentException($"NedenOlaylar içinde #{dizi[i]} birden fazla kez geçiyor.", nameof(nedenler));
                }
            }

            return new ReadOnlyCollection<long>(dizi);
        }
    }
}
