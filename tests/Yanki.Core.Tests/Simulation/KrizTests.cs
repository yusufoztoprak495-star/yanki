using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Decision;
using Yanki.Core.Events;
using Yanki.Core.Mind;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;
using Yanki.Core.Tests.Decision;
using Kaydedici = Yanki.Core.Tests.Simulation.KonusmaAdimiTests.Kaydedici;

namespace Yanki.Core.Tests.Simulation
{
    /// <summary>
    /// Stres krizi (6. hafta, Teslim 7). Adım 4: <c>stres(t) ≥ eşik</c> ise en üst uygun seviyede kriz; kriz seçilmiş eylem gibi adım 6
    /// yolundan geçer, krizdeki NPC o tick eylem seçmez, stres düşer, sonuç kuralları sıradaki tick'te. Fikstür: Ayse (durustluk ayarlı),
    /// Can, Zeki han'da; Veli pazar'da. Kasaba Hanı içeriği DEĞİLDİR.
    /// </summary>
    public class KrizTests
    {
        private static string Yalan(string tabani) =>
            "{ \"id\": \"yalan_soyle\", \"olay_turu\": \"anlatti\", \"hedef\": \"npc\", \"taban\": 1, \"aktarim\": \"yalan\", "
            + "\"yalan_onermeleri\": [\"degil(borclu(Mehmet,Ayse,500))\"], \"celisen_ozellikler\": [\"durustluk\"], \"stres_tabani\": " + tabani + " }";

        private static string Git(string tabani) =>
            KararFikstur.Eylem("git", "komsu_mekan", "\"celisen_ozellikler\": [\"cesaret\"], \"stres_tabani\": " + tabani);

