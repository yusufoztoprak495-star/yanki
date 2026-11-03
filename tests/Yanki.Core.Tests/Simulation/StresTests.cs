using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Yanki.Core.Decision;
using Yanki.Core.Mind;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;
using Yanki.Core.Tests.Decision;

namespace Yanki.Core.Tests.Simulation
{
    /// <summary>
    /// Stres (6. hafta, Teslim 6): kişiliğe aykırı eylem seçilince <c>stres += stres_tabani × Σ(çelişen özellik değeri)</c>, 6.3'te
    /// <c>stres_degisti</c> olayıyla, <c>KuralId: eylem:&lt;id&gt;</c>, nedeni eylem olayı. Fikstür: Ayse (durustluk 0.8), Can, Veli.
    /// </summary>
    public class StresTests
    {
        private const string Yalan = "{ \"id\": \"yalan_soyle\", \"olay_turu\": \"anlatti\", \"hedef\": \"npc\", \"taban\": 1, \"aktarim\": \"yalan\", "
            + "\"yalan_onermeleri\": [\"degil(borclu(Mehmet,Ayse,500))\"], \"celisen_ozellikler\": [\"durustluk\"], \"stres_tabani\": 15 }";

        private static Simulasyon Kur(IActionSelector secici, params string[] eylemler)
        {
            // Ayşe'nin dürüstlüğü 0.8 (OyuncuAnlatimiTests senaryosunda 0.5).
            var json = Regex.Replace(OyuncuAnlatimiTests.SenaryoJson, "(\"id\": \"Ayse\".*?\"durustluk\": )0[.]5", "${1}0.8", RegexOptions.Singleline);
            var senaryo = SenaryoYukleyici.Yukle(json, "s.json");
            Assert.True(senaryo.Basarili, string.Join("\n", senaryo.Hatalar));
            var kurallar = KuralYukleyici.Yukle("{ \"kurallar\": [] }", "k.json", senaryo.Senaryo!.OlayTurleri);
            return Simulasyon.Olustur(senaryo.Senaryo!, kurallar.Kurallar, 42, new ZihinAyarlari { KanitSonumuTickBasina = 0m }, eylemler: KararFikstur.Yukle(eylemler), secici: secici);
        }

        private static void Anlat(Simulasyon sim)
        {
            sim.Submit(OyuncuAnlatimiTests.Anlat("Ayse", OyuncuAnlatimiTests.Borc));
            sim.Tick(1);
        }

        [Fact]
        public void Secilen_celisen_eylem_stres_olayini_6_3te_yazar_nedeni_eylem_olayidir()
        {
            var sim = Kur(new SabitSecici((npc, tick) => npc == "Ayse" && tick == 2 ? "yalan_soyle|Can" : null), Yalan);
            Anlat(sim);
            sim.Tick(1);

            var stres = Assert.Single(sim.Gunluk.Olaylar, o => o.Tur == "stres_degisti");
            var anlatti = sim.Gunluk.Olaylar.Single(o => o.Tur == "anlatti" && o.Fail == "Ayse");
            Assert.Equal("Ayse", stres.Deger("npc"));
            Assert.Equal("12", stres.Deger("delta"));   // 15 × durustluk 0.8
            Assert.Equal("eylem:yalan_soyle", stres.KuralId);
            Assert.Equal(new[] { anlatti.Id }, stres.NedenOlaylar.ToArray());
            Assert.Equal(12m, sim.Zihinler["Ayse"].Stres(2));
            Assert.Equal(0m, sim.Zihinler["Can"].Stres(2));
        }

        [Fact]
        public void Eylem_secilmediyse_stres_degismez()
        {
            var sim = Kur(new SabitSecici((npc, tick) => null), Yalan);
            Anlat(sim);
            sim.Tick(5);

            Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.Tur == "stres_degisti");
            Assert.Equal(0m, sim.Zihinler["Ayse"].Stres(6));
        }

        [Fact]
        public void Stres_zamanla_soner()
        {
            var sim = Kur(new SabitSecici((npc, tick) => npc == "Ayse" && tick == 2 ? "yalan_soyle|Can" : null), Yalan);
            Anlat(sim);
            sim.Tick(11);

            Assert.Equal(12m - 0.2m * 10m, sim.Zihinler["Ayse"].Stres(12));
        }

        [Fact]
        public void Hareket_eyleminin_celiskisi_de_stres_yazar_nedeni_geldi_olayidir()
        {
            var git = KararFikstur.Eylem("git", "komsu_mekan", "\"celisen_ozellikler\": [\"cesaret\"], \"stres_tabani\": 10");
            var sim = Kur(new SabitSecici((npc, tick) => npc == "Can" && tick == 1 ? "git|pazar" : null), git);
            sim.Tick(1);

            var stres = Assert.Single(sim.Gunluk.Olaylar, o => o.Tur == "stres_degisti");
            var geldi = sim.Gunluk.Olaylar.Single(o => o.Tur == "geldi" && o.Fail == "Can");
            Assert.Equal("Can", stres.Deger("npc"));
            Assert.Equal("5", stres.Deger("delta"));   // 10 × cesaret 0.5
            Assert.Equal("eylem:git", stres.KuralId);
            Assert.Equal(new[] { geldi.Id }, stres.NedenOlaylar.ToArray());
        }

        [Fact]
        public void Celiskisiz_eylem_stres_olayi_yazmaz()
        {
            var sim = Kur(new SabitSecici((npc, tick) => npc == "Can" && tick == 1 ? "selam|" : null), KararFikstur.Eylem("selam", "yok"));
            sim.Tick(2);

            Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.Tur == "stres_degisti");
        }
    }
}
