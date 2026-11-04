using Xunit;
using Yanki.Core.Events;
using Yanki.Core.Mind;

namespace Yanki.Core.Tests.Mind
{
    /// <summary>
    /// 8. hafta (5 ms profili): <c>Zihin.Inanilan</c> her önerme için bütün inançları tarıyordu (konuşan başına O(P²)); tick'in ~%90'ı buradaydı.
    /// Çelişki dizini yalnızca çelişebilecek önermeleri (aynı olgunun olumlu/olumsuzu, aynı tek değerli anahtar) tarar. Sonuç naif referansla
    /// (<c>InanilanReferans</c>, eski tam tarama) her önerme ve tick için birebir aynı olmalıdır.
    /// </summary>
    public class InancCelisiDizinTests
    {
        private static Zihin Kur()
        {
            var zihin = new Zihin("Ayse", new Kisilik(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, 0.5m), new ZihinAyarlari { KanitSonumuTickBasina = 0.01m });
            var gunluk = new OlayGunlugu();

            void Ekle(long tick, string onerme, decimal guc)
            {
                var olay = ZihinOlaylari.KanitEklendi(gunluk.SonrakiId, tick, gunluk.SonrakiSira(tick), "Ayse", onerme, "gordu", "Ayse", guc, new long[0], "algi");
                gunluk.Ekle(olay);
                zihin.Uygula(olay);
            }

            // Olumlu/olumsuz çiftler: güçler farklı, eşit (son kanıt tick'iyle kırılır) ve ikisi de zayıf.
            for (var i = 0; i < 12; i++)
            {
                Ekle(2, "borclu(Ali,Veli," + (100 + i) + ")", 0.30m + 0.02m * i);
            }

            for (var i = 0; i < 12; i++)
            {
                Ekle(3, "degil(borclu(Ali,Veli," + (100 + i) + "))", i % 3 == 0 ? 0.30m + 0.02m * i : 0.55m);
            }

            // Eşitliği son kanıt tick'iyle kıranlar: olumluya sonradan (tick 4) aynı güçte ikinci bir kaynak gelir.
            for (var i = 0; i < 12; i += 4)
            {
                Ekle(4, "borclu(Ali,Veli," + (100 + i) + ")", 0.10m);
            }

            // Tek değerli anahtarlar: aynı anahtar farklı değerler çelişir; olumsuzu başka bir olumlu değerle çelişmez ama kendi olumlusuyla çelişir.
            Ekle(4, "konum!(Ali,han)", 0.5m);
            Ekle(4, "konum!(Ali,pazar)", 0.6m);
            Ekle(5, "konum!(Ali,kilise)", 0.6m);
            Ekle(5, "degil(konum!(Ali,han))", 0.45m);
            Ekle(6, "konum!(Veli,han)", 0.4m);

            // Çelişmeyen dolgu.
            for (var i = 0; i < 40; i++)
            {
                Ekle(6, "gordu(N" + i + ",han)", 0.2m + 0.01m * (i % 30));
            }

            return zihin;
        }

        [Fact]
        public void Celiski_dizinli_Inanilan_naif_referansla_her_onerme_ve_tickte_ayni()
        {
            var zihin = Kur();
            var kazanan = 0;
            var kaybeden = 0;
            foreach (var tick in new long[] { 6, 30, 120, 400 })
            {
                foreach (var onerme in zihin.Onermeler)
                {
                    var hizli = zihin.Inanilan(onerme, tick);
                    Assert.Equal(zihin.InanilanReferans(onerme, tick), hizli);
                    if (hizli)
                    {
                        kazanan++;
                    }
                    else
                    {
                        kaybeden++;
                    }
                }
            }

            Assert.True(kazanan > 50, "inanılan önermeler olmalı");
            Assert.True(kaybeden > 20, "çelişkide kaybeden önermeler olmalı; aksi halde test boş geçer");
        }

        [Fact]
        public void Celiski_dizini_InancDerecesi_ve_hash_sonucunu_degistirmez()
        {
            var zihin = Kur();
            var desen = OnermeDeseni.Ayristir("borclu(*,*,*)");

            Assert.Equal(zihin.InancDerecesiReferans(desen, 50), zihin.InancDerecesi(desen, 50));
        }
    }
}
