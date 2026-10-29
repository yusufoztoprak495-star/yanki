using System;

namespace Yanki.Core.Mind
{
    /// <summary>Kanıt türü (plan 5.4): kendi gözüyle gördü, birinden duydu, çıkarım yaptı.</summary>
    public enum KanitTuru
    {
        /// <summary>Doğrudan algıyla görüldü (<c>gordu</c>).</summary>
        Gordu,

        /// <summary>Birinden duyuldu (<c>duydu</c>).</summary>
        Duydu,

        /// <summary>Çıkarımla elde edildi (<c>cikarim</c>).</summary>
        Cikarim,
    }

    /// <summary><see cref="KanitTuru"/> ile olay yükündeki ASCII adlar arasında çeviri.</summary>
    public static class KanitTurleri
    {
        /// <summary>Türün olay yükündeki adı.</summary>
        public static string Ad(KanitTuru tur)
        {
            switch (tur)
            {
                case KanitTuru.Gordu: return "gordu";
                case KanitTuru.Duydu: return "duydu";
                default: return "cikarim";
            }
        }

        /// <summary>Adı türe çevirir.</summary>
        /// <exception cref="ArgumentException">Ad bilinmiyorsa.</exception>
        public static KanitTuru Coz(string ad)
        {
            switch (ad)
            {
                case "gordu": return KanitTuru.Gordu;
                case "duydu": return KanitTuru.Duydu;
                case "cikarim": return KanitTuru.Cikarim;
                default: throw new ArgumentException($"Bilinmeyen kanıt türü: \"{ad}\" (gordu, duydu, cikarim).");
            }
        }
    }

    /// <summary>Süreli fikir modifier'ı (<c>minnet</c>, <c>hakaret_etti</c> ...). Değişmez.</summary>
    public sealed class FikirModifier
    {
        /// <summary>Türü; aynı türden kopyalar birikir.</summary>
        public string Tur { get; }

        /// <summary>Etki değeri (fikre eklenen, kopya çarpanından önce).</summary>
        public decimal Deger { get; }

        /// <summary>Başlangıç tick'i.</summary>
        public long BaslangicTick { get; }

        /// <summary>Süre (tick); <c>[başlangıç, başlangıç + süre)</c> aralığında aktiftir.</summary>
        public long Sure { get; }

        /// <summary>Modifier'ı doğuran (<c>modifier_eklendi</c>) olayın kimliği.</summary>
        public long NedenOlayId { get; }

        /// <summary>Yeni modifier.</summary>
        public FikirModifier(string tur, decimal deger, long baslangicTick, long sure, long nedenOlayId)
        {
            Tur = tur ?? throw new ArgumentNullException(nameof(tur));
            Deger = deger;
            BaslangicTick = baslangicTick;
            Sure = sure;
            NedenOlayId = nedenOlayId;
        }
    }

    /// <summary>Baskın duygu: tür, başlangıç şiddeti, hedef (boş olabilir) ve neden olay. Değişmez.</summary>
    public sealed class Duygu
    {
        /// <summary>Duygu türü (<c>ofke</c>, <c>korku</c> ...).</summary>
        public string Tur { get; }

        /// <summary>Başlangıç şiddeti (0..1); güncel şiddet <see cref="Sonumleme.DuyguSiddeti"/> ile hesaplanır.</summary>
        public decimal Siddet { get; }

        /// <summary>Duygunun yöneldiği varlık; yoksa boş metin.</summary>
        public string Hedef { get; }

        /// <summary>Duygunun başladığı tick.</summary>
        public long BaslangicTick { get; }

        /// <summary>Duyguyu doğuran (<c>duygu_degisti</c>) olayın kimliği.</summary>
        public long NedenOlayId { get; }

        /// <summary>Yeni duygu.</summary>
        public Duygu(string tur, decimal siddet, string hedef, long baslangicTick, long nedenOlayId)
        {
            Tur = tur ?? throw new ArgumentNullException(nameof(tur));
            Siddet = siddet;
            Hedef = hedef ?? string.Empty;
            BaslangicTick = baslangicTick;
            NedenOlayId = nedenOlayId;
        }
    }

    /// <summary>Stres değeri ve ölçüldüğü tick; sönme bu noktadan başlar. Değişmez.</summary>
    public sealed class StresDurumu
    {
        /// <summary>Ölçüm tick'indeki stres (0..100).</summary>
        public decimal Deger { get; }

        /// <summary>Ölçüm tick'i.</summary>
        public long Tick { get; }

        /// <summary>Yeni stres durumu.</summary>
        public StresDurumu(decimal deger, long tick)
        {
            Deger = deger;
            Tick = tick;
        }
    }

    /// <summary>Bir inancı destekleyen kanıt. Silinmez; gücü zamanla zayıflar. Değişmez.</summary>
    public sealed class Kanit
    {
        /// <summary>Kanıt türü.</summary>
        public KanitTuru Tur { get; }

        /// <summary>Kaynak: <c>duydu</c> için anlatan, <c>gordu</c> için NPC'nin kendisi, <c>cikarim</c> için çıkarımı yapan.</summary>
        public string Kaynak { get; }

        /// <summary>Kanıtın eklendiği tick.</summary>
        public long Tick { get; }

        /// <summary>Başlangıç gücü (0..1).</summary>
        public decimal Guc { get; }

        /// <summary>Kanıtı ekleyen (<c>kanit_eklendi</c>) olayın kimliği.</summary>
        public long OlayId { get; }

        /// <summary>Yeni kanıt.</summary>
        public Kanit(KanitTuru tur, string kaynak, long tick, decimal guc, long olayId)
        {
            Tur = tur;
            Kaynak = kaynak ?? throw new ArgumentNullException(nameof(kaynak));
            Tick = tick;
            Guc = guc;
            OlayId = olayId;
        }
    }

    /// <summary>Hafıza kaydı: olay kimliği ve önem (0..1). Değişmez.</summary>
    public sealed class HafizaKaydi
    {
        /// <summary>Hatırlanan olayın günlükteki kimliği.</summary>
        public long OlayId { get; }

        /// <summary>Kayıt anındaki önem.</summary>
        public decimal Onem { get; }

        /// <summary>Kaydın eklendiği tick.</summary>
        public long Tick { get; }

        /// <summary>Yeni kayıt.</summary>
        public HafizaKaydi(long olayId, decimal onem, long tick)
        {
            OlayId = olayId;
            Onem = onem;
            Tick = tick;
        }
    }
}
