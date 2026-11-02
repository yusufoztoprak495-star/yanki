using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Yanki.Core.Json
{
    /// <summary>JSON değerinin türü.</summary>
    public enum JsonTuru
    {
        /// <summary><c>null</c></summary>
        Bos,

        /// <summary><c>true</c> / <c>false</c></summary>
        Mantiksal,

        /// <summary>Sayı; her zaman <see cref="decimal"/> (ADR-007).</summary>
        Sayi,

        /// <summary>Metin.</summary>
        Metin,

        /// <summary>Dizi.</summary>
        Dizi,

        /// <summary>Nesne; özellikler dosyadaki sırayla tutulur.</summary>
        Nesne,
    }

    /// <summary>
    /// Ayrıştırılmış JSON ağacının değişmez düğümü. Okuyucu (<see cref="JsonAyristirici"/>) kaynak konumunu
    /// (<see cref="Satir"/>, <see cref="Sutun"/>) doldurur; doğrulayıcılar hata mesajlarında bunu kullanır.
    /// Programla kurulan düğümlerin konumu 0'dır.
    /// </summary>
    public sealed class JsonDegeri
    {
        private static readonly IReadOnlyList<JsonDegeri> BosElemanlar = new ReadOnlyCollection<JsonDegeri>(new List<JsonDegeri>());

        private static readonly IReadOnlyList<KeyValuePair<string, JsonDegeri>> BosOzellikler =
            new ReadOnlyCollection<KeyValuePair<string, JsonDegeri>>(new List<KeyValuePair<string, JsonDegeri>>());

        private readonly bool _mantiksal;
        private readonly decimal _sayi;
        private readonly string _metin;

        /// <summary>Değerin türü.</summary>
        public JsonTuru Tur { get; }

        /// <summary>Kaynakta 1 tabanlı satır; programla kurulduysa 0.</summary>
        public int Satir { get; }

        /// <summary>Kaynakta 1 tabanlı sütun; programla kurulduysa 0.</summary>
        public int Sutun { get; }

        /// <summary>Dizi elemanları; dizi değilse boş.</summary>
        public IReadOnlyList<JsonDegeri> Elemanlar { get; }

        /// <summary>Nesne özellikleri, dosyadaki sırayla; nesne değilse boş.</summary>
        public IReadOnlyList<KeyValuePair<string, JsonDegeri>> Ozellikler { get; }

        /// <summary>Mantıksal değer.</summary>
        /// <exception cref="InvalidOperationException">Tür <see cref="JsonTuru.Mantiksal"/> değilse.</exception>
        public bool MantiksalDeger => Tur == JsonTuru.Mantiksal ? _mantiksal : throw TurHatasi(JsonTuru.Mantiksal);

        /// <summary>Sayı değeri.</summary>
        /// <exception cref="InvalidOperationException">Tür <see cref="JsonTuru.Sayi"/> değilse.</exception>
        public decimal SayiDeger => Tur == JsonTuru.Sayi ? _sayi : throw TurHatasi(JsonTuru.Sayi);

        /// <summary>Metin değeri.</summary>
        /// <exception cref="InvalidOperationException">Tür <see cref="JsonTuru.Metin"/> değilse.</exception>
        public string MetinDeger => Tur == JsonTuru.Metin ? _metin : throw TurHatasi(JsonTuru.Metin);

        private JsonDegeri(
            JsonTuru tur, bool mantiksal, decimal sayi, string metin,
            IReadOnlyList<JsonDegeri>? elemanlar, IReadOnlyList<KeyValuePair<string, JsonDegeri>>? ozellikler, int satir, int sutun)
        {
            Tur = tur;
            _mantiksal = mantiksal;
            _sayi = sayi;
            _metin = metin;
            Elemanlar = elemanlar ?? BosElemanlar;
            Ozellikler = ozellikler ?? BosOzellikler;
            Satir = satir;
            Sutun = sutun;
        }

        /// <summary>Anahtarı olan nesne özelliği; anahtar yoksa ya da bu bir nesne değilse <c>null</c>.</summary>
        public JsonDegeri? Al(string anahtar)
        {
            foreach (var kv in Ozellikler)
            {
                if (string.Equals(kv.Key, anahtar, StringComparison.Ordinal))
                {
                    return kv.Value;
                }
            }

            return null;
        }

        /// <summary><c>null</c> değeri kurar.</summary>
        public static JsonDegeri Bos() => new JsonDegeri(JsonTuru.Bos, false, 0m, string.Empty, null, null, 0, 0);

        /// <summary>Mantıksal değer kurar.</summary>
        public static JsonDegeri Mantiksal(bool deger) => new JsonDegeri(JsonTuru.Mantiksal, deger, 0m, string.Empty, null, null, 0, 0);

        /// <summary>Sayı kurar.</summary>
        public static JsonDegeri Sayi(decimal deger) => new JsonDegeri(JsonTuru.Sayi, false, deger, string.Empty, null, null, 0, 0);

        /// <summary>Metin kurar.</summary>
        public static JsonDegeri Metin(string deger) =>
            new JsonDegeri(JsonTuru.Metin, false, 0m, deger ?? throw new ArgumentNullException(nameof(deger)), null, null, 0, 0);

        /// <summary>Dizi kurar.</summary>
        public static JsonDegeri Dizi(params JsonDegeri[] elemanlar) =>
            new JsonDegeri(JsonTuru.Dizi, false, 0m, string.Empty, new ReadOnlyCollection<JsonDegeri>(new List<JsonDegeri>(elemanlar)), null, 0, 0);

        /// <summary>Nesne kurar; özellikler verilen sırayla yazılır.</summary>
        /// <exception cref="ArgumentException">Anahtar tekrar ederse.</exception>
        public static JsonDegeri Nesne(params (string Anahtar, JsonDegeri Deger)[] ozellikler)
        {
            var liste = new List<KeyValuePair<string, JsonDegeri>>(ozellikler.Length);
            var gorulen = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var (anahtar, deger) in ozellikler)
            {
                if (!gorulen.Add(anahtar))
                {
                    throw new ArgumentException($"Nesnede \"{anahtar}\" anahtarı birden fazla kez geçiyor.", nameof(ozellikler));
                }

                liste.Add(new KeyValuePair<string, JsonDegeri>(anahtar, deger));
            }

            return new JsonDegeri(JsonTuru.Nesne, false, 0m, string.Empty, null, new ReadOnlyCollection<KeyValuePair<string, JsonDegeri>>(liste), 0, 0);
        }

        internal static JsonDegeri OkunanBos(int satir, int sutun) =>
            new JsonDegeri(JsonTuru.Bos, false, 0m, string.Empty, null, null, satir, sutun);

        internal static JsonDegeri OkunanMantiksal(bool deger, int satir, int sutun) =>
            new JsonDegeri(JsonTuru.Mantiksal, deger, 0m, string.Empty, null, null, satir, sutun);

        internal static JsonDegeri OkunanSayi(decimal deger, int satir, int sutun) =>
            new JsonDegeri(JsonTuru.Sayi, false, deger, string.Empty, null, null, satir, sutun);

        internal static JsonDegeri OkunanMetin(string deger, int satir, int sutun) =>
            new JsonDegeri(JsonTuru.Metin, false, 0m, deger, null, null, satir, sutun);

        internal static JsonDegeri OkunanDizi(List<JsonDegeri> elemanlar, int satir, int sutun) =>
            new JsonDegeri(JsonTuru.Dizi, false, 0m, string.Empty, new ReadOnlyCollection<JsonDegeri>(elemanlar), null, satir, sutun);

        internal static JsonDegeri OkunanNesne(List<KeyValuePair<string, JsonDegeri>> ozellikler, int satir, int sutun) =>
            new JsonDegeri(JsonTuru.Nesne, false, 0m, string.Empty, null, new ReadOnlyCollection<KeyValuePair<string, JsonDegeri>>(ozellikler), satir, sutun);

        private InvalidOperationException TurHatasi(JsonTuru beklenen) =>
            new InvalidOperationException($"JSON değeri {beklenen} değil, {Tur}.");
    }
}
