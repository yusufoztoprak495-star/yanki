using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Yanki.Core.Criteria;

namespace Yanki.Core.Decision
{
    /// <summary>
    /// Bir ölçütün katkısı: sorgunun ham sonucu (<see cref="Girdi"/>), <c>min</c>/<c>max</c> ile 0..1'e normalize hali
    /// (<see cref="Normal"/>) ve eğri çıktısı (<see cref="Cikti"/>). Hepsi 6 ondalığa yuvarlanmıştır; her değer bir öncekinden
    /// izdeki sayılarla yeniden hesaplanabilir.
    /// </summary>
    public sealed class OlcutKatkisi
    {
        /// <summary>Ölçüt tanımı.</summary>
        public Olcut Olcut { get; }

        /// <summary>Sorgunun ham sayısal sonucu (doğru/yanlış için 1/0).</summary>
        public decimal Girdi { get; }

        /// <summary>Normalize girdi, 0..1.</summary>
        public decimal Normal { get; }

        /// <summary>Eğri çıktısı, 0..1.</summary>
        public decimal Cikti { get; }

        /// <summary>Kurar.</summary>
        public OlcutKatkisi(Olcut olcut, decimal girdi, decimal normal, decimal cikti)
        {
            Olcut = olcut ?? throw new ArgumentNullException(nameof(olcut));
            Girdi = girdi;
            Normal = normal;
            Cikti = cikti;
        }
    }

    /// <summary>
    /// Puanlanmış aday: taban, ölçüt katkıları, üç çarpan ve son puan.
    /// <c>Puan = Yuvarla(Taban × Π Cikti × Kisilik × Fikir × RuhHali)</c>; hepsi 6 ondalıktır.
    /// </summary>
    public sealed class AdayPuani
    {
        /// <summary>Aday.</summary>
        public Aday Aday { get; }

        /// <summary>Eylemin tabanı.</summary>
        public decimal Taban { get; }

        /// <summary>Ölçüt katkıları, eylemdeki sırayla.</summary>
        public IReadOnlyList<OlcutKatkisi> Olcutler { get; }

        /// <summary>Kişilik çarpanı, 0.25–2.0.</summary>
        public decimal KisilikCarpani { get; }

        /// <summary>Fikir çarpanı, 0.6–1.5; hedefsiz ve komşu mekan eyleminde 1.</summary>
        public decimal FikirCarpani { get; }

        /// <summary>Ruh hali çarpanı, 0.8–1.25.</summary>
        public decimal RuhHaliCarpani { get; }

        /// <summary>Son puan.</summary>
        public decimal Puan { get; }

        /// <summary>Kurar.</summary>
        public AdayPuani(
            Aday aday, decimal taban, IEnumerable<OlcutKatkisi> olcutler, decimal kisilikCarpani, decimal fikirCarpani, decimal ruhHaliCarpani, decimal puan)
        {
            Aday = aday ?? throw new ArgumentNullException(nameof(aday));
            Taban = taban;
            Olcutler = new ReadOnlyCollection<OlcutKatkisi>(new List<OlcutKatkisi>(olcutler ?? throw new ArgumentNullException(nameof(olcutler))));
            KisilikCarpani = kisilikCarpani;
            FikirCarpani = fikirCarpani;
            RuhHaliCarpani = ruhHaliCarpani;
            Puan = puan;
        }
    }

    /// <summary>
    /// Adayın puanını hesaplar (plan 5.5). Yalnızca karar verenin zihninden ve kriter sözlüğünün izin verdiği sorgulardan beslenir;
    /// hiçbir şey yazmaz. Minnet gibi modifier'lar yalnızca o hedefe yönelik adayın fikir çarpanını etkiler, kişilik çarpanını değil.
    /// </summary>
    public static class Puanlayici
    {
        /// <summary>Adayı puanlar.</summary>
        public static AdayPuani Puanla(KararBaglami baglam, Aday aday)
        {
            if (baglam == null)
            {
                throw new ArgumentNullException(nameof(baglam));
            }

            if (aday == null)
            {
                throw new ArgumentNullException(nameof(aday));
            }

            var eylem = aday.Eylem;
            var sorguBaglami = baglam.SorguBaglami(aday.Hedef);

            var katkilar = new List<OlcutKatkisi>(eylem.Olcutler.Count);
            var carpim = 1m;
            foreach (var olcut in eylem.Olcutler)
            {
                var girdi = Egriler.Yuvarla(Sayiya(baglam.Cozucu.Coz(olcut.Alan, sorguBaglami), olcut));
                var normal = Egriler.Yuvarla(Kirp01((girdi - olcut.Min) / (olcut.Max - olcut.Min)));
                var cikti = Egriler.Hesapla(olcut.Egri, normal);
                katkilar.Add(new OlcutKatkisi(olcut, girdi, normal, cikti));
                carpim *= cikti;
            }

            var kisilik = CarpanHesaplayici.Kisilik(eylem.KisilikCarpani, baglam.Zihin.Kisilik);
            var fikir = eylem.FikirCarpani != null && aday.Hedef.Length > 0
                ? CarpanHesaplayici.Fikir(eylem.FikirCarpani, baglam.Zihin.Fikir(aday.Hedef, baglam.Tick))
                : 1m;
            var ruhHali = CarpanHesaplayici.RuhHali(eylem.RuhHaliCarpani, tur => baglam.Zihin.DuyguSiddeti(tur, baglam.Tick));

            var puan = Egriler.Yuvarla(eylem.Taban * carpim * kisilik * fikir * ruhHali);
            return new AdayPuani(aday, eylem.Taban, katkilar, kisilik, fikir, ruhHali, puan);
        }

        private static decimal Sayiya(SorguDegeri deger, Olcut olcut)
        {
            switch (deger.Tur)
            {
                case DegerTuru.Sayi:
                    return deger.SayiDegeri;
                case DegerTuru.Mantiksal:
                    return deger.MantiksalDegeri ? 1m : 0m;
                default:
                    throw new InvalidOperationException($"Ölçüt '{olcut.Alan}' sayısal ya da doğru/yanlış sonuç vermeli (doğrulayıcı bunu yükleme anında reddeder).");
            }
        }

        private static decimal Kirp01(decimal d) => d < 0m ? 0m : d > 1m ? 1m : d;
    }
}
