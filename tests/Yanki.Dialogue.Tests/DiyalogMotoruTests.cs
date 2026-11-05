using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Mind;
using Yanki.Core.Simulation;

namespace Yanki.Dialogue.Tests
{
    /// <summary>
    /// Diyalog motoru (8. hafta): şablon seçimi (C), tekrar kontrolleri (D), hafıza göndermeleri (E), followup (G). Fikstürdeki şablonlar yalnızca motoru sınar;
    /// içerik Yusuf'un alanıdır (DURAK 2).
    /// </summary>
    public class DiyalogMotoruTests
    {
        private const string Adlar = "'adlar': { 'Ali': 'Ali', 'Veli': 'Veli', 'Can': 'Can', 'oyuncu': { 'ad': 'yabancı', 'cins': true } },";

        private static string Dosya(string sablonlar, string esleme = "'yardim_etti': { 'niyet': 'tesekkur', 'konusan': 'hedef' }", string gramer = "") =>
            "{ 'dil': 'tr', " + Adlar + " 'esleme': { " + esleme + " }, 'gramer': { " + gramer + " }, 'sablonlar': [ " + sablonlar + " ] }";

        private const string Genel = "{ 'id': 'genel', 'concept': 'tesekkur', 'genel': true, 'metin': 'Sağ ol.' }";

        private static string Ozel(string id, string kriterler, string metin = "Özel.", string ek = "") =>
            "{ 'id': '" + id + "', 'concept': 'tesekkur', 'kriterler': [ " + kriterler + " ], 'metin': '" + metin + "'" + (ek.Length > 0 ? ", " + ek : string.Empty) + " }";

        private const string Minnet = "{ 'alan': 'modifier_var(dinleyen, minnet)', 'op': '==', 'deger': true, 'zorunlu': true }";
        private const string HerZamanDogru = "{ 'alan': 'kisilik(sosyallik)', 'op': '>=', 'deger': 0, 'zorunlu': true, 'agirlik': 5 }";

        private static (Simulasyon Sim, DiyalogMotoru Motor) Oyna(
            string sablon, int tick, Func<int, IEnumerable<PlayerAction>> eylemler, ZihinAyarlari? zihin = null, ulong seed = 42, string? npcEylemleri = null,
            IDialogueRenderer? renderer = null)
        {
            var sim = DiyalogFikstur.Sim(npcEylemleri, seed, zihin);
            var motor = DiyalogMotoru.Baglan(sim, DiyalogFikstur.Sablon(sablon), renderer: renderer);
            for (var t = 1; t <= tick; t++)
            {
                foreach (var e in eylemler(t))
                {
                    sim.Submit(e);
                }

                sim.Tick(1);
            }

            return (sim, motor);
        }

        private static IEnumerable<PlayerAction> Yardim(string hedef) => new[] { new PlayerAction("yardim_etti", hedef) };

        private static Func<int, IEnumerable<PlayerAction>> Tickler(string hedef, params int[] tickler) =>
            t => tickler.Contains(t) ? Yardim(hedef) : new PlayerAction[0];

        private static readonly ZihinAyarlari SonumsuzHafiza = new ZihinAyarlari { HafizaSonumuTickBasina = 0m };

        // ---- C: şablon seçimi ----

        [Fact]
        public void Spesifik_varyant_geneli_yener_kriterler_konusanin_zihni_uzerinden_degerlendirilir()
        {
            var (_, motor) = Oyna(Dosya(Genel + "," + Ozel("minnetli", Minnet, "Unutmam.")), 1, Tickler("Ali", 1));

            var satir = Assert.Single(motor.Satirlar);
            Assert.Equal(("minnetli", "Unutmam.", "Ali", "oyuncu"), (satir.SablonId, satir.Metin, satir.Konusan, satir.Dinleyen));
            Assert.Contains(satir.NedenSatirlari, n => n.Contains("şablon genel: puan 0", StringComparison.Ordinal));
            Assert.Contains(satir.NedenSatirlari, n => n.Contains("şablon minnetli: puan 1 SEÇİLDİ", StringComparison.Ordinal));
        }

