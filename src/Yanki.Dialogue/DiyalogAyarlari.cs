using System;

namespace Yanki.Dialogue
{
    /// <summary>
    /// Diyalog katmanının ayarları. Zaman ifadesi eşikleri ve ifade metinleri YER TUTUCUDUR: denge ve metin alanı Yusuf'undur (8. hafta, E kararı:
    /// göreli ifade; takvim yok, oyun içi günden hesaplanır, 1 tick = 1 saat). Senaryo şeması değişmez; ayarlar şablon dosyasına ve hash'e girmez.
    /// </summary>
    public sealed class DiyalogAyarlari
    {
        /// <summary>Bir oyun içi günün tick sayısı (1 tick = 1 saat).</summary>
        public int GunUzunlugu { get; set; } = 24;

        /// <summary>Bu gün farkından itibaren "N gün önce" yerine <see cref="HaftaIfadesi"/> kullanılır.</summary>
        public int HaftaEsigiGun { get; set; } = 7;

        /// <summary>Bu gün farkından itibaren <see cref="UzunSureIfadesi"/> kullanılır.</summary>
        public int UzunSureEsigiGun { get; set; } = 30;

        /// <summary>0 gün farkı.</summary>
        public string BugunIfadesi { get; set; } = "bugün";

        /// <summary>1 gün farkı.</summary>
        public string DunIfadesi { get; set; } = "dün";

        /// <summary>2 gün farkı.</summary>
        public string EvvelsiGunIfadesi { get; set; } = "evvelsi gün";

        /// <summary>Hafta eşiğinden uzun süreye kadar.</summary>
        public string HaftaIfadesi { get; set; } = "geçen hafta";

        /// <summary>Uzun süre eşiğinden sonra.</summary>
        public string UzunSureIfadesi { get; set; } = "çok önce";

        /// <summary>
        /// Baskın duygu → ton eşlemesi (B kararı: ton baskın duygudan türer; stres() ton tablosunda kullanılmaz). YER TUTUCU: tablo ve ton adları Yusuf'un alanıdır (DURAK 2).
        /// Anahtar duygu türü (ör. ofke), değer ton adı (ASCII). Tabloda olmayan duygu ve duygu yokluğu için <see cref="VarsayilanTon"/> kullanılır.
        /// </summary>
        public System.Collections.Generic.SortedDictionary<string, string> TonTablosu { get; } = new System.Collections.Generic.SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["ofke"] = "gergin",
            ["korku"] = "urkek",
        };

        /// <summary>Tabloda karşılığı olmayan duygu ve duygusuzluk için ton.</summary>
        public string VarsayilanTon { get; set; } = "notr";

        /// <summary>Değerlerin tutarlılığını denetler.</summary>
        /// <exception cref="ArgumentException">Bir eşik ya da gün uzunluğu geçersizse.</exception>
        public void Dogrula()
        {
            if (GunUzunlugu < 1)
            {
                throw new ArgumentException("GunUzunlugu 1 ya da daha büyük olmalı.");
            }

            if (HaftaEsigiGun < 3 || UzunSureEsigiGun <= HaftaEsigiGun)
            {
                throw new ArgumentException("Eşikler 3 <= HaftaEsigiGun < UzunSureEsigiGun olmalı.");
            }
        }
    }
}
