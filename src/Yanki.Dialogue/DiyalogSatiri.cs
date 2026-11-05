using System;
using System.Collections.Generic;
using System.Text;
using Yanki.Core.World;

namespace Yanki.Dialogue
{
    /// <summary>
    /// Üretilen bir diyalog satırı. <c>KuralId</c> ya da eylem kimliği içermez (yalan gizliliği); neden satırları da yalnızca niyet, şablon ve kriter bilgisini taşır.
    /// </summary>
    public sealed class DiyalogSatiri
    {
        private readonly List<string> _neden;

        internal DiyalogSatiri(
            int no, long tick, long olayId, string konusan, string dinleyen, string sablonId, string metin, string varsayilanMetin, bool followup, List<string> neden, string mekan = "")
        {
            Mekan = mekan;
            No = no;
            Tick = tick;
            OlayId = olayId;
            Konusan = konusan;
            Dinleyen = dinleyen;
            SablonId = sablonId;
            Metin = metin;
            VarsayilanMetin = varsayilanMetin;
            Followup = followup;
            _neden = neden;
        }

        /// <summary>Dinleyensiz satırın mekanı (dinleyenli satırda boş); kimin gördüğünü demo süzer.</summary>
        public string Mekan { get; }

        /// <summary>Satır numarası (1'den, üretim sırasıyla; <c>--neden</c> bunu kullanır).</summary>
        public int No { get; }

        /// <summary>Satırın üretildiği tick (followup'ta ana olaydan bir sonraki tick).</summary>
        public long Tick { get; }

        /// <summary>Satırı doğuran olayın günlük kimliği.</summary>
        public long OlayId { get; }

        /// <summary>Konuşan NPC.</summary>
        public string Konusan { get; }

        /// <summary>Dinleyen.</summary>
        public string Dinleyen { get; }

        /// <summary>Seçilen şablonun kimliği.</summary>
        public string SablonId { get; }

        /// <summary>Gösterilecek metin (verilen renderer'ın çıktısı).</summary>
        public string Metin { get; }

        /// <summary>Varsayılan şablon renderer'ının çıktısı; <c>diyalog_hash</c> her zaman bundan hesaplanır (eklenti replay'i bozamaz).</summary>
        public string VarsayilanMetin { get; }

        /// <summary>Ana satırın followup'ı (cevap) mu.</summary>
        public bool Followup { get; }

        /// <summary>Neden satırları: niyet, seçilen şablon, tutan ve tutmayan kriterler, elenen şablonlar, hafıza göndermesi, followup notları (<c>--neden</c>).</summary>
        public IReadOnlyList<string> NedenSatirlari => _neden;

        internal void NotEkle(string not) => _neden.Add(not);
    }

    /// <summary>
    /// <c>diyalog_hash</c>: satırların (olay kimliği, konuşan, dinleyen, şablon, varsayılan renderer metni) kanonik metninin hash'i. CRLF/LF aynı değeri verir (metindeki satır sonları
    /// normalize edilir). Tick ve neden satırları hash'e girmez.
    /// </summary>
    public static class DiyalogHashu
    {
        /// <summary>Satırların 16 karakterlik onaltılık hash'i.</summary>
        public static string Hesapla(IEnumerable<DiyalogSatiri> satirlar)
        {
            if (satirlar == null)
            {
                throw new ArgumentNullException(nameof(satirlar));
            }

            var sb = new StringBuilder("DIYALOG|1\n");
            foreach (var s in satirlar)
            {
                sb.Append(s.OlayId.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('|')
                    .Append(s.Konusan).Append('|').Append(s.Dinleyen).Append('|').Append(s.Mekan).Append('|').Append(s.SablonId).Append('|')
                    .Append(s.VarsayilanMetin.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\\n")).Append('\n');
            }

            return Hash64.Hex(Hash64.Hesapla(sb.ToString()));
        }
    }
}
