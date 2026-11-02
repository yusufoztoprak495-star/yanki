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
    /// Tick döngüsünün 5. (karar) ve 6. (olaya dönüşme) adımları (ADR-011). Fikstür: Ali ve Veli han'da, Can pazar'da, oyuncu han'da.
    /// Bu, Kasaba Hanı içeriği DEĞİLDİR; yalnızca motor mekaniğini sınamak içindir.
    /// </summary>
    public class NpcKarariTickTests
    {
        private static readonly string[] KuralTurleri = { "yardim_etti", "tehdit_etti", "selam_oldu" };

        private static string Selam(string ek = "") => KararFikstur.Eylem("selam", "yok", ek);

        private static string Git() => KararFikstur.Eylem("git", "komsu_mekan");

        private static Simulasyon Kur(
            IActionSelector? secici, string[] eylemler, string? kuralJson = null, ulong seed = 42, string? senaryoJson = null, bool yardimKuraliVar = true)
        {
            var kurallar = new List<KuralTanimi>();
            if (yardimKuraliVar)
            {
                kurallar.AddRange(KuralYukleyici.Yukle(SimFikstur.YardimKurali, "k.json", KuralTurleri).Kurallar);
            }

            if (kuralJson != null)
            {
                var sonuc = KuralYukleyici.Yukle(kuralJson, "k2.json", KuralTurleri);
                Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
                kurallar.AddRange(sonuc.Kurallar);
            }

            var senaryo = senaryoJson == null ? SimFikstur.Senaryo() : SenaryoYukleyici.Yukle(senaryoJson, "s.json").Senaryo!;
            return Simulasyon.Olustur(senaryo, kurallar, seed, eylemler: KararFikstur.Yukle(eylemler), secici: secici);
        }

        // Ali tick 1'de selam verir; Veli han'dan pazara, Can pazardan han'a gider (yer değiştirirler).
        private static SabitSecici Senaryo1() => new SabitSecici((npc, tick) =>
        {
            if (tick != 1)
            {
                return null;
            }

            return npc == "Ali" ? "selam|" : npc == "Veli" ? "git|pazar" : "git|han";
        });

        private static string[] Eylemler1 => new[] { Selam(), Git() };

        private const string SelamTanigiKurali = @"{ ""kurallar"": [ {
  ""id"": ""selam_tanigi"", ""ne_zaman"": ""selam_oldu"",
  ""kriterler"": [ { ""alan"": ""olay_rolu(tanik)"", ""op"": ""=="", ""deger"": true } ],
  ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""selamlandi"", ""deger"": 5, ""sure"": 50 } ] } ] }";

        private static Olay Tek(Simulasyon s, string tur, string npc, long tick) =>
            s.Gunluk.Olaylar.Single(o => o.Tur == tur && o.Fail == npc && o.Tick == tick);

        private static IEnumerable<Olay> AlgiOlaylari(Simulasyon s, long olayId, string npc) =>
            s.Gunluk.Olaylar.Where(o => o.KuralId == "algi" && o.NedenOlaylar.Contains(olayId) && o.Deger("npc") == npc);

        // ---- eylem yok ----

        [Fact]
        public void Eylem_dosyasi_yoksa_npc_adimi_hicbir_olay_uretmez()
        {
            var sim = SimFikstur.Yap();

            sim.Tick(5);

            Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.Tur == "karar_verildi" || o.KuralId.StartsWith("eylem:", StringComparison.Ordinal));
            Assert.Equal(1, sim.Gunluk.Olaylar.Count(o => o.Tur == "varlik_eklendi" && o.Deger("id") == "Ali"));
        }

        // ---- olay sırası ve alanlar ----

        [Fact]
        public void Secilen_eylem_icin_karar_eylem_algi_ve_etki_olaylari_bu_sirayla_yazilir()
        {
            var sim = Kur(Senaryo1(), Eylemler1);
            sim.Tick(1);

            var karar = Tek(sim, "karar_verildi", "Ali", 1);
            var selam = Tek(sim, "selam_oldu", "Ali", 1);
            var algilar = sim.Gunluk.Olaylar.Where(o => o.KuralId == "algi" && o.NedenOlaylar.Contains(selam.Id)).ToList();
            var etki = Assert.Single(sim.Gunluk.Olaylar, o => o.KuralId == "eylem:selam" && o.Tur == "hafiza_eklendi");

            Assert.True(karar.Id < selam.Id);
            Assert.Equal(4, algilar.Count); // Ali ve Veli × (kanıt + hafıza)
            Assert.All(algilar, a => Assert.True(a.Id > selam.Id && a.Id < etki.Id));
            Assert.Equal("han", karar.Mekan);
            Assert.Equal("han", selam.Mekan);
        }

        [Fact]
        public void Eylem_olayi_alanlari_ve_nedenleri()
        {
            var sim = Kur(Senaryo1(), Eylemler1);
            sim.Tick(1);

            var karar = Tek(sim, "karar_verildi", "Ali", 1);
            var selam = Tek(sim, "selam_oldu", "Ali", 1);

            Assert.Equal("eylem:selam", selam.KuralId);
            Assert.Equal(string.Empty, selam.Hedef);
            Assert.Equal(new[] { karar.Id }, selam.NedenOlaylar.ToArray());
            Assert.Equal("karar", karar.KuralId);
            Assert.Equal("selam", karar.Deger("secilen_eylem"));
            Assert.Equal(string.Empty, karar.Deger("secilen_hedef"));
            Assert.Equal(string.Empty, karar.Hedef);
        }

        [Fact]
        public void Karar_olaylari_npc_kimlik_sirasiyla_yazilir_her_tick_her_npc_icin()
        {
            var sim = Kur(Senaryo1(), Eylemler1);
            sim.Tick(2);

            var kararlar = GunlukImzasi.Turde(sim, "karar_verildi").Select(o => o.Tick + ":" + o.Fail).ToArray();
            Assert.Equal(new[] { "1:Ali", "1:Can", "1:Veli", "2:Ali", "2:Can", "2:Veli" }, kararlar);
        }

        [Fact]
        public void Secim_yapilmayan_npc_icin_bos_secimli_karar_olayi_yazilir_eylem_olayi_yazilmaz()
        {
            var sim = Kur(Senaryo1(), Eylemler1);
            sim.Tick(2);

            var karar = Tek(sim, "karar_verildi", "Ali", 2);
            Assert.Equal(string.Empty, karar.Deger("secilen_eylem"));
            Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.Tick == 2 && o.KuralId.StartsWith("eylem:", StringComparison.Ordinal));
        }

        [Fact]
        public void Aday_olmayan_npc_icin_karar_olayi_yazilmaz()
        {
            // Can pazarda tek başına: npc hedefli tek eylemin hedefi yok → aday yok → olay yok. Ali ve Veli birbirine hedef olabilir.
            var sim = Kur(new SabitSecici((npc, tick) => null), new[] { KararFikstur.Eylem("yardim", "npc") });
            sim.Tick(1);

            var kararlar = GunlukImzasi.Turde(sim, "karar_verildi").Select(o => o.Fail).ToArray();
            Assert.Equal(new[] { "Ali", "Veli" }, kararlar);
        }

        [Fact]
        public void Adaylar_var_ama_hepsi_sifir_puanliysa_bos_secimli_karar_olayi_yazilir()
        {
            const string sifir = "\"olcutler\": [ { \"alan\": \"kisilik(cesaret)\", \"egri\": { \"tur\": \"basamak\", \"esik\": 0.9 } } ]";
            var sim = Kur(null, new[] { KararFikstur.Eylem("selam", "yok", sifir) });
            sim.Tick(1);

            var kararlar = GunlukImzasi.Turde(sim, "karar_verildi").ToList();
            Assert.Equal(3, kararlar.Count);
            Assert.All(kararlar, k => Assert.Equal(string.Empty, k.Deger("secilen_eylem")));
            Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.KuralId == "eylem:selam");
            Assert.Null(sim.KararIzi("Ali")!.Secilen);
        }

        // ---- eş zamanlı algı ----

        [Fact]
        public void Ayni_tickte_ayrilan_tanik_eylemi_gorur_varan_gormez()
        {
            var sim = Kur(Senaryo1(), Eylemler1);
            sim.Tick(1);

            var selam = Tek(sim, "selam_oldu", "Ali", 1);
            Assert.NotEmpty(AlgiOlaylari(sim, selam.Id, "Veli")); // han'dan ayrılan tanık görür
            Assert.NotEmpty(AlgiOlaylari(sim, selam.Id, "Ali"));
            Assert.Empty(AlgiOlaylari(sim, selam.Id, "Can")); // han'a varan görmez
        }

        [Fact]
        public void Yer_degistiren_iki_npc_birbirinin_geldi_olayini_gorur()
        {
            var sim = Kur(Senaryo1(), Eylemler1);
            sim.Tick(1);

            var veliGeldi = Tek(sim, "geldi", "Veli", 1);
            var canGeldi = Tek(sim, "geldi", "Can", 1);

            Assert.Equal("pazar", veliGeldi.Mekan);
            Assert.Equal("han", canGeldi.Mekan);
            Assert.NotEmpty(AlgiOlaylari(sim, canGeldi.Id, "Veli")); // Veli han'daydı, Can han'a geldi
            Assert.NotEmpty(AlgiOlaylari(sim, veliGeldi.Id, "Can")); // Can pazardaydı, Veli pazara geldi
            Assert.Equal("pazar", sim.Dunya.TekDeger("konum!", "Veli"));
            Assert.Equal("han", sim.Dunya.TekDeger("konum!", "Can"));
        }

        [Fact]
        public void Hareket_olaylarinin_hedefi_karsi_mekandir_ve_onerme_mekani_tasir()
        {
            var sim = Kur(Senaryo1(), Eylemler1);
            sim.Tick(1);

            var ayrildi = Tek(sim, "ayrildi", "Veli", 1);
            var geldi = Tek(sim, "geldi", "Veli", 1);

            Assert.Equal(("pazar", "han", "eylem:git"), (ayrildi.Hedef, ayrildi.Mekan, ayrildi.KuralId));
            Assert.Equal(("han", "pazar", "eylem:git"), (geldi.Hedef, geldi.Mekan, geldi.KuralId));
            Assert.Single(sim.Zihinler["Ali"].Kanitlar("ayrildi(Veli,pazar)"));
            Assert.Single(sim.Zihinler["Can"].Kanitlar("geldi(Veli,han)"));
        }

        [Fact]
        public void Konum_degisimi_adim_6nin_en_sonunda_ayri_olayla_yazilir()
        {
            var sim = Kur(Senaryo1(), Eylemler1);
            sim.Tick(1);

            var konumlar = sim.Gunluk.Olaylar.Where(o => o.Tur == "gercek_eklendi" && o.KuralId == "eylem:git").ToList();
            Assert.Equal(new[] { "konum!(Can,han)", "konum!(Veli,pazar)" }, konumlar.Select(o => o.Deger("gercek")).ToArray());

            var son = sim.Gunluk.Olaylar.Where(o => o.Tick == 1).Last();
            Assert.Equal(konumlar[konumlar.Count - 1].Id, son.Id);
            var canGeldi = Tek(sim, "geldi", "Can", 1);
            Assert.Equal(new[] { canGeldi.Id }, konumlar[0].NedenOlaylar.ToArray());
            Assert.All(sim.Gunluk.Olaylar.Where(o => o.Tick == 1 && o.KuralId == "algi"), a => Assert.True(a.Id < konumlar[0].Id));
        }

        // ---- sonraki tick: saklanan algılayanlar ----

        [Fact]
        public void Kurallar_npc_eylemleri_icin_sonraki_tickte_saklanan_algilayanlarla_calisir()
        {
            var sim = Kur(Senaryo1(), Eylemler1, SelamTanigiKurali);
            sim.Tick(2);

            // Veli selamı gördü ve sonra pazara gitti: kural yine de ona çalışır. Can selamdan sonra han'a geldi ama görmedi: çalışmaz.
            var modifierlar = sim.Gunluk.Olaylar.Where(o => o.KuralId == "selam_tanigi").ToList();
            var m = Assert.Single(modifierlar);
            Assert.Equal("Veli", m.Deger("npc"));
            Assert.Equal("Ali", m.Deger("hedef"));
            Assert.Equal(2, m.Tick);

            var selam = Tek(sim, "selam_oldu", "Ali", 1);
            Assert.Contains(selam.Id, m.NedenOlaylar);
            Assert.Contains(m.NedenOlaylar, id => sim.Gunluk[id].KuralId == "algi" && sim.Gunluk[id].Deger("npc") == "Veli");
        }

        [Fact]
        public void Saklanan_olaylar_sonraki_tickte_ikinci_kez_algilanmaz()
        {
            var sim = Kur(Senaryo1(), Eylemler1, SelamTanigiKurali);
            sim.Tick(1);
            var selam = Tek(sim, "selam_oldu", "Ali", 1);
            var tick1 = AlgiSayisi(sim, selam.Id);

            sim.Tick(3);

            Assert.Equal(4, tick1);
            Assert.Equal(tick1, AlgiSayisi(sim, selam.Id));
            Assert.Single(sim.Gunluk.Olaylar, o => o.Tur == "geldi" && o.Fail == "Can");
            var canGeldi = Tek(sim, "geldi", "Can", 1);
            Assert.Equal(AlgiSayisi(sim, canGeldi.Id), AlgiSayisi(sim, canGeldi.Id));
            Assert.All(new[] { selam.Id, canGeldi.Id, Tek(sim, "ayrildi", "Veli", 1).Id }, id => Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.KuralId == "algi" && o.Tick > 1 && o.NedenOlaylar.Contains(id)));
        }

        private static int AlgiSayisi(Simulasyon s, long olayId) =>
            s.Gunluk.Olaylar.Count(o => o.KuralId == "algi" && o.NedenOlaylar.Contains(olayId));

        [Fact]
        public void Tick_bolmeleri_ayni_hash_ve_ayni_gunlugu_verir()
        {
            var bolunmus = Kur(Senaryo1(), Eylemler1, SelamTanigiKurali);
            bolunmus.Tick(1);
            bolunmus.Tick(4);
            var tek = Kur(Senaryo1(), Eylemler1, SelamTanigiKurali);
            tek.Tick(5);

            Assert.Equal(tek.DurumHash(), bolunmus.DurumHash());
            Assert.Equal(GunlukImzasi.Al(tek), GunlukImzasi.Al(bolunmus));
        }

        [Fact]
        public void Tetik_kuyrugu_olay_kimligine_gore_artan_sirada_kurulur()
        {
            // Tick 1: oyuncu yardımı → kural çıktısı X (düşük kimlik); Ali'nin selamı A; selamın etkisi E (en yüksek kimlik).
            // Tick 2'de rule tetikleri X, A, E sırasıyla değerlendirilmeli (devredilen kuyruk + saklı NPC olayları kimliğe göre).
            const string kurallar = @"{ ""kurallar"": [
  { ""id"": ""r_mod"", ""ne_zaman"": ""modifier_eklendi"", ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.3 } ] },
  { ""id"": ""r_sel"", ""ne_zaman"": ""selam_oldu"", ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.3 } ] } ] }";
            var selam = KararFikstur.Eylem("selam", "yok", string.Empty, 1m,
                "[ { \"tur\": \"modifier\", \"hedef\": \"oyuncu\", \"modifier\": \"sevinc\", \"deger\": 3, \"sure\": 50 } ]");
            var sim = Kur(new SabitSecici((npc, tick) => tick == 1 && npc == "Ali" ? "selam|" : null), new[] { selam }, kurallar);

            sim.Submit(new PlayerAction("yardim_etti", "Ali"));
            sim.Tick(2);

            var siralama = sim.Gunluk.Olaylar.Where(o => o.Tick == 2 && (o.KuralId == "r_mod" || o.KuralId == "r_sel")).Select(o => o.KuralId + ":" + o.Deger("npc")).ToArray();
            Assert.Equal(new[] { "r_mod:Ali", "r_mod:Veli", "r_sel:Ali", "r_sel:Veli", "r_mod:Ali" }, siralama);
        }

        // ---- karar olayları ----

        [Fact]
        public void Karar_olaylari_algilanmaz_ve_kural_tetiklemez()
        {
            var sim = Kur(Senaryo1(), Eylemler1, SelamTanigiKurali);
            sim.Tick(3);

            foreach (var karar in GunlukImzasi.Turde(sim, "karar_verildi"))
            {
                // Karar olayını neden gösteren tek olay türü o kararın eylem olaylarıdır.
                var sonuclar = sim.Gunluk.Olaylar.Where(o => o.NedenOlaylar.Contains(karar.Id)).ToList();
                Assert.All(sonuclar, o => Assert.StartsWith("eylem:", o.KuralId, StringComparison.Ordinal));
                Assert.DoesNotContain(sonuclar, o => o.KuralId == "algi");
            }
        }

        // ---- eylem etkileri ----

        [Fact]
        public void Eylem_etkileri_eylem_kural_kimligiyle_ve_eylem_olayi_nedeniyle_yazilir()
        {
            var selam = KararFikstur.Eylem("selam", "yok", string.Empty, 1m,
                "[ { \"tur\": \"modifier\", \"hedef\": \"oyuncu\", \"modifier\": \"sevinc\", \"deger\": 3, \"sure\": 50 } ]");
            var sim = Kur(Senaryo1(), new[] { selam, Git() });
            sim.Tick(1);

            var olay = Tek(sim, "selam_oldu", "Ali", 1);
            var etki = Assert.Single(sim.Gunluk.Olaylar, o => o.Tur == "modifier_eklendi" && o.KuralId == "eylem:selam");

            Assert.Equal("Ali", etki.Deger("npc"));
            Assert.Equal("oyuncu", etki.Deger("hedef"));
            Assert.Equal("sevinc", etki.Deger("tur"));
            Assert.Equal(new[] { olay.Id }, etki.NedenOlaylar.ToArray());
            Assert.True(sim.Zihinler["Ali"].ModifierVar("oyuncu", "sevinc", 1));
            Assert.True(etki.Id > sim.Gunluk.Olaylar.Where(o => o.KuralId == "algi" && o.Tick == 1).Max(o => o.Id));
        }

        // ---- tek durum görüntüsü ----

        [Fact]
        public void Ayni_adimdaki_kararlar_adim_basindaki_ayni_durumu_gorur()
        {
            // selam: kendi selamını hatırlamıyorsa. karsilik: Ali'nin selamını hatırlıyorsa (ve daha yüksek puanlı).
            var selam = KararFikstur.Eylem("selam", "yok", "\"on_kosullar\": [ { \"alan\": \"hafiza_var(selam_oldu, ben, 24)\", \"op\": \"==\", \"deger\": false } ]");
            var karsilik = KararFikstur.Eylem("karsilik", "yok", "\"on_kosullar\": [ { \"alan\": \"hafiza_var(selam_oldu, Ali, 24)\", \"op\": \"==\", \"deger\": true } ]", 5m);
            var sim = Kur(new UtilitySelector(new SecimAyarlari(1, 0.7m)), new[] { selam, karsilik });

            sim.Tick(2);

            // Tick 1: kimse henüz selam görmemişti; Veli, Ali'nin aynı adımdaki selamını göremez → herkes selam verir.
            Assert.Equal("selam", Tek(sim, "karar_verildi", "Veli", 1).Deger("secilen_eylem"));
            Assert.Equal("selam", Tek(sim, "karar_verildi", "Ali", 1).Deger("secilen_eylem"));
            Assert.Equal("selam", Tek(sim, "karar_verildi", "Can", 1).Deger("secilen_eylem"));
            // Tick 2: Veli artık Ali'nin selamını hatırlar ve karşılık verir.
            Assert.Equal("karsilik", Tek(sim, "karar_verildi", "Veli", 2).Deger("secilen_eylem"));
        }

        // ---- sıra ve determinizm ----

        private static string NpcSirasiTersSenaryo()
        {
            var satirlar = SimFikstur.SenaryoJson.Split('\n').ToList();
            var npc = satirlar.Where(s => s.TrimStart().StartsWith("{ \"id\": \"", StringComparison.Ordinal) && s.Contains("\"kisilik\"")).ToList();
            Assert.Equal(3, npc.Count);
            var ters = npc.Select(s => s.TrimEnd().TrimEnd(',')).Reverse().ToList();
            var ilk = satirlar.IndexOf(npc[0]);
            satirlar.RemoveRange(ilk, 3);
            satirlar.InsertRange(ilk, ters.Select((s, i) => i < 2 ? s + "," : s));
            return string.Join("\n", satirlar);
        }

        [Fact]
        public void Senaryodaki_npc_siralamasi_sonucu_degistirmez()
        {
            var normal = Kur(Senaryo1(), Eylemler1, SelamTanigiKurali);
            var ters = Kur(Senaryo1(), Eylemler1, SelamTanigiKurali, senaryoJson: NpcSirasiTersSenaryo());
            Assert.NotEqual(SimFikstur.SenaryoJson, NpcSirasiTersSenaryo());

            normal.Tick(4);
            ters.Tick(4);

            Assert.Equal(normal.DurumHash(), ters.DurumHash());
            Assert.Equal(GunlukImzasi.Al(normal), GunlukImzasi.Al(ters));
        }

        [Fact]
        public void Seciciye_npc_kimlik_sirasiyla_danisilir()
        {
            var secici = Senaryo1();
            var sim = Kur(secici, Eylemler1);

            sim.Tick(2);

            Assert.Equal(new[] { "Ali@1", "Can@1", "Veli@1", "Ali@2", "Can@2", "Veli@2" }, secici.Cagrilar.ToArray());
        }

        [Fact]
        public void Ayni_seed_ayni_gunlugu_farkli_seed_top_n_icinde_farkli_secim_uretebilir()
        {
            var uc = new[] { KararFikstur.Eylem("a", taban: 3m), KararFikstur.Eylem("b", taban: 3m), KararFikstur.Eylem("c", taban: 3m) };

            string Calistir(ulong seed)
            {
                var sim = Kur(null, uc, seed: seed);
                sim.Tick(3);
                return GunlukImzasi.Al(sim);
            }

            Assert.Equal(Calistir(5), Calistir(5));
            var secimler = new SortedSet<string>(StringComparer.Ordinal);
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var sim = Kur(null, uc, seed: seed);
                sim.Tick(1);
                secimler.Add(sim.KararIzi("Ali")!.Secilen!.Eylem.Id);
            }

            Assert.True(secimler.Count > 1, "farklı seed'ler farklı seçim üretmeli: " + string.Join(",", secimler));
        }

        // ---- dış API ----

        [Fact]
        public void KararIzi_ve_son_karar_olayi_npcnin_son_kararini_verir()
        {
            var sim = Kur(Senaryo1(), Eylemler1);

            Assert.Null(sim.KararIzi("Ali"));
            sim.Tick(2);

            Assert.Equal(2, sim.KararIzi("Ali")!.Tick);
            Assert.Null(sim.KararIzi("Ali")!.Secilen);
            Assert.Equal(Tek(sim, "karar_verildi", "Ali", 2).Id, sim.SonKararOlayi("Ali")!.Id);
            Assert.Null(sim.KararIzi("oyuncu"));
        }

        [Fact]
        public void Karar_olayi_yuku_izin_kanonik_json_metnidir()
        {
            var sim = Kur(Senaryo1(), Eylemler1);
            sim.Tick(1);

            var olay = Tek(sim, "karar_verildi", "Ali", 1);

            Assert.Equal(KararIziYazici.Yaz(sim.KararIzi("Ali")!), olay.Deger("iz"));
        }
    }
}