        private const string ItirafKurali = @"{ ""kurallar"": [ {
  ""id"": ""itiraf_karsiligi"", ""ne_zaman"": ""itiraf_etti"",
  ""kriterler"": [ { ""alan"": ""olay_rolu(hedef)"", ""op"": ""=="", ""deger"": true } ],
  ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""yalan_itirafi"", ""deger"": 10, ""sure"": 50 } ]
} ] }";

        private static Simulasyon Kur(
            IActionSelector secici, string eylem, string durustluk = "0.8", string? krizler = KrizYukleyiciTests.Gecerli, decimal sonum = 0m,
            string npcSirasi = "Ayse,Can,Zeki,Veli", ulong seed = 42, StresAyarlari? stres = null)
        {
            var senaryo = SenaryoYukleyici.Yukle(KonusmaAdimiTests.Senaryo(npcSirasi, ayseDurustluk: durustluk), "s.json");
            Assert.True(senaryo.Basarili, string.Join("\n", senaryo.Hatalar));
            IReadOnlyList<KrizSatiri> satirlar = new KrizSatiri[0];
            if (krizler != null)
            {
                var k = KrizYukleyici.Yukle(krizler, "krizler.json");
                Assert.True(k.Basarili, string.Join("\n", k.Hatalar));
                satirlar = k.Satirlar;
            }

            // Kuralların olay türleri kriz tablosu yüklenmese de bilinir olsun (tablonun satırlarından).
            var turler = senaryo.Senaryo!.OlayTurleri.Concat(KrizYukleyici.Yukle(KrizYukleyiciTests.Gecerli, "k.json").Satirlar.Select(s => s.OlayTuru)).ToList();
            var kurallar = KuralYukleyici.Yukle(krizler == null ? "{ \"kurallar\": [] }" : ItirafKurali, "k.json", turler);
            Assert.True(kurallar.Basarili, string.Join("\n", kurallar.Hatalar));
            return Simulasyon.Olustur(
                senaryo.Senaryo!, kurallar.Kurallar, seed, new ZihinAyarlari { KanitSonumuTickBasina = 0m, StresSonumuTickBasina = sonum },
                eylemler: KararFikstur.Yukle(eylem), secici: secici, krizler: satirlar, stresAyarlari: stres);
        }

        // Tick 1: oyuncu Ayşe'ye borcu anlatır. Tick 2: Ayşe Can'a yalan söyler (tek aday yalan_soyle|Can; stres tick 2'de yazılır).
        private static Simulasyon YalanSenaryosu(string tabani, string durustluk = "0.8", decimal sonum = 0m, string? krizler = KrizYukleyiciTests.Gecerli, Kaydedici? secici = null)
        {
            var sim = Kur(secici ?? new Kaydedici().Sec("Ayse", 2, "yalan_soyle|Can"), Yalan(tabani), durustluk, krizler, sonum);
            sim.Submit(OyuncuAnlatimiTests.Anlat("Ayse", OyuncuAnlatimiTests.Borc));
            sim.Tick(2);
            return sim;
        }

        private static IEnumerable<Olay> Krizler(Simulasyon sim) => sim.Gunluk.Olaylar.Where(o => o.KuralId.StartsWith("kriz:", StringComparison.Ordinal) && o.Tur != "stres_degisti" && !ZihinOlaylari.ZihinOlayiMi(o.Tur));

        // ---- eşikler ----

        [Fact]
        public void Tam_esikte_kriz_cikar_ve_kisilige_uygun_satir_secilir()
        {
            var sim = YalanSenaryosu("62.5");   // 62.5 × 0.8 = 50.0
            Assert.Equal(50m, sim.Zihinler["Ayse"].Stres(2));
            Assert.Empty(Krizler(sim));          // kriz tick 3'ün 4. adımında

            sim.Tick(1);
            var kriz = Assert.Single(Krizler(sim));
            Assert.Equal("itiraf_etti", kriz.Tur);
            Assert.Equal("Ayse", kriz.Fail);
            Assert.Equal("Can", kriz.Hedef);
            Assert.Equal("han", kriz.Mekan);
            Assert.Equal("kriz:itiraf", kriz.KuralId);
            Assert.Equal(3, kriz.Tick);
        }

        [Fact]
        public void Esik_altinda_kriz_yoktur()
        {
            var sim = YalanSenaryosu("62");   // 49.6
            sim.Tick(10);

            Assert.Empty(Krizler(sim));
            Assert.Equal(49.6m, sim.Zihinler["Ayse"].Stres(2));
        }

        [Fact]
        public void Sonumle_esik_altina_dusen_stres_kriz_uretmez_sonumsuzken_uretir()
        {
            var sonumlu = YalanSenaryosu("63", sonum: 1m);   // 50.4; kriz denetimi tick 3'te: 49.4
            sonumlu.Tick(1);
            Assert.Empty(Krizler(sonumlu));

            var sonumsuz = YalanSenaryosu("63");
            sonumsuz.Tick(1);
            Assert.Single(Krizler(sonumsuz));
        }

        [Fact]
        public void Iki_esik_birden_asilirsa_ustteki_seviyenin_krizi_cikar()
        {
            var sim = YalanSenaryosu("100");   // 80: seviye 2; Ayşe'nin kin_tutma'sı 0.5 olduğundan patla uygun değil, yedek kac
            sim.Tick(1);

            var kriz = Assert.Single(Krizler(sim));
            Assert.Equal("kacti", kriz.Tur);
            Assert.Equal("kriz:kac", kriz.KuralId);
            Assert.Equal(string.Empty, kriz.Hedef);
            Assert.Equal(20m, sim.Zihinler["Ayse"].Stres(3));   // 80 − 60
        }

        [Fact]
        public void Kriz_stresi_dusurur_ve_dusus_olayi_krizi_neden_gosterir()
        {
            var sim = YalanSenaryosu("62.5");
            sim.Tick(1);

            var kriz = Krizler(sim).Single();
            var dusus = Assert.Single(sim.Gunluk.Olaylar, o => o.Tur == "stres_degisti" && o.KuralId == "kriz:itiraf");
            Assert.Equal("-30", dusus.Deger("delta"));
            Assert.Equal(new[] { kriz.Id }, dusus.NedenOlaylar.ToArray());
            Assert.Equal(20m, sim.Zihinler["Ayse"].Stres(3));
        }

        [Fact]
        public void Kriz_sonrasi_ayni_kriz_tekrarlanmaz()
        {
            var sim = YalanSenaryosu("62.5");
            sim.Tick(15);

            Assert.Single(Krizler(sim));
        }

        [Fact]
        public void Stres_100_iken_kriz_sonrasi_ikinci_kriz_cikmaz()
        {
            var sim = YalanSenaryosu("100", durustluk: "1.0");   // 100
            Assert.Equal(100m, sim.Zihinler["Ayse"].Stres(2));
            sim.Tick(20);

            Assert.Single(Krizler(sim));
            Assert.Equal(40m, sim.Zihinler["Ayse"].Stres(3));
        }

        // ---- satır seçimi ----

        [Fact]
        public void Kriterleri_tutmayan_nezaket_yedek_satiri_kullanir_kisilige_gore_farkli_satir()
        {
            var durust = YalanSenaryosu("100", durustluk: "0.8");   // 100 × 0.8 = 80 → seviye 2; yedek kac
            var az = YalanSenaryosu("100", durustluk: "0.6");        // 60 → seviye 1; itiraf (durustluk ≥ 0.7) tutmaz → yedek sakinles
            durust.Tick(1);
            az.Tick(1);

            Assert.Equal("kriz:kac", Krizler(durust).Single().KuralId);
            var kriz = Krizler(az).Single();
            Assert.Equal("kriz:sakinles", kriz.KuralId);
            Assert.Equal("sakinlesti", kriz.Tur);
        }

        [Fact]
        public void Durust_npc_itiraf_eder_yedek_degil()
        {
            var sim = YalanSenaryosu("70", durustluk: "0.8");   // 56 → seviye 1; durustluk 0.8 ≥ 0.7 → itiraf
            sim.Tick(1);

            Assert.Equal("kriz:itiraf", Krizler(sim).Single().KuralId);
        }

        [Fact]
        public void Hedefi_ayni_mekanda_olmayan_satir_elenir_yedege_dusulur()
        {
            var secici = new Kaydedici().Sec("Ayse", 2, "yalan_soyle|Can").Sec("Can", 2, "git|pazar");
            var sim = Kur(secici, Yalan("70") + ", " + Git("1"));
            sim.Submit(OyuncuAnlatimiTests.Anlat("Ayse", OyuncuAnlatimiTests.Borc));
            sim.Tick(3);

            // Can pazar'a gitti: yalan kurbanı artık aynı mekanda değil; itiraf elenir, sakinles gelir.
            Assert.Equal("kriz:sakinles", Krizler(sim).Single().KuralId);
        }

        [Fact]
        public void Itiraf_yalan_soylenmemisse_kurban_yoktur_satir_elenir()
        {
            var sim = Kur(new Kaydedici(), Yalan("70"));
            sim.Zihinler["Ayse"].Uygula(ZihinOlaylari.StresDegisti(9000, 1, 0, "Ayse", 56m, new long[0], "test"));
            sim.Tick(1);

            Assert.Equal("kriz:sakinles", Krizler(sim).Single().KuralId);
        }

        // ---- kriz tablosu yok / yüksek stresle başlama ----

        [Fact]
        public void Kriz_tablosu_yoksa_kriz_uretilmez_stres_yine_calisir()
        {
            var sim = YalanSenaryosu("100", krizler: null);
            sim.Tick(10);

            Assert.Empty(Krizler(sim));
            Assert.Equal(80m, sim.Zihinler["Ayse"].Stres(12));
        }

        [Fact]
        public void Senaryo_yuksek_stresle_baslarsa_kriz_ilk_tickte_cikar_nedeni_olmayan_neden_uydurulmaz()
        {
            var sim = Kur(new Kaydedici(), Yalan("70"));
            sim.Zihinler["Ayse"].Uygula(ZihinOlaylari.StresDegisti(9000, 0, 0, "Ayse", 60m, new long[0], "test"));   // günlükte olmayan artış
            sim.Tick(1);

            var kriz = Assert.Single(Krizler(sim));
            Assert.Equal(1, kriz.Tick);
            Assert.Empty(kriz.NedenOlaylar);
        }

        // ---- adım 6 yolu ----

        [Fact]
        public void Krizdeki_npc_o_tick_eylem_secmez_karar_verildi_yazilmaz()
        {
            var secici = new Kaydedici().Sec("Ayse", 2, "yalan_soyle|Can");
            var sim = YalanSenaryosu("62.5", secici: secici);
            sim.Tick(1);

            Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.Tur == "karar_verildi" && o.Fail == "Ayse" && o.Tick == 3);
            Assert.False(secici.Adaylar.ContainsKey("Ayse@3"));
        }

        [Fact]
        public void Kriz_nedeni_en_yeni_stres_artisi_olaylaridir_ve_why_eyleme_ve_karara_iner()
        {
            var sim = YalanSenaryosu("62.5");
            sim.Tick(1);

            var kriz = Krizler(sim).Single();
            var artis = sim.Gunluk.Olaylar.Single(o => o.Tur == "stres_degisti" && o.KuralId == "eylem:yalan_soyle");
            Assert.Equal(new[] { artis.Id }, kriz.NedenOlaylar.ToArray());

            var zincir = sim.Neden(kriz.Id);
            Assert.Contains(zincir, o => o.Tur == "stres_degisti" && o.KuralId == "eylem:yalan_soyle");
            Assert.Contains(zincir, o => o.Tur == "anlatti" && o.KuralId == "eylem:yalan_soyle");
            Assert.Contains(zincir, o => o.Tur == "karar_verildi" && o.Fail == "Ayse" && o.Tick == 2);
        }

        [Fact]
        public void Kriz_en_yeni_bes_stres_artisini_tasir_eskileri_degil()
        {
            var secici = new SabitSecici((npc, tick) => npc == "Can" && tick <= 10 ? (tick % 2 == 1 ? "git|pazar" : "git|han") : null);
            var sim = Kur(secici, Git("10"), durustluk: "0.5");   // her gidiş +5; 10. gidişte 50
            sim.Tick(11);

            var artislar = sim.Gunluk.Olaylar.Where(o => o.Tur == "stres_degisti" && o.Deger("npc") == "Can" && o.KuralId == "eylem:git").ToList();
            Assert.Equal(10, artislar.Count);
            var kriz = Assert.Single(Krizler(sim));
            Assert.Equal("kriz:sakinles", kriz.KuralId);
            Assert.Equal(artislar.Skip(5).Select(o => o.Id).OrderBy(x => x).ToArray(), kriz.NedenOlaylar.ToArray());
        }

        [Fact]
        public void Kriz_algilanir_etkileri_yazilir_ve_sonuc_kurali_sonraki_tickte_calisir()
        {
            var sim = YalanSenaryosu("62.5");
            sim.Tick(1);   // kriz tick 3

            var kriz = Krizler(sim).Single();
            Assert.Equal(0.9m, sim.Zihinler["Can"].Guven("itiraf_etti(Ayse,Can)", 3));
            Assert.Equal(0.9m, sim.Zihinler["Zeki"].Guven("itiraf_etti(Ayse,Can)", 3));   // acik olay: tanık da görür
            Assert.Contains(sim.Zihinler["Ayse"].HafizaKayitlari, h => h.OlayId == kriz.Id && h.Onem == 0.9m);   // satır etkisi: hafıza
            Assert.False(sim.Zihinler["Can"].ModifierVar("Ayse", "yalan_itirafi", 3));   // kural sıradaki tick'te

            sim.Tick(1);
            Assert.True(sim.Zihinler["Can"].ModifierVar("Ayse", "yalan_itirafi", 4));
            Assert.False(sim.Zihinler["Zeki"].ModifierVar("Ayse", "yalan_itirafi", 4));

            var zincir = sim.Neden(sim.Gunluk.Olaylar.Last(o => o.Tur == "modifier_eklendi" && o.Deger("npc") == "Can").Id);
            Assert.Contains(zincir, o => o.Tur == "itiraf_etti");
            Assert.Contains(zincir, o => o.Tur == "stres_degisti" && o.KuralId == "eylem:yalan_soyle");
            Assert.Contains(zincir, o => o.Tur == "karar_verildi" && o.Fail == "Ayse");
        }

        // ---- determinizm ----

        [Fact]
        public void Ayni_seed_ayni_girdi_ayni_hash_ve_gunluk_npc_sirasi_degisse_de_ayni()
        {
            string[] Calistir(string sira)
            {
                var sim = Kur(new Kaydedici().Sec("Ayse", 2, "yalan_soyle|Can"), Yalan("62.5"), npcSirasi: sira);
                sim.Submit(OyuncuAnlatimiTests.Anlat("Ayse", OyuncuAnlatimiTests.Borc));
                sim.Tick(20);
                return new[] { sim.SonHash, GunlukImzasi.Al(sim) };
            }

            var a = Calistir("Ayse,Can,Zeki,Veli");
            Assert.Equal(a, Calistir("Ayse,Can,Zeki,Veli"));
            Assert.Equal(a, Calistir("Veli,Zeki,Can,Ayse"));
        }

        [Fact]
        public void Esikler_degistirilebilir_ve_tutarsiz_ayar_olusturmada_reddedilir()
        {
            var sim = Kur(new Kaydedici().Sec("Ayse", 2, "yalan_soyle|Can"), Yalan("62.5"), stres: new StresAyarlari { Esikler = new[] { 40m, 70m }, Dususler = new[] { 35m, 61m } });
            Assert.NotNull(sim);

            Assert.Throws<ArgumentException>(() =>
                Kur(new Kaydedici(), Yalan("62.5"), stres: new StresAyarlari { Esikler = new[] { 50m, 80m }, Dususler = new[] { 20m, 60m } }));
        }

        [Fact]
        public void Kriz_satirinin_seviyesi_esik_sayisini_asarsa_olusturma_reddedilir()
        {
            var senaryo = SenaryoYukleyici.Yukle(KonusmaAdimiTests.Senaryo(), "s.json").Senaryo!;
            var satirlar = KrizYukleyici.Yukle(KrizYukleyiciTests.Gecerli, "k.json").Satirlar;

            Assert.Throws<ArgumentException>(() =>
                Simulasyon.Olustur(senaryo, new KuralTanimi[0], 1, krizler: satirlar, stresAyarlari: new StresAyarlari { Esikler = new[] { 50m }, Dususler = new[] { 60m } }));
        }

        [Fact]
        public void Seviye_basina_yedek_satir_olmayan_tablo_olusturmada_reddedilir()
        {
            var senaryo = SenaryoYukleyici.Yukle(KonusmaAdimiTests.Senaryo(), "s.json").Senaryo!;
            var yalnizSeviye1 = KrizYukleyici.Yukle(
                "{ \"krizler\": [ { \"id\": \"y1\", \"seviye\": 1, \"olay_turu\": \"y1_oldu\", \"hedef\": \"yok\" } ] }", "k.json", 1).Satirlar;

            Assert.Throws<ArgumentException>(() => Simulasyon.Olustur(senaryo, new KuralTanimi[0], 1, krizler: yalnizSeviye1));
        }
    }
}
