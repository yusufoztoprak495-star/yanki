using System;
using System.Collections.Generic;
using Yanki.Core.Events;

namespace Yanki.Core.Brakes
{
    /// <summary>Fren ayarları (denge alanı: sayılar ayar sınıfında durur). Senaryo dosyasından değil kod/kurulumdan gelir.</summary>
    public sealed class FrenAyarlari
    {
        /// <summary>Varsayılan zincir derinliği sınırı D (plan 5.10).</summary>
        public const int VarsayilanZincirDerinligi = 4;

        /// <summary>
        /// Kural→kural zincirinin en fazla derinliği D. Derinlik tick'ler arası ölçülür (4. haftadaki kaskad v0 her kural çıktısını sıradaki
        /// tick'e bıraktığı için aynı tick'te derinlik hep 1'dir): D'yi aşacak kural çalışmaz ve <c>zincir</c> freni yazılır.
        /// </summary>
        public int ZincirDerinligi { get; }

        /// <summary>Ayar kurar.</summary>
        public FrenAyarlari(int zincirDerinligi = VarsayilanZincirDerinligi)
        {
            if (zincirDerinligi < 1)
            {
                throw new ArgumentException("Zincir derinliği en az 1 olmalı.", nameof(zincirDerinligi));
            }

            ZincirDerinligi = zincirDerinligi;
        }
    }

    /// <summary>
    /// Olay zincirinin derinliği (7. hafta, ADR-013, Seçenek 1). <c>Olay</c> tipine alan eklenmez: derinlik günlükten türer (<see cref="Isle"/>).
    /// Kuralın ürettiği olay <c>1 + nedenlerindeki en büyük derinlik</c> alır; algı olayları (<c>KuralId: algi</c>) derinlik EKLEMEZ
    /// (nedenlerinin en büyüğünü taşır); oyuncu, NPC eylemi, kriz, senaryo ve diğer bütün olaylar 0 başlar.
    /// </summary>
    public sealed class ZincirDerinligi
    {
        private readonly SortedSet<string> _kurallar;
        private readonly SortedDictionary<long, int> _derinlik = new SortedDictionary<long, int>();

        /// <summary>Sonuç kuralı kimlikleriyle kurar.</summary>
        public ZincirDerinligi(IEnumerable<string> kuralKimlikleri)
        {
            _kurallar = new SortedSet<string>(kuralKimlikleri ?? throw new ArgumentNullException(nameof(kuralKimlikleri)), StringComparer.Ordinal);
        }

        /// <summary>Günlüğe eklenen her olay bununla işlenir (nedenleri kendisinden önce işlenmiş olmalıdır).</summary>
        public void Isle(Olay olay)
        {
            if (olay == null)
            {
                throw new ArgumentNullException(nameof(olay));
            }

            var kural = _kurallar.Contains(olay.KuralId);
            var algi = string.Equals(olay.KuralId, "algi", StringComparison.Ordinal);
            if (!kural && !algi)
            {
                _derinlik[olay.Id] = 0;
                return;
            }

            _derinlik[olay.Id] = (kural ? 1 : 0) + EnBuyuk(olay.NedenOlaylar);
        }

        /// <summary>Olayın derinliği; bilinmiyorsa 0.</summary>
        public int Derinlik(long olayId) => _derinlik.TryGetValue(olayId, out var d) ? d : 0;

        /// <summary>Verilen neden olaylarının en büyük derinliği.</summary>
        public int EnBuyuk(IEnumerable<long> nedenler)
        {
            var en = 0;
            foreach (var n in nedenler)
            {
                en = Math.Max(en, Derinlik(n));
            }

            return en;
        }
    }

    /// <summary><see cref="Rules.KuralMotoru"/> değerlendirmesinin frenle ilgili girdileri; hepsi isteğe bağlıdır (yoksa fren uygulanmaz).</summary>
    public sealed class FrenBaglami
    {
        /// <summary>Cooldown/tetiklenme sayaçları.</summary>
        public TetikSayaclari? Sayaclar { get; }

        /// <summary>Genel sınır M (tick başına en fazla sonuç çıktısı); <c>null</c> sınırsız.</summary>
        public int? GenelSinir { get; }

        /// <summary>Zincir derinliği görünümü.</summary>
        public ZincirDerinligi? Zincir { get; }

        /// <summary>Zincir derinliği sınırı D.</summary>
        public int ZincirSiniri { get; }

        /// <summary>Kurar.</summary>
        public FrenBaglami(TetikSayaclari? sayaclar, int? genelSinir, ZincirDerinligi? zincir, int zincirSiniri)
        {
            Sayaclar = sayaclar;
            GenelSinir = genelSinir;
            Zincir = zincir;
            ZincirSiniri = zincirSiniri;
        }
    }
}
