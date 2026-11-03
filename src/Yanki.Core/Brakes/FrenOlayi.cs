using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Yanki.Core.Events;

namespace Yanki.Core.Brakes
{
    /// <summary>Fren türleri (<c>fren_uygulandi</c> olayının <c>fren</c> yükü ve <c>KuralId: fren:&lt;tur&gt;</c>).</summary>
    public static class FrenTurleri
    {
        /// <summary>Cooldown penceresi içindeki tetikleme.</summary>
        public const string Cooldown = "cooldown";

        /// <summary><c>en_fazla</c> sınırı aşıldı.</summary>
        public const string EnFazla = "en_fazla";

        /// <summary>Kuralın tick başına en fazla sınırı.</summary>
        public const string TickSiniri = "tick_siniri";

        /// <summary>Senaryonun tick başına en fazla sonuç sınırı (M).</summary>
        public const string GenelSinir = "genel_sinir";

        /// <summary>Zincir derinliği sınırı (D).</summary>
        public const string Zincir = "zincir";

        /// <summary>Korumalı varlık ihlali.</summary>
        public const string Korumali = "korumali";

        /// <summary>Kuralın başvurduğu rol olayda tanımsız (kural o olay için atlandı).</summary>
        public const string TanimsizRol = "tanimsiz_rol";

        /// <summary>Etki hedefi bir kişi (NPC ya da oyuncu) değil (ör. mekan); etki üretilmedi.</summary>
        public const string KisiOlmayanHedef = "kisi_olmayan_hedef";

        /// <summary>Yay değişkeni sınıra kırpıldı.</summary>
        public const string YaySiniri = "yay_siniri";
    }

    /// <summary>Bir frenin ne zaman, kim için, neye karşı uygulandığı; <see cref="FrenOlayi.Yap"/> bunu olaya çevirir.</summary>
    public sealed class FrenBilgisi
    {
        /// <summary><see cref="FrenTurleri"/> değerlerinden biri.</summary>
        public string Tur { get; }

        /// <summary>Freni uygulayan kural, eylem ya da değişken kimliği.</summary>
        public string Kimlik { get; }

        /// <summary>Etkilenen NPC; yoksa boş.</summary>
        public string Npc { get; }

        /// <summary>Hedef (eylem hedefi, korunan varlık, değişken adı); yoksa boş.</summary>
        public string Hedef { get; }

        /// <summary>Olayın geçtiği mekan (tetikleyicinin mekanı); yoksa boş.</summary>
        public string Mekan { get; }

        /// <summary>Engellenen tetikleyici olay (varsa).</summary>
        public IReadOnlyList<long> NedenOlaylar { get; }

        /// <summary>Fren bilgisi kurar.</summary>
        public FrenBilgisi(string tur, string kimlik, string npc, string hedef, string mekan, IEnumerable<long> nedenOlaylar)
        {
            if (string.IsNullOrEmpty(tur))
            {
                throw new ArgumentException("Fren türü boş olamaz.", nameof(tur));
            }

            Tur = tur;
            Kimlik = kimlik ?? string.Empty;
            Npc = npc ?? string.Empty;
            Hedef = hedef ?? string.Empty;
            Mekan = mekan ?? string.Empty;
            NedenOlaylar = new ReadOnlyCollection<long>(new List<long>(nedenOlaylar ?? new long[0]));
        }
    }

    /// <summary>
    /// <c>fren_uygulandi</c> olayı (7. hafta, ADR-013). Yük: <c>fren</c>, <c>kimlik</c>, <c>npc</c>, <c>hedef</c>; <c>KuralId</c>
    /// <c>fren:&lt;tur&gt;</c>; <c>NedenOlaylar</c> engellenen tetikleyici. Algılanmaz ve kural tetiklemez: bir zihin olayı değildir
    /// (<c>ZihinOlayiMi</c>'ye girmez) ve hiçbir tetik kuyruğuna alınmaz.
    /// </summary>
    public static class FrenOlayi
    {
        /// <summary>Olay türü.</summary>
        public const string Tur = "fren_uygulandi";

        /// <summary>Fren bilgisini günlüğe eklenecek olaya çevirir.</summary>
        public static Olay Yap(long id, long tick, int sira, FrenBilgisi fren)
        {
            if (fren == null)
            {
                throw new ArgumentNullException(nameof(fren));
            }

            return new Olay(
                id, tick, sira, Tur, string.Empty, string.Empty, fren.Mekan,
                new[]
                {
                    new KeyValuePair<string, string>("fren", fren.Tur),
                    new KeyValuePair<string, string>("kimlik", fren.Kimlik),
                    new KeyValuePair<string, string>("npc", fren.Npc),
                    new KeyValuePair<string, string>("hedef", fren.Hedef),
                },
                fren.NedenOlaylar,
                "fren:" + fren.Tur);
        }
    }
}
