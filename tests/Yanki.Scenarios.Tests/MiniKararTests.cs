using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Yanki.Cli;
using Yanki.Core.Decision;
using Yanki.Core.Events;
using Yanki.Core.Simulation;

namespace Yanki.Scenarios.Tests
{
    /// <summary>
    /// samples/MiniKarar uçtan uca (5. hafta kontrol noktası): NPC kendi eylemini seçer → olaya dönüşür → başka NPC algılar → kural çalışır
    /// → fikir değişir → <c>why</c> bu zinciri karar izine ve puan bileşenlerine kadar gösterir → aynı seed aynı sonucu verir.
    /// Akış (seed 42, toplam 24 tick): <c>t3 yardim_etti:Ayse</c>, <c>t5 hareket:pazar</c>. Golden: <c>tests/golden/mini-karar-1.json</c>.
    /// </summary>
    public class MiniKararTests
    {
        private static Simulasyon Calistir(ulong seed = 42)
        {
            var yol = Path.Combine(RepoKoku(), "samples", "MiniKarar", "scenario.json");
            var hata = new StringWriter();
            var sim = SenaryoDosyasi.Yukle(yol, seed, hata);
            Assert.True(sim != null, hata.ToString());
            sim!.Tick(2);
            sim.Submit(new PlayerAction("yardim_etti", "Ayse"));
            sim.Tick(2);
            sim.Submit(new PlayerAction("hareket", "pazar"));
            sim.Tick(20);
            Assert.Equal(24, sim.SimdikiTick);
            return sim;
        }

        // ---- yardımcılar (günlükten türetilir) ----

        private static IEnumerable<Olay> NpcEylemleri(Simulasyon s) =>
            s.Gunluk.Olaylar.Where(o => o.KuralId.StartsWith("eylem:", StringComparison.Ordinal) && o.Tur != "ayrildi" && o.Tur != "geldi" && o.Tur != "gercek_eklendi" && !ZihinTuru(o.Tur));

        private static bool ZihinTuru(string tur) => tur == "hafiza_eklendi" || tur == "modifier_eklendi" || tur == "eksen_degisti" || tur == "duygu_degisti" || tur == "kanit_eklendi" || tur == "stres_degisti";

        // tick başındaki konumlar: önceki tick'lerde yazılmış konum! olaylarından.
        private static Dictionary<string, string> TickBasiKonumlar(Simulasyon s, long tick)
        {
            var k = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var o in s.Gunluk.Olaylar)
            {
                if (o.Tick >= tick)
                {
                    break;
                }

                if (o.Tur == "gercek_eklendi")
                {
                    var m = Regex.Match(o.Deger("gercek"), @"^konum!\((\w+),(\w+)\)$");
                    if (m.Success)
                    {
                        k[m.Groups[1].Value] = m.Groups[2].Value;
                    }
                }
            }

            return k;
        }

        private static bool Algiladi(Simulasyon s, long olayId, string npc) =>
            s.Gunluk.Olaylar.Any(o => o.KuralId == "algi" && o.Tur == "hafiza_eklendi" && o.Deger("npc") == npc && o.NedenOlaylar.Contains(olayId));

        // ---- 1. NPC eylemi kuralı tetikler ve başka NPC'nin fikrini değiştirir ----

        [Fact]
        public void NPC_eylemi_yardim_minneti_kuralini_tetikler_ve_hedefin_fikrini_degistirir()
        {
            var sim = Calistir();

            var yardimlar = NpcEylemleri(sim).Where(o => o.Tur == "yardim_etti" && o.Tick < sim.SimdikiTick).ToList(); // kural bir sonraki tick'te çalışır
            Assert.NotEmpty(yardimlar);
            foreach (var y in yardimlar)
            {
                var minnet = sim.Gunluk.Olaylar.Single(o => o.KuralId == "yardim_minneti" && o.Tick == y.Tick + 1 && o.Deger("npc") == y.Hedef && o.Deger("hedef") == y.Fail);
                Assert.Equal("minnet", minnet.Deger("tur"));
                Assert.Contains(y.Id, minnet.NedenOlaylar);
                Assert.True(sim.Zihinler.Fikir(y.Hedef, y.Fail, y.Tick + 1) > 0m, $"{y.Hedef} -> {y.Fail} fikri yardım sonrası pozitif olmalı");
            }
        }

