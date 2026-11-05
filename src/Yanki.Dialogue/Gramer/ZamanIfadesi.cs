using System;
using System.Globalization;

namespace Yanki.Dialogue.Gramer
{
    /// <summary>
    /// Göreli zaman ifadesi (bugün, dün, üç gün önce ...). Takvim yoktur: ifade, olayın ve şimdiki tick'in OYUN İÇİ GÜN numaralarının farkından hesaplanır
    /// (gün no = floor(tick / <see cref="DiyalogAyarlari.GunUzunlugu"/>)). Eşikler ve metinler ayardandır.
    /// </summary>
    internal static class ZamanIfadesi
    {
        private static readonly string[] Sayilar = { "sıfır", "bir", "iki", "üç", "dört", "beş", "altı", "yedi", "sekiz", "dokuz", "on" };

        public static string Uret(long olayTick, long simdikiTick, DiyalogAyarlari ayar)
        {
            if (ayar == null)
            {
                throw new ArgumentNullException(nameof(ayar));
            }

            if (olayTick > simdikiTick)
            {
                throw new ArgumentException($"Olay (tick {olayTick}) şimdiki tick'ten ({simdikiTick}) sonra olamaz.", nameof(olayTick));
            }

            var fark = GunNo(simdikiTick, ayar) - GunNo(olayTick, ayar);
            if (fark == 0)
            {
                return ayar.BugunIfadesi;
            }

            if (fark == 1)
            {
                return ayar.DunIfadesi;
            }

            if (fark == 2)
            {
                return ayar.EvvelsiGunIfadesi;
            }

            if (fark < ayar.HaftaEsigiGun)
            {
                return Sayi(fark) + " gün önce";
            }

            return fark < ayar.UzunSureEsigiGun ? ayar.HaftaIfadesi : ayar.UzunSureIfadesi;
        }

        private static long GunNo(long tick, DiyalogAyarlari ayar) => Math.Max(tick, 0L) / ayar.GunUzunlugu;

        private static string Sayi(long n) => n >= 0 && n < Sayilar.Length ? Sayilar[n] : n.ToString(CultureInfo.InvariantCulture);
    }
}
