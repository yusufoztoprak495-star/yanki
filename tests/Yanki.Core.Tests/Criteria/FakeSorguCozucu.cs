using System;
using System.Collections.Generic;
using Yanki.Core.Criteria;

namespace Yanki.Core.Tests.Criteria
{
    /// <summary>Testler için sahte çözücü: kanonik çağrı metnine göre hazır değer döndürür.</summary>
    internal sealed class FakeSorguCozucu : ISorguCozucu
    {
        private readonly Dictionary<string, SorguDegeri> _degerler = new Dictionary<string, SorguDegeri>(StringComparer.Ordinal);

        public int CagriSayisi { get; private set; }

        public FakeSorguCozucu Ayarla(string cagri, SorguDegeri deger)
        {
            _degerler[IfadeAyristirici.Ayristir(cagri).ToString()] = deger;
            return this;
        }

        public SorguDegeri Coz(SorguCagrisi cagri, SorguBaglami baglam)
        {
            CagriSayisi++;
            if (_degerler.TryGetValue(cagri.ToString(), out var d))
            {
                return d;
            }

            throw new InvalidOperationException("Sahte çözücüde ayarlanmamış çağrı: " + cagri);
        }
    }
}