        // ---- 2. Oyuncu eylemi NPC'de minnet oluşturur ----

        [Fact]
        public void Oyuncunun_yardimi_Ayse_de_minnet_olusturur()
        {
            var sim = Calistir();

            var minnet = Assert.Single(sim.Gunluk.Olaylar, o => o.KuralId == "yardim_minneti" && o.Deger("npc") == "Ayse" && o.Deger("hedef") == "oyuncu");

            Assert.Equal(3, minnet.Tick);
            Assert.Equal("minnet", minnet.Deger("tur"));
            Assert.Equal("15", minnet.Deger("deger"));
            Assert.True(sim.Zihinler.Fikir("Ayse", "oyuncu", 3) >= 15m);
        }

        // ---- 3. Eş zamanlı algı: ayrılan tanık görür, varan görmez ----

        [Fact]
        public void Ayni_tickte_ayrilan_tanik_eylemi_gorur_varan_gormez()
        {
            var sim = Calistir();
            var ayrilanGoren = 0;
            var varanGormeyen = 0;
            var hatalar = new List<string>();

            foreach (var e in NpcEylemleri(sim))
            {
                var once = TickBasiKonumlar(sim, e.Tick);
                foreach (var g in sim.Gunluk.Olaylar.Where(o => o.Tick == e.Tick && o.Tur == "geldi" && o.Fail != e.Fail && sim.Zihinler.Var(o.Fail)))
                {
                    var ayrildi = sim.Gunluk.Olaylar.Single(o => o.Tick == e.Tick && o.Tur == "ayrildi" && o.Fail == g.Fail);
                    var varisMi = g.Mekan == e.Mekan && once[g.Fail] != e.Mekan;
                    var ayrilisMi = ayrildi.Mekan == e.Mekan && once[g.Fail] == e.Mekan;
                    if (ayrilisMi)
                    {
                        ayrilanGoren++;
                        if (!Algiladi(sim, e.Id, g.Fail))
                        {
                            hatalar.Add($"t{e.Tick} #{e.Id}: ayrılan {g.Fail} olayı görmedi");
                        }
                    }

                    if (varisMi)
                    {
                        varanGormeyen++;
                        if (Algiladi(sim, e.Id, g.Fail))
                        {
                            hatalar.Add($"t{e.Tick} #{e.Id}: varan {g.Fail} olayı gördü");
                        }
                    }
                }
            }

            Assert.Empty(hatalar);
            Assert.True(ayrilanGoren > 0, "akışta aynı tick'te ayrılan bir tanık bulunmalı");
            Assert.True(varanGormeyen > 0, "akışta aynı tick'te varan bir NPC bulunmalı");
        }

        // ---- 4. Saklı algılayan: tanık taşındıktan sonra da kural doğru NPC için çalışır ----

        [Fact]
        public void Kural_tanik_tasindiktan_sonra_da_dogru_npc_icin_calisir()
        {
            var sim = Calistir();
            var tasinanHedefler = 0;

            foreach (var y in NpcEylemleri(sim).Where(o => o.Tur == "yardim_etti" && o.Tick < sim.SimdikiTick))
            {
                var sonra = TickBasiKonumlar(sim, y.Tick + 1);
                if (sonra[y.Hedef] != y.Mekan)
                {
                    tasinanHedefler++;
                    // Hedef olayın mekanından ayrıldı ama kural yine de ona çalıştı.
                    Assert.Single(sim.Gunluk.Olaylar, o => o.KuralId == "yardim_minneti" && o.Tick == y.Tick + 1 && o.Deger("npc") == y.Hedef);
                }

                // Olayı algılamayan kimseye çalışmadı.
                foreach (var m in sim.Gunluk.Olaylar.Where(o => o.KuralId == "yardim_minneti" && o.Tick == y.Tick + 1 && o.NedenOlaylar.Contains(y.Id)))
                {
                    Assert.True(Algiladi(sim, y.Id, m.Deger("npc")), $"{m.Deger("npc")} olayı algılamadan kural çalıştı");
                }
            }

            Assert.True(tasinanHedefler > 0, "akışta yardımın hedefi, yardımla aynı tick'te mekandan ayrılmalı");
        }

        // ---- 5. Oyuncu hareketi ----