        [Fact]
        public void Zorunlu_kriter_tutmayinca_sablon_elenir_genel_kalir()
        {
            var yok = "{ 'alan': 'modifier_var(dinleyen, sevimsiz)', 'op': '==', 'deger': true, 'zorunlu': true }";
            var (_, motor) = Oyna(Dosya(Genel + "," + Ozel("yok", yok)), 1, Tickler("Ali", 1));

            var satir = Assert.Single(motor.Satirlar);
            Assert.Equal("genel", satir.SablonId);
            Assert.Contains(satir.NedenSatirlari, n => n.Contains("şablon yok: elendi (zorunlu kriter tutmadı)", StringComparison.Ordinal));
        }

        [Fact]
        public void Zorunlu_disindaki_kriterler_puan_katmaya_devam_eder_agirlikli_kismi_eslesme()
        {
            var sosyal = "{ 'alan': 'kisilik(sosyallik)', 'op': '>=', 'deger': 0.5, 'agirlik': 3 }";
            var tutmayan = "{ 'alan': 'kisilik(cesaret)', 'op': '>', 'deger': 0.99, 'agirlik': 9 }";
            var sablon = Dosya(Genel + "," + Ozel("sade", Minnet, "Sade.") + "," + Ozel("zengin", Minnet + "," + sosyal + "," + tutmayan, "Zengin."));

            var (_, motor) = Oyna(sablon, 1, Tickler("Ali", 1));

            var satir = Assert.Single(motor.Satirlar);
            Assert.Equal("zengin", satir.SablonId);   // puan 1 + 3 = 4 (tutmayan kriter puan katmaz ama şablonu elemez)
            Assert.Contains(satir.NedenSatirlari, n => n.Contains("şablon zengin: puan 4 SEÇİLDİ", StringComparison.Ordinal) && n.Contains("tutmayan: kisilik(cesaret)", StringComparison.Ordinal));
        }

        [Fact]
        public void Sifir_puan_esitliginde_hafiza_bulunmayan_gonderme_sablonu_asla_secilmez()
        {
            // Hafıza göndermesi bağlanamıyorsa şablon aday olamaz; zorunlu hafiza_var kriteri de ayrıca korur (ADR-010'un şablon karşılığı). 40 farklı seed'de RNG eşitliği kıramaz.
            var gecmis = Ozel(
                "gecmis", "{ 'alan': 'hafiza_var(yardim_etti, oyuncu, 168)', 'op': '==', 'deger': true, 'zorunlu': true }", "#hafiza.zaman# yardım ettin.",
                "'hafiza': { 'tur': 'yardim_etti', 'fail': 'oyuncu', 'son_n_tick': 168 }");

            for (ulong seed = 1; seed <= 40; seed++)
            {
                var (_, motor) = Oyna(Dosya(Genel + "," + gecmis), 1, Tickler("Ali", 1), SonumsuzHafiza, seed);

                Assert.Equal("genel", Assert.Single(motor.Satirlar).SablonId);
            }
        }

        // ---- D: tekrar kontrolleri ----

        [Fact]
        public void Tekrar_bekleme_tam_pencere_sinirinda_tick_eksi_son_en_az_N_ise_serbest()
        {
            var ozel = Ozel("ozel", HerZamanDogru, "Özel.", "'tekrar': { 'tekrar_bekleme': 5 }");

            var (_, motor) = Oyna(Dosya(Genel + "," + ozel), 7, Tickler("Ali", 1, 5, 6));

            // tick 1: özel; tick 5: 5−1=4 < 5 engelli → genel; tick 6: 6−1=5 ≥ 5 serbest → özel
            Assert.Equal(new[] { "ozel", "genel", "ozel" }, motor.Satirlar.Select(s => s.SablonId).ToArray());
        }

