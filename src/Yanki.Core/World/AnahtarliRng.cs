using System;
using System.Collections.Generic;
using System.Text;

namespace Yanki.Core.World
{
    /// <summary>
    /// Tek, anahtarlı rastgele sayı üreteci. Akış yalnızca <c>(seed, tick, anahtarlar)</c> ile belirlenir;
    /// başka akışların çağrı sırası ya da sayısı sonucu etkilemez. Tohum, anahtarlardan <see cref="Hash64"/> ile,
    /// akış SplitMix64 ile üretilir.
    /// </summary>
    public sealed class AnahtarliRng
    {
        private const ulong Gama = 0x9E3779B97F4A7C15UL;
        private ulong _durum;

        private AnahtarliRng(ulong durum)
        {
            _durum = durum;
        }

        /// <summary>
        /// Verilen seed, tick ve anahtarlar için akış kurar. Aynı girdi her zaman aynı akışı verir.
        /// Anahtarlar uzunluk önekiyle kodlanır; <c>("ab","c")</c> ile <c>("a","bc")</c> çakışmaz.
        /// </summary>
        public static AnahtarliRng Icin(ulong seed, long tick, params string[] anahtarlar)
        {
            if (anahtarlar == null)
            {
                throw new ArgumentNullException(nameof(anahtarlar));
            }

            var bayt = new List<byte>(16 + (anahtarlar.Length * 16));
            SekizBaytEkle(bayt, seed);
            SekizBaytEkle(bayt, unchecked((ulong)tick));
            foreach (var anahtar in anahtarlar)
            {
                if (anahtar == null)
                {
                    throw new ArgumentNullException(nameof(anahtarlar), "Anahtarlar null olamaz.");
                }

                var utf8 = Encoding.UTF8.GetBytes(anahtar);
                var uzunluk = (uint)utf8.Length;
                bayt.Add((byte)uzunluk);
                bayt.Add((byte)(uzunluk >> 8));
                bayt.Add((byte)(uzunluk >> 16));
                bayt.Add((byte)(uzunluk >> 24));
                bayt.AddRange(utf8);
            }

            return new AnahtarliRng(Hash64.Hesapla(bayt.ToArray()));
        }

        /// <summary>SplitMix64 akışını doğrudan bir iç durumdan başlatır (referans vektörlerini sınamak için).</summary>
        internal static AnahtarliRng DurumdanKur(ulong durum) => new AnahtarliRng(durum);

        /// <summary>Akışın sonraki 64 bit değeri (SplitMix64).</summary>
        public ulong SonrakiUlong()
        {
            unchecked
            {
                _durum += Gama;
            }

            return Hash64.Karistir(_durum);
        }

        /// <summary>
        /// <c>[min, maxHaric)</c> aralığında tek biçimli bir tamsayı. Modulo yanlılığı red örneklemesiyle giderilir.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="min"/> &gt;= <paramref name="maxHaric"/> ise.</exception>
        public int Aralik(int min, int maxHaric)
        {
            if (min >= maxHaric)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHaric), $"Aralık boş: min={min}, maxHaric={maxHaric}.");
            }

            var genislik = (ulong)((long)maxHaric - min);
            // 2^64 mod genislik: bunun altındaki değerler atılır, kalan aralık genislik'in tam katıdır.
            var esik = unchecked(0UL - genislik) % genislik;
            ulong r;
            do
            {
                r = SonrakiUlong();
            }
            while (r < esik);

            return (int)(min + (long)(r % genislik));
        }

        /// <summary><c>[0, 1)</c> aralığında bir <see cref="decimal"/>: 53 bitlik tamsayı / 2^53.</summary>
        public decimal SonrakiDecimal01() => (decimal)(SonrakiUlong() >> 11) / 9007199254740992m;

        private static void SekizBaytEkle(List<byte> hedef, ulong deger)
        {
            for (var i = 0; i < 8; i++)
            {
                hedef.Add((byte)(deger >> (8 * i)));
            }
        }
    }
}
