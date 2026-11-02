using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Decision;
using Yanki.Core.Events;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;
using Yanki.Core.Tests.Decision;

namespace Yanki.Core.Tests.Simulation
{
    /// <summary>
    /// K3: günlük kısa izi taşır; geçmiş bir kararın TAM izi replay ile o karara kadar gidilerek yeniden üretilir ve günlükteki kısa izle
    /// karşılaştırılır. Bellekte NPC başına yalnızca SON tam iz tutulur; gizli önbellek yoktur, geçmiş izler açıkça istenir.
    /// Fikstür: Ali ve Veli han'da, Can pazar'da; üç eşit puanlı hedefsiz eylem (zar atılır). Kasaba Hanı içeriği değildir.
    /// </summary>
    public class TamIzYenidenUretimTests
    {
        private static string[] Uc() => new[]
        {
            KararFikstur.Eylem("a", taban: 3m, ek: "\"olcutler\": [ { \"alan\": \"kisilik(sosyallik)\" } ]"),
            KararFikstur.Eylem("b", taban: 3m),
            KararFikstur.Eylem("c", taban: 3m),
            KararFikstur.Eylem("yardim", "npc", taban: 2.5m),
        };

        private const string YardimKurali = "{ \"kurallar\": [ { \"id\": \"yardim_tanigi\", \"ne_zaman\": \"yardim_oldu\", \"kriterler\": [ { \"alan\": \"olay_rolu(hedef)\", \"op\": \"==\", \"deger\": true } ],"
            + " \"etkiler\": [ { \"tur\": \"modifier\", \"hedef\": \"olay.fail\", \"modifier\": \"minnet\", \"deger\": 5, \"sure\": 100 } ] } ] }";

        private static Simulasyon Yeni(ulong seed = 42, string[]? eylemJson = null, IActionSelector? secici = null)
        {
            var kural = KuralYukleyici.Yukle(YardimKurali, "k.json", new[] { "yardim_etti", "tehdit_etti", "yardim_oldu" });
            Assert.True(kural.Basarili, string.Join("\n", kural.Hatalar));
            return Simulasyon.Olustur(SimFikstur.Senaryo(), kural.Kurallar, seed, eylemler: KararFikstur.Yukle(eylemJson ?? Uc()), secici: secici);
        }

        // Oyuncu eylemleriyle 12 tick'lik bir oturum; sim ve onu yeniden kuran kayıt döner.
        private static (Simulasyon Sim, OturumKaydi Kayit) Calistir(ulong seed = 42)
        {
            var sim = Yeni(seed);
            sim.Tick(3);
            sim.Submit(new PlayerAction("yardim_etti", "Ali"));
            sim.Tick(4);
            sim.Submit(new PlayerAction("hareket", "pazar"));
            sim.Tick(5);
            return (sim, OturumKaydi.Uret(sim, "senaryo.json"));
        }

        // ---- yakalama API'si ----

        [Fact]
        public void Gecmis_tick_icin_tam_iz_istemek_acik_hatadir()
        {
            var sim = Yeni();
            sim.Tick(3);

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => sim.TamIziIste("Ali", 2));

            Assert.Contains("replay", ex.Message);
        }

        [Fact]
        public void Bilinmeyen_npc_icin_tam_iz_istemek_hatadir() =>
            Assert.Throws<ArgumentException>(() => Yeni().TamIziIste("Hayalet", 1));

        [Fact]
        public void Istenen_tickte_tam_iz_yakalanir_istenmeyenler_yakalanmaz()
        {
            var sim = Yeni();
            sim.TamIziIste("Ali", 3);
            sim.Tick(5);

            var iz = sim.YakalananIz("Ali", 3);

            Assert.NotNull(iz);
            Assert.Equal(3, iz!.Tick);
            Assert.NotEmpty(iz.Adaylar);
            Assert.Null(sim.YakalananIz("Ali", 2));
            Assert.Null(sim.YakalananIz("Veli", 3));
            Assert.Equal(5, sim.KararIzi("Ali")!.Tick);
        }

        [Fact]
        public void Son_tam_izler_npc_basina_birden_fazla_kayit_biriktirmez()
        {
            var sim = Yeni();

            sim.Tick(60);

            Assert.Equal(3, sim.TutulanTamIzSayisi); // Ali, Can, Veli: NPC başına tek son iz
            sim.TamIziIste("Ali", 61);
            sim.Tick(10);
            Assert.Equal(4, sim.TutulanTamIzSayisi); // yalnızca açıkça istenen bir iz eklendi
        }