        [Fact]
        public void Oyuncu_hareketi_ayrildi_ve_geldi_uretir_ve_inanclarda_mekan_gorunur()
        {
            var sim = Calistir();

            var ayrildi = Assert.Single(sim.Gunluk.Olaylar, o => o.Tur == "ayrildi" && o.Fail == "oyuncu");
            var geldi = Assert.Single(sim.Gunluk.Olaylar, o => o.Tur == "geldi" && o.Fail == "oyuncu");
            Assert.Equal(5, ayrildi.Tick);
            Assert.Equal(("han", "pazar"), (ayrildi.Mekan, ayrildi.Hedef));
            Assert.Equal(("pazar", "han"), (geldi.Mekan, geldi.Hedef));
            Assert.Equal("pazar", sim.Dunya.TekDeger("konum!", "oyuncu"));

            // han'dakiler oyuncunun pazara gittiğini, pazardakiler oyuncunun hana'dan geldiğini görür; mekan önermede yazar.
            var goren = sim.Zihinler.Kimlikler.Where(n => sim.Zihinler[n].Kanitlar("ayrildi(oyuncu,pazar)").Count > 0).ToList();
            var gelenGoren = sim.Zihinler.Kimlikler.Where(n => sim.Zihinler[n].Kanitlar("geldi(oyuncu,han)").Count > 0).ToList();
            Assert.Contains("Ayse", goren);
            Assert.NotEmpty(gelenGoren);
            Assert.DoesNotContain(gelenGoren, n => goren.Contains(n));
        }

        // ---- 6. Havuzda ≥2 aday, zarla seçim; seed farkı ----

        private static List<(long Tick, string Npc, string Secilen, int Havuz, bool Zarli)> Kararlar(Simulasyon sim) =>
            sim.Gunluk.Olaylar.Where(o => o.Tur == "karar_verildi").Select(o =>
            {
                var k = KisaIz.Oku(o);
                return (o.Tick, o.Fail, o.Deger("secilen_eylem") + ">" + o.Deger("secilen_hedef"), k.Havuz.Count, k.Zar.HasValue);
            }).ToList();

        [Fact]
        public void En_az_bir_kararda_havuzda_iki_aday_vardir_ve_secim_zarla_belirlenir()
        {
            var kararlar = Kararlar(Calistir());

            var zarli = kararlar.Where(k => k.Havuz >= 2).ToList();
            Assert.NotEmpty(zarli);
            Assert.All(zarli, k => Assert.True(k.Zarli));
            Assert.DoesNotContain(kararlar.Where(k => k.Havuz < 2), k => k.Zarli);
        }

        [Fact]
        public void Seed_42_ve_7_en_az_bir_kararda_farkli_secim_uretir()
        {
            var a = Kararlar(Calistir(42));
            var b = Kararlar(Calistir(7));

            var farklar = a.Zip(b, (x, y) => (x, y)).Where(p => p.x.Tick == p.y.Tick && p.x.Npc == p.y.Npc && p.x.Secilen != p.y.Secilen).ToList();
            Assert.NotEmpty(farklar);
        }

        [Fact]
        public void Ayni_seed_ayni_hash_ve_ayni_olay_gunlugunu_verir()
        {
            var a = Calistir(42);
            var b = Calistir(42);

            Assert.Equal(a.SonHash, b.SonHash);
            Assert.Equal(Imza(a), Imza(b));
            Assert.NotEqual(Calistir(7).SonHash, a.SonHash);
        }

        private static string Imza(Simulasyon s) =>
            string.Join("\n", s.Gunluk.Olaylar.Select(o =>
                $"{o.Id}|{o.Tick}|{o.Sira}|{o.Tur}|{o.Fail}|{o.Hedef}|{o.Mekan}|{string.Join(";", o.Yuk.Select(kv => kv.Key + "=" + kv.Value))}|{string.Join(",", o.NedenOlaylar)}|{o.KuralId}"));

        // ---- 7. hafiza_var ile tekrar sınırı, dinlen ----