        [Fact]
        public void Bir_kez_konusan_basinadir()
        {
            var ozel = Ozel("ozel", HerZamanDogru, "Özel.", "'tekrar': 'bir_kez'");
            var (_, motor) = Oyna(Dosya(Genel + "," + ozel), 4, t => t == 1 || t == 2 ? Yardim("Ali") : t == 3 ? Yardim("Veli") : new PlayerAction[0]);

            Assert.Equal(new[] { "ozel", "genel", "ozel" }, motor.Satirlar.Select(s => s.SablonId).ToArray());   // Ali: özel, genel; Veli: kendi bir_kez'i
            Assert.Equal(new[] { "Ali", "Ali", "Veli" }, motor.Satirlar.Select(s => s.Konusan).ToArray());
        }

        [Fact]
        public void Tekrarsiz_grup_tukenene_kadar_tekrar_yok_tukenince_dongu()
        {
            string Uye(string id) => Ozel(id, HerZamanDogru, id, "'grup': 'g', 'tekrar': 'tekrarsiz'");
            var (_, motor) = Oyna(Dosya(Genel + "," + Uye("A") + "," + Uye("B") + "," + Uye("C")), 6, Tickler("Ali", 1, 2, 3, 4, 5, 6));

            var kullanilan = motor.Satirlar.Select(s => s.SablonId).ToArray();
            Assert.Equal(6, kullanilan.Length);
            Assert.Equal(new[] { "A", "B", "C" }, kullanilan.Take(3).OrderBy(x => x, StringComparer.Ordinal).ToArray());   // ilk tur: her üye bir kez
            Assert.Equal(new[] { "A", "B", "C" }, kullanilan.Skip(3).OrderBy(x => x, StringComparer.Ordinal).ToArray());   // tükendi → döngü: ikinci tur yine her üye bir kez
        }

        [Fact]
        public void Ayni_tick_ayni_NPCnin_iki_satiri_ayni_zari_cekmez_RNG_alani_olay_kimligini_icerir()
        {
            var a = new Niyet("Ali", "oyuncu", "tesekkur", string.Empty, "notr", "notr", new NiyetGerekcesi("notr", "yok"), 10, 3, false);
            var b = new Niyet("Ali", "oyuncu", "tesekkur", string.Empty, "notr", "notr", new NiyetGerekcesi("notr", "yok"), 11, 3, false);
            var f = new Niyet("Ali", "oyuncu", "tesekkur", string.Empty, "notr", "notr", new NiyetGerekcesi("notr", "yok"), 10, 3, true);

            Assert.NotEqual(DiyalogMotoru.RngAlani(a), DiyalogMotoru.RngAlani(b));
            Assert.NotEqual(DiyalogMotoru.RngAlani(a), DiyalogMotoru.RngAlani(f));   // followup ayrımı

            // Davranış: iki eşit puanlı şablon, aynı tick'te iki yardım: 30 tick içinde iki satırın farklı seçildiği en az bir tick olmalı.
            var eylem = new[] { new PlayerAction("yardim_etti", "Ali"), new PlayerAction("yardim_etti", "Ali") };
            var ikili = Dosya(Genel + "," + Ozel("A", HerZamanDogru, "A") + "," + Ozel("B", HerZamanDogru, "B"));
            var (_, motor) = Oyna(ikili, 30, t => Enumerable.Range(0, 1).SelectMany(_ => eylem));
            var farkliTick = motor.Satirlar.GroupBy(s => s.Tick).Count(g => g.Select(s => s.SablonId).Distinct().Count() > 1);

            Assert.True(farkliTick > 0, "iki satır hiçbir tick'te farklı zar çekmedi");
        }

        // ---- E: hafıza göndermeleri ----

