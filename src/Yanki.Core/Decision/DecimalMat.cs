namespace Yanki.Core.Decision
{
    /// <summary>
    /// Elle yazılmış decimal matematik (ADR-007: <c>Math.Pow</c>/<c>Math.Exp</c> ve kayan nokta yasak).
    /// </summary>
    public static class DecimalMat
    {
        /// <summary><see cref="Us"/> girdisinin alt sınırı; altındaki girdi buna kırpılır.</summary>
        public const decimal UsAltSinir = -30m;

        /// <summary><see cref="Us"/> girdisinin üst sınırı; üstündeki girdi buna kırpılır.</summary>
        public const decimal UsUstSinir = 30m;

        // Sabit terim sayısı ve sabit kare alma sayısı: sonuç yalnızca girdiye bağlıdır, yakınsama denetimi yoktur.
        private const int TaylorTerimSayisi = 18;
        private const int KareAlmaSayisi = 6;
        private const decimal Bolen = 64m; // 2^KareAlmaSayisi

        /// <summary>
        /// <c>e^y</c>. <paramref name="y"/> önce <c>[−30, 30]</c> aralığına kırpılır (e^30 ≈ 1.07e13, decimal'e rahat sığar).
        /// Yöntem: <c>|y|/64</c> için 18 terimli Taylor serisi, sonra 6 kez kare alma; negatif girdide <c>1/e^|y|</c>.
        /// Göreli hata 1e-15'in çok altındadır (testte <c>tools/golden_ref.py</c> ile doğrulanır).
        /// </summary>
        public static decimal Us(decimal y)
        {
            if (y > UsUstSinir)
            {
                y = UsUstSinir;
            }
            else if (y < UsAltSinir)
            {
                y = UsAltSinir;
            }

            var negatif = y < 0m;
            var z = (negatif ? -y : y) / Bolen;

            var toplam = 1m;
            var terim = 1m;
            for (var i = 1; i <= TaylorTerimSayisi; i++)
            {
                terim = terim * z / i;
                toplam += terim;
            }

            for (var i = 0; i < KareAlmaSayisi; i++)
            {
                toplam *= toplam;
            }

            return negatif ? 1m / toplam : toplam;
        }
    }
}
