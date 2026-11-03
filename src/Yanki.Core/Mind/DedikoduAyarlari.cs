using System;

namespace Yanki.Core.Mind
{
    /// <summary>
    /// Dedikodu (konu seçimi ve aktarım gücü) sayıları (plan 5.3, ADR-012). Bütün değerler Yusuf'un denge alanıdır; varsayılanlar
    /// Yusuf'un 6. hafta onayıdır. 1 tick = 1 oyun saati olduğundan yenilik penceresi 168 bir haftadır.
    /// </summary>
    public sealed class DedikoduAyarlari
    {
        /// <summary>Yenilik <c>1 − (tick − kanıt_tick) / YenilikPenceresi</c> (alt sınır 0).</summary>
        public long YenilikPenceresi { get; init; } = 168;

        /// <summary>Konu puanı bu değerin altındaysa konu anlatılmaz.</summary>
        public decimal KonuEsigi { get; init; } = 0.1m;

        /// <summary>İlgi: dinleyen önermede geçiyorsa.</summary>
        public decimal IlgiDinleyen { get; init; } = 1m;

        /// <summary>İlgi: konuşanın inandığı başka bir önerme dinleyeni konuya bağlıyorsa (tek adım).</summary>
        public decimal IlgiBagli { get; init; } = 0.7m;

        /// <summary>İlgi: konuşan önermede geçiyorsa.</summary>
        public decimal IlgiKonusan { get; init; } = 0.5m;

        /// <summary>İlgi: hiçbiri değilse.</summary>
        public decimal IlgiYok { get; init; } = 0.25m;

        /// <summary>Önermeye bağlı olayın hafıza kaydı yoksa kullanılan önem.</summary>
        public decimal VarsayilanOnem { get; init; } = 0.5m;

        /// <summary><c>güven_çarpanı = min(Üst, Taban + Eğim × güven)</c>; güven dinleyenin konuşana duyduğu <c>guven</c> eksenidir.</summary>
        public decimal GuvenCarpaniTaban { get; init; } = 0.7m;

        /// <summary>Güven çarpanının eksen başına eğimi.</summary>
        public decimal GuvenCarpaniEgim { get; init; } = 0.003m;

        /// <summary>Güven çarpanının üst sınırı; aktarılan güç kaynak kanıtın gücünü geçemez.</summary>
        public decimal GuvenCarpaniUst { get; init; } = 0.95m;

        /// <summary>Aktarılan güç bunun altındaysa dinleyene kanıt yazılmaz (söylenti ölür).</summary>
        public decimal GucEsigi { get; init; } = 0.15m;

        /// <summary>Konuşan yalnızca inandığı ve ham güveni bu değerde ya da üstünde olan önermeyi doğru diye anlatır.</summary>
        public decimal AnlatmaGuveniEsigi { get; init; } = 0.3m;

        /// <summary>Oyuncunun anlattığı önermenin kendi kanıt gücü (oyuncunun zihni yoktur).</summary>
        public decimal OyuncuKanitGucu { get; init; } = 1m;

        /// <summary>Konu sınırı <c>KonuSinirTaban + ⌊sosyallik × KonuSinirSosyallik⌋</c> (+1 fikir bandı yakin ya da dost ise).</summary>
        public int KonuSinirTaban { get; init; } = 1;

        /// <summary>Sosyallik katsayısı.</summary>
        public int KonuSinirSosyallik { get; init; } = 2;

        /// <summary>Ayarların tutarlılığını denetler.</summary>
        /// <exception cref="ArgumentException">Değer aralığı geçersizse.</exception>
        public void Dogrula()
        {
            if (YenilikPenceresi <= 0)
            {
                throw new ArgumentException("YenilikPenceresi pozitif olmalı.");
            }

            ZeroOne(KonuEsigi, nameof(KonuEsigi));
            ZeroOne(IlgiDinleyen, nameof(IlgiDinleyen));
            ZeroOne(IlgiBagli, nameof(IlgiBagli));
            ZeroOne(IlgiKonusan, nameof(IlgiKonusan));
            ZeroOne(IlgiYok, nameof(IlgiYok));
            ZeroOne(VarsayilanOnem, nameof(VarsayilanOnem));
            ZeroOne(GuvenCarpaniUst, nameof(GuvenCarpaniUst));
            ZeroOne(GucEsigi, nameof(GucEsigi));
            ZeroOne(AnlatmaGuveniEsigi, nameof(AnlatmaGuveniEsigi));
            ZeroOne(OyuncuKanitGucu, nameof(OyuncuKanitGucu));
            if (GuvenCarpaniTaban < 0m || GuvenCarpaniTaban > 1m)
            {
                throw new ArgumentException("GuvenCarpaniTaban 0..1 aralığında olmalı.");
            }

            if (KonuSinirTaban < 1 || KonuSinirSosyallik < 0)
            {
                throw new ArgumentException("KonuSinirTaban en az 1, KonuSinirSosyallik negatif olamaz.");
            }
        }

        private static void ZeroOne(decimal deger, string ad)
        {
            if (deger < 0m || deger > 1m)
            {
                throw new ArgumentException($"{ad} 0..1 aralığında olmalı.");
            }
        }
    }
}
