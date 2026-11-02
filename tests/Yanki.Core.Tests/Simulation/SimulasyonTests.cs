using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Events;
using Yanki.Core.Simulation;

namespace Yanki.Core.Tests.Simulation
{
    public class SimulasyonTests
    {
        private static PlayerAction Yardim(string hedef = "Ali") => new PlayerAction("yardim_etti", hedef);

        private static IEnumerable<Olay> Turde(Simulasyon s, string tur) => s.Gunluk.Olaylar.Where(o => o.Tur == tur);

        [Fact]
        public void Kurulum_olaylari_tick_0da_senaryo_kuraliyla_yazilir()
        {
            var s = SimFikstur.Yap();

            Assert.Equal(0, s.SimdikiTick);
            Assert.All(s.Gunluk.Olaylar, o =>
            {
                Assert.Equal(0, o.Tick);
                Assert.Equal("senaryo", o.KuralId);
            });
            Assert.Equal("han", s.Dunya.TekDeger("konum!", "oyuncu"));
            Assert.Equal("pazar", s.Dunya.TekDeger("konum!", "Can"));
            Assert.True(s.Dunya.BagliMi("han", "pazar"));
            Assert.True(s.Zihinler.Var("Ali"));
            Assert.Equal("oyuncu", s.OyuncuId);
        }

        [Fact]
        public void Submit_beklemeye_alir_Tick_olaya_cevirir()
        {
            var s = SimFikstur.Yap();
            var once = s.Gunluk.Sayi;

            s.Submit(Yardim());
            Assert.Equal(once, s.Gunluk.Sayi);

            s.Tick(1);

            var olay = Turde(s, "yardim_etti").Single();
            Assert.Equal(1, s.SimdikiTick);
            Assert.Equal(1, olay.Tick);
            Assert.Equal("player", olay.KuralId);
            Assert.Equal("oyuncu", olay.Fail);
            Assert.Equal("Ali", olay.Hedef);
            Assert.Equal("han", olay.Mekan);
            Assert.Empty(olay.NedenOlaylar);
        }

        [Fact]
        public void Eylem_gunlugu_islendigi_tickle_kaydedilir()
        {
            var s = SimFikstur.Yap();
            s.Tick(2);
            s.Submit(Yardim());
            s.Tick(1);

            var kayit = Assert.Single(s.EylemGunlugu);
            Assert.Equal(3, kayit.Tick);
            Assert.Equal("yardim_etti", kayit.Eylem.Tur);
        }

        [Fact]
        public void Ayni_mekandaki_npcler_algilar_baska_mekandakiler_algilamaz()
        {
            var s = SimFikstur.Yap();
            s.Submit(Yardim());
            s.Tick(1);

            var gordu = Turde(s, "kanit_eklendi").Select(o => o.Deger("npc")).ToArray();
            Assert.Equal(new[] { "Ali", "Veli" }, gordu);
            Assert.All(Turde(s, "kanit_eklendi"), o => Assert.Equal("algi", o.KuralId));
            Assert.Equal(new[] { "Ali", "Veli" }, Turde(s, "hafiza_eklendi").Select(o => o.Deger("npc")).ToArray());
        }

        [Fact]
        public void Kural_algilayan_her_npc_icin_birer_kez_ve_ordinal_sirayla_calisir()
        {
            var s = SimFikstur.Yap();
            s.Submit(Yardim());
            s.Tick(1);

            var modifierlar = Turde(s, "modifier_eklendi").ToList();
            Assert.Equal(new[] { "Ali", "Veli" }, modifierlar.Select(o => o.Deger("npc")).ToArray());
            Assert.All(modifierlar, o =>
            {
                Assert.Equal("yardim_minneti", o.KuralId);
                Assert.Equal("oyuncu", o.Deger("hedef"));
                Assert.Equal("minnet", o.Deger("tur"));
                Assert.Equal("15", o.Deger("deger"));
                Assert.Equal(1, o.Tick);
            });
        }

        [Fact]
        public void Kural_fikri_degistirir_ve_baska_mekandaki_npc_etkilenmez()
        {
            var s = SimFikstur.Yap();
            s.Submit(Yardim());
            s.Tick(1);

            Assert.Equal(15m, s.Zihinler.Fikir("Ali", "oyuncu", s.SimdikiTick));
            Assert.Equal(15m, s.Zihinler.Fikir("Veli", "oyuncu", s.SimdikiTick));
            Assert.Equal(0m, s.Zihinler.Fikir("Can", "oyuncu", s.SimdikiTick));
        }

