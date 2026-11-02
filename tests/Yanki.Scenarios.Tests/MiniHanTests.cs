using System;
using System.IO;
using System.Linq;
using Xunit;
using Yanki.Cli;
using Yanki.Core.Events;
using Yanki.Core.Simulation;

namespace Yanki.Scenarios.Tests
{
    /// <summary>
    /// samples/MiniHan uçtan uca: oyuncu eylemi → algı → kural → fikir değişimi → <c>why</c> zinciri (4. hafta kontrol noktası).
    /// Akış: t1 <c>yardim_etti</c> → Ayse, t2 <c>tehdit_etti</c> → Can (ikisi de han'da; Veli pazarda).
    /// </summary>
    public class MiniHanTests
    {
        private static Simulasyon Calistir(ulong seed = 42)
        {
            var yol = Path.Combine(RepoKoku(), "samples", "MiniHan", "scenario.json");
            var hata = new StringWriter();
            var sim = SenaryoDosyasi.Yukle(yol, seed, hata);
            Assert.True(sim != null, hata.ToString());
            sim!.Submit(new PlayerAction("yardim_etti", "Ayse"));
            sim.Tick(1);
            sim.Submit(new PlayerAction("tehdit_etti", "Can"));
            sim.Tick(5);
            return sim;
        }

        [Fact]
        public void Eylem_fikri_degistirir_hedef_ve_tanik_ayrimi_dogrudur()
        {
            var sim = Calistir();
            var t = sim.SimdikiTick;

            // Ayse: yardımın hedefi (minnet +15) ve tehdidin tanığı (tehdit_gordu −5).
            var ayse = sim.Zihinler.FikirDokumu("Ayse", "oyuncu", t);
            Assert.Equal(10m, ayse.Toplam);
            Assert.Contains(ayse.Katkilar, k => k.Kaynak == "modifier:minnet" && k.Deger == 15m);
            Assert.Contains(ayse.Katkilar, k => k.Kaynak == "modifier:tehdit_gordu" && k.Deger == -5m);

            // Can: tehdidin hedefi (korku +15, tehdit_edildi −20, öfke); yardımın hedefi olmadığı için minnet yok.
            var can = sim.Zihinler.FikirDokumu("Can", "oyuncu", t);
            Assert.Equal(-23m, can.Toplam);
            Assert.Equal(15m, sim.Zihinler["Can"].Eksen("oyuncu", "korku"));
            Assert.DoesNotContain(can.Katkilar, k => k.Kaynak == "modifier:minnet");
            Assert.DoesNotContain(can.Katkilar, k => k.Kaynak == "modifier:tehdit_gordu");
            Assert.Contains(sim.Gunluk.Olaylar, o => o.Tur == "duygu_degisti" && o.Deger("npc") == "Can" && o.Deger("tur") == "ofke");

            // Veli başka mekanda: hiçbir şey görmez.
            Assert.Equal(0m, sim.Zihinler.Fikir("Veli", "oyuncu", t));
            Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.Tur != "varlik_eklendi" && o.Tick > 0 && o.Mekan == "pazar");
            Assert.Empty(sim.Zihinler["Veli"].Hafiza(t));
        }

        [Fact]
        public void Why_zinciri_modifierdan_oyuncu_eylemine_iner()
        {
            var sim = Calistir();
            var modifier = sim.Gunluk.Olaylar.Single(o => o.Tur == "modifier_eklendi" && o.Deger("npc") == "Ayse" && o.Deger("tur") == "minnet");
            var eylem = sim.Gunluk.Olaylar.Single(o => o.Tur == "yardim_etti");

            // Fikir dökümündeki katkının nedeni modifier olayıdır; oradan eyleme inilir.
            var katki = sim.Zihinler.FikirDokumu("Ayse", "oyuncu", sim.SimdikiTick).Katkilar.Single(k => k.Kaynak == "modifier:minnet");
            Assert.Equal(modifier.Id, katki.NedenOlayId);

            var zincir = sim.Neden(modifier.Id);
            Assert.Contains(zincir, o => o.Id == eylem.Id && o.KuralId == "player");
            Assert.Contains(zincir, o => o.Tur == "kanit_eklendi" && o.KuralId == "algi" && o.Deger("npc") == "Ayse");
            Assert.Equal("yardim_minneti", modifier.KuralId);

            var satirlar = NedenYazici.Yaz(sim.Gunluk, modifier.Id);
            Assert.StartsWith("#" + modifier.Id + " modifier_eklendi", satirlar[0]);
            Assert.Contains(satirlar, l => l.Contains("yardim_etti") && l.Contains("kural player"));
        }

        [Fact]
        public void Tanik_modifierinin_zinciri_tehdit_eylemine_iner()
        {
            var sim = Calistir();
            var modifier = sim.Gunluk.Olaylar.Single(o => o.Tur == "modifier_eklendi" && o.Deger("tur") == "tehdit_gordu");
            var eylem = sim.Gunluk.Olaylar.Single(o => o.Tur == "tehdit_etti");

            Assert.Equal("tehdit_tanigi", modifier.KuralId);
            Assert.Contains(sim.Neden(modifier.Id), o => o.Id == eylem.Id);
        }

        [Fact]
        public void Her_kural_olayi_kural_kimligi_ve_nedeni_tasir()
        {
            var sim = Calistir();
            var kuralOlaylari = sim.Gunluk.Olaylar.Where(o => o.Tick > 0 && o.KuralId != "player" && o.KuralId != "algi").ToList();

            Assert.NotEmpty(kuralOlaylari);
            Assert.All(kuralOlaylari, o =>
            {
                Assert.Contains(o.KuralId, new[] { "yardim_minneti", "tehdit_korkusu", "tehdit_cesur_ofkesi", "tehdit_tanigi" });
                Assert.NotEmpty(o.NedenOlaylar);
            });
            Assert.All(sim.Gunluk.Olaylar.Where(o => o.Tick > 0 && o.KuralId != "player"), o => Assert.NotEmpty(o.NedenOlaylar));
        }

        [Fact]
        public void Ayni_seed_ayni_hash_farkli_eylem_farkli_hash()
        {
            Assert.Equal(Calistir().DurumHash(), Calistir().DurumHash());

            var yol = Path.Combine(RepoKoku(), "samples", "MiniHan", "scenario.json");
            var baska = SenaryoDosyasi.Yukle(yol, 42, new StringWriter())!;
            baska.Submit(new PlayerAction("tehdit_etti", "Ayse"));
            baska.Tick(1);
            baska.Submit(new PlayerAction("yardim_etti", "Can"));
            baska.Tick(5);
            Assert.NotEqual(Calistir().DurumHash(), baska.DurumHash());
        }

        [Fact]
        public void Golden_oturum_bu_akisin_hashini_saklar()
        {
            var kayit = OturumKaydi.Oku(File.ReadAllText(Path.Combine(RepoKoku(), "tests", "golden", "mini-han-1.json")), "mini-han-1.json");

            Assert.True(kayit.Basarili, string.Join("\n", kayit.Hatalar));
            Assert.Equal(Calistir((ulong)42).DurumHash(), kayit.Kayit!.SonHash);
        }

        private static string RepoKoku()
        {
            var dizin = new DirectoryInfo(AppContext.BaseDirectory);
            while (dizin != null && !File.Exists(Path.Combine(dizin.FullName, "Yanki.slnx")))
            {
                dizin = dizin.Parent;
            }

            return dizin?.FullName ?? throw new InvalidOperationException("Repo kökü (Yanki.slnx) bulunamadı.");
        }
    }
}
