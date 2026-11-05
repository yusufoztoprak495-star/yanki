using System;
using System.Collections.Generic;

namespace Yanki.Dialogue
{
    /// <summary>Ton kuralı: baskın duygu bu türdeyse ve şiddeti eşik ve üstündeyse ton verilir. Kurallar sırayla denenir, ilk eşleşen kazanır.</summary>
    public sealed class TonKurali
    {
        /// <summary>Kuralı kurar.</summary>
        public TonKurali(string duygu, decimal esik, string ton)
        {
            Duygu = duygu;
            Esik = esik;
            Ton = ton;
        }

        /// <summary>Baskın duygunun türü (ör. ofke).</summary>
        public string Duygu { get; }

        /// <summary>Şiddet eşiği (≥).</summary>
        public decimal Esik { get; }

        /// <summary>Ton adı (ASCII).</summary>
        public string Ton { get; }
    }

    /// <summary>
    /// Diyalog katmanının ayarları. Eşikler YER TUTUCUDUR ve ayar sınıfında durur (şablon dosyasına ve hash'e girmez); denge alanı Yusuf'undur. Zaman ifadesi (E kararı):
    /// göreli; takvim yok, oyun günü farkından hesaplanır, gün no = floor(tick / <see cref="GunUzunlugu"/>), 1 tick = 1 saat.
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
        public string UzunSureIfadesi { get; set; } = "uzun zaman önce";

        /// <summary>
        /// Baskın duygu → ton kuralları (B kararı: ton baskın duygudan türer; stres() ton tablosunda KULLANILMAZ). Varsayılan: ofke ≥ 0.6 ofkeli, ofke ≥ 0.2 gergin, korku ≥ 0.2 urkek.
        /// Eşikler yer tutucudur.
        /// </summary>
        public IList<TonKurali> TonKurallari { get; } = new List<TonKurali>
        {
            new TonKurali("ofke", 0.6m, "ofkeli"),
            new TonKurali("ofke", 0.2m, "gergin"),
            new TonKurali("korku", 0.2m, "urkek"),
        };

        /// <summary>Hiçbir kural eşleşmezse ton.</summary>
        public string VarsayilanTon { get; set; } = "notr";

        /// <summary>Baskın duygu türü ve şiddetinden ton seçer.</summary>
        public string TonSec(string duygu, decimal siddet)
        {
            foreach (var k in TonKurallari)
            {
                if (string.Equals(k.Duygu, duygu, StringComparison.Ordinal) && siddet >= k.Esik)
                {
                    return k.Ton;
                }
            }

            return VarsayilanTon;
        }

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
