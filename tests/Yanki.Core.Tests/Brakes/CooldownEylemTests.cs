using System.Linq;
using Xunit;
using Yanki.Core.Decision;
using Yanki.Core.Events;
using Yanki.Core.Mind;
using Yanki.Core.Simulation;
using Yanki.Core.Tests.Decision;
using Yanki.Core.Tests.Simulation;

namespace Yanki.Core.Tests.Brakes
{
    /// <summary>
    /// 7. hafta (B): eylem cooldown ve en_fazla. Fikstür: Ayse, Can, Zeki han'da, Veli pazar'da (KonusmaAdimiTests.Senaryo). Kasaba Hanı içeriği DEĞİLDİR.
    /// Karar başına 1 sayılır; aday frenleri AdayUretici'de hesaplanır (adım 5 saf) ve 6.1'de yazılır.
    /// </summary>
    public class CooldownEylemTests
    {
        private static Simulasyon Kur(string[] eylemler, IActionSelector? secici = null)
        {
            var senaryo = SenaryoYukleyici.Yukle(KonusmaAdimiTests.Senaryo(), "s.json");
            Assert.True(senaryo.Basarili, string.Join("\n", senaryo.Hatalar));
            return Simulasyon.Olustur(
                senaryo.Senaryo!, new Yanki.Core.Rules.KuralTanimi[0], 42, new ZihinAyarlari { KanitSonumuTickBasina = 0m },
                eylemler: KararFikstur.Yukle(eylemler), secici: secici);
        }

        private static long[] Ticks(Simulasyon s, string tur, string fail) =>
            s.Gunluk.Olaylar.Where(o => o.Tur == tur && o.Fail == fail).Select(o => o.Tick).ToArray();

        private static Olay[] Frenler(Simulasyon s, string npc, string? tur = null) =>
            s.Frenler(tur, npc).ToArray();

        [Fact]
        public void Eylem_cooldown_3_t1_t4_t7de_calisir_aradaki_tickler_kapali()
        {
            var s = Kur(new[] { KararFikstur.Eylem("selam", "yok", "\"cooldown\": 3") });
            s.Tick(7);

            Assert.Equal(new long[] { 1, 4, 7 }, Ticks(s, "selam_oldu", "Ayse"));
        }

        [Fact]
        public void Aday_freni_engel_basina_bir_kez_yazilir_engel_kalkip_yeniden_baslarsa_bir_kez_daha()
        {
            var s = Kur(new[] { KararFikstur.Eylem("selam", "yok", "\"cooldown\": 3") });
            s.Tick(7);

            var fren = Frenler(s, "Ayse", "cooldown");
            Assert.Equal(new long[] { 2, 5 }, fren.Select(f => f.Tick).ToArray());   // t3 ve t6'da tekrar yok
            Assert.All(fren, f =>
            {
                Assert.Equal("selam", f.Deger("kimlik"));
                Assert.Equal(string.Empty, f.Deger("hedef"));
                Assert.Equal("fren:cooldown", f.KuralId);
            });
            var ilkTetikleme = s.Gunluk.Olaylar.First(o => o.Tur == "selam_oldu" && o.Fail == "Ayse");
            Assert.Equal(new[] { ilkTetikleme.Id }, fren[0].NedenOlaylar.ToArray());   // neden: engeli doğuran son tetikleme
            Assert.Contains(s.Neden(fren[0].Id), o => o.Id == ilkTetikleme.Id);
        }

        [Fact]
        public void Elenme_nedeni_karar_izinde_gorunur()
        {
            var s = Kur(new[] { KararFikstur.Eylem("selam", "yok", "\"cooldown\": 3"), KararFikstur.Eylem("dinlen", "yok", string.Empty, 0.1m) });
            s.Tick(2);

            var iz = s.KararIzi("Ayse")!;
            var selam = iz.Adaylar.Single(a => a.Aday.Eylem.Id == "selam");
            Assert.True(selam.Elendi);
            Assert.Contains("cooldown", selam.Neden);
        }

        [Fact]
        public void On_kosulu_tutmayan_aday_icin_fren_olayi_yazilmaz_ama_neden_izde_yine_gorunur()
        {
            // selam bir kez çalışınca hafiza_var(selam_oldu, ben, 24) tutar ve ön koşul (== false) 24 tick tutmaz; cooldown da kapalı.
            var selam = KararFikstur.Eylem("selam", "yok", "\"cooldown\": 3, \"on_kosullar\": [ { \"alan\": \"hafiza_var(selam_oldu, ben, 24)\", \"op\": \"==\", \"deger\": false } ]");
            var s = Kur(new[] { selam, KararFikstur.Eylem("dinlen", "yok", string.Empty, 0.1m) });
            s.Tick(2);

            var elenen = s.KararIzi("Ayse")!.Adaylar.Single(a => a.Aday.Eylem.Id == "selam");
            Assert.True(elenen.Elendi);
            Assert.Contains("on_kosul", elenen.Neden);
            Assert.Contains("cooldown", elenen.Neden);                         // cooldown kapalıyken de ikisi birden görünür

            s.Tick(4);
            Assert.Equal(new long[] { 1 }, Ticks(s, "selam_oldu", "Ayse"));    // t4'te cooldown açıldı ama ön koşul hâlâ tutmuyor
            Assert.Empty(Frenler(s, "Ayse"));                                  // ön koşul tutsaydı bile aday olmayacaktı: fren olayı yok
        }

