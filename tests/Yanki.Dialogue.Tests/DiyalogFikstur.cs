using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Decision;
using Yanki.Core.Events;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;

namespace Yanki.Dialogue.Tests
{
    /// <summary>
    /// Diyalog testlerinin sahte (fikstür) senaryosu: Ali ve Veli han'da, Can pazar'da. Bu, Kasaba Hanı içeriği DEĞİLDİR; yalnızca motor davranışını sınamak içindir
    /// (şablon metinleri ve senaryo içeriği Yusuf'un alanıdır, DURAK 2).
    /// </summary>
    internal static class DiyalogFikstur
    {
        public const string SenaryoJson = @"{
  ""surum"": 1, ""ad"": ""DiyalogDeneme"", ""mekanlar"": [""han"", ""pazar""], ""baglantilar"": [[""han"", ""pazar""]],
  ""oyuncu"": { ""id"": ""oyuncu"", ""konum"": ""han"" },
  ""npcler"": [
    { ""id"": ""Ali"", ""konum"": ""han"", ""kisilik"": { ""acgozluluk"": 0.5, ""durustluk"": 0.7, ""cesaret"": 0.5, ""sosyallik"": 0.5, ""kin_tutma"": 0.5, ""merhamet"": 0.5 } },
    { ""id"": ""Can"", ""konum"": ""pazar"", ""kisilik"": { ""acgozluluk"": 0.5, ""durustluk"": 0.5, ""cesaret"": 0.5, ""sosyallik"": 0.5, ""kin_tutma"": 0.5, ""merhamet"": 0.5 } },
    { ""id"": ""Veli"", ""konum"": ""han"", ""kisilik"": { ""acgozluluk"": 0.2, ""durustluk"": 0.5, ""cesaret"": 0.5, ""sosyallik"": 0.5, ""kin_tutma"": 0.9, ""merhamet"": 0.5 } }
  ],
  ""gercekler"": [], ""olay_turleri"": [""yardim_etti"", ""tehdit_etti""], ""kural_dosyalari"": []
}";

        public const string Kurallar = @"{ ""kurallar"": [
  { ""id"": ""yardim_minneti"", ""ne_zaman"": ""yardim_etti"", ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""minnet"", ""deger"": 15, ""sure"": 1000 } ] }
] }";

        public static Simulasyon Sim(string? eylemlerJson = null, ulong seed = 42, Yanki.Core.Mind.ZihinAyarlari? zihin = null)
        {
            var senaryo = SenaryoYukleyici.Yukle(SenaryoJson, "senaryo.json");
            Assert.True(senaryo.Basarili, string.Join("\n", senaryo.Hatalar));
            var kurallar = KuralYukleyici.Yukle(Kurallar, "kurallar.json", senaryo.Senaryo!.OlayTurleri);
            Assert.True(kurallar.Basarili, string.Join("\n", kurallar.Hatalar));
            IReadOnlyList<EylemTanimi>? eylemler = null;
            if (eylemlerJson != null)
            {
                var e = EylemYukleyici.Yukle(eylemlerJson, "eylemler.json");
                Assert.True(e.Basarili, string.Join("\n", e.Hatalar));
                eylemler = e.Eylemler;
            }

            return Simulasyon.Olustur(senaryo.Senaryo!, kurallar.Kurallar, seed, zihinAyarlari: zihin, eylemler: eylemler);
        }

        public static SablonDosyasi Sablon(string jsonTekTirnak)
        {
            var sonuc = SablonYukleyici.Yukle(jsonTekTirnak.Replace('\'', '"'), "sablon.json");
            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            return sonuc.Dosya!;
        }

        public static Olay Olayi(Simulasyon sim, string tur) => sim.Gunluk.Olaylar.First(o => o.Tur == tur);
    }
}
