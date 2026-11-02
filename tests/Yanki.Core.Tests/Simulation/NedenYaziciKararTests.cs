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
    /// <c>yanki why</c>: nedensellik zinciri modifier'dan algıya, NPC eylemine ve karar izine (adaylar, puan bileşenleri, seçim) iner.
    /// Fikstür: Ali ve Veli han'da, Can pazar'da; Kasaba Hanı içeriği değildir.
    /// </summary>
    public class NedenYaziciKararTests
    {
        private const string SelamTanigi = @"{ ""kurallar"": [ {
  ""id"": ""selam_tanigi"", ""ne_zaman"": ""selam_oldu"",
  ""kriterler"": [ { ""alan"": ""olay_rolu(tanik)"", ""op"": ""=="", ""deger"": true } ],
  ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""selamlandi"", ""deger"": 5, ""sure"": 50 } ] } ] }";

        private static Simulasyon Kur()
        {
            var selam = KararFikstur.Eylem(
                "selam", "yok",
                "\"olcutler\": [ { \"alan\": \"kisilik(sosyallik)\", \"egri\": { \"tur\": \"lojistik\", \"k\": 8, \"m\": 0.4 } } ], "
                + "\"kisilik_carpani\": { \"ozellikler\": [ { \"ozellik\": \"sosyallik\" } ] }");
            var kural = KuralYukleyici.Yukle(SelamTanigi, "k.json", new[] { "yardim_etti", "tehdit_etti", "selam_oldu" });
            Assert.True(kural.Basarili, string.Join("\n", kural.Hatalar));
            var sim = Simulasyon.Olustur(SimFikstur.Senaryo(), kural.Kurallar, 42, eylemler: KararFikstur.Yukle(selam), secici: new UtilitySelector(new SecimAyarlari(1, 0.7m)));
            foreach (var npc in new[] { "Ali", "Can", "Veli" })
            {
                sim.TamIziIste(npc, 1);
            }

            sim.Tick(2);
            return sim;
        }

        // Tick 1 kararlarının tam izleri (replay'in yapacağı yakalamanın aynısı): karar olayı kimliği → tam iz.
        private static IReadOnlyDictionary<long, KararIzi> TamIzler(Simulasyon sim) =>
            sim.Gunluk.Olaylar.Where(o => o.Tur == "karar_verildi" && o.Tick == 1).ToDictionary(o => o.Id, o => sim.YakalananIz(o.Fail, 1)!);

        [Fact]
        public void Karar_olayinin_ozeti_buyuk_iz_yukunu_dokmez()
        {
            var sim = Kur();
            var karar = sim.SonKararOlayi("Ali")!;

            var ozet = NedenYazici.Ozet(karar);

            Assert.Contains("karar_verildi", ozet);
            Assert.Contains("secilen_eylem=", ozet);
            Assert.DoesNotContain("iz=", ozet);
            Assert.DoesNotContain("{", ozet);
        }

        [Fact]
        public void Zincir_modifierdan_algiya_eyleme_ve_karar_izine_iner()
        {
            var sim = Kur();
            var modifier = sim.Gunluk.Olaylar.First(o => o.KuralId == "selam_tanigi");

            var satirlar = NedenYazici.Yaz(sim.Gunluk, modifier.Id, TamIzler(sim));
            var metin = string.Join("\n", satirlar);

            Assert.Contains("modifier_eklendi", satirlar[0]);
            Assert.Contains(satirlar, s => s.Contains("kanit_eklendi") && s.Contains("kural algi"));
            Assert.Contains(satirlar, s => s.Contains("selam_oldu") && s.Contains("kural eylem:selam"));
            Assert.Contains(satirlar, s => s.Contains("karar_verildi") && s.Contains("kural karar"));
            Assert.Contains("karar izi: Ali", metin);
            Assert.Contains("aday selam: puan", metin);
            Assert.Contains("lojistik(k=8, m=0.4)", metin);
            Assert.Contains("kişilik", metin);
        }

        [Fact]
        public void Karar_izi_satirlari_karar_olayinin_altina_girintili_yazilir()
        {
            var sim = Kur();
            var modifier = sim.Gunluk.Olaylar.First(o => o.KuralId == "selam_tanigi");

            var satirlar = NedenYazici.Yaz(sim.Gunluk, modifier.Id, TamIzler(sim)).ToList();

            var kararSatiri = satirlar.FindIndex(s => s.Contains("karar_verildi"));
            var detay = satirlar.FindIndex(s => s.Contains("karar izi:"));
            Assert.True(detay == kararSatiri + 1, "karar izi satırları karar olayının hemen altında olmalı");
            var girinti = satirlar[kararSatiri].Length - satirlar[kararSatiri].TrimStart().Length;
            Assert.True(satirlar[detay].Length - satirlar[detay].TrimStart().Length > girinti);
        }

        [Fact]
        public void Karar_olayinin_kendisi_sorgulaninca_izi_yazilir()
        {
            var sim = Kur();

            var satirlar = NedenYazici.Yaz(sim.Gunluk, sim.SonKararOlayi("Ali")!.Id);

            // Tam iz verilmediği için günlükteki kısa iz gösterilir.
            Assert.Contains(satirlar, s => s.Contains("karar izi (kısa): Ali"));
            Assert.Contains(satirlar, s => s.Contains("seçilen:"));
            Assert.DoesNotContain(satirlar, s => s.Contains("aday selam: puan"));
        }

        [Fact]
        public void Tam_iz_verilmeyen_zincir_kisa_izle_calisir_ve_bilesenleri_yazmaz()
        {
            var sim = Kur();
            var modifier = sim.Gunluk.Olaylar.First(o => o.KuralId == "selam_tanigi");

            var satirlar = NedenYazici.Yaz(sim.Gunluk, modifier.Id);

            Assert.Contains(satirlar, s => s.Contains("karar izi (kısa)"));
            Assert.DoesNotContain(satirlar, s => s.Contains("lojistik"));
        }

        [Fact]
        public void Tam_iz_verilen_karar_olayi_sorgulaninca_bilesenler_yazilir()
        {
            var sim = Kur();
            var karar = sim.Gunluk.Olaylar.First(o => o.Tur == "karar_verildi" && o.Tick == 1 && o.Fail == "Ali");

            var satirlar = NedenYazici.Yaz(sim.Gunluk, karar.Id, TamIzler(sim));

            Assert.Contains(satirlar, s => s.Contains("karar izi: Ali"));
            Assert.Contains(satirlar, s => s.Contains("aday selam: puan"));
        }
    }
}
