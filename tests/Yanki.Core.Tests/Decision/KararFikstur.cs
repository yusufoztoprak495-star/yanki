using System.Collections.Generic;
using Xunit;
using Yanki.Core.Criteria;
using Yanki.Core.Decision;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;

namespace Yanki.Core.Tests.Decision
{
    /// <summary>
    /// Karar testlerinin sahte (fikstür) yardımcıları. Senaryo <c>SimFikstur</c>'ündür: Ali ve Veli han'da, Can pazar'da, oyuncu han'da.
    /// Bu, Kasaba Hanı içeriği DEĞİLDİR; yalnızca motor davranışını sınamak içindir.
    /// </summary>
    internal static class KararFikstur
    {
        /// <summary>Tek eylemin JSON nesnesi. Olay türü <c>&lt;id&gt;_oldu</c>; komsu_mekan için olay türü yazılmaz.</summary>
        public static string Eylem(string id, string hedef = "yok", string ek = "", decimal taban = 1m, string etkiler = "[ { \"tur\": \"hafiza\", \"onem\": 0.5 } ]")
        {
            var tabanMetni = taban.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var olayTuru = hedef == "komsu_mekan" ? string.Empty : "\"olay_turu\": \"" + id + "_oldu\", ";
            var etkiAlani = hedef == "komsu_mekan" && etkiler == "[ { \"tur\": \"hafiza\", \"onem\": 0.5 } ]" ? string.Empty : ", \"etkiler\": " + etkiler;
            return "{ \"id\": \"" + id + "\", " + olayTuru + "\"hedef\": \"" + hedef + "\", \"taban\": " + tabanMetni
                + (ek.Length > 0 ? ", " + ek : string.Empty) + etkiAlani + " }";
        }

        public static string Dosya(params string[] eylemler) => "{ \"eylemler\": [ " + string.Join(", ", eylemler) + " ] }";

        public static IReadOnlyList<EylemTanimi> Yukle(params string[] eylemler)
        {
            var sonuc = EylemYukleyici.Yukle(Dosya(eylemler), "eylemler.json");
            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            return sonuc.Eylemler;
        }

        public static Simulasyon Sim(SenaryoTanimi senaryo, IEnumerable<EylemTanimi> eylemler, IEnumerable<KuralTanimi>? kurallar = null, ulong seed = 42) =>
            Simulasyon.Olustur(senaryo, kurallar ?? new KuralTanimi[0], seed, eylemler: eylemler);

        public static Simulasyon Sim(IEnumerable<EylemTanimi> eylemler, ulong seed = 42) =>
            Sim(SimFiksturSenaryo.Senaryo(), eylemler, null, seed);

        /// <summary>Verilen NPC'nin o anki karar bağlamı.</summary>
        public static KararBaglami Baglam(Simulasyon sim, string npc) =>
            new KararBaglami(
                npc, sim.OyuncuId, sim.Seed, sim.SimdikiTick + 1, sim.Dunya, sim.Zihinler[npc],
                new ZihinSorguCozucusu(sim.Dunya, sim.Zihinler, sim.Gunluk, sim.SimdikiTick + 1));
    }

    /// <summary><c>SimFikstur</c> senaryosuna erişim (SimFikstur başka ad alanında).</summary>
    internal static class SimFiksturSenaryo
    {
        public static SenaryoTanimi Senaryo() => Yanki.Core.Tests.Simulation.SimFikstur.Senaryo();
    }
}
