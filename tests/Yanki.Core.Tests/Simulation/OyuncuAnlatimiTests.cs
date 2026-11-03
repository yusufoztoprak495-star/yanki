using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Events;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;

namespace Yanki.Core.Tests.Simulation
{
    /// <summary>
    /// Oyuncu anlatımı (6. hafta, Teslim 5): <c>anlatti</c> türünde oyuncu eylemi. Fikstür: Ayse ve Can han'da, Veli pazar'da, oyuncu han'da.
    /// Bu, Kasaba Hanı içeriği DEĞİLDİR; yalnızca motor mekaniğini sınamak içindir.
    /// </summary>
    public class OyuncuAnlatimiTests
    {
        internal const string Borc = "borclu(Mehmet,Ayse,500)";

        internal const string SenaryoJson = @"{
  ""surum"": 1, ""ad"": ""Dedikodu"",
  ""mekanlar"": [""han"", ""pazar""],
  ""baglantilar"": [[""han"", ""pazar""]],
  ""oyuncu"": { ""id"": ""oyuncu"", ""konum"": ""han"" },
  ""npcler"": [
    { ""id"": ""Ayse"", ""konum"": ""han"", ""kisilik"": { ""acgozluluk"": 0.5, ""durustluk"": 0.5, ""cesaret"": 0.5, ""sosyallik"": 0.5, ""kin_tutma"": 0.5, ""merhamet"": 0.5 } },
    { ""id"": ""Can"", ""konum"": ""han"", ""kisilik"": { ""acgozluluk"": 0.5, ""durustluk"": 0.5, ""cesaret"": 0.5, ""sosyallik"": 0.5, ""kin_tutma"": 0.5, ""merhamet"": 0.5 } },
    { ""id"": ""Veli"", ""konum"": ""pazar"", ""kisilik"": { ""acgozluluk"": 0.5, ""durustluk"": 0.5, ""cesaret"": 0.5, ""sosyallik"": 0.5, ""kin_tutma"": 0.5, ""merhamet"": 0.5 } }
  ],
  ""gercekler"": [],
  ""olay_turleri"": [],
  ""kural_dosyalari"": [""kurallar.json""]
}";

