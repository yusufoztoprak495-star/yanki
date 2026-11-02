using System;

namespace Yanki.Core.Decision
{
    /// <summary>Ölçüt eğri tipleri (plan 5.5).</summary>
    public enum EgriTuru
    {
        /// <summary><c>f(x) = x</c>.</summary>
        Dogrusal,

        /// <summary><c>f(x) = x^k</c>, <c>k</c> tam sayı 1..8.</summary>
        Ustel,

        /// <summary><c>f(x) = 1 / (1 + e^(−k(x−m)))</c>.</summary>
        Lojistik,

        /// <summary><c>f(x) = 1</c> eğer <c>x ≥ m</c>, yoksa 0.</summary>
        Basamak,
    }

    /// <summary><see cref="EgriTuru"/> ile JSON'daki ASCII adlar arasında çeviri.</summary>
    public static class EgriTurleri
    {
        /// <summary>Türün JSON adı.</summary>
        public static string Ad(EgriTuru tur)
        {
            switch (tur)
            {
                case EgriTuru.Dogrusal: return "dogrusal";
                case EgriTuru.Ustel: return "ustel";
                case EgriTuru.Lojistik: return "lojistik";
                default: return "basamak";
            }
        }

        /// <summary>Adı türe çevirir; bilinmiyorsa <c>false</c> (büyük/küçük harf duyarlı).</summary>
        public static bool TryCoz(string ad, out EgriTuru tur)
        {
            switch (ad)
            {
                case "dogrusal": tur = EgriTuru.Dogrusal; return true;
                case "ustel": tur = EgriTuru.Ustel; return true;
                case "lojistik": tur = EgriTuru.Lojistik; return true;
                case "basamak": tur = EgriTuru.Basamak; return true;
                default: tur = EgriTuru.Dogrusal; return false;
            }
        }
    }

    /// <summary>
    /// Bir ölçütün eğri tanımı. <see cref="K"/> ve <see cref="M"/> türe göre anlam kazanır; kullanılmayanlar 0'dır:
    /// üstel için <c>K</c> = üs; lojistik için <c>K</c> = dikçe, <c>M</c> = orta nokta; basamak için <c>M</c> = eşik.
    /// </summary>
    public sealed class EgriTanimi
    {
        /// <summary>Lojistik <c>k</c>'nın izin verilen en büyük değeri.</summary>
        public const decimal LojistikEnBuyukK = 100m;

        /// <summary>Üstel <c>k</c>'nın en küçük ve en büyük değeri.</summary>
        public const int UstelEnKucukK = 1;

        /// <summary>Üstel <c>k</c>'nın en büyük değeri.</summary>
        public const int UstelEnBuyukK = 8;

        /// <summary>Eğri türü.</summary>
        public EgriTuru Tur { get; }

        /// <summary>Üs (üstel) ya da dikçe (lojistik).</summary>
        public decimal K { get; }

        /// <summary>Orta nokta (lojistik) ya da eşik (basamak).</summary>
        public decimal M { get; }

        private EgriTanimi(EgriTuru tur, decimal k, decimal m)
        {
            Tur = tur;
            K = k;
            M = m;
        }

        /// <summary>Doğrusal eğri.</summary>
        public static EgriTanimi Dogrusal() => new EgriTanimi(EgriTuru.Dogrusal, 0m, 0m);

        /// <summary>Üstel eğri.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="k"/> 1..8 dışındaysa.</exception>
        public static EgriTanimi Ustel(int k)
        {
            if (k < UstelEnKucukK || k > UstelEnBuyukK)
            {
                throw new ArgumentOutOfRangeException(nameof(k), k, "Üstel eğride k tam sayı olup 1..8 aralığında olmalı.");
            }

            return new EgriTanimi(EgriTuru.Ustel, k, 0m);
        }

        /// <summary>Lojistik eğri.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="k"/> (0, 100] dışındaysa ya da <paramref name="m"/> 0..1 dışındaysa.</exception>
        public static EgriTanimi Lojistik(decimal k, decimal m)
        {
            if (k <= 0m || k > LojistikEnBuyukK)
            {
                throw new ArgumentOutOfRangeException(nameof(k), k, "Lojistik eğride k 0'dan büyük ve en çok 100 olmalı.");
            }

            ZeroBirAraliginda(m, nameof(m));
            return new EgriTanimi(EgriTuru.Lojistik, k, m);
        }

        /// <summary>Basamak eğrisi.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="esik"/> 0..1 dışındaysa.</exception>
        public static EgriTanimi Basamak(decimal esik)
        {
            ZeroBirAraliginda(esik, nameof(esik));
            return new EgriTanimi(EgriTuru.Basamak, 0m, esik);
        }

        private static void ZeroBirAraliginda(decimal deger, string ad)
        {
            if (deger < 0m || deger > 1m)
            {
                throw new ArgumentOutOfRangeException(ad, deger, "Değer 0..1 aralığında olmalı.");
            }
        }
    }

    /// <summary>Eğri hesabı: girdi 0..1'e kırpılır, çıktı 0..1'e kırpılır ve 6 ondalığa yuvarlanır (ToEven, <c>KanonikYazici</c> kuralı).</summary>
    public static class Egriler
    {
        /// <summary>Karar hesabındaki bütün ara değerlerin ortak yuvarlaması: 6 ondalık, <see cref="MidpointRounding.ToEven"/>.</summary>
        public static decimal Yuvarla(decimal deger) =>
            Math.Round(deger, World.KanonikYazici.OndalikBasamak, MidpointRounding.ToEven);

        /// <summary>Eğriyi <paramref name="x"/> (önce 0..1'e kırpılır) için hesaplar.</summary>
        public static decimal Hesapla(EgriTanimi egri, decimal x)
        {
            if (egri == null)
            {
                throw new ArgumentNullException(nameof(egri));
            }

            x = Kirp01(x);
            decimal sonuc;
            switch (egri.Tur)
            {
                case EgriTuru.Dogrusal:
                    sonuc = x;
                    break;
                case EgriTuru.Ustel:
                    sonuc = 1m;
                    for (var i = 0; i < (int)egri.K; i++)
                    {
                        sonuc *= x;
                    }

                    break;
                case EgriTuru.Lojistik:
                    sonuc = 1m / (1m + DecimalMat.Us(-egri.K * (x - egri.M)));
                    break;
                default:
                    sonuc = x >= egri.M ? 1m : 0m;
                    break;
            }

            return Yuvarla(Kirp01(sonuc));
        }

        private static decimal Kirp01(decimal d) => d < 0m ? 0m : d > 1m ? 1m : d;
    }
}