        private static string Gecmis(long pencere = 168, string ek = "") =>
            Ozel(
                "gecmis", "{ 'alan': 'hafiza_var(yardim_etti, oyuncu, " + pencere + ")', 'op': '==', 'deger': true, 'zorunlu': true }",
                "#hafiza.zaman# #hafiza.fail# bana yardım etmişti.", "'hafiza': { 'tur': 'yardim_etti', 'fail': 'oyuncu', 'son_n_tick': " + pencere + " }" + ek);

        [Fact]
        public void Hafiza_gondermesi_yalnizca_konusanin_hatirladigi_gercek_olaya_yapilir_ilk_olayda_gonderme_yok()
        {
            var (_, motor) = Oyna(Dosya(Genel + "," + Gecmis()), 31, Tickler("Ali", 1, 30), SonumsuzHafiza);

            Assert.Equal(new[] { "genel", "gecmis" }, motor.Satirlar.Select(s => s.SablonId).ToArray());   // ilk yardımın öncesi yok (tetikleyici olayın kendisi gönderme değildir)
            Assert.Equal("Dün yabancı bana yardım etmişti.", motor.Satirlar[1].Metin);   // satırın ilk harfi otomatik büyür
            Assert.Contains(motor.Satirlar[1].NedenSatirlari, n => n.StartsWith("hafıza göndermesi: olay #", StringComparison.Ordinal));
        }

        [Fact]
        public void Hafiza_penceresi_tam_sinirda_N_tick_dahil_N_arti_bir_haric()
        {
            var (_, tam) = Oyna(Dosya(Genel + "," + Gecmis(168)), 169, Tickler("Ali", 1, 169), SonumsuzHafiza);
            var (_, bir) = Oyna(Dosya(Genel + "," + Gecmis(168)), 170, Tickler("Ali", 1, 170), SonumsuzHafiza);

            Assert.Equal("gecmis", tam.Satirlar.Last().SablonId);    // 169 − 1 = 168 ≤ 168
            Assert.Equal("genel", bir.Satirlar.Last().SablonId);     // 170 − 1 = 169 > 168
        }

        [Fact]
        public void Silik_hafiza_kaydi_gorunmez()
        {
            var hizliUnutan = new ZihinAyarlari { HafizaSonumuTickBasina = 0.05m };

            var (_, motor) = Oyna(Dosya(Genel + "," + Gecmis()), 100, Tickler("Ali", 1, 100), hizliUnutan);
            var (_, kontrol) = Oyna(Dosya(Genel + "," + Gecmis()), 100, Tickler("Ali", 1, 100), SonumsuzHafiza);

            Assert.Equal("genel", motor.Satirlar.Last().SablonId);      // kayıt silikleşti: hâlâ 168 tick penceresinde ama hatırlanmıyor
            Assert.Equal("gecmis", kontrol.Satirlar.Last().SablonId);   // aynı koşul sönümsüz: hatırlanıyor
        }

        [Fact]
        public void Baskasinin_hafizasi_okunmaz_gormedigi_olaya_gonderme_yok()
        {
            // tick 1: oyuncu han'da Ali'ye yardım eder (Can pazarda, görmez). tick 2: oyuncu pazara gider. tick 30: Can'a yardım eder.
            var (_, motor) = Oyna(
                Dosya(Genel + "," + Gecmis()), 30,
                t => t == 1 ? Yardim("Ali") : t == 2 ? new[] { new PlayerAction("hareket", "pazar") } : t == 30 ? Yardim("Can") : new PlayerAction[0], SonumsuzHafiza);

            var can = motor.Satirlar.Last(s => s.Konusan == "Can");
            Assert.Equal("genel", can.SablonId);   // Can tick 1'deki olayı hiç duymadı/görmedi: Ali'nin hafızası Can için kullanılmaz
        }

