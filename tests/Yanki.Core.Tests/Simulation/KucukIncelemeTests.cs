using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Events;
using Yanki.Core.Mind;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;
using Yanki.Core.Tests.Decision;
using Kaydedici = Yanki.Core.Tests.Simulation.KonusmaAdimiTests.Kaydedici;

namespace Yanki.Core.Tests.Simulation
{
    /// <summary>7. hafta küçük kod incelemesi maddeleri (I): tek durum görüntüsü (a), krizin tüketilmiş nedenleri (c).</summary>
    public class KucukIncelemeTests
    {
        private static IEnumerable<Olay> Krizler(Simulasyon sim) =>
            sim.Gunluk.Olaylar.Where(o => o.KuralId.StartsWith("kriz:", System.StringComparison.Ordinal) && o.Tur != "stres_degisti" && !ZihinOlaylari.ZihinOlayiMi(o.Tur));

        [Fact]
        public void Eylem_etkilerinin_gercek_var_yok_denetimi_adim_oncesi_durumdan_yapilir()
        {
            var ekle = KararFikstur.Eylem("ekle", "yok", string.Empty, 1m, "[ { \"tur\": \"gercek\", \"islem\": \"ekle\", \"gercek\": \"bayrak(a)\" } ]");
            var sil = KararFikstur.Eylem("sil", "yok", string.Empty, 1m, "[ { \"tur\": \"gercek\", \"islem\": \"kaldir\", \"gercek\": \"bayrak(a)\" } ]");
            var secici = new SabitSecici((npc, tick) => tick != 1 ? null : npc == "Ali" ? "ekle|" : npc == "Veli" ? "sil|" : null);
            var sim = Simulasyon.Olustur(SimFikstur.Senaryo(), new KuralTanimi[0], 42, eylemler: KararFikstur.Yukle(ekle, sil), secici: secici);
            sim.Tick(1);

            // Ali (ordinal önce) bayrağı ekler; Veli'nin kaldırması adım öncesi görüntüde bayrak yok olduğundan etkisizdir: son kimlik kazanmaz, bayrak kalır.
            Assert.True(sim.Dunya.Var(Yanki.Core.World.Gercek.Ayristir("bayrak(a)")));
            Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.Tur == "gercek_kaldirildi");
            Assert.Single(sim.Gunluk.Olaylar, o => o.Tur == "gercek_eklendi" && o.KuralId == "eylem:ekle");
        }

        [Fact]
        public void Ikinci_krizin_nedenleri_ilk_krizde_tuketilen_stres_artislarini_saymaz()
        {
            var yalan = "{ \"id\": \"yalan_soyle\", \"olay_turu\": \"anlatti\", \"hedef\": \"npc\", \"taban\": 1, \"aktarim\": \"yalan\", "
                + "\"yalan_onermeleri\": [\"degil(borclu(Mehmet,Ayse,500))\"], \"celisen_ozellikler\": [\"durustluk\"], \"stres_tabani\": 62.5 }";
            var senaryo = SenaryoYukleyici.Yukle(KonusmaAdimiTests.Senaryo(ayseDurustluk: "0.8"), "s.json").Senaryo!;
            var krizler = KrizYukleyici.Yukle(KrizYukleyiciTests.Gecerli, "krizler.json").Satirlar;
            var kurallar = KuralYukleyici.Yukle("{ \"kurallar\": [] }", "k.json", senaryo.OlayTurleri).Kurallar;
            var secici = new Kaydedici().Sec("Ayse", 2, "yalan_soyle|Can").Sec("Ayse", 4, "yalan_soyle|Zeki");
            var sim = Simulasyon.Olustur(
                senaryo, kurallar, 42, new ZihinAyarlari { KanitSonumuTickBasina = 0m, StresSonumuTickBasina = 0m },
                eylemler: KararFikstur.Yukle(yalan), secici: secici, krizler: krizler);
            sim.Submit(OyuncuAnlatimiTests.Anlat("Ayse", OyuncuAnlatimiTests.Borc));
            sim.Tick(6);

            var artislar = sim.Gunluk.Olaylar.Where(o => o.Tur == "stres_degisti" && o.Deger("npc") == "Ayse" && o.KuralId == "eylem:yalan_soyle").ToList();
            var krizOlaylari = Krizler(sim).Where(o => o.Fail == "Ayse").ToList();
            Assert.Equal(2, artislar.Count);
            Assert.Equal(2, krizOlaylari.Count);
            Assert.Equal(new[] { artislar[0].Id }, krizOlaylari[0].NedenOlaylar.ToArray());
            Assert.Equal(new[] { artislar[1].Id }, krizOlaylari[1].NedenOlaylar.ToArray());   // ilk artış ilk krizde tüketildi
        }
    }
}
