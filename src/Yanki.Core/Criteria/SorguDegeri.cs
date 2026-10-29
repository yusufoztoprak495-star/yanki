using System;

namespace Yanki.Core.Criteria
{
    /// <summary><see cref="SorguDegeri"/> değerinin türü.</summary>
    public enum DegerTuru
    {
        /// <summary>Sayı.</summary>
        Sayi,

        /// <summary>Doğru/yanlış.</summary>
        Mantiksal,

        /// <summary>Metin: varlık kimliği ya da bant adı.</summary>
        Metin,

        /// <summary>Kapalı sayı aralığı <c>[alt, üst]</c>; yalnızca kriter değeri olarak kullanılır.</summary>
        Aralik,
    }

    /// <summary>
    /// Bir sorgunun döndürdüğü ya da bir kriterin karşılaştırdığı değer: sayı (<see cref="decimal"/>), mantıksal,
    /// metin ya da aralık. Değişmez.
    /// </summary>
    public readonly struct SorguDegeri
    {
        private readonly string? _metin;

        /// <summary>Değerin türü.</summary>
        public DegerTuru Tur { get; }

        /// <summary>Sayı değeri; aralıkta alt sınır.</summary>
        public decimal SayiDegeri { get; }

        /// <summary>Aralığın üst sınırı (yalnızca <see cref="DegerTuru.Aralik"/>).</summary>
        public decimal UstSinir { get; }

        /// <summary>Mantıksal değer.</summary>
        public bool MantiksalDegeri { get; }

        /// <summary>Metin değeri; metin değilse boş.</summary>
        public string MetinDegeri => _metin ?? string.Empty;

        private SorguDegeri(DegerTuru tur, decimal sayi, decimal ust, bool mantiksal, string? metin)
        {
            Tur = tur;
            SayiDegeri = sayi;
            UstSinir = ust;
            MantiksalDegeri = mantiksal;
            _metin = metin;
        }

        /// <summary>Sayı değeri.</summary>
        public static SorguDegeri Sayi(decimal deger) => new SorguDegeri(DegerTuru.Sayi, deger, 0m, false, null);

        /// <summary>Mantıksal değer.</summary>
        public static SorguDegeri Mantiksal(bool deger) => new SorguDegeri(DegerTuru.Mantiksal, 0m, 0m, deger, null);

        /// <summary>Metin değeri (varlık kimliği ya da bant adı).</summary>
        public static SorguDegeri Metin(string deger) =>
            new SorguDegeri(DegerTuru.Metin, 0m, 0m, false, deger ?? throw new ArgumentNullException(nameof(deger)));

        /// <summary>Kapalı aralık <c>[alt, ust]</c>.</summary>
        /// <exception cref="ArgumentException"><paramref name="alt"/> &gt; <paramref name="ust"/> ise.</exception>
        public static SorguDegeri Aralik(decimal alt, decimal ust)
        {
            if (alt > ust)
            {
                throw new ArgumentException($"Aralığın alt sınırı üst sınırından büyük olamaz: [{World.KanonikYazici.Sayi(alt)}, {World.KanonikYazici.Sayi(ust)}].");
            }

            return new SorguDegeri(DegerTuru.Aralik, alt, ust, false, null);
        }

        /// <summary>Okunur yazım: <c>-10</c>, <c>true</c>, <c>soguk</c>, <c>[2, 4]</c>.</summary>
        public override string ToString()
        {
            switch (Tur)
            {
                case DegerTuru.Sayi: return World.KanonikYazici.Sayi(SayiDegeri);
                case DegerTuru.Mantiksal: return MantiksalDegeri ? "true" : "false";
                case DegerTuru.Metin: return MetinDegeri;
                default: return "[" + World.KanonikYazici.Sayi(SayiDegeri) + ", " + World.KanonikYazici.Sayi(UstSinir) + "]";
            }
        }
    }
}