        [Fact]
        public void Tekrar_hafiza_var_ile_sinirlanir_ve_NPC_dinlen_ile_bosta_kalabilir()
        {
            var sim = Calistir();
            var hatalar = new List<string>();

            foreach (var (eylemTuru, pencere) in new[] { ("selam_verdi", 6), ("yardim_etti", 12) })
            {
                foreach (var npc in new[] { "Ayse", "Can", "Veli" })
                {
                    var ticklar = NpcEylemleri(sim).Where(o => o.Tur == eylemTuru && o.Fail == npc).Select(o => o.Tick).ToList();
                    for (var i = 1; i < ticklar.Count; i++)
                    {
                        if (ticklar[i] - ticklar[i - 1] <= pencere)
                        {
                            hatalar.Add($"{npc} {eylemTuru}: tick {ticklar[i - 1]} ve {ticklar[i]} pencere {pencere} içinde");
                        }
                    }
                }
            }

            Assert.Empty(hatalar);
            Assert.Contains(NpcEylemleri(sim), o => o.Tur == "dinlendi" && o.Fail == "Ayse");
            Assert.Contains(NpcEylemleri(sim), o => o.Tur == "dinlendi" && o.Fail == "Can");
            // Sınır gerçekten bağlayıcı: her NPC için selam 24 tick'te 4 kez bile seçilemez (pencere 6 → en çok 4).
            Assert.All(new[] { "Ayse", "Can", "Veli" }, n => Assert.True(NpcEylemleri(sim).Count(o => o.Tur == "selam_verdi" && o.Fail == n) <= 4));
        }

        // ---- why: modifier → algı → eylem olayı → karar → adaylar ve puan bileşenleri (gerçek CLI çıktısı) ----

        private static (int Kod, string Cikti, string Hata) Cli(params string[] args)
        {
            var cikti = new StringWriter();
            var hata = new StringWriter();
            var kod = KomutCalistirici.Calistir(args, cikti, hata);
            return (kod, cikti.ToString(), hata.ToString());
        }

        [Fact]
        public void Why_modifierdan_algiya_eyleme_ve_karar_izinin_puan_bilesenlerine_iner()
        {
            var oturum = Path.Combine(Path.GetTempPath(), "yanki-minikarar-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var senaryo = Path.Combine(RepoKoku(), "samples", "MiniKarar", "scenario.json");
                var (kod, run, hata) = Cli(
                    "run", senaryo, "--seed", "42", "--tick", "2", "--eylem", "yardim_etti:Ayse", "--tick", "2", "--eylem", "hareket:pazar", "--tick", "20",
                    "--kaydet", oturum);
                Assert.True(kod == 0, hata);

                // Bir NPC'nin yardımından doğan ilk minnet modifier'ı (oyuncunun değil).
                var minnet = Regex.Matches(run, @"^#(\d+) modifier_eklendi \(tick \d+, kural yardim_minneti\) .*$", RegexOptions.Multiline)
                    .Cast<Match>().First(m => !m.Value.Contains("hedef=oyuncu"));
                var id = minnet.Groups[1].Value;

                var (whyKod, why, whyHata) = Cli("why", oturum, id);

                Assert.True(whyKod == 0, whyHata);
                Assert.Contains("modifier_eklendi", why);
                Assert.Contains("kural yardim_minneti", why);
                Assert.Contains("kanit_eklendi", why);
                Assert.Contains("yardim_etti", why);
                Assert.Contains("kural eylem:yardim_et", why);
                Assert.Contains("karar_verildi", why);
                Assert.Contains("karar izi:", why);
                Assert.DoesNotContain("karar izi (kısa)", why); // tam iz replay ile üretildi
                Assert.Contains("aday yardim_et", why);
                Assert.Contains("puan ", why);
                Assert.Contains("taban 1", why);
                Assert.Contains("kisilik(merhamet)", why);
                Assert.Contains("× kişilik ", why);
                Assert.Contains("× fikir ", why);
                Assert.Contains("× ruh hali ", why);
            }
            finally
            {
                File.Delete(oturum);
            }
        }

        [Fact]
        public void Why_karar_olayi_icin_yeniden_uretilen_iz_gunlukteki_kisa_izle_eslesir_replay_hash_i_tutar()
        {
            var golden = Path.Combine(RepoKoku(), "tests", "golden", "mini-karar-1.json");
            var (kod, cikti, hata) = Cli("replay", golden);
            Assert.True(kod == 0, hata);
            Assert.Contains("hash tutuyor", cikti);

            var sim = Calistir();
            foreach (var karar in sim.Gunluk.Olaylar.Where(o => o.Tur == "karar_verildi" && o.Tick % 6 == 0))
            {
                var (whyKod, why, whyHata) = Cli("why", golden, karar.Id.ToString());
                Assert.True(whyKod == 0, whyHata);
                Assert.Contains("karar izi: " + karar.Fail, why);
            }
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
