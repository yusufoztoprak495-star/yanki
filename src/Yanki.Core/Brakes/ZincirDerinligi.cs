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

        /// <summary>Genel sınır M varsayılanının tabanı (senaryoda <c>frenler.tick_basina_en_fazla_sonuc</c> yoksa).</summary>
        public const int VarsayilanGenelSinirTabani = 32;

        /// <summary>Genel sınır M varsayılanının NPC başına çarpanı.</summary>
        public const int VarsayilanGenelSinirNpcCarpani = 8;

        /// <summary>
        /// Senaryo M'yi yazmadıysa uygulanan tick başına en fazla sonuç çıktısı: <c>max(taban, çarpan × NPC sayısı)</c> (Yusuf'un kararı,
        /// 3 golden'da en çok 6, 8 NPC stres fikstüründe 13 çıktı ölçüldü).
        /// </summary>
        public int GenelSinirTabani { get; }

        /// <summary>M varsayılanının NPC başına çarpanı.</summary>
        public int GenelSinirNpcCarpani { get; }

        /// <summary>Varsayılan M'yi NPC sayısı için hesaplar.</summary>
        public int VarsayilanGenelSinir(int npcSayisi) => Math.Max(GenelSinirTabani, GenelSinirNpcCarpani * npcSayisi);

        /// <summary>Ayar kurar.</summary>
        public FrenAyarlari(
            int zincirDerinligi = VarsayilanZincirDerinligi, int genelSinirTabani = VarsayilanGenelSinirTabani, int genelSinirNpcCarpani = VarsayilanGenelSinirNpcCarpani)
        {
            if (genelSinirTabani < 1 || genelSinirNpcCarpani < 0)
            {
                throw new ArgumentException("Genel sınır tabanı en az 1, NPC çarpanı en az 0 olmalı.");
            }

            GenelSinirTabani = genelSinirTabani;
            GenelSinirNpcCarpani = genelSinirNpcCarpani;
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

        /// <summary>Korumalı varlıklar.</summary>
        public Korumalilik Korumali { get; }

        /// <summary>Yay değişkenleri (kriter <c>yay(ad)</c> okur).</summary>
        public YayDegiskenleri Yay { get; }

        /// <summary>Kurar.</summary>
        public FrenBaglami(TetikSayaclari? sayaclar, int? genelSinir, ZincirDerinligi? zincir, int zincirSiniri, Korumalilik? korumali = null, YayDegiskenleri? yay = null)
        {
            Yay = yay ?? YayDegiskenleri.Yok;
            Korumali = korumali ?? Korumalilik.Yok;
            Sayaclar = sayaclar;
            GenelSinir = genelSinir;
            Zincir = zincir;
            ZincirSiniri = zincirSiniri;
        }
    }
}
