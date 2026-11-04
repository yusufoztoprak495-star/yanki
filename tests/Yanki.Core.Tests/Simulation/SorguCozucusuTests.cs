using Xunit;
using Yanki.Core.Criteria;
using Yanki.Core.Simulation;

namespace Yanki.Core.Tests.Simulation
{
    /// <summary>
    /// 8. hafta (A kararı): çekirdeğe yalnızca hash'e girmeyen salt okunur sorgu eklenir. <c>Simulasyon.SorguCozucusu()</c> şimdiki tick'in sonundaki durumu okur
    /// (diyalog katmanı OlayYayinlandi'da bunu kullanır); simülasyonu, günlüğü ve durum hash'ini değiştirmez.
    /// </summary>
    public class SorguCozucusuTests
    {
        [Fact]
        public void Cozucu_simdiki_durumu_okur_ve_simulasyona_dokunmaz()
        {
            var sim = SimFikstur.Yap();
            sim.Submit(new PlayerAction("yardim_etti", "Ali"));
            sim.Tick(2);
            var hash = sim.DurumHash();
            var olaySayisi = sim.Gunluk.Sayi;
            var tick = sim.SimdikiTick;

            var cozucu = sim.SorguCozucusu();
            var baglam = new SorguBaglami("Ali", sim.OyuncuId, dinleyen: sim.OyuncuId);
            var kisilik = cozucu.Coz(KriterOkuyucu.AlanOku("kisilik(durustluk)"), baglam);
            var fikir = cozucu.Coz(KriterOkuyucu.AlanOku("fikir(oyuncu)"), baglam);
            var ayni = cozucu.Coz(KriterOkuyucu.AlanOku("ayni_mekanda(oyuncu)"), baglam);

            Assert.Equal(0.7m, kisilik.SayiDegeri);
            Assert.Equal(15m, fikir.SayiDegeri);                       // yardım minneti: kural tick 2'de çalıştı
            Assert.True(ayni.MantiksalDegeri);
            Assert.Equal(hash, sim.DurumHash());
            Assert.Equal(olaySayisi, sim.Gunluk.Sayi);
            Assert.Equal(tick, sim.SimdikiTick);
        }
    }
}
