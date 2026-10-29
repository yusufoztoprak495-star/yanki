using System;

namespace Yanki.Core.Criteria
{
    /// <summary>Tek bir kriterin değerlendirme sonucu: tuttu mu, ne gözlendi, puana ne kattı.</summary>
    public sealed class KriterSonucu
    {
        /// <summary>Değerlendirilen kriter.</summary>
        public Kriter Kriter { get; }

        /// <summary>Kriter tuttu mu.</summary>
        public bool Tuttu { get; }

        /// <summary>Çözücünün döndürdüğü gözlenen değer.</summary>
        public SorguDegeri Gozlenen { get; }

        /// <summary>Puana katkı: tuttuysa kriterin ağırlığı, tutmadıysa 0.</summary>
        public decimal Katki { get; }

        /// <summary>Yeni sonuç.</summary>
        public KriterSonucu(Kriter kriter, bool tuttu, SorguDegeri gozlenen)
        {
            Kriter = kriter ?? throw new ArgumentNullException(nameof(kriter));
            Tuttu = tuttu;
            Gozlenen = gozlenen;
            Katki = tuttu ? kriter.Agirlik : 0m;
        }
    }

    /// <summary>Bir kriteri bir çözücü ve bağlamla değerlendirir.</summary>
    public static class KriterDegerlendirici
    {
        /// <summary>Kriteri değerlendirir.</summary>
        /// <exception cref="InvalidOperationException">Çözücü beklenmeyen türde değer döndürürse ya da bir rol bağlamda tanımsızsa.</exception>
        public static KriterSonucu Degerlendir(Kriter kriter, ISorguCozucu cozucu, SorguBaglami baglam)
        {
            if (kriter == null)
            {
                throw new ArgumentNullException(nameof(kriter));
            }

            if (cozucu == null)
            {
                throw new ArgumentNullException(nameof(cozucu));
            }

            if (baglam == null)
            {
                throw new ArgumentNullException(nameof(baglam));
            }

            var tanim = SorguSozlugu.Bul(kriter.Alan.Ad)!;
            var gozlenen = cozucu.Coz(kriter.Alan, baglam);
            var beklenen = tanim.Donus == DonusTuru.Sayi ? DegerTuru.Sayi
                : tanim.Donus == DonusTuru.Mantiksal ? DegerTuru.Mantiksal
                : DegerTuru.Metin;
            if (gozlenen.Tur != beklenen)
            {
                throw new InvalidOperationException(
                    $"'{tanim.Ad}' çözücüsü {beklenen} türünde değer döndürmeliydi, {gozlenen.Tur} döndü.");
            }

            return new KriterSonucu(kriter, Karsilastir(kriter, tanim, gozlenen, baglam), gozlenen);
        }

        private static bool Karsilastir(Kriter k, FonksiyonTanimi tanim, SorguDegeri gozlenen, SorguBaglami baglam)
        {
            switch (tanim.Donus)
            {
                case DonusTuru.Sayi:
                    return SayiKarsilastir(k, gozlenen.SayiDegeri);
                case DonusTuru.Mantiksal:
                    return Esitlik(k.Op, gozlenen.MantiksalDegeri == k.Deger.MantiksalDegeri);
                case DonusTuru.Bant:
                    return Esitlik(k.Op, string.Equals(gozlenen.MetinDegeri, k.Deger.MetinDegeri, StringComparison.Ordinal));
                default:
                    var hedef = RolAdlari.TryCoz(k.Deger.MetinDegeri, out var rol) ? baglam.Coz(rol) : k.Deger.MetinDegeri;
                    return Esitlik(k.Op, string.Equals(gozlenen.MetinDegeri, hedef, StringComparison.Ordinal));
            }
        }

        private static bool Esitlik(KriterOperatoru op, bool esit) => op == KriterOperatoru.Esit ? esit : !esit;

        private static bool SayiKarsilastir(Kriter k, decimal x)
        {
            var d = k.Deger.SayiDegeri;
            switch (k.Op)
            {
                case KriterOperatoru.Kucuk: return x < d;
                case KriterOperatoru.KucukEsit: return x <= d;
                case KriterOperatoru.Buyuk: return x > d;
                case KriterOperatoru.BuyukEsit: return x >= d;
                case KriterOperatoru.Esit: return x == d;
                case KriterOperatoru.EsitDegil: return x != d;
                default: return x >= d && x <= k.Deger.UstSinir;
            }
        }
    }
}