        [Fact]
        public void Tam_iz_yakalamak_hash_i_ve_gunlugu_degistirmez()
        {
            var yakalamasiz = Yeni();
            yakalamasiz.Tick(12);
            var yakalamali = Yeni();
            for (var t = 1; t <= 12; t++)
            {
                foreach (var npc in new[] { "Ali", "Can", "Veli" })
                {
                    yakalamali.TamIziIste(npc, t);
                }

                yakalamali.Tick(1);
            }

            Assert.Equal(yakalamasiz.DurumHash(), yakalamali.DurumHash());
            Assert.Equal(GunlukImzasi.Al(yakalamasiz), GunlukImzasi.Al(yakalamali));
        }

        // ---- geçmiş karar: yalnızca kısa iz ----

        [Fact]
        public void Gecmis_karar_icin_api_acikca_kisa_izi_dondurur()
        {
            var (sim, _) = Calistir();
            var eskiKarar = sim.Gunluk.Olaylar.First(o => o.Tur == "karar_verildi" && o.Tick == 2 && o.Fail == "Ali");

            var kisa = sim.GecmisKararKisaIzi(eskiKarar.Id);

            Assert.IsType<KisaIz>(kisa);
            Assert.Equal(2, kisa.Tick);
            Assert.Equal("Ali", kisa.Npc);
            Assert.Equal(eskiKarar.Deger("secilen_eylem"), kisa.SecilenEylem);
            Assert.Throws<ArgumentException>(() => sim.GecmisKararKisaIzi(sim.Gunluk.Olaylar.First(o => o.Tur != "karar_verildi").Id));
        }

        // ---- yeniden üretim ----

        private static Func<Simulasyon> Fabrika(ulong seed = 42, IActionSelector? secici = null) => () => Yeni(seed, secici: secici);

        [Fact]
        public void Yeniden_uretilen_tam_iz_gunlukteki_kisa_izle_birebir_eslesir()
        {
            var (sim, kayit) = Calistir();
            var kararlar = sim.Gunluk.Olaylar.Where(o => o.Tur == "karar_verildi").ToList();
            Assert.True(kararlar.Count > 30);

            foreach (var karar in kararlar.Where(k => k.Tick % 3 == 0 || k.Tick == 12))
            {
                var tam = KararIziYenidenUretici.Uret(Fabrika(), kayit, karar);

                var kisa = KisaIz.Oku(karar);
                Assert.Null(kisa.Fark(tam));
                Assert.Equal(karar.Fail, tam.Npc);
                Assert.Equal(karar.Tick, tam.Tick);
                Assert.Equal(kisa.SecilenEylem, tam.Secilen?.Eylem.Id ?? string.Empty);
                Assert.Equal(kisa.Havuz.Select(h => h.Anahtar).ToArray(), tam.Havuz.ToArray());
                Assert.Equal(kisa.Zar, tam.Zar);
                Assert.Equal(KararIziYazici.YazKisa(tam), karar.Deger("iz"));
            }
        }

        [Fact]
        public void Yeniden_uretilen_tam_iz_ayrintilari_icerir_kisa_iz_icermez()
        {
            var (sim, kayit) = Calistir();
            var karar = sim.Gunluk.Olaylar.First(o => o.Tur == "karar_verildi" && o.Tick == 5 && o.Fail == "Ali");

            var tam = KararIziYenidenUretici.Uret(Fabrika(), kayit, karar);

            Assert.True(tam.Adaylar.Count >= 4);
            Assert.All(tam.Adaylar.Where(a => !a.Elendi), a => Assert.NotNull(a.Puan));
            Assert.DoesNotContain("olcutler", karar.Deger("iz"));
        }

        [Fact]
        public void Birden_cok_karar_tek_replay_ile_uretilir()
        {
            var (sim, kayit) = Calistir();
            var secilen = sim.Gunluk.Olaylar.Where(o => o.Tur == "karar_verildi" && (o.Tick == 2 || o.Tick == 9)).ToList();

            var izler = KararIziYenidenUretici.UretCoklu(Fabrika(), kayit, secilen);

            Assert.Equal(secilen.Select(o => o.Id).OrderBy(x => x).ToArray(), izler.Keys.OrderBy(x => x).ToArray());
            Assert.All(secilen, o => Assert.Null(KisaIz.Oku(o).Fark(izler[o.Id])));
        }