        [Fact]
        public void En_yeni_hafiza_kazanir_eskisi_zaman_ifadesini_belirlemez()
        {
            // olaylar tick 1 (gün 0), tick 60 (gün 2); şimdi tick 100 (gün 4): en yeni (60) ile fark 2 → "evvelsi gün"; en eskiyle 4 olurdu.
            var (_, motor) = Oyna(Dosya(Genel + "," + Gecmis()), 100, Tickler("Ali", 1, 60, 100), SonumsuzHafiza);

            Assert.Equal("Evvelsi gün yabancı bana yardım etmişti.", motor.Satirlar.Last().Metin);
        }

        // ---- G: followup ----

        private const string CevapKinli = "{ 'id': 'cevap_kinli', 'concept': 'cevap', 'kriterler': [ { 'alan': 'kisilik(kin_tutma)', 'op': '>=', 'deger': 0.8, 'zorunlu': true } ], 'metin': 'Kinli cevap.' }";
        private const string CevapGenel = "{ 'id': 'cevap_genel', 'concept': 'cevap', 'genel': true, 'metin': 'Cevap.' }";

        private static string SonraliGenel(string hedef) =>
            "{ 'id': 'genel', 'concept': 'tesekkur', 'genel': true, 'metin': 'Sağ ol.', 'sonra': { 'hedef': '" + hedef + "', 'concept': 'cevap' } }";

        [Fact]
        public void Oyuncu_kaynakli_olayda_followup_ayni_anda_uretilir_hedef_kendi_zihniyle_secer()
        {
            // Ali (kin 0.5) konuşur; followup hedefi Veli (kin 0.9): kriterler Veli'nin zihni üzerinden değerlendirilir → cevap_kinli.
            var (_, motor) = Oyna(Dosya(SonraliGenel("Veli") + "," + CevapKinli + "," + CevapGenel), 1, Tickler("Ali", 1));

            Assert.Equal(2, motor.Satirlar.Count);
            var cevap = motor.Satirlar[1];
            Assert.Equal((true, "Veli", "Ali", "cevap_kinli", "Kinli cevap."), (cevap.Followup, cevap.Konusan, cevap.Dinleyen, cevap.SablonId, cevap.Metin));
            Assert.Equal(0, motor.BekleyenFollowupSayisi);
            Assert.Contains(motor.Satirlar[0].NedenSatirlari, n => n.StartsWith("followup: satır #2", StringComparison.Ordinal));
        }

        [Fact]
        public void Followup_hedefi_ana_konusanla_ayni_mekanda_degilse_duser_ve_nedeni_gorunur()
        {
            var (_, motor) = Oyna(Dosya(SonraliGenel("Can") + "," + CevapGenel), 1, Tickler("Ali", 1));   // Can pazarda, Ali han'da

            Assert.Single(motor.Satirlar);
            Assert.Contains(motor.Satirlar[0].NedenSatirlari, n => n.Contains("followup düştü", StringComparison.Ordinal) && n.Contains("aynı mekanda değil", StringComparison.Ordinal));
        }

        [Fact]
        public void Followup_hedefi_NPC_degilse_ya_konusanin_kendisiyse_duser()
        {
            var (_, a) = Oyna(Dosya(SonraliGenel("oyuncu") + "," + CevapGenel), 1, Tickler("Ali", 1));   // dinleyen oyuncu: NPC değil
            var (_, b) = Oyna(Dosya(SonraliGenel("Ali") + "," + CevapGenel), 1, Tickler("Ali", 1));

            Assert.Single(a.Satirlar);
            Assert.Single(b.Satirlar);
            Assert.Contains(a.Satirlar[0].NedenSatirlari, n => n.Contains("followup düştü", StringComparison.Ordinal) && n.Contains("NPC değil", StringComparison.Ordinal));
            Assert.Contains(b.Satirlar[0].NedenSatirlari, n => n.Contains("kendisi", StringComparison.Ordinal));
        }