        [Fact]
        public void Modifier_suresi_dolunca_fikir_geri_doner()
        {
            var s = SimFikstur.Yap();
            s.Submit(Yardim());
            s.Tick(1);
            Assert.Equal(15m, s.Zihinler.Fikir("Ali", "oyuncu", s.SimdikiTick));

            s.Tick(120);

            Assert.Equal(0m, s.Zihinler.Fikir("Ali", "oyuncu", s.SimdikiTick));
        }

        [Fact]
        public void Neden_zinciri_modifierden_algi_uzerinden_oyuncu_eylemine_iner()
        {
            var s = SimFikstur.Yap();
            s.Submit(Yardim());
            s.Tick(1);

            var eylem = Turde(s, "yardim_etti").Single();
            var modifier = Turde(s, "modifier_eklendi").First(o => o.Deger("npc") == "Ali");

            Assert.Contains(eylem.Id, modifier.NedenOlaylar);
            var zincir = s.Neden(modifier.Id);
            Assert.Contains(zincir, o => o.Id == eylem.Id);
            Assert.Contains(zincir, o => o.Tur == "kanit_eklendi" && o.Deger("npc") == "Ali");
            Assert.Equal("player", zincir.Last().KuralId);
        }

        [Fact]
        public void Fikir_dokumundeki_katki_nedeni_modifier_olayidir()
        {
            var s = SimFikstur.Yap();
            s.Submit(Yardim());
            s.Tick(1);

            var katki = s.Zihinler.FikirDokumu("Ali", "oyuncu", s.SimdikiTick).Katkilar.Single(k => k.Kaynak.StartsWith("modifier:"));
            Assert.Equal("modifier_eklendi", s.Gunluk[katki.NedenOlayId].Tur);
            Assert.Contains(s.Neden(katki.NedenOlayId), o => o.KuralId == "player");
        }

