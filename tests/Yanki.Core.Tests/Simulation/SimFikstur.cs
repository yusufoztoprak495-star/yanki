using System.Collections.Generic;
using Xunit;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;

namespace Yanki.Core.Tests.Simulation
{
    /// <summary>
    /// Motor testlerinin sahte (fikstür) senaryosu: üç NPC, iki mekan. Bu, Kasaba Hanı içeriği DEĞİLDİR;
    /// yalnızca motor davranışını sınamak içindir (mini senaryo içeriği Yusuf'un alanıdır).
    /// </summary>
    internal static class SimFikstur
    {
        public const string SenaryoJson = @"{
  ""surum"": 1,
  ""ad"": ""Deneme"",
  ""mekanlar"": [""han"", ""pazar""],
  ""baglantilar"": [[""han"", ""pazar""]],
  ""oyuncu"": { ""id"": ""oyuncu"", ""konum"": ""han"" },
  ""npcler"": [
    { ""id"": ""Ali"", ""konum"": ""han"", ""kisilik"": { ""acgozluluk"": 0.5, ""durustluk"": 0.7, ""cesaret"": 0.5, ""sosyallik"": 0.5, ""kin_tutma"": 0.5, ""merhamet"": 0.5 } },
    { ""id"": ""Can"", ""konum"": ""pazar"", ""kisilik"": { ""acgozluluk"": 0.5, ""durustluk"": 0.5, ""cesaret"": 0.5, ""sosyallik"": 0.5, ""kin_tutma"": 0.5, ""merhamet"": 0.5 } },
    { ""id"": ""Veli"", ""konum"": ""han"", ""kisilik"": { ""acgozluluk"": 0.2, ""durustluk"": 0.5, ""cesaret"": 0.5, ""sosyallik"": 0.5, ""kin_tutma"": 0.9, ""merhamet"": 0.5 } }
  ],
  ""gercekler"": [],
  ""olay_turleri"": [""yardim_etti"", ""tehdit_etti""],
  ""kural_dosyalari"": [""kurallar.json""]
}";

        /// <summary>Yardım edene 15 puanlık modifier veren tek kuralın JSON'u.</summary>
        public const string YardimKurali = @"{ ""kurallar"": [ {
  ""id"": ""yardim_minneti"", ""ne_zaman"": ""yardim_etti"",
  ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""minnet"", ""deger"": 15, ""sure"": 100 } ]
} ] }";

        public static SenaryoTanimi Senaryo()
        {
            var sonuc = SenaryoYukleyici.Yukle(SenaryoJson, "senaryo.json");
            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            return sonuc.Senaryo!;
        }

        public static IReadOnlyList<KuralTanimi> Kurallar(string json)
        {
            var sonuc = KuralYukleyici.Yukle(json, "kurallar.json", Senaryo().OlayTurleri);
            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            return sonuc.Kurallar;
        }

        public static Simulasyon Yap(string kuralJson = YardimKurali, ulong seed = 42) =>
            Simulasyon.Olustur(Senaryo(), Kurallar(kuralJson), seed);
    }
}
