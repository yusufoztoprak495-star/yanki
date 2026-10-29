using System;
using System.Globalization;

namespace Yanki.Core.World
{
    /// <summary>
    /// Hash'e ve gerçek argümanlarına giren değerlerin tek, kültürden bağımsız yazımı (ADR-007).
    /// </summary>
    public static class KanonikYazici
    {
        /// <summary>Kanonik yazımdaki en fazla ondalık basamak sayısı.</summary>
        public const int OndalikBasamak = 6;

        /// <summary>
        /// <see cref="decimal"/> değerini kanonik metne çevirir: <see cref="MidpointRounding.ToEven"/> ile
        /// <see cref="OndalikBasamak"/> basamağa yuvarlar, sondaki sıfırları kırpar, <see cref="CultureInfo.InvariantCulture"/> kullanır.
        /// Eksi sıfır yazılmaz (<c>-0.0000004</c> → <c>0</c>).
        /// </summary>
        public static string Sayi(decimal deger)
        {
            var yuvarlak = Math.Round(deger, OndalikBasamak, MidpointRounding.ToEven);
            if (yuvarlak == 0m)
            {
                return "0";
            }

            return yuvarlak.ToString("0.######", CultureInfo.InvariantCulture);
        }
    }
}
