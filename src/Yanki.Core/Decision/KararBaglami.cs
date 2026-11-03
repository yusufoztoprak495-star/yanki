using System;
using Yanki.Core.Brakes;
using Yanki.Core.Criteria;
using Yanki.Core.Events;
using Yanki.Core.Mind;
using Yanki.Core.World;

namespace Yanki.Core.Decision
{
    /// <summary>
    /// Bir NPC'nin bir tick'teki karar bağlamı: yalnızca o NPC'nin zihni, dünya (konum ve mekan grafiği için) ve kriter sözlüğünün
    /// çözücüsü. Karar kodu bunların dışına çıkmaz; bağlam salt okunurdur ve hiçbir şey yazılmaz.
    /// </summary>
    public sealed class KararBaglami
    {
        /// <summary>Karar veren NPC.</summary>
        public string Npc { get; }

        /// <summary>Oyuncu varlığının kimliği.</summary>
        public string OyuncuId { get; }

        /// <summary>Simülasyon seed'i; seçim RNG'si <c>AnahtarliRng.Icin(Seed, Tick, Npc, "karar")</c> ile kurulur.</summary>
        public ulong Seed { get; }

        /// <summary>Kararın verildiği tick.</summary>
        public long Tick { get; }

        /// <summary>Dünya (salt okunur kullanılır).</summary>
        public Dunya Dunya { get; }

        /// <summary>Karar verenin zihni.</summary>
        public Zihin Zihin { get; }

        /// <summary>Olay günlüğü (salt okunur): hafızadaki olayların türü ve failı buradan okunur.</summary>
        public OlayGunlugu Gunluk { get; }

        /// <summary>Kriter sözlüğü çözücüsü (zihin ve dünya sorguları).</summary>
        public ISorguCozucu Cozucu { get; }

        /// <summary>Konuşma eylemlerinin konu hesabı (tick içi önbellekli, salt okunur); konuşma eylemi yoksa <c>null</c> olabilir.</summary>
        public KonuHesabi? KonuHesabi { get; }

        /// <summary>Cooldown/en_fazla sayaçları (salt okunur, günlükten türetilmiş); sınırlı eylem yoksa <c>null</c> olabilir.</summary>
        public TetikSayaclari? Sayaclar { get; }

        /// <summary>Korumalı varlıklar; yoksa <c>null</c> (korumalı varlık yok).</summary>
        public Korumalilik? Korumali { get; }

        /// <summary>Bağlam kurar.</summary>
        public KararBaglami(
            string npc, string oyuncuId, ulong seed, long tick, Dunya dunya, Zihin zihin, OlayGunlugu gunluk, ISorguCozucu cozucu, KonuHesabi? konuHesabi = null,
            TetikSayaclari? sayaclar = null, Korumalilik? korumali = null)
        {
            if (string.IsNullOrEmpty(npc))
            {
                throw new ArgumentException("Karar veren NPC boş olamaz.", nameof(npc));
            }

            Npc = npc;
            OyuncuId = oyuncuId ?? string.Empty;
            Seed = seed;
            Tick = tick;
            Dunya = dunya ?? throw new ArgumentNullException(nameof(dunya));
            Zihin = zihin ?? throw new ArgumentNullException(nameof(zihin));
            Gunluk = gunluk ?? throw new ArgumentNullException(nameof(gunluk));
            Cozucu = cozucu ?? throw new ArgumentNullException(nameof(cozucu));
            KonuHesabi = konuHesabi;
            Sayaclar = sayaclar;
            Korumali = korumali;
        }

        /// <summary>Verilen hedef için sorgu bağlamı: <c>ben</c> = karar veren, <c>eylem.hedef</c> = aday hedefi (boşsa tanımsız).</summary>
        public SorguBaglami SorguBaglami(string hedef, EylemTanimi? eylem = null) =>
            new SorguBaglami(
                Npc, OyuncuId, eylemHedef: hedef ?? string.Empty,
                konuDesenleri: eylem != null && eylem.Aktarim == AktarimTuru.Dogru ? eylem.KonuDesenleri : null);
    }
}