        [Fact]
        public void Farkli_seed_ile_yeniden_uretim_sessizce_gecmez_acik_hata_verir()
        {
            var (sim, kayit) = Calistir(seed: 42);
            var havuzlu = sim.Gunluk.Olaylar.First(o => o.Tur == "karar_verildi" && KisaIz.Oku(o).Havuz.Count > 1);

            // Aynı eylem günlüğü, başka seed: replay çalışır ama zarlar/seçimler farklıdır → günlükteki kısa iz uyuşmaz.
            var baskaSeedKaydi = new OturumKaydi(43, kayit.Senaryo, kayit.TickSayisi, kayit.Eylemler, kayit.SonHash);
            var ex = Assert.Throws<KararIziUyusmazligiException>(() => KararIziYenidenUretici.Uret(Fabrika(seed: 43), baskaSeedKaydi, havuzlu));

            Assert.Contains("uyuşmuyor", ex.Message);
            Assert.Contains("#" + havuzlu.Id, ex.Message);
        }

        [Fact]
        public void Seed_uyusmazligi_replayde_acik_hatadir()
        {
            var (sim, kayit) = Calistir(seed: 42);
            var karar = sim.Gunluk.Olaylar.First(o => o.Tur == "karar_verildi");

            Assert.Throws<ArgumentException>(() => KararIziYenidenUretici.Uret(Fabrika(seed: 43), kayit, karar));
        }

        [Fact]
        public void Gunluktaki_kisa_iz_degistirilmisse_yeniden_uretim_acik_hata_verir()
        {
            var (sim, kayit) = Calistir();
            var karar = sim.Gunluk.Olaylar.First(o => o.Tur == "karar_verildi" && o.Tick == 4 && o.Fail == "Veli");
            var kurcalanmis = new Olay(
                karar.Id, karar.Tick, karar.Sira, karar.Tur, karar.Fail, karar.Hedef, karar.Mekan,
                karar.Yuk.Select(kv => kv.Key == "iz" ? new KeyValuePair<string, string>("iz", kv.Value.Replace("\"puan\":", "\"puan\":9")) : kv).ToList(),
                karar.NedenOlaylar, karar.KuralId);

            Assert.Throws<KararIziUyusmazligiException>(() => KararIziYenidenUretici.Uret(Fabrika(), kayit, kurcalanmis));
        }

        [Fact]
        public void Farkli_secici_ile_yeniden_uretim_uyusmazlik_bildirir()
        {
            var (sim, kayit) = Calistir();
            var karar = sim.Gunluk.Olaylar.First(o => o.Tur == "karar_verildi" && o.Tick == 6 && o.Fail == "Ali");

            Assert.Throws<KararIziUyusmazligiException>(() =>
                KararIziYenidenUretici.Uret(Fabrika(secici: new UtilitySelector(new SecimAyarlari(1, 1m))), kayit, karar));
        }

        [Fact]
        public void Karar_olmayan_olay_ya_da_gelecekteki_tick_hatadir()
        {
            var (sim, kayit) = Calistir();
            var baska = sim.Gunluk.Olaylar.First(o => o.Tur != "karar_verildi");

            Assert.Throws<ArgumentException>(() => KararIziYenidenUretici.Uret(Fabrika(), kayit, baska));
        }

        [Fact]
        public void Yeniden_uretim_orijinal_simulasyonu_degistirmez()
        {
            var (sim, kayit) = Calistir();
            var hash = sim.DurumHash();
            var imza = GunlukImzasi.Al(sim);
            var karar = sim.Gunluk.Olaylar.First(o => o.Tur == "karar_verildi" && o.Tick == 7);

            KararIziYenidenUretici.Uret(Fabrika(), kayit, karar);

            Assert.Equal(hash, sim.DurumHash());
            Assert.Equal(imza, GunlukImzasi.Al(sim));
        }

        [Fact]
        public void Oturum_kaydi_npc_karari_ya_da_iz_icermez()
        {
            var (_, kayit) = Calistir();

            var json = OturumKaydi.Yaz(kayit);

            Assert.DoesNotContain("karar", json);
            Assert.DoesNotContain("havuz", json);
            Assert.DoesNotContain("iz\"", json);
        }
    }
}
