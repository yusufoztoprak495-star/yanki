using System;

namespace Yanki.Core.Decision
{
    /// <summary>
    /// Üç çarpanın hesabı (ADR-011). Hepsi parçalı doğrusaldır ve nötr girdide 1.0 verir. Girdi önce kendi aralığına kırpılır,
    /// sonuç 6 ondalığa yuvarlanır ve en son <see cref="CarpanSiniri"/> ile kırpılır. Tanım yoksa (<c>null</c>) çarpan 1.0'dır.
    /// </summary>
    public static class CarpanHesaplayici
    {
        /// <summary>
        /// Kişilik çarpanı: özelliklerin (ters için <c>1−v</c>) ağırlıklı ortalaması <c>v</c>;
        /// 0 → 0.25, 0.5 → 1.0, 1 → 2.0 (parçalı doğrusal).
        /// </summary>
        public static decimal Kisilik(KisilikCarpaniTanimi? tanim, Mind.Kisilik kisilik)
        {
            if (tanim == null)
            {
                return 1m;
            }

            if (kisilik == null)
            {
                throw new ArgumentNullException(nameof(kisilik));
            }

            var toplam = 0m;
            var agirlikToplami = 0m;
            foreach (var o in tanim.Ozellikler)
            {
                var v = Kirp(kisilik.Deger(o.Ozellik), 0m, 1m);
                toplam += o.Agirlik * (o.Ters ? 1m - v : v);
                agirlikToplami += o.Agirlik;
            }

            var ort = toplam / agirlikToplami;
            var carpan = ort <= 0.5m ? 0.25m + (1.5m * ort) : 1m + (2m * (ort - 0.5m));
            return Sinirla(carpan, CarpanSiniri.KisilikAlt, CarpanSiniri.KisilikUst);
        }

        /// <summary>
        /// Fikir çarpanı: fikir −100..100'e kırpılır (<c>azalan</c> yönde işareti çevrilir);
        /// −100 → 0.6, 0 → 1.0, 100 → 1.5 (parçalı doğrusal).
        /// </summary>
        public static decimal Fikir(FikirCarpaniTanimi? tanim, decimal fikir)
        {
            if (tanim == null)
            {
                return 1m;
            }

            var t = Kirp(fikir, -100m, 100m) / 100m;
            if (tanim.Yon == FikirYonu.Azalan)
            {
                t = -t;
            }

            var carpan = t < 0m ? 1m + (0.4m * t) : 1m + (0.5m * t);
            return Sinirla(carpan, CarpanSiniri.FikirAlt, CarpanSiniri.FikirUst);
        }

        /// <summary>
        /// Ruh hali çarpanı: <c>Σ ağırlık × şiddet</c> (ters duygu eksi işaretli; şiddet 0..1'e kırpılır), toplam −1..1'e kırpılır;
        /// −1 → 0.8, 0 → 1.0, 1 → 1.25 (parçalı doğrusal).
        /// </summary>
        /// <param name="tanim">Tanım.</param>
        /// <param name="duyguSiddeti">Duygu türü → güncel şiddet.</param>
        public static decimal RuhHali(RuhHaliCarpaniTanimi? tanim, Func<string, decimal> duyguSiddeti)
        {
            if (tanim == null)
            {
                return 1m;
            }

            if (duyguSiddeti == null)
            {
                throw new ArgumentNullException(nameof(duyguSiddeti));
            }

            var toplam = 0m;
            foreach (var d in tanim.Duygular)
            {
                var s = Kirp(duyguSiddeti(d.Duygu), 0m, 1m);
                toplam += d.Agirlik * (d.Ters ? -s : s);
            }

            toplam = Kirp(toplam, -1m, 1m);
            var carpan = toplam < 0m ? 1m + (0.2m * toplam) : 1m + (0.25m * toplam);
            return Sinirla(carpan, CarpanSiniri.RuhHaliAlt, CarpanSiniri.RuhHaliUst);
        }

        private static decimal Sinirla(decimal carpan, decimal alt, decimal ust) =>
            Kirp(Egriler.Yuvarla(carpan), alt, ust);

        private static decimal Kirp(decimal d, decimal alt, decimal ust) => d < alt ? alt : d > ust ? ust : d;
    }
}