        /// <summary>Borç haberini duyan dinleyen, anlatana aileme_iftira modifier'ı verir (konu bazlı kural).</summary>
        internal const string IftiraKurali = @"{ ""kurallar"": [ {
  ""id"": ""aile_iftirasi"", ""ne_zaman"": ""anlatti"",
  ""kriterler"": [ { ""alan"": ""olay_konusu(borclu(Mehmet, *, *))"", ""op"": ""=="", ""deger"": true } ],
  ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""aileme_iftira"", ""deger"": -22, ""sure"": 18 } ]
} ] }";

        internal static Yanki.Core.Simulation.Simulasyon Kur(string kuralJson = IftiraKurali, ulong seed = 42)
        {
            var senaryo = SenaryoYukleyici.Yukle(SenaryoJson, "s.json");
            Assert.True(senaryo.Basarili, string.Join("\n", senaryo.Hatalar));
            var kurallar = KuralYukleyici.Yukle(kuralJson, "k.json", senaryo.Senaryo!.OlayTurleri);
            Assert.True(kurallar.Basarili, string.Join("\n", kurallar.Hatalar));
            return Simulasyon.Olustur(senaryo.Senaryo!, kurallar.Kurallar, seed);
        }

        internal static PlayerAction Anlat(string hedef, string onerme, params KeyValuePair<string, string>[] ek) =>
            new PlayerAction("anlatti", hedef, new[] { new KeyValuePair<string, string>("onerme", onerme) }.Concat(ek));

        [Fact]
        public void Anlatti_senaryoda_bildirilmeden_gonderilebilir()
        {
            var sim = Kur();
            sim.Submit(Anlat("Ayse", Borc));
            sim.Tick(1);

            Assert.Contains(sim.Gunluk.Olaylar, o => o.Tur == "anlatti" && o.KuralId == "player");
        }

        [Fact]
        public void Hedef_ayni_mekanda_degilse_reddedilir()
        {
            var sim = Kur();
            Assert.Throws<ArgumentException>(() => sim.Submit(Anlat("Veli", Borc)));
        }

        [Fact]
        public void Oyuncu_ayni_tick_hareket_ettiyse_varis_mekani_denetlenir()
        {
            var sim = Kur();
            sim.Submit(new PlayerAction("hareket", "pazar"));

            Assert.Throws<ArgumentException>(() => sim.Submit(Anlat("Ayse", Borc)));   // Ayşe han'da kaldı, oyuncu pazar'a gidiyor
            sim.Submit(Anlat("Veli", Borc));                                           // Veli pazar'da
        }

        [Theory]
        [InlineData("han")]       // mekan
        [InlineData("oyuncu")]    // oyuncunun kendisi
        [InlineData("")]          // hedefsiz
        [InlineData("Yok")]       // olmayan varlık
        public void Hedef_ayni_mekandaki_bir_npc_olmali(string hedef) =>
            Assert.Throws<ArgumentException>(() => Kur().Submit(Anlat(hedef, Borc)));

        [Theory]
        [InlineData("borclu(")]
        [InlineData("")]
        [InlineData("anlatti(Ali,Ayse)")]
        public void Onerme_gecerli_olmali(string onerme) =>
            Assert.Throws<ArgumentException>(() => Kur().Submit(Anlat("Ayse", onerme)));

        [Fact]
        public void Onerme_yuku_zorunludur_ve_baska_anahtar_yazilamaz()
        {
            var sim = Kur();
            Assert.Throws<ArgumentException>(() => sim.Submit(new PlayerAction("anlatti", "Ayse")));
            Assert.Throws<ArgumentException>(() => sim.Submit(Anlat("Ayse", Borc, new KeyValuePair<string, string>("kok", "Can"))));
            Assert.Throws<ArgumentException>(() => sim.Submit(Anlat("Ayse", Borc, new KeyValuePair<string, string>("gorunurluk", "gizli"))));
            sim.Submit(Anlat("Ayse", Borc, new KeyValuePair<string, string>("gorunurluk", "acik")));
        }

        [Fact]
        public void Anlatim_olayi_yalan_ya_da_dogru_ayni_bicimdedir_ve_icerik_yalnizca_onermededir()
        {
            var sim = Kur();
            sim.Submit(Anlat("Ayse", Borc));
            sim.Tick(1);

            var olay = sim.Gunluk.Olaylar.Single(o => o.Tur == "anlatti");
            Assert.Equal("oyuncu", olay.Fail);
            Assert.Equal("Ayse", olay.Hedef);
            Assert.Equal("han", olay.Mekan);
            Assert.Equal("player", olay.KuralId);
            Assert.Empty(olay.NedenOlaylar);
            Assert.Equal(new[] { "gorunurluk=sessiz", "kok=oyuncu", "onerme=" + Borc }, olay.Yuk.Select(kv => kv.Key + "=" + kv.Value).ToArray());
        }

        [Fact]
        public void Dinleyene_duydu_kanit_dusen_oyuncunun_gucu_guven_carpaniyla_azalir()
        {
            var sim = Kur();
            sim.Submit(Anlat("Ayse", Borc));
            sim.Tick(1);

            var anlatti = sim.Gunluk.Olaylar.Single(o => o.Tur == "anlatti");
            var kanit = Assert.Single(sim.Zihinler["Ayse"].Kanitlar(Borc));
            Assert.Equal(Yanki.Core.Mind.KanitTuru.Duydu, kanit.Tur);
            Assert.Equal("oyuncu", kanit.Kaynak);
            Assert.Equal("oyuncu", kanit.Kok);
            Assert.Equal(0.7m, kanit.Guc);   // 1.0 × (0.7 + 0.003 × 0)

            var olay = sim.Gunluk[kanit.OlayId];
            Assert.Equal(new[] { anlatti.Id }, olay.NedenOlaylar.ToArray());
            Assert.Equal("player", olay.KuralId);
        }

        [Fact]
        public void Algi_akisi_dinleyene_anlatti_gozlemi_ve_hafizayi_yazar_tanik_sessizde_hicbir_sey_almaz()
        {
            var sim = Kur();
            sim.Submit(Anlat("Ayse", Borc));
            sim.Tick(1);

            var anlatti = sim.Gunluk.Olaylar.Single(o => o.Tur == "anlatti");
            Assert.Equal(0.9m, sim.Zihinler["Ayse"].Guven("anlatti(oyuncu,Ayse)", 1));
            Assert.Contains(sim.Zihinler["Ayse"].HafizaKayitlari, h => h.OlayId == anlatti.Id);
            Assert.Empty(sim.Zihinler["Can"].Onermeler);          // Can aynı odada ama sessiz konuşma
            Assert.Empty(sim.Zihinler["Can"].HafizaKayitlari);
        }

        [Fact]
        public void Acik_gorunurlukte_tanik_yalnizca_konusmanin_oldugunu_gorur_icerigini_degil()
        {
            var sim = Kur();
            sim.Submit(Anlat("Ayse", Borc, new KeyValuePair<string, string>("gorunurluk", "acik")));
            sim.Tick(1);

            Assert.Equal(new[] { "anlatti(oyuncu,Ayse)" }, sim.Zihinler["Can"].Onermeler.ToArray());
            Assert.Empty(sim.Zihinler["Can"].Kanitlar(Borc));
            Assert.Single(sim.Zihinler["Ayse"].Kanitlar(Borc));
        }

        [Fact]
        public void Kural_ayni_tickte_konu_bazinda_tetiklenir_ve_why_oyuncu_eylemine_iner()
        {
            var sim = Kur();
            sim.Submit(Anlat("Ayse", Borc));
            sim.Tick(1);

            Assert.True(sim.Zihinler["Ayse"].ModifierVar("oyuncu", "aileme_iftira", 1));
            Assert.False(sim.Zihinler["Can"].ModifierVar("oyuncu", "aileme_iftira", 1));

            var modifier = sim.Gunluk.Olaylar.Single(o => o.Tur == "modifier_eklendi");
            var zincir = sim.Neden(modifier.Id);
            Assert.Contains(zincir, o => o.Tur == "anlatti" && o.KuralId == "player");
        }

        [Fact]
        public void Baska_konudaki_anlatim_kurali_tetiklemez()
        {
            var sim = Kur();
            sim.Submit(Anlat("Ayse", "kardes(Mehmet,Can)"));
            sim.Tick(1);

            Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.Tur == "modifier_eklendi");
        }

        [Fact]
        public void Oyuncunun_zihni_yoktur_yalan_kaydi_tutulmaz_ve_dunya_gercegi_degismez()
        {
            var sim = Kur();
            var dunyaOnce = sim.Dunya.DurumHash();
            sim.Submit(Anlat("Ayse", "borclu(Mehmet,Ali,999)"));   // dünya gerçeğine aykırı (ya da hiç doğrulanamaz)
            sim.Tick(1);

            Assert.Equal(dunyaOnce, sim.Dunya.DurumHash());
            Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.Tur == "aktarim_kaydedildi");
            Assert.Single(sim.Zihinler["Ayse"].Kanitlar("borclu(Mehmet,Ali,999)"));
        }

        [Fact]
        public void Eylem_gunlugune_girer_ve_ayni_girdi_ayni_hash_ve_gunluk_verir()
        {
            string[] Calistir()
            {
                var sim = Kur();
                sim.Submit(Anlat("Ayse", Borc));
                sim.Tick(3);
                Assert.Single(sim.EylemGunlugu, e => e.Eylem.Tur == "anlatti");
                return new[] { sim.SonHash, GunlukImzasi.Al(sim) };
            }

            Assert.Equal(Calistir(), Calistir());
        }

        [Fact]
        public void Ayni_onerme_ayni_kisiye_tekrar_anlatilinca_ayni_kokten_kanit_yenilenir_birikmez()
        {
            var sim = Kur();
            sim.Submit(Anlat("Ayse", Borc));
            sim.Tick(1);
            sim.Submit(Anlat("Ayse", Borc));
            sim.Tick(1);

            var kanit = Assert.Single(sim.Zihinler["Ayse"].Kanitlar(Borc));
            Assert.Equal(2, kanit.Tick);
            Assert.Equal(0.7m, sim.Zihinler["Ayse"].Guven(Borc, 2));
        }
    }
}
