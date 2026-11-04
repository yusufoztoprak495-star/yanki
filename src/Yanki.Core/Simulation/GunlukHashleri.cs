using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Yanki.Core.Events;
using Yanki.Core.World;

namespace Yanki.Core.Simulation
{
    /// <summary>
    /// Bütün olay günlüğünün kanonik hash'i (<c>gunluk_hash</c>, ADR-013). Durum hash'inden (<c>son_hash</c>) farkı, durumu etkilemeyen her olayı da
    /// (<c>karar_verildi</c> ve fren olayları dahil) kapsamasıdır: seçici ya da karar izi değişirse durum aynı kalsa bile bu hash değişir.
    /// <c>son_hash</c> hesabı değişmez.
    /// </summary>
    public static class GunlukHashu
    {
        /// <summary>Günlüğün 64 bit hash'i (16 karakter küçük harfli onaltılık).</summary>
        public static string Hesapla(OlayGunlugu gunluk)
        {
            if (gunluk == null)
            {
                throw new ArgumentNullException(nameof(gunluk));
            }

            var sb = new StringBuilder();
            foreach (var o in gunluk.Olaylar)
            {
                sb.Append(o.Id.ToString(CultureInfo.InvariantCulture)).Append('\u001f')
                    .Append(o.Tick.ToString(CultureInfo.InvariantCulture)).Append('\u001f')
                    .Append(o.Sira.ToString(CultureInfo.InvariantCulture)).Append('\u001f')
                    .Append(o.Tur).Append('\u001f').Append(o.Fail).Append('\u001f').Append(o.Hedef).Append('\u001f').Append(o.Mekan).Append('\u001f')
                    .Append(o.KuralId).Append('\u001f');
                foreach (var kv in o.Yuk)
                {
                    sb.Append(kv.Key).Append('\u001e').Append(kv.Value).Append('\u001d');
                }

                sb.Append('\u001f');
                foreach (var n in o.NedenOlaylar)
                {
                    sb.Append(n.ToString(CultureInfo.InvariantCulture)).Append(',');
                }

                sb.Append('\n');
            }

            return Hash64.Hex(Hash64.Hesapla(sb.ToString()));
        }
    }

    /// <summary>
    /// Senaryo girdilerinin hash'i (<c>senaryo_hash</c>, ADR-013): senaryo, kural, eylem ve kriz dosyalarının METİNLERİ verilen sırayla.
    /// Satır sonları normalleştirilir (CRLF/CR → LF), böylece aynı içerik Windows ve Linux'ta aynı değeri verir. Çekirdek dosya okumaz;
    /// metinleri CLI verir.
    /// </summary>
    public static class SenaryoHashu
    {
        /// <summary>Metinlerin hash'i; sıra ve her metnin uzunluğu hash'e girer (iki dosyanın birleşimi tek dosyadan ayırt edilir).</summary>
        public static string Hesapla(IEnumerable<string> metinler)
        {
            if (metinler == null)
            {
                throw new ArgumentNullException(nameof(metinler));
            }

            var sb = new StringBuilder();
            foreach (var m in metinler)
            {
                var n = (m ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
                sb.Append(n.Length.ToString(CultureInfo.InvariantCulture)).Append('\u001f').Append(n).Append('\u001e');
            }

            return Hash64.Hex(Hash64.Hesapla(sb.ToString()));
        }
    }

    /// <summary>Kayıttaki <c>senaryo_hash</c> yüklenen senaryo girdilerinin hash'iyle tutmadı: senaryo, kural, eylem ya da kriz dosyaları kayıttan sonra değişmiş.</summary>
    public sealed class SenaryoDegistiHatasi : InvalidOperationException
    {
        /// <summary>Kayıttaki hash.</summary>
        public string Beklenen { get; }

        /// <summary>Şimdiki girdilerin hash'i.</summary>
        public string Gercek { get; }

        /// <summary>Kurar.</summary>
        public SenaryoDegistiHatasi(string beklenen, string gercek)
            : base($"Senaryo girdileri kayıttan sonra değişmiş: kayıtlı senaryo_hash {beklenen}, şimdiki {gercek}. Oturum bu senaryoyla yeniden oynatılamaz.")
        {
            Beklenen = beklenen;
            Gercek = gercek;
        }
    }
}
