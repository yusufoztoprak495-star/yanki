using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Yanki.Core.Events
{
    /// <summary>
    /// Tek doğruluk kaynağı: yalnızca sonuna ekleme yapılabilen, kayıtları değişmeyen olay günlüğü (plan 5.1, ADR-001).
    /// Her ekleme sırasında Id, Tick, Sira ve nedensellik kuralları doğrulanır.
    /// </summary>
    public sealed class OlayGunlugu
    {
        private readonly List<Olay> _olaylar = new List<Olay>();
        private readonly ReadOnlyCollection<Olay> _salt;

        /// <summary>Boş bir günlük kurar.</summary>
        public OlayGunlugu()
        {
            _salt = new ReadOnlyCollection<Olay>(_olaylar);
        }

        /// <summary>Günlükteki olay sayısı.</summary>
        public int Sayi => _olaylar.Count;

        /// <summary>Olaylar, ekleme (= Id) sırasıyla; değiştirilemez görünüm.</summary>
        public IReadOnlyList<Olay> Olaylar => _salt;

        /// <summary>Bir sonraki olayın alması gereken Id.</summary>
        public long SonrakiId => _olaylar.Count + 1L;

        /// <summary>Verilen tick'teki bir sonraki olayın alması gereken <see cref="Olay.Sira"/>.</summary>
        public int SonrakiSira(long tick)
        {
            if (_olaylar.Count > 0 && _olaylar[_olaylar.Count - 1].Tick == tick)
            {
                return _olaylar[_olaylar.Count - 1].Sira + 1;
            }

            return 0;
        }

        /// <summary>Verilen Id'li olay günlükte var mı.</summary>
        public bool Var(long id) => id >= 1 && id <= _olaylar.Count;

        /// <summary>Id ile olayı getirir.</summary>
        /// <exception cref="ArgumentOutOfRangeException">Id günlükte yoksa.</exception>
        public Olay this[long id]
        {
            get
            {
                if (!Var(id))
                {
                    throw new ArgumentOutOfRangeException(nameof(id), $"Günlükte #{id} kimlikli olay yok (olay sayısı: {_olaylar.Count}).");
                }

                return _olaylar[(int)(id - 1)];
            }
        }

        /// <summary>Olayı günlüğün sonuna ekler.</summary>
        /// <exception cref="InvalidOperationException">
        /// Id ardışık değilse, Tick geriye gidiyorsa, Sira tick içinde 0'dan başlayıp birer birer artmıyorsa
        /// ya da NedenOlaylar içinde günlükte olmayan bir kimlik varsa.
        /// </exception>
        public void Ekle(Olay olay)
        {
            if (olay == null)
            {
                throw new ArgumentNullException(nameof(olay));
            }

            if (olay.Id != SonrakiId)
            {
                throw new InvalidOperationException($"Olay Id'si ardışık olmalı: beklenen {SonrakiId}, gelen {olay.Id}.");
            }

            if (_olaylar.Count > 0 && olay.Tick < _olaylar[_olaylar.Count - 1].Tick)
            {
                throw new InvalidOperationException(
                    $"Tick geriye gidemez: son olay tick {_olaylar[_olaylar.Count - 1].Tick}, gelen olay #{olay.Id} tick {olay.Tick}.");
            }

            var beklenenSira = SonrakiSira(olay.Tick);
            if (olay.Sira != beklenenSira)
            {
                throw new InvalidOperationException(
                    $"Sira tick içinde 0'dan başlayıp birer artmalı: olay #{olay.Id} (tick {olay.Tick}) için beklenen {beklenenSira}, gelen {olay.Sira}.");
            }

            foreach (var neden in olay.NedenOlaylar)
            {
                if (!Var(neden))
                {
                    throw new InvalidOperationException(
                        $"Olay #{olay.Id} nedeni olarak #{neden} gösteriyor, ama bu olay günlükte yok (neden, yeni olaydan önce eklenmiş olmalı).");
                }
            }

            _olaylar.Add(olay);
        }

        /// <summary>
        /// Olayın nedensellik zincirini geriye doğru izler: olaya doğrudan ya da dolaylı yol açan tüm olaylar,
        /// Id'si azalan sırada ve tekrarsız döner. Olayın kendisi dahil değildir.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Id günlükte yoksa.</exception>
        public IReadOnlyList<Olay> Neden(long olayId)
        {
            var baslangic = this[olayId];

            // Nedenler her zaman kendilerinden küçük Id'lidir; bu yüzden azalan Id sırası geçerli bir ters topolojik sıradır.
            var ziyaret = new SortedSet<long>();
            var yigin = new Stack<Olay>();
            yigin.Push(baslangic);
            while (yigin.Count > 0)
            {
                foreach (var nedenId in yigin.Pop().NedenOlaylar)
                {
                    if (ziyaret.Add(nedenId))
                    {
                        yigin.Push(this[nedenId]);
                    }
                }
            }

            var sonuc = new List<Olay>(ziyaret.Count);
            foreach (var id in ziyaret.Reverse())
            {
                sonuc.Add(this[id]);
            }

            return new ReadOnlyCollection<Olay>(sonuc);
        }
    }
}
