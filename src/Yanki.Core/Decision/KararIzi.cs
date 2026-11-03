using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Yanki.Core.Decision
{
    /// <summary>Seçim ayarları: puan ağırlıklı seçimin havuzu. <c>EnIyiN = 1</c> deterministik argmax demektir.</summary>
    public sealed class SecimAyarlari
    {
        /// <summary>Varsayılan havuz büyüklüğü.</summary>
        public const int VarsayilanN = 3;

        /// <summary>Varsayılan eşik: en yüksek puanın yüzdesi.</summary>
        public const decimal VarsayilanEsik = 0.7m;

        /// <summary>Havuzdaki en fazla aday sayısı, en az 1.</summary>
        public int EnIyiN { get; }

        /// <summary>Havuza girmek için en yüksek puanın en az bu oranına ulaşmak gerekir, (0, 1].</summary>
        public decimal Esik { get; }

        /// <summary>Ayar kurar.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="enIyiN"/> 1'den küçükse ya da <paramref name="esik"/> (0, 1] dışındaysa.</exception>
        public SecimAyarlari(int enIyiN = VarsayilanN, decimal esik = VarsayilanEsik)
        {
            if (enIyiN < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(enIyiN), enIyiN, "Havuz büyüklüğü (N) en az 1 olmalı.");
            }

            if (esik <= 0m || esik > 1m)
            {
                throw new ArgumentOutOfRangeException(nameof(esik), esik, "Eşik 0'dan büyük ve en çok 1 olmalı.");
            }

            EnIyiN = enIyiN;
            Esik = esik;
        }
    }

    /// <summary>
    /// Karar izindeki bir aday: ya elenmiştir (<see cref="Elendi"/>, <see cref="Neden"/>), ya da puanlanmıştır (<see cref="Puan"/>,
    /// <see cref="Sira"/> puan sırası, <see cref="HavuzdaMi"/>). Puanlamayan seçiciler (GOAP/BT adaptörleri) <see cref="Puan"/>'ı boş bırakabilir.
    /// </summary>
    public sealed class AdayIzi
    {
        /// <summary>Aday.</summary>
        public Aday Aday { get; }

        /// <summary>Ön koşul ya da hedef yokluğu nedeniyle elendi mi.</summary>
        public bool Elendi { get; }

        /// <summary>Elenme nedeni; elenmediyse boş.</summary>
        public string Neden { get; }

        /// <summary>Puan bileşenleri; elenen adayda (ya da puanlamayan seçicide) <c>null</c>.</summary>
        public AdayPuani? Puan { get; }

        /// <summary>Puan sırası, 1'den başlar (puan azalan, eşitlikte eylem kimliği ve hedef ordinal); elenende 0.</summary>
        public int Sira { get; }

        /// <summary>Top-N havuzunda mı.</summary>
        public bool HavuzdaMi { get; }

        /// <summary>Kurar.</summary>
        public AdayIzi(Aday aday, bool elendi, string neden, AdayPuani? puan, int sira, bool havuzdaMi)
        {
            Aday = aday ?? throw new ArgumentNullException(nameof(aday));
            Elendi = elendi;
            Neden = neden ?? string.Empty;
            Puan = puan;
            Sira = sira;
            HavuzdaMi = havuzdaMi;
        }
    }

    /// <summary>
    /// Karar izi (plan 5.5): bütün adaylar, elenenler ve nedeni, ölçüt katkıları, üç çarpan, son puan, top-N havuzu, zar, seçilen,
    /// seçilenin çeliştiği kişilik özellikleri (5. hafta: yalnızca görünür, stres yazılmaz) ve karara katkı veren zihin kaynakları.
    /// Değişmez veri nesnesidir; <c>karar_verildi</c> olayının yükü bundan kanonik JSON olarak üretilir.
    /// </summary>
    public sealed class KararIzi
    {
        /// <summary>Karar veren NPC.</summary>
        public string Npc { get; }

        /// <summary>Kararın verildiği tick.</summary>
        public long Tick { get; }

        /// <summary>Kullanılan seçim ayarları.</summary>
        public SecimAyarlari Ayarlar { get; }

        /// <summary>Bütün adaylar (elenenler dahil), eylem kimliği sonra hedef, ordinal sıralı.</summary>
        public IReadOnlyList<AdayIzi> Adaylar { get; }

        /// <summary>Top-N havuzunun aday anahtarları (<c>eylem|hedef</c>), seçim sırasıyla (puan azalan).</summary>
        public IReadOnlyList<string> Havuz { get; }

        /// <summary>Havuzdan seçerken atılan zar (0..1, 6 ondalık); zar atılmadıysa (havuz 0 ya da 1 aday) <c>null</c>.</summary>
        public decimal? Zar { get; }

        /// <summary>Seçilen aday; seçilemediyse (hepsi 0) <c>null</c>.</summary>
        public Aday? Secilen { get; }

        /// <summary>Seçilen eylemin çeliştiği kişilik özellikleri, ordinal sıralı; seçim yoksa boş.</summary>
        public IReadOnlyList<string> CelisenOzellikler { get; }

        /// <summary>Seçilen eylem uygulanırsa konuşanın stresine eklenecek miktar (<c>stres_tabani × Σ çelişen özellik</c>); yoksa 0.</summary>
        public decimal StresArtisi { get; }

        /// <summary>Karara katkı veren zihin kaynaklarının olay kimlikleri (aktif modifier'ı doğuran olay, ruh halinin nedeni vb.), artan ve tekrarsız.</summary>
        public IReadOnlyList<long> KaynakOlaylar { get; }

        /// <summary>Kaynak olayı bulunamayan girdiler (ör. birikmiş eksen değeri); uydurulmaz, burada raporlanır.</summary>
        public IReadOnlyList<string> Kaynaksiz { get; }

        /// <summary>Kurar.</summary>
        public KararIzi(
            string npc,
            long tick,
            SecimAyarlari ayarlar,
            IEnumerable<AdayIzi> adaylar,
            IEnumerable<string> havuz,
            decimal? zar,
            Aday? secilen,
            IEnumerable<string> celisenOzellikler,
            IEnumerable<long> kaynakOlaylar,
            IEnumerable<string> kaynaksiz,
            decimal stresArtisi = 0m)
        {
            StresArtisi = stresArtisi;
            Npc = npc ?? throw new ArgumentNullException(nameof(npc));
            Tick = tick;
            Ayarlar = ayarlar ?? throw new ArgumentNullException(nameof(ayarlar));
            Adaylar = new ReadOnlyCollection<AdayIzi>(new List<AdayIzi>(adaylar ?? throw new ArgumentNullException(nameof(adaylar))));
            Havuz = new ReadOnlyCollection<string>(new List<string>(havuz ?? throw new ArgumentNullException(nameof(havuz))));
            Zar = zar;
            Secilen = secilen;
            CelisenOzellikler = new ReadOnlyCollection<string>(new List<string>(celisenOzellikler ?? throw new ArgumentNullException(nameof(celisenOzellikler))));
            var k = new SortedSet<long>(kaynakOlaylar ?? throw new ArgumentNullException(nameof(kaynakOlaylar)));
            KaynakOlaylar = new ReadOnlyCollection<long>(new List<long>(k));
            Kaynaksiz = new ReadOnlyCollection<string>(new List<string>(kaynaksiz ?? throw new ArgumentNullException(nameof(kaynaksiz))));
        }

        /// <summary>
        /// Puanlamayan bir seçici (ör. bir GOAP/BT adaptörü ya da test seçicisi) için en küçük iz: seçilen aday ve onun çelişen özellikleri.
        /// </summary>
        public static KararIzi Basit(KararBaglami baglam, Aday secilen)
        {
            if (baglam == null)
            {
                throw new ArgumentNullException(nameof(baglam));
            }

            if (secilen == null)
            {
                throw new ArgumentNullException(nameof(secilen));
            }

            return new KararIzi(
                baglam.Npc,
                baglam.Tick,
                new SecimAyarlari(),
                new[] { new AdayIzi(secilen, false, string.Empty, null, 1, true) },
                new[] { secilen.Anahtar },
                null,
                secilen,
                secilen.Eylem.CelisenOzellikler,
                new long[0],
                new string[0],
                secilen.Eylem.StresArtisi(baglam.Zihin.Kisilik));
        }
    }

    /// <summary>Seçicinin sonucu: seçilen aday (yoksa <c>null</c>) ve karar izi.</summary>
    public sealed class SecimSonucu
    {
        /// <summary>Seçilen aday; hiçbiri seçilemediyse <c>null</c>.</summary>
        public Aday? Secilen { get; }

        /// <summary>Karar izi.</summary>
        public KararIzi Iz { get; }

        /// <summary>Kurar.</summary>
        public SecimSonucu(Aday? secilen, KararIzi iz)
        {
            Secilen = secilen;
            Iz = iz ?? throw new ArgumentNullException(nameof(iz));
        }
    }

    /// <summary>
    /// Eylem seçici arayüzü (plan 5.5). Bir oyun, <see cref="UtilitySelector"/> yerine kendi GOAP ya da davranış ağacı çözümünü takabilir;
    /// eylem kataloğu, zihin sorguları ve olay üretimi aynı kalır. Simülasyon yalnızca bu arayüzün sonucunu (seçilen aday ve iz) kullanır.
    /// </summary>
    public interface IActionSelector
    {
        /// <summary>
        /// Karar verenin adayları arasından birini seçer. <paramref name="adaylar"/> ön koşulları tutan adaylardır (eylem kimliği sonra hedef
        /// sıralı); <paramref name="elenenler"/> izde göstermek içindir.
        /// </summary>
        SecimSonucu Sec(KararBaglami baglam, IReadOnlyList<Aday> adaylar, IReadOnlyList<ElenenAday> elenenler);
    }
}