        [Fact]
        public void Kuralin_urettigi_olay_ayni_tickte_baska_kural_tetiklemez_sonraki_tickte_tetikler()
        {
            const string kurallar = @"{ ""kurallar"": [
  { ""id"": ""a_ilk"", ""ne_zaman"": ""yardim_etti"",
    ""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""olay.fail"", ""eksen"": ""guven"", ""delta"": 10 } ] },
  { ""id"": ""b_zincir"", ""ne_zaman"": ""eksen_degisti"",
    ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.4 } ] } ] }";
            var s = SimFikstur.Yap(kurallar);
            s.Submit(Yardim());

            s.Tick(1);
            Assert.DoesNotContain(s.Gunluk.Olaylar, o => o.KuralId == "b_zincir");
            Assert.Equal(2, Turde(s, "eksen_degisti").Count());

            s.Tick(1);
            var zincir = s.Gunluk.Olaylar.Where(o => o.KuralId == "b_zincir").ToList();
            Assert.Equal(2, zincir.Count);
            Assert.All(zincir, o => Assert.Equal(2, o.Tick));
            Assert.All(zincir, o => Assert.Equal("hafiza_eklendi", o.Tur));
            // Zincir: hafıza ← eksen_degisti(a_ilk) ← yardim_etti.
            Assert.Contains(s.Neden(zincir[0].Id), o => o.Tur == "yardim_etti");
        }

        [Fact]
        public void Zihin_olayini_yalnizca_sahibi_algilar()
        {
            const string kurallar = @"{ ""kurallar"": [
  { ""id"": ""a_ilk"", ""ne_zaman"": ""yardim_etti"",
    ""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""olay.fail"", ""eksen"": ""guven"", ""delta"": 10 } ] },
  { ""id"": ""b_zincir"", ""ne_zaman"": ""eksen_degisti"",
    ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.4 } ] } ] }";
            var s = SimFikstur.Yap(kurallar);
            s.Submit(Yardim());
            s.Tick(2);

            // Ali'nin ve Veli'nin eksen olayını yalnızca kendileri algılar: kişi başına tek zincir olayı.
            var zincir = s.Gunluk.Olaylar.Where(o => o.KuralId == "b_zincir").Select(o => o.Deger("npc")).ToArray();
            Assert.Equal(new[] { "Ali", "Veli" }, zincir);
        }

        [Fact]
        public void Tum_kurallar_adim_basindaki_durumu_gorur()
        {
            // İkinci kural, birincinin yazdığı modifier'ı aynı tick'te görmez.
            const string kurallar = @"{ ""kurallar"": [
  { ""id"": ""a_birinci"", ""ne_zaman"": ""yardim_etti"",
    ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""minnet"", ""deger"": 5, ""sure"": 50 } ] },
  { ""id"": ""b_ikinci"", ""ne_zaman"": ""yardim_etti"",
    ""kriterler"": [ { ""alan"": ""modifier_var(olay.fail, minnet)"", ""op"": ""=="", ""deger"": true } ],
    ""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""olay.fail"", ""eksen"": ""sevgi"", ""delta"": 1 } ] } ] }";
            var s = SimFikstur.Yap(kurallar);
            s.Submit(Yardim());
            s.Tick(1);

            Assert.DoesNotContain(s.Gunluk.Olaylar, o => o.KuralId == "b_ikinci");
        }

        [Fact]
        public void Zorunlu_kriter_tutmazsa_kural_calismaz()
        {
            const string kurallar = @"{ ""kurallar"": [ { ""id"": ""k1"", ""ne_zaman"": ""yardim_etti"",
  ""kriterler"": [ { ""alan"": ""fikir(olay.fail)"", ""op"": ""<"", ""deger"": -5, ""zorunlu"": true } ],
  ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.4 } ] } ] }";
            var s = SimFikstur.Yap(kurallar);
            s.Submit(Yardim());
            s.Tick(1);

            Assert.DoesNotContain(s.Gunluk.Olaylar, o => o.KuralId == "k1");
        }

        [Fact]
        public void Kural_yalnizca_tum_kriterler_tutunca_calisir_agirlik_etkilemez()
        {
            const string kurallar = @"{ ""kurallar"": [
  { ""id"": ""k_hepsi"", ""ne_zaman"": ""yardim_etti"",
    ""kriterler"": [ { ""alan"": ""fikir(olay.fail)"", ""op"": ""<="", ""deger"": 0, ""agirlik"": 0 },
                     { ""alan"": ""stres()"", ""op"": ""=="", ""deger"": 0 } ],
    ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.4 } ] },
  { ""id"": ""k_biri_tutmaz"", ""ne_zaman"": ""yardim_etti"",
    ""kriterler"": [ { ""alan"": ""fikir(olay.fail)"", ""op"": ""<="", ""deger"": 0, ""agirlik"": 100 },
                     { ""alan"": ""stres()"", ""op"": "">"", ""deger"": 50 } ],
    ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.4 } ] } ] }";
            var s = SimFikstur.Yap(kurallar);
            s.Submit(Yardim());
            s.Tick(1);

            Assert.Contains(s.Gunluk.Olaylar, o => o.KuralId == "k_hepsi");
            Assert.DoesNotContain(s.Gunluk.Olaylar, o => o.KuralId == "k_biri_tutmaz");
        }

        [Fact]
        public void Kriteri_olmayan_kural_her_tetikleyici_olayda_calisir()
        {
            const string kurallar = @"{ ""kurallar"": [ { ""id"": ""her_zaman"", ""ne_zaman"": ""yardim_etti"",
  ""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""olay.fail"", ""eksen"": ""guven"", ""delta"": 1 } ] } ] }";
            var s = SimFikstur.Yap(kurallar);
            s.Submit(Yardim("Ali"));
            s.Tick(1);
            s.Submit(Yardim("Can"));
            s.Tick(1);

            Assert.Equal(2, s.Gunluk.Olaylar.Count(o => o.KuralId == "her_zaman" && o.Tick == 1));
            Assert.Equal(2, s.Gunluk.Olaylar.Count(o => o.KuralId == "her_zaman" && o.Tick == 2));
        }

        private const string RolKurallari = @"{ ""kurallar"": [
  { ""id"": ""tehdit_korkusu"", ""ne_zaman"": ""tehdit_etti"",
    ""kriterler"": [ { ""alan"": ""olay_rolu(hedef)"", ""op"": ""=="", ""deger"": true } ],
    ""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""olay.fail"", ""eksen"": ""korku"", ""delta"": 15 } ] },
  { ""id"": ""tehdit_tanigi"", ""ne_zaman"": ""tehdit_etti"",
    ""kriterler"": [ { ""alan"": ""olay_rolu(tanik)"", ""op"": ""=="", ""deger"": true } ],
    ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""tehdit_gordu"", ""deger"": -5, ""sure"": 100 } ] } ] }";

        [Fact]
        public void Daha_once_tehdit_edilen_npc_baskasina_yapilan_tehdidin_tanigidir_hedefi_degil()
        {
            // Regresyon: Ali once kendisi tehdit edildi (tehdit_etti(oyuncu,Ali) inanci kalici), sonra Veli'ye yapilan tehdidi goruyor.
            // Ali bu olayda yalnizca tanik; hedef kurali calismamali.
            var s = SimFikstur.Yap(RolKurallari);
            s.Submit(new PlayerAction("tehdit_etti", "Ali"));
            s.Tick(1);
            s.Submit(new PlayerAction("tehdit_etti", "Veli"));
            s.Tick(1);

            var ikinci = s.Gunluk.Olaylar.Where(o => o.Tick == 2 && o.KuralId != "player" && o.KuralId != "algi").ToList();
            Assert.Equal("tehdit_tanigi", Assert.Single(ikinci, o => o.Deger("npc") == "Ali").KuralId);
            Assert.Equal("tehdit_korkusu", Assert.Single(ikinci, o => o.Deger("npc") == "Veli").KuralId);

            var ilk = s.Gunluk.Olaylar.Where(o => o.Tick == 1 && o.KuralId != "player" && o.KuralId != "algi").ToList();
            Assert.Equal("tehdit_korkusu", Assert.Single(ilk, o => o.Deger("npc") == "Ali").KuralId);
            Assert.Equal("tehdit_tanigi", Assert.Single(ilk, o => o.Deger("npc") == "Veli").KuralId);
        }

        [Fact]
        public void Baska_mekandaki_npc_hicbir_role_girmez()
        {
            var s = SimFikstur.Yap(RolKurallari);
            s.Submit(new PlayerAction("tehdit_etti", "Ali"));
            s.Tick(1);

            Assert.DoesNotContain(s.Gunluk.Olaylar, o => o.Tick == 1 && o.Tur != "varlik_eklendi" && o.Tur != "gercek_eklendi" && o.Tur != "mekan_baglandi" && o.Yuk.Any(y => y.Key == "npc" && y.Value == "Can"));
        }

        [Fact]
        public void Hafiza_etkisi_ayni_olay_icin_tek_kayit_birakir_ve_max_onemi_tutar()
        {
            const string kurallar = @"{ ""kurallar"": [ { ""id"": ""hatirla"", ""ne_zaman"": ""tehdit_etti"",
  ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.9 } ] } ] }";
            var s = SimFikstur.Yap(kurallar);
            s.Submit(new PlayerAction("tehdit_etti", "Ali"));
            s.Tick(1);

            var eylem = s.Gunluk.Olaylar.Single(o => o.Tur == "tehdit_etti");
            var kayit = Assert.Single(s.Zihinler["Ali"].HafizaKayitlari);
            Assert.Equal(eylem.Id, kayit.OlayId);
            Assert.Equal(0.9m, kayit.Onem);
            // Degisiklik olayla kaydedilir: algi (0.5) ve kural (0.9) iki ayri hafiza_eklendi olayidir.
            Assert.Equal(2, s.Gunluk.Olaylar.Count(o => o.Tur == "hafiza_eklendi" && o.Deger("npc") == "Ali"));
        }

        [Fact]
        public void Olayda_tanimsiz_rol_gerektiren_kural_o_olay_icin_atlanir()
        {
            const string kurallar = @"{ ""kurallar"": [ { ""id"": ""hedefli"", ""ne_zaman"": ""yardim_etti"",
  ""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""olay.hedef"", ""eksen"": ""guven"", ""delta"": 3 } ] } ] }";
            var s = SimFikstur.Yap(kurallar);

            s.Submit(new PlayerAction("yardim_etti"));
            s.Tick(1);
            Assert.DoesNotContain(s.Gunluk.Olaylar, o => o.KuralId == "hedefli");

            s.Submit(Yardim("Can"));
            s.Tick(1);
            Assert.Contains(s.Gunluk.Olaylar, o => o.KuralId == "hedefli");
        }

        [Fact]
        public void Gizli_eylemi_NPC_algilamaz_ve_kural_calismaz()
        {
            var s = SimFikstur.Yap();
            s.Submit(new PlayerAction("yardim_etti", "Ali", new[] { new KeyValuePair<string, string>("gorunurluk", "gizli") }));
            s.Tick(1);

            Assert.DoesNotContain(s.Gunluk.Olaylar, o => o.KuralId == "algi" || o.KuralId == "yardim_minneti");
        }

        [Fact]
        public void Eksen_duygu_hafiza_etkileri_dogru_olaylari_uretir()
        {
            const string kurallar = @"{ ""kurallar"": [ { ""id"": ""hepsi"", ""ne_zaman"": ""tehdit_etti"",
  ""etkiler"": [
    { ""tur"": ""eksen"", ""hedef"": ""olay.fail"", ""eksen"": ""korku"", ""delta"": 12 },
    { ""tur"": ""duygu"", ""duygu"": ""korku"", ""siddet"": 0.6, ""hedef"": ""olay.fail"" },
    { ""tur"": ""hafiza"", ""onem"": 0.9 } ] } ] }";
            var s = SimFikstur.Yap(kurallar);
            s.Submit(new PlayerAction("tehdit_etti", "Ali"));
            s.Tick(1);

            var t = s.SimdikiTick;
            Assert.Equal(12m, s.Zihinler["Ali"].Eksen("oyuncu", "korku"));
            Assert.Equal(0.6m, s.Zihinler["Ali"].DuyguSiddeti("korku", t));
            Assert.Equal("oyuncu", s.Zihinler["Ali"].Duygu!.Hedef);
            var eylem = Turde(s, "tehdit_etti").Single();
            Assert.Contains(s.Zihinler["Ali"].Hafiza(t), h => h.OlayId == eylem.Id && h.Onem == 0.9m);
            Assert.All(s.Gunluk.Olaylar.Where(o => o.KuralId == "hepsi"), o => Assert.Contains(eylem.Id, o.NedenOlaylar));
        }

        [Fact]
        public void Gercek_etkisi_dunyayi_degistirir_tekrarsiz_yazilir_ve_sonraki_tickte_algilanir()
        {
            const string kurallar = @"{ ""kurallar"": [ { ""id"": ""iz"", ""ne_zaman"": ""tehdit_etti"",
  ""etkiler"": [ { ""tur"": ""gercek"", ""islem"": ""ekle"", ""gercek"": ""korkutuldu({olay.hedef})"" } ] } ] }";
            var s = SimFikstur.Yap(kurallar);
            s.Submit(new PlayerAction("tehdit_etti", "Ali"));
            s.Tick(1);

            // Ali ve Veli algıladı ama gerçek tek kez yazılır.
            var yazilan = Turde(s, "gercek_eklendi").Where(o => o.KuralId == "iz").ToList();
            Assert.Single(yazilan);
            Assert.Equal("korkutuldu(Ali)", yazilan[0].Deger("gercek"));
            Assert.True(s.Dunya.Var(Yanki.Core.World.Gercek.Ayristir("korkutuldu(Ali)")));
            Assert.Equal("iz", yazilan[0].KuralId);
            Assert.Equal("han", yazilan[0].Mekan);

            // Sonraki tick'te mekandakiler yeni gerçeği algılar (kural olayı da dış olay sayılır).
            s.Tick(1);
            Assert.Contains(Turde(s, "kanit_eklendi"), o => o.Tick == 2 && o.Deger("onerme") == "korkutuldu(Ali)");
        }

        [Fact]
        public void Gercek_kaldir_etkisi_olmayan_gercegi_atlar()
        {
            const string kurallar = @"{ ""kurallar"": [ { ""id"": ""sil"", ""ne_zaman"": ""tehdit_etti"",
  ""etkiler"": [ { ""tur"": ""gercek"", ""islem"": ""kaldir"", ""gercek"": ""yok_boyle(Ali)"" } ] } ] }";
            var s = SimFikstur.Yap(kurallar);
            s.Submit(new PlayerAction("tehdit_etti", "Ali"));

            s.Tick(1);

            Assert.DoesNotContain(s.Gunluk.Olaylar, o => o.Tur == "gercek_kaldirildi");
        }

        [Fact]
        public void Submit_gecersiz_eylemi_reddeder()
        {
            var s = SimFikstur.Yap();
            Assert.Throws<ArgumentException>(() => s.Submit(new PlayerAction("bilinmeyen_eylem", "Ali")));
            Assert.Throws<ArgumentException>(() => s.Submit(new PlayerAction("yardim_etti", "Hayalet")));
            Assert.Throws<ArgumentException>(() =>
                s.Submit(new PlayerAction("yardim_etti", "Ali", new[] { new KeyValuePair<string, string>("gorunurluk", "belki") })));
            Assert.Throws<ArgumentNullException>(() => s.Submit(null!));
        }

        [Fact]
        public void Tick_negatif_sayi_reddeder_ve_sifir_bir_sey_yapmaz()
        {
            var s = SimFikstur.Yap();
            var hash = s.DurumHash();
            s.Tick(0);
            Assert.Equal(0, s.SimdikiTick);
            Assert.Equal(hash, s.DurumHash());
            Assert.Throws<ArgumentOutOfRangeException>(() => s.Tick(-1));
        }

        [Fact]
        public void Olay_yayini_tick_sonunda_yeni_olaylari_sirayla_bildirir()
        {
            var s = SimFikstur.Yap();
            var gelen = new List<long>();
            s.OlayYayinlandi += o => gelen.Add(o.Id);
            var once = s.Gunluk.Sayi;
            s.Submit(Yardim());

            s.Tick(1);

            var beklenen = Enumerable.Range(once + 1, s.Gunluk.Sayi - once).Select(i => (long)i).ToArray();
            Assert.Equal(beklenen, gelen.ToArray());
        }

        [Fact]
        public void Ayni_seed_ve_ayni_girdi_ayni_hash_verir()
        {
            string Calistir(ulong seed)
            {
                var s = SimFikstur.Yap(seed: seed);
                s.Submit(Yardim());
                s.Tick(2);
                s.Submit(new PlayerAction("tehdit_etti", "Veli"));
                s.Tick(3);
                return s.DurumHash();
            }

            Assert.Equal(Calistir(7), Calistir(7));
            Assert.Equal(16, Calistir(7).Length);
        }

        [Fact]
        public void Farkli_girdi_farkli_hash_verir()
        {
            var a = SimFikstur.Yap();
            a.Submit(Yardim("Ali"));
            a.Tick(1);
            var b = SimFikstur.Yap();
            b.Submit(Yardim("Veli"));
            b.Tick(1);
            var c = SimFikstur.Yap();
            c.Tick(1);

            Assert.NotEqual(a.DurumHash(), c.DurumHash());
            Assert.NotEqual(a.DurumHash(), SimFikstur.Yap().DurumHash());
            Assert.NotEqual(a.DurumHash(), b.DurumHash());
        }

        [Fact]
        public void Tick_ayni_toplam_adimda_parcali_ilerletmekle_ayni_durumu_verir()
        {
            var a = SimFikstur.Yap();
            a.Submit(Yardim());
            a.Tick(5);
            var b = SimFikstur.Yap();
            b.Submit(Yardim());
            b.Tick(1);
            b.Tick(4);

            Assert.Equal(a.DurumHash(), b.DurumHash());
        }

        [Fact]
        public void SonHash_tick_sonunda_guncellenir()
        {
            var s = SimFikstur.Yap();
            s.Submit(Yardim());
            s.Tick(1);
            Assert.Equal(s.DurumHash(), s.SonHash);
        }

        [Fact]
        public void Kuraldaki_bilinmeyen_varlik_hedefi_kurulumda_reddedilir()
        {
            const string kurallar = @"{ ""kurallar"": [ { ""id"": ""k1"", ""ne_zaman"": ""yardim_etti"",
  ""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""Hayalet"", ""eksen"": ""guven"", ""delta"": 3 } ] } ] }";
            var ex = Assert.Throws<ArgumentException>(() => SimFikstur.Yap(kurallar));
            Assert.Contains("k1", ex.Message);
            Assert.Contains("Hayalet", ex.Message);
        }
    }
}
