using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Decision;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;
using Yanki.Core.Tests.Decision;

namespace Yanki.Core.Tests.Simulation
{
    /// <summary>
    /// Aynı eylemin her tick tekrarı, cooldown yazılmadan <c>hafiza_var(olay_turu, ben, n)</c> ön koşuluyla sınırlanabilir mi (5. hafta,
    /// görev §9)? Bu testler yeteneği ve sınırlarını belgeler. Cooldown 7. haftadadır; buradaki sınırlar o zaman gözden geçirilir.
    /// Fikstür: üç NPC aynı mekanda (A, B, C); gerçek içerik değildir.
    /// </summary>
    public class TekrarSiniriTests
    {
        private const string Kisilik = "{ \"acgozluluk\": 0.5, \"durustluk\": 0.5, \"cesaret\": 0.5, \"sosyallik\": 0.5, \"kin_tutma\": 0.5, \"merhamet\": 0.5 }";

        private static SenaryoTanimi UcNpcAyniYerde()
        {
            var json = "{ \"surum\": 1, \"mekanlar\": [\"han\"], \"baglantilar\": [], \"oyuncu\": { \"id\": \"oyuncu\", \"konum\": \"han\" }, \"npcler\": ["
                + string.Join(", ", new[] { "A", "B", "C" }.Select(n => "{ \"id\": \"" + n + "\", \"konum\": \"han\", \"kisilik\": " + Kisilik + " }"))
                + "], \"olay_turleri\": [\"x_oldu\"] }";
            var sonuc = SenaryoYukleyici.Yukle(json, "s.json");
            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            return sonuc.Senaryo!;
        }

        private static Simulasyon Kur(int pencere)
        {
            var eylemler = KararFikstur.Yukle(
                KararFikstur.Eylem("selam", "yok", "\"on_kosullar\": [ { \"alan\": \"hafiza_var(selam_oldu, ben, " + pencere + ")\", \"op\": \"==\", \"deger\": false } ]", 2m),
                KararFikstur.Eylem("yardim", "npc", "\"on_kosullar\": [ { \"alan\": \"hafiza_var(yardim_oldu, ben, " + pencere + ")\", \"op\": \"==\", \"deger\": false } ]", 3m),
                KararFikstur.Eylem("dinlen", "yok", string.Empty, 1m));
            return Simulasyon.Olustur(UcNpcAyniYerde(), new KuralTanimi[0], 42, eylemler: eylemler, secici: new UtilitySelector(new SecimAyarlari(1, 0.7m)));
        }

        private static List<long> SecilenTickler(Simulasyon sim, string npc, string eylem, int tickSayisi)
        {
            sim.Tick(tickSayisi);
            return sim.Gunluk.Olaylar
                .Where(o => o.Tur == "karar_verildi" && o.Fail == npc && o.Deger("secilen_eylem") == eylem)
                .Select(o => o.Tick)
                .ToList();
        }

        [Fact]
        public void Sinir_tekrari_pencere_kadar_bekletir_sonra_yeniden_secilebilir()
        {
            // Pencere 5. Tick 1'de en yüksek puanlı yardim seçilir, tick 2'de yardim engelli olduğundan selam gelir. selam ise tick 3..7 (fark
            // 1..5) engelli, tick 8'de yeniden serbesttir: döngü pencere + 1 = 6 tick.
            var sim = Kur(5);

            var tickler = SecilenTickler(sim, "A", "selam", 20);

            Assert.Equal(new long[] { 2, 8, 14, 20 }, tickler.ToArray());
            // Her iki eylem de engelliyken NPC boşta kalmaz: yedek eylem (dinlen) seçilir.
            Assert.Equal("dinlen", sim.Gunluk.Olaylar.Single(o => o.Tur == "karar_verildi" && o.Fail == "A" && o.Tick == 3).Deger("secilen_eylem"));
        }

        [Fact]
        public void Sinir_eylem_turune_gore_isler_hedefe_gore_degil()
        {
            // A tick 1'de bir kişiye yardım eder; ikinci kişiye de yardım etmesi pencere boyunca engellenir (hedef ayrımı yok).
            var sim = Kur(5);
            sim.Tick(6);

            var yardimlar = sim.Gunluk.Olaylar.Where(o => o.Tur == "yardim_oldu" && o.Fail == "A").ToList();

            Assert.Equal(new long[] { 1 }, yardimlar.Select(o => o.Tick).ToArray());
            Assert.Single(yardimlar.Select(o => o.Hedef).Distinct());
        }

        [Fact]
        public void Sinir_hafiza_unutulunca_pencere_dolmadan_kalkar()
        {
            // Algıdan gelen hafıza önemi 0.5'tir ve tick başına 0.002 söner; 0.1'in altına inince (200 tick sonra) unutulur. Pencere 500 olsa bile
            // sınır o zaman kalkar: hafiza_var yalnızca HÂLÂ hatırlanan olaya bakar (selam tick 2'de, 202'de yeniden serbest).
            var sim = Kur(500);

            var tickler = SecilenTickler(sim, "A", "selam", 260);

            Assert.Equal(new long[] { 2, 202 }, tickler.ToArray());
        }

        [Fact]
        public void Sinirsiz_eylem_her_tick_secilir_sinir_ise_secimi_azaltir()
        {
            var eylemler = KararFikstur.Yukle(KararFikstur.Eylem("selam", "yok", string.Empty, 2m), KararFikstur.Eylem("dinlen", "yok", string.Empty, 1m));
            var sinirsiz = Simulasyon.Olustur(UcNpcAyniYerde(), new KuralTanimi[0], 42, eylemler: eylemler, secici: new UtilitySelector(new SecimAyarlari(1, 0.7m)));
            var sinirli = Kur(5);

            var sinirsizSayi = SecilenTickler(sinirsiz, "A", "selam", 20).Count;
            var sinirliSayi = SecilenTickler(sinirli, "A", "selam", 20).Count;

            Assert.Equal(20, sinirsizSayi);
            Assert.Equal(4, sinirliSayi);
        }
    }
}
