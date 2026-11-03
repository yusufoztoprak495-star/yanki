using System;
using System.Collections.Generic;

namespace Yanki.Core.Mind
{
    /// <summary>
    /// Stres kriz eşikleri ve kriz sonrası düşüşler (plan 5.4, ADR-012). <see cref="Esikler"/> artan sıralıdır; <c>seviye k</c> (1 tabanlı)
    /// <c>Esikler[k-1]</c> eşiğine ve <c>Dususler[k-1]</c> düşüşüne karşılık gelir. Değerler Yusuf'un denge alanıdır.
    /// </summary>
    public sealed class StresAyarlari
    {
        /// <summary>Kriz eşikleri, artan (0..100).</summary>
        public IReadOnlyList<decimal> Esikler { get; init; } = new[] { 50m, 80m };

        /// <summary>Her seviyede kriz sonrası stres düşüşü; <see cref="Esikler"/> ile aynı sayıda.</summary>
        public IReadOnlyList<decimal> Dususler { get; init; } = new[] { 30m, 60m };

        /// <summary>
        /// Değişmez: kriz sonrası stres her seviyede en alt eşiğin altında kalmalı, yoksa her tick yeniden kriz çıkar.
        /// Üst seviye dışındaki her i için <c>Esikler[i+1] − Dususler[i] ≤ Esikler[0]</c>; en üst seviye için <c>100 − Dususler[son] &lt; Esikler[0]</c>.
        /// </summary>
        /// <exception cref="ArgumentException">Ayarlar tutarsızsa.</exception>
        public void Dogrula()
        {
            if (Esikler == null || Dususler == null || Esikler.Count == 0 || Esikler.Count != Dususler.Count)
            {
                throw new ArgumentException("Esikler ve Dususler aynı sayıda ve en az bir öğeli olmalı.");
            }

            for (var i = 0; i < Esikler.Count; i++)
            {
                if (Esikler[i] <= 0m || Esikler[i] > 100m)
                {
                    throw new ArgumentException($"Esikler[{i}] 0 ile 100 arasında (0 hariç) olmalı: {Esikler[i]}.");
                }

                if (i > 0 && Esikler[i] <= Esikler[i - 1])
                {
                    throw new ArgumentException("Esikler kesin artan olmalı.");
                }

                if (Dususler[i] <= 0m)
                {
                    throw new ArgumentException($"Dususler[{i}] pozitif olmalı.");
                }
            }

            var son = Esikler.Count - 1;
            for (var i = 0; i < son; i++)
            {
                if (Esikler[i + 1] - Dususler[i] > Esikler[0])
                {
                    throw new ArgumentException(
                        $"Seviye {i + 1}: {Esikler[i + 1]} − düşüş {Dususler[i]} = {Esikler[i + 1] - Dususler[i]}, en alt eşik {Esikler[0]}'i aşıyor; kriz sonrası stres alt eşiğin altında kalmalı (aksi halde kriz her tick yinelenir).");
                }
            }

            if (100m - Dususler[son] >= Esikler[0])
            {
                throw new ArgumentException(
                    $"Seviye {son + 1}: 100 − düşüş {Dususler[son]} = {100m - Dususler[son]}, en alt eşik {Esikler[0]}'in altında değil; kriz sonrası stres alt eşiğin altında kalmalı.");
            }
        }
    }
}
