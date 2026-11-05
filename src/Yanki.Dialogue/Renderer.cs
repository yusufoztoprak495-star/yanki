using System;
using System.Collections.Generic;
using Yanki.Core.World;
using Yanki.Dialogue.Gramer;

namespace Yanki.Dialogue
{
    /// <summary>
    /// Bir yuvaya (konusan, dinleyen, konu, hafiza.zaman ...) bağlanmış gerçek: ya ad (ek alabilir) ya düz metin. "Seçilmiş gerçekler" yalnızca bunlardır (H kararı):
    /// motor neyin söyleneceğini belirler, renderer yalnızca söyleyişi seçer.
    /// </summary>
    public sealed class SecilmisGercek
    {
        internal SecilmisGercek(string ad, string deger, bool adMi, bool ozelAd, bool ince = false, string? yumusak = null)
        {
            Yumusak = yumusak;
            Ad = ad;
            Deger = deger;
            AdMi = adMi;
            OzelAd = ozelAd;
            Ince = ince;
        }

        /// <summary>Yuva adı (konusan, dinleyen, oyuncu, konu, hafiza.zaman, hafiza.fail).</summary>
        public string Ad { get; }

        /// <summary>Görünen değer (Ayşe, hancı, "dün", konu cümlesi).</summary>
        public string Deger { get; }

        /// <summary>Ek alabilen bir ad mı.</summary>
        public bool AdMi { get; }

        /// <summary>Özel ad ise ekten önce kesme işareti gelir.</summary>
        public bool OzelAd { get; }

        /// <summary>Yumuşak l (Kemal): arka ünlüyle yazılsa da ince ünlü eki alır.</summary>
        public bool Ince { get; }

        /// <summary>p, ç, t, k ile biten cins isim için yazarın verdiği yumuşak gövde (borç → borc); yoksa boş.</summary>
        public string? Yumusak { get; }
    }

    /// <summary>
    /// Renderer'ın göreceği HER ŞEY (H kararı, beyaz liste, reflection testiyle kilitli): niyet, seçilen şablon metni, seçilmiş gerçekler ve gerekçe.
    /// Simülasyon, Zihin, Olay, karar izi ya da KuralId BULUNMAZ; çıktı tek yönlüdür ve simülasyona ya da tekrar durumuna geri yazılmaz.
    /// </summary>
    public sealed class RenderGirdisi
    {
        internal RenderGirdisi(Niyet niyet, string sablonMetni, IReadOnlyList<SecilmisGercek> secilmisGercekler)
        {
            Niyet = niyet;
            SablonMetni = sablonMetni;
            SecilmisGercekler = secilmisGercekler;
            Gerekce = niyet.Gerekce;
        }

        /// <summary>Niyet.</summary>
        public Niyet Niyet { get; }

        /// <summary>Seçilen şablonun ham metni (yuva ve gramer etiketleri açılmamış).</summary>
        public string SablonMetni { get; }

        /// <summary>Seçilmiş gerçekler.</summary>
        public IReadOnlyList<SecilmisGercek> SecilmisGercekler { get; }

        /// <summary>Gösterilmesine izin verilen gerekçe.</summary>
        public NiyetGerekcesi Gerekce { get; }
    }

    /// <summary>
    /// Diyalog metnini üreten eklenti arayüzü (plan 5.7). Eklenti metni yapay zekayla yazdırabilir, ama niyeti, içeriği ve gerekçeyi motor belirler. LLM kodu ve ağ erişimi bu
    /// hafta YOKTUR; yalnızca bu arayüz ve şablon tabanlı varsayılan uygulama (<see cref="SablonRenderer"/>) vardır. Eş zamanlı imza (ADR-014): Task tabanlı bir uygulama v0.2 sonrası
    /// ayrı bir arayüzle eklenebilir.
    /// </summary>
    public interface IDialogueRenderer
    {
        /// <summary>Girdiden tek bir satır metni üretir. Çıktı simülasyona geri yazılmaz.</summary>
        string Render(RenderGirdisi girdi);
    }

    /// <summary>
    /// Şablon tabanlı varsayılan renderer: şablon metnindeki gramer kurallarını ve yuvaları açar. Yalnızca anahtarlı RNG kullanır (seed, niyet tick'i, konuşan, olay kimliği);
    /// kültüre bağlı hiçbir dönüşüm yoktur. <c>diyalog_hash</c> her zaman bunun çıktısından hesaplanır.
    /// </summary>
    public sealed class SablonRenderer : IDialogueRenderer
    {
        private readonly SablonDosyasi _dosya;
        private readonly ulong _seed;

        /// <summary>Renderer'ı şablon dosyasının gramerine ve verilen seed'e bağlar.</summary>
        public SablonRenderer(SablonDosyasi dosya, ulong seed)
        {
            _dosya = dosya ?? throw new ArgumentNullException(nameof(dosya));
            _seed = seed;
        }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Bir yuva bağlanamıyor ya da bir ek getirilemiyorsa (şablon aday olamaz).</exception>
        public string Render(RenderGirdisi girdi)
        {
            if (girdi == null)
            {
                throw new ArgumentNullException(nameof(girdi));
            }

            var olgular = new SortedDictionary<string, MetinOlgusu>(StringComparer.Ordinal);
            foreach (var g in girdi.SecilmisGercekler)
            {
                olgular[g.Ad] = g.AdMi ? MetinOlgusu.Ad(g.Deger, g.OzelAd, g.Ince, g.Yumusak) : MetinOlgusu.Metin(g.Deger);
            }

            var rng = AnahtarliRng.Icin(_seed, girdi.Niyet.Tick, girdi.Niyet.Konusan, DiyalogMotoru.RngAlani(girdi.Niyet) + ":metin");
            var turkce = _dosya.Dil == "tr";
            return TurkceEkler.BasHarfBuyut(GramerAcilimi.Ac(girdi.SablonMetni, _dosya.Gramer, olgular, rng, turkce), turkce);
        }
    }
}