        private const string SelamEylemi = "{ \"eylemler\": [ { \"id\": \"selam\", \"olay_turu\": \"selam_oldu\", \"hedef\": \"npc\", \"taban\": 1, \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.5 } ] } ] }";

        [Fact]
        public void NPC_kaynakli_olayda_followup_bir_sonraki_tick_sonunda_uretilir_ana_satir_gecikmez()
        {
            const string esleme = "'selam_oldu': { 'niyet': 'selam', 'konusan': 'fail' }";
            const string ana = "{ 'id': 'selam_genel', 'concept': 'selam', 'genel': true, 'metin': 'Selam.', 'sonra': { 'hedef': 'dinleyen', 'concept': 'cevap' } }";
            var sablon = Dosya(ana + "," + CevapGenel, esleme);
            var sim = DiyalogFikstur.Sim(SelamEylemi);
            var motor = DiyalogMotoru.Baglan(sim, DiyalogFikstur.Sablon(sablon));

            sim.Tick(1);
            var tick1Ana = motor.Satirlar.Count(s => !s.Followup);

            Assert.True(tick1Ana >= 2, "ana satırlar tick 1 sonunda hemen üretilir");
            Assert.DoesNotContain(motor.Satirlar, s => s.Followup);
            Assert.Equal(tick1Ana, motor.BekleyenFollowupSayisi);

            sim.Tick(1);

            var cevaplar = motor.Satirlar.Where(s => s.Followup).ToList();
            Assert.Equal(tick1Ana, cevaplar.Count);                    // bekleyenler bir sonraki tick sonunda üretildi
            Assert.All(cevaplar, c => Assert.Equal(2, c.Tick));
            Assert.All(cevaplar, c => Assert.NotEqual(c.Konusan, c.Dinleyen));
        }

        [Fact]
        public void Followup_simulasyona_etki_etmez_olay_uretmez_fikir_degistirmez()
        {
            var sablon = Dosya(SonraliGenel("Veli") + "," + CevapKinli + "," + CevapGenel);
            var (simAcik, motor) = Oyna(sablon, 3, Tickler("Ali", 1, 2));
            var simKapali = DiyalogFikstur.Sim();
            for (var t = 1; t <= 3; t++)
            {
                if (t <= 2)
                {
                    simKapali.Submit(new PlayerAction("yardim_etti", "Ali"));
                }

                simKapali.Tick(1);
            }

            Assert.True(motor.Satirlar.Count(s => s.Followup) >= 1);
            Assert.Equal(simKapali.DurumHash(), simAcik.DurumHash());
            Assert.Equal(simKapali.Gunluk.Sayi, simAcik.Gunluk.Sayi);
        }

        // ---- A: diyalog kapatılabilir ----

        [Fact]
        public void Diyalog_acik_ve_kapali_son_hash_ayni()
        {
            var (acik, _) = Oyna(Dosya(Genel + "," + Gecmis()), 40, Tickler("Ali", 1, 20, 30), npcEylemleri: SelamEylemi);
            var kapali = DiyalogFikstur.Sim(SelamEylemi);
            for (var t = 1; t <= 40; t++)
            {
                if (t == 1 || t == 20 || t == 30)
                {
                    kapali.Submit(new PlayerAction("yardim_etti", "Ali"));
                }

                kapali.Tick(1);
            }

            Assert.Equal(kapali.DurumHash(), acik.DurumHash());
            Assert.Equal(kapali.SonHash, acik.SonHash);
        }

        [Fact]
        public void Dispose_aboneligi_kaldirir()
        {
            var sim = DiyalogFikstur.Sim();
            var motor = DiyalogMotoru.Baglan(sim, DiyalogFikstur.Sablon(Dosya(Genel)));
            motor.Dispose();

            sim.Submit(new PlayerAction("yardim_etti", "Ali"));
            sim.Tick(1);

            Assert.Empty(motor.Satirlar);
        }
    }
}
