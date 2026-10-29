using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Yanki.Core.World;

namespace Yanki.Core.Criteria
{
    /// <summary>Seçime giren aday: kararlı bir kimlik, taşıdığı değer ve kriterleri.</summary>
    public sealed class Aday<T>
    {
        /// <summary>Adayın kimliği; eşitlik bozmada ve izlerde sıralama anahtarıdır. Boş olamaz, listede tekil olmalı.</summary>
        public string Id { get; }

        /// <summary>Adayın taşıdığı değer (eylem tanımı, şablon vb.).</summary>
        public T Deger { get; }

        /// <summary>Adayın kriterleri.</summary>
        public IReadOnlyList<Kriter> Kriterler { get; }

        /// <summary>Yeni aday kurar; kriter listesi kopyalanır.</summary>
        /// <exception cref="ArgumentException"><paramref name="id"/> boşsa.</exception>
        public Aday(string id, T deger, IEnumerable<Kriter> kriterler)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Aday kimliği boş olamaz.", nameof(id));
            }

            Id = id;
            Deger = deger;
            Kriterler = new ReadOnlyCollection<Kriter>(new List<Kriter>(kriterler ?? throw new ArgumentNullException(nameof(kriterler))));
        }
    }

    /// <summary>
    /// Bir adayın eşleşme izi: hangi kriter tuttu, hangisi tutmadı, toplam puan ve elenip elenmediği.
    /// Gelecekteki <c>why</c> aracı bunu kullanır.
    /// </summary>
    public sealed class EslesmeIzi
    {
        /// <summary>Adayın kimliği.</summary>
        public string AdayId { get; }

        /// <summary>Tutmayan bir zorunlu kriter yüzünden elendi mi.</summary>
        public bool Elendi { get; }

        /// <summary>Tutan kriterlerin ağırlık toplamı (elenmiş adaylar için de hesaplanır).</summary>
        public decimal Puan { get; }

        /// <summary>Her kriterin sonucu, adayın kriter sırasıyla (kısa devre yok).</summary>
        public IReadOnlyList<KriterSonucu> Sonuclar { get; }

        /// <summary>Tutan kriter sayısı; puan eşitliğinde "daha özgül" olanı seçmek için kullanılır (ADR-003).</summary>
        public int TutanKriterSayisi { get; }

        /// <summary>Yeni iz.</summary>
        public EslesmeIzi(string adayId, bool elendi, decimal puan, IEnumerable<KriterSonucu> sonuclar)
        {
            AdayId = adayId;
            Elendi = elendi;
            Puan = puan;
            var liste = new List<KriterSonucu>(sonuclar);
            Sonuclar = new ReadOnlyCollection<KriterSonucu>(liste);
            foreach (var s in liste)
            {
                if (s.Tuttu)
                {
                    TutanKriterSayisi++;
                }
            }
        }
    }

    /// <summary>Bir seçimin sonucu ve tüm adayların izleri.</summary>
    public sealed class SecimSonucu<T>
    {
        /// <summary>Elenmemiş bir aday bulundu mu.</summary>
        public bool Bulundu => Secilen != null;

        /// <summary>Seçilen aday; bulunamadıysa <c>null</c>.</summary>
        public Aday<T>? Secilen { get; }

        /// <summary>Tüm adayların izleri, aday kimliğine göre ordinal sıralı.</summary>
        public IReadOnlyList<EslesmeIzi> Izler { get; }

        /// <summary>En yüksek puanda birden fazla elenmemiş aday vardı ve RNG ile seçildi mi.</summary>
        public bool EsitlikVardi { get; }

        /// <summary>Yeni sonuç.</summary>
        public SecimSonucu(Aday<T>? secilen, IEnumerable<EslesmeIzi> izler, bool esitlikVardi)
        {
            Secilen = secilen;
            Izler = new ReadOnlyCollection<EslesmeIzi>(new List<EslesmeIzi>(izler));
            EsitlikVardi = esitlikVardi;
        }
    }

    /// <summary>Adayları kriterleriyle puanlar ve en iyisini seçer (plan 5.9).</summary>
    public static class Secici
    {
        /// <summary>
        /// Adayı puanlar. Puan, tutan kriterlerin ağırlık toplamıdır; tutmayan bir <c>zorunlu</c> kriter adayı eler.
        /// Tüm kriterler değerlendirilir (iz eksiksiz olsun diye).
        /// </summary>
        public static EslesmeIzi Puanla<T>(Aday<T> aday, ISorguCozucu cozucu, SorguBaglami baglam)
        {
            if (aday == null)
            {
                throw new ArgumentNullException(nameof(aday));
            }

            var sonuclar = new List<KriterSonucu>(aday.Kriterler.Count);
            var puan = 0m;
            var elendi = false;
            foreach (var kriter in aday.Kriterler)
            {
                var s = KriterDegerlendirici.Degerlendir(kriter, cozucu, baglam);
                sonuclar.Add(s);
                puan += s.Katki;
                if (kriter.Zorunlu && !s.Tuttu)
                {
                    elendi = true;
                }
            }

            return new EslesmeIzi(aday.Id, elendi, puan, sonuclar);
        }

        /// <summary>
        /// Elenmemiş adaylar arasından seçer: önce en yüksek puan; puan eşitse daha çok kriteri tutan (daha özgül)
        /// aday kazanır (ADR-003); o da eşitse eşit adaylar kimliğe göre ordinal sıralanır ve <paramref name="rng"/> ile
        /// biri seçilir, böylece sonuç girdi sırasından bağımsızdır. RNG'den yalnızca bu son durumda değer çekilir.
        /// </summary>
        /// <exception cref="ArgumentException">Aday kimlikleri tekil değilse.</exception>
        public static SecimSonucu<T> EnIyiSec<T>(IReadOnlyList<Aday<T>> adaylar, ISorguCozucu cozucu, SorguBaglami baglam, AnahtarliRng rng)
        {
            if (adaylar == null)
            {
                throw new ArgumentNullException(nameof(adaylar));
            }

            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            var sirali = new List<Aday<T>>(adaylar);
            sirali.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            for (var i = 1; i < sirali.Count; i++)
            {
                if (string.Equals(sirali[i - 1].Id, sirali[i].Id, StringComparison.Ordinal))
                {
                    throw new ArgumentException($"Aday kimliği tekrar ediyor: \"{sirali[i].Id}\".", nameof(adaylar));
                }
            }

            var izler = new List<EslesmeIzi>(sirali.Count);
            var enIyi = new List<Aday<T>>();
            var enYuksek = 0m;
            var enCokTutan = 0;
            foreach (var aday in sirali)
            {
                var iz = Puanla(aday, cozucu, baglam);
                izler.Add(iz);
                if (iz.Elendi)
                {
                    continue;
                }

                var daha = enIyi.Count == 0
                    || iz.Puan > enYuksek
                    || (iz.Puan == enYuksek && iz.TutanKriterSayisi > enCokTutan);
                if (daha)
                {
                    enIyi.Clear();
                    enYuksek = iz.Puan;
                    enCokTutan = iz.TutanKriterSayisi;
                    enIyi.Add(aday);
                }
                else if (iz.Puan == enYuksek && iz.TutanKriterSayisi == enCokTutan)
                {
                    enIyi.Add(aday);
                }
            }

            if (enIyi.Count == 0)
            {
                return new SecimSonucu<T>(null, izler, false);
            }

            if (enIyi.Count == 1)
            {
                return new SecimSonucu<T>(enIyi[0], izler, false);
            }

            return new SecimSonucu<T>(enIyi[rng.Aralik(0, enIyi.Count)], izler, true);
        }
    }
}
