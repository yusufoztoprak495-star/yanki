using System;
using System.Collections.Generic;
using Yanki.Core.World;

namespace Yanki.Core.Decision
{
    /// <summary>
    /// Varsayılan seçici (plan 5.5): adayları puanlar; puanı en yüksek adayın <c>Esik</c> oranına (varsayılan %70) ulaşan ilk <c>EnIyiN</c>
    /// (varsayılan 3) aday arasından puan ağırlıklı seçer. <c>EnIyiN = 1</c> argmax'tır. Puanı 0 olan aday seçilemez.
    /// Sıra: puan azalan, eşitlikte eylem kimliği ve hedef ordinal. Zar <c>AnahtarliRng.Icin(Seed, tick, npcId, "karar")</c> akışından gelir,
    /// bu yüzden NPC işleme sırası seçimi etkilemez. Hiçbir şey yazmaz.
    /// </summary>
    public sealed class UtilitySelector : IActionSelector
    {
        private readonly SecimAyarlari _ayarlar;

        /// <summary>Seçici kurar.</summary>
        public UtilitySelector(SecimAyarlari? ayarlar = null)
        {
            _ayarlar = ayarlar ?? new SecimAyarlari();
        }

        /// <inheritdoc />
        public SecimSonucu Sec(KararBaglami baglam, IReadOnlyList<Aday> adaylar, IReadOnlyList<ElenenAday> elenenler)
        {
            if (baglam == null)
            {
                throw new ArgumentNullException(nameof(baglam));
            }

            if (adaylar == null)
            {
                throw new ArgumentNullException(nameof(adaylar));
            }

            if (elenenler == null)
            {
                throw new ArgumentNullException(nameof(elenenler));
            }

            var puanlar = new List<AdayPuani>(adaylar.Count);
            foreach (var aday in adaylar)
            {
                puanlar.Add(Puanlayici.Puanla(baglam, aday));
            }

            puanlar.Sort((a, b) =>
            {
                var c = b.Puan.CompareTo(a.Puan);
                return c != 0 ? c : AdayUretimi.Karsilastir(a.Aday.Eylem.Id, a.Aday.Hedef, b.Aday.Eylem.Id, b.Aday.Hedef);
            });

            // Havuz: puan > 0, en yüksek puanın eşik oranına ulaşan ilk N.
            var havuz = new List<AdayPuani>();
            if (puanlar.Count > 0 && puanlar[0].Puan > 0m)
            {
                var esikPuani = Egriler.Yuvarla(puanlar[0].Puan * _ayarlar.Esik);
                foreach (var p in puanlar)
                {
                    if (p.Puan <= 0m || p.Puan < esikPuani || havuz.Count == _ayarlar.EnIyiN)
                    {
                        break;
                    }

                    havuz.Add(p);
                }
            }

            decimal? zar = null;
            AdayPuani? secilen = null;
            if (havuz.Count == 1)
            {
                secilen = havuz[0];
            }
            else if (havuz.Count > 1)
            {
                var z = Egriler.Yuvarla(AnahtarliRng.Icin(baglam.Seed, baglam.Tick, baglam.Npc, "karar").SonrakiDecimal01());
                zar = z;
                var toplam = 0m;
                foreach (var p in havuz)
                {
                    toplam += p.Puan;
                }

                var nokta = z * toplam;
                var kumulatif = 0m;
                secilen = havuz[havuz.Count - 1];
                foreach (var p in havuz)
                {
                    kumulatif += p.Puan;
                    if (nokta < kumulatif)
                    {
                        secilen = p;
                        break;
                    }
                }
            }

            var izAdaylari = new List<AdayIzi>(puanlar.Count + elenenler.Count);
            for (var i = 0; i < puanlar.Count; i++)
            {
                izAdaylari.Add(new AdayIzi(puanlar[i].Aday, false, string.Empty, puanlar[i], i + 1, havuz.Contains(puanlar[i])));
            }

            foreach (var e in elenenler)
            {
                izAdaylari.Add(new AdayIzi(new Aday(e.Eylem, e.Hedef), true, e.Neden, null, 0, false));
            }

            izAdaylari.Sort((a, b) => AdayUretimi.Karsilastir(a.Aday.Eylem.Id, a.Aday.Hedef, b.Aday.Eylem.Id, b.Aday.Hedef));

            var havuzAnahtarlari = new List<string>(havuz.Count);
            foreach (var p in havuz)
            {
                havuzAnahtarlari.Add(p.Aday.Anahtar);
            }

            var secilenAday = secilen?.Aday;
            var iz = new KararIzi(
                baglam.Npc,
                baglam.Tick,
                _ayarlar,
                izAdaylari,
                havuzAnahtarlari,
                zar,
                secilenAday,
                secilenAday != null ? secilenAday.Eylem.CelisenOzellikler : new string[0],
                new long[0],
                new string[0]);
            return new SecimSonucu(secilenAday, iz);
        }
    }
}