        [Fact]
        public void Kapsam_npc_tum_hedefleri_kapatir_kapsam_hedef_yalnizca_o_hedefi()
        {
            var npc = Kur(new[] { KararFikstur.Eylem("dost", "npc", "\"cooldown\": 100, \"cooldown_kapsam\": \"npc\"") });
            npc.Tick(4);
            Assert.Single(npc.Gunluk.Olaylar, o => o.Tur == "dost_oldu" && o.Fail == "Ayse");

            var hedef = Kur(new[] { KararFikstur.Eylem("dost", "npc", "\"cooldown\": 100, \"cooldown_kapsam\": \"hedef\"") });
            hedef.Tick(4);
            var olaylar = hedef.Gunluk.Olaylar.Where(o => o.Tur == "dost_oldu" && o.Fail == "Ayse").ToArray();
            Assert.Equal(2, olaylar.Length);                                   // Can ve Zeki: biri kapanınca diğeri açık
            Assert.Equal(2, olaylar.Select(o => o.Hedef).Distinct().Count());
        }

        [Fact]
        public void Kapsam_varsayilani_hedeftir()
        {
            var s = Kur(new[] { KararFikstur.Eylem("dost", "npc", "\"cooldown\": 100") });
            s.Tick(4);
            Assert.Equal(2, s.Gunluk.Olaylar.Count(o => o.Tur == "dost_oldu" && o.Fail == "Ayse"));
        }

        [Fact]
        public void Hareket_ayrildi_ve_geldi_sayaci_bir_kez_artirir_en_fazla_2_iki_hareket_demektir()
        {
            var s = Kur(new[] { KararFikstur.Eylem("git", "komsu_mekan", "\"en_fazla\": 2, \"cooldown_kapsam\": \"npc\"") });
            s.Tick(5);

            Assert.Equal(2, s.Gunluk.Olaylar.Count(o => o.Tur == "geldi" && o.Fail == "Ayse"));
            Assert.Equal(2, s.Gunluk.Olaylar.Count(o => o.Tur == "ayrildi" && o.Fail == "Ayse"));
            var fren = Frenler(s, "Ayse", "en_fazla");
            Assert.Single(fren);                                               // en_fazla hiç kalkmaz: t3'te bir kez, sonrasında tekrar yok
            Assert.Equal(3, fren[0].Tick);
        }

        [Fact]
        public void Konusmada_cok_konulu_anlatti_olaylari_sayaci_bir_kez_artirir()
        {
            var anlat = "{ \"id\": \"anlat\", \"olay_turu\": \"anlatti\", \"hedef\": \"npc\", \"taban\": 1, \"aktarim\": \"dogru\", \"en_fazla\": 2, "
                + "\"cooldown_kapsam\": \"npc\", \"konu_desenleri\": [\"borclu(*,*,*)\", \"kardes(*,*)\"] }";
            var k = new KonusmaAdimiTests.Kaydedici().Sec("Ayse", 2, "anlat|Can").Sec("Ayse", 3, "anlat|Zeki");
            var senaryo = SenaryoYukleyici.Yukle(KonusmaAdimiTests.Senaryo(), "s.json").Senaryo!;
            var sim = Simulasyon.Olustur(
                senaryo, new Yanki.Core.Rules.KuralTanimi[0], 42, new ZihinAyarlari { KanitSonumuTickBasina = 0m },
                eylemler: KararFikstur.Yukle(anlat), secici: k);
            sim.Submit(OyuncuAnlatimiTests.Anlat("Ayse", "borclu(Mehmet,Ayse,500)"));
            sim.Submit(OyuncuAnlatimiTests.Anlat("Ayse", "kardes(Mehmet,Can)"));
            sim.Tick(4);

            // t2 ve t3'te ikişer konu = iki karar. Karar başına 1 sayıldığı için ikinci karar (t3) kapalı değildir; t4 kapanmıştır.
            Assert.Equal(2, sim.Gunluk.Olaylar.Count(o => o.Tur == "anlatti" && o.Fail == "Ayse" && o.Tick == 2));
            Assert.Equal(2, sim.Gunluk.Olaylar.Count(o => o.Tur == "anlatti" && o.Fail == "Ayse" && o.Tick == 3));
            // t3'ün seçilebilmesi (Kaydedici aday bulamazsa atar) iki konulu t2 kararının sayacı bir artırdığını gösterir; t4'te aday kalmaz.
            Assert.DoesNotContain("Ayse@4", k.Adaylar.Keys);
        }

        [Fact]
        public void Fren_alani_olmayan_eylemde_sayac_ve_fren_olayi_yoktur()
        {
            var s = Kur(new[] { KararFikstur.Eylem("selam", "yok") });
            s.Tick(5);

            Assert.Empty(s.Frenler());
            Assert.Equal(string.Empty, s.SayaclarKanonikMetni);
        }
    }
}
