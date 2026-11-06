using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Yanki.Authoring;
using Yanki.Cli;
using Yanki.Core.Events;
using Yanki.Core.Simulation;
using Yanki.Dialogue;

namespace Yanki.Scenarios.Tests
{
    /// <summary>
    /// samples/KasabaHani (9. hafta kontrol noktası). Değişmezler, DENGE sayılarına değil yapıya bağlıdır; sayısal değerler (tetiklenme oranları, kişilik, taban) yer tutucudur
    /// ve README'de belirtilir (11. hafta denge turunda ayarlanır). Plan 7 sahnesi bir senaryo testidir: oyuncu Ayşe'ye Mehmet'in borcunu anlatır, Ayşe Can'a anlatır,
    /// Can'da aileme_iftira(oyuncu) oluşur, Can'ın satırı söylentiden önce ve sonra farklıdır.
    /// <para>
    /// Beklenti kaynağı ve pencereler (Yusuf'un DURAK 2/2b isteği). Seed seçimi içeren test kalmadı: plan 7 sırası 1..20 seed'in hepsinde sınanır. Pencereler:
    /// 48 = golden betiğinin ufku; 168 = bir oyun içi hafta (plan 10, coverage varsayılanı), değişmezler için ufuk seçimi sonucu etkilemez;
    /// T, T+1, T+2 = günlükten bulunan anlatım tick'ine göre SIRA (T: Ayşe'nin Can'a anlatımı; oyuncu eylemi adım 1'de işlendiği için T selamı söylentiden önce, kural çıktısı T+1'de yazılır, T+2 selamı sonradan);
    /// mühür kuralı oyuncu eylemiyle aynı tick'te (oyuncu eylemi adım 1, kural adım 3). Yalnızca "seed sayısı" (20, 4, 3) ve "en az bir seed'de gerçekleşmeli" koruyucuları yer tutucudur.
    /// </para>
    /// NOT (süreç): bu dosya ilk içerik taslağının koşusundan SONRA yazıldı (mekanik keşif için taslak önce koşuldu); testler içeriğe uydurulmadı, gevşetilmedi.
    /// </summary>
    public class KasabaHaniTests
    {
        private readonly Xunit.Abstractions.ITestOutputHelper Cikti;

        public KasabaHaniTests(Xunit.Abstractions.ITestOutputHelper cikti) => Cikti = cikti;

        private static readonly string Dizin = Path.Combine(RepoKoku(), "samples", "KasabaHani");

        private static string RepoKoku()
        {
            var yol = AppContext.BaseDirectory;
            while (!File.Exists(Path.Combine(yol, "Yanki.slnx")))
            {
                yol = Path.GetDirectoryName(yol)!;
            }

            return yol;
        }

        private static YuklenenSenaryo Yukle()
        {
            var girdi = DogrulamaKomutu.GirdiOku(Dizin, null, new StringWriter());
            Assert.NotNull(girdi);
            var sonuc = SenaryoDogrulayici.Yukle(girdi!, out var yuklenen);
            Assert.True(yuklenen != null, string.Join("\n", sonuc.Bulgular.Select(b => b.ToString())));
            return yuklenen!;
        }

        private static Betik BetikOku(string ad) =>
            BetikOkuyucu.Oku(File.ReadAllText(Path.Combine(Dizin, "oynanislar", ad + ".json")), ad);

        private static readonly string[] BetikAdlari = { "hirsizlik_suclama", "iyilik_muhur", "kotuluk_saklama", "plan7_borc" };

        /// <summary>Betiği koşar (geçersiz kalan eylemler atlanır, kapsama ile aynı kural); isteğe bağlı diyalog motoru bağlar.</summary>
        // Süre (DURAK 2c madde 1): 168 tick'lik betik × seed koşuları test sınıfı başına BİR kez koşulur ve paylaşılır (salt okunur; determinizm aynı, diyalog bağlamak durumu değiştirmez:
        // Diyalog_acik_ya_da_kapali testi bunu önbelleksiz KosYeni ile ayrıca kanıtlar). Önbellekli koşular diyalog motoru bağlı gelir.
        private static readonly object OnbellekKilidi = new object();

        private static readonly Dictionary<string, Lazy<(Simulasyon, DiyalogMotoru?)>> Onbellek = new Dictionary<string, Lazy<(Simulasyon, DiyalogMotoru?)>>();

        private static (Simulasyon Sim, DiyalogMotoru? Motor) Kos(ulong seed, string betik, long tick, bool diyalog)
        {
            if (tick != 168)
            {
                return KosYeni(seed, betik, tick, diyalog);
            }

            Lazy<(Simulasyon, DiyalogMotoru?)> kayit;
            lock (OnbellekKilidi)
            {
                var anahtar = seed.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + betik;
                if (!Onbellek.TryGetValue(anahtar, out kayit!))
                {
                    kayit = new Lazy<(Simulasyon, DiyalogMotoru?)>(() => KosYeni(seed, betik, 168, true));
                    Onbellek[anahtar] = kayit;
                }
            }

            return kayit.Value;
        }

        private static (Simulasyon Sim, DiyalogMotoru? Motor) KosYeni(ulong seed, string betik, long tick, bool diyalog)
        {
            var y = Yukle();
            var sim = y.SimulasyonKur(seed);
            var motor = diyalog ? DiyalogMotoru.Baglan(sim, y.Sablon!) : null;
            var b = BetikOku(betik);
            var sira = 0;
            for (long t = 1; t <= tick; t++)
            {
                while (sira < b.Eylemler.Count && b.Eylemler[sira].Tick == t)
                {
                    try
                    {
                        sim.Submit(b.Eylemler[sira].Eylem);
                    }
                    catch (ArgumentException)
                    {
                    }

                    sira++;
                }

                sim.Tick(1);
            }

            return (sim, motor);
        }

        // ---- Senaryo yapısı ve bütçe (plan 10) ----

        [Fact]
        public void Senaryo_butceye_uyar_ve_plan_7_iliskileri_kilitlidir()
        {
            var y = Yukle();

            Assert.Equal(8, y.Senaryo.Npcler.Count);
            Assert.InRange(y.Eylemler.Count, 12, 18);
            Assert.InRange(y.Kurallar.Count, 25, 35);
            Assert.InRange(y.Sablon!.SablonSayisi, 60, 80);
            Assert.Equal(2, y.Senaryo.YayDegiskenleri.Count);
            Assert.InRange(y.Senaryo.Korumali.Varliklar.Count(), 1, 2);

            Assert.Contains("borclu(Mehmet,Riza,500)", y.Senaryo.Gercekler);
            Assert.Contains("kardes(Mehmet,Can)", y.Senaryo.Gercekler);
            var ayse = y.Senaryo.Npcler.Single(n => n.Id == "Ayse");
            var can = y.Senaryo.Npcler.Single(n => n.Id == "Can");
            Assert.True(ayse.Kisilik.Deger("sosyallik") >= 0.8m, "Ayşe'nin sosyalliği yüksek olmalı (plan 7)");
            Assert.True(can.Kisilik.Deger("merhamet") >= 0.7m, "Can'ın merhameti yüksek olmalı (plan 7)");
        }

        [Fact]
        public void Her_npc_girdisi_en_fazla_10_alan_tasir()
        {
            // Plan 10: "her NPC dosyası en fazla yaklaşık 10 alan". Şema NPC'leri scenario.json içinde tutar: sınır NPC girdisi başına uygulanır (id, konum + 6 kişilik değeri = 8).
            var kok = Yanki.Core.Json.JsonAyristirici.Ayristir(File.ReadAllText(Path.Combine(Dizin, "scenario.json")));
            foreach (var n in kok.Al("npcler")!.Elemanlar)
            {
                var alanSayisi = n.Ozellikler.Count - 1 + n.Al("kisilik")!.Ozellikler.Count;
                Assert.InRange(alanSayisi, 1, 10);
            }
        }

        // ---- Plan 7 sahnesi (SIRA iddiası, seed'e bağlı değil) ----

        private static (Simulasyon Sim, DiyalogMotoru? Motor) KosOzel(ulong seed, IEnumerable<(long Tick, PlayerAction Eylem)> eylemler, long tick, bool diyalog)
        {
            var y = Yukle();
            var sim = y.SimulasyonKur(seed);
            var motor = diyalog ? DiyalogMotoru.Baglan(sim, y.Sablon!) : null;
            var liste = eylemler.ToList();
            for (long t = 1; t <= tick; t++)
            {
                foreach (var e in liste.Where(x => x.Tick == t))
                {
                    sim.Submit(e.Eylem);
                }

                sim.Tick(1);
            }

            return (sim, motor);
        }

        private static (long, PlayerAction) BorcuAyseyeAnlat() =>
            (1, new PlayerAction("anlatti", "Ayse", new[] { new KeyValuePair<string, string>("onerme", "borclu(Mehmet,Riza,500)") }));

        private static (long, PlayerAction) CanaSelam(long tick) => (tick, new PlayerAction("selam_oldu", "Can"));

        /// <summary>Ayşe'nin Can'a borcu anlattığı ilk tick; yoksa <c>null</c>.</summary>
        private static long? AyseCanTicki(Simulasyon sim) =>
            sim.Gunluk.Olaylar.Where(o => o.Tur == "anlatti" && o.Fail == "Ayse" && o.Hedef == "Can" && o.Deger("onerme") == "borclu(Mehmet,Riza,500)").Select(o => (long?)o.Tick).FirstOrDefault();

        private const int PlanYediSeed = 20;

        private static readonly Dictionary<ulong, (Simulasyon Sim, long? T)> TabanOnbellegi = new Dictionary<ulong, (Simulasyon, long?)>();

        /// <summary>Yalnızca oyuncu → Ayşe borç anlatımı; Ayşe'nin Can'a anlatımı (T) görülünce bir tick daha (kural çıktısı T+1) koşup durur; anlatım hiç olmazsa 168'e kadar koşar.</summary>
        private static (Simulasyon Sim, long? T) TabanErken(ulong seed)
        {
            lock (OnbellekKilidi)
            {
                if (TabanOnbellegi.TryGetValue(seed, out var onceki))
                {
                    return onceki;
                }

                var y = Yukle();
                var sim = y.SimulasyonKur(seed);
                var (tick1, eylem) = BorcuAyseyeAnlat();
                long? t = null;
                for (long k = 1; k <= 168; k++)
                {
                    if (k == tick1)
                    {
                        sim.Submit(eylem);
                    }

                    sim.Tick(1);
                    if (t == null)
                    {
                        t = AyseCanTicki(sim);
                    }

                    if (t != null && k >= t.Value + 1)
                    {
                        break;
                    }
                }

                TabanOnbellegi[seed] = (sim, t);
                return (sim, t);
            }
        }

        /// <summary>
        /// Plan 7'nin iddiası SIRA'dır: aynı seed'de günlükten Ayşe'nin Can'a anlatımının tick'i (T) bulunur; oyuncunun Can'a selamı bir koşuda T'de (anlatımla aynı tick, oyuncu
        /// eylemi adım 1'de işlenir: Can henüz söylentiyi duymamıştır), bir koşuda T+2'de (anlatım T'de, kural çıktısı T+1'de yazılır, T+2 selamı bunu görür) gönderilir.
        /// Önce Durum 1 (selam_sakin), sonra Durum 2 (selam_iftira_ayse) şablonu beklenir. Anlatımın gerçekleştiği her seed'de tutmalıdır; tutmayan seed testi düşürür.
        /// </summary>
        [Fact]
        public void Plan_7_sirasi_20_seedin_anlatimin_gerceklestigi_hepsinde_Durum_1_sonra_Durum_2()
        {
            var basarisiz = new List<string>();
            var anlatimOlan = 0;
            var zincirOlan = 0;
            for (ulong seed = 1; seed <= PlanYediSeed; seed++)
            {
                var (taban, t) = TabanErken(seed);
                if (t == null)
                {
                    continue;
                }

                anlatimOlan++;

                // Zincir: modifier T+1'de, oyuncu → Ayşe → Can anlatımlarına iner (why).
                var modifier = taban.Gunluk.Olaylar.Where(o => o.Tur == "modifier_eklendi" && o.Deger("npc") == "Can" && o.Deger("tur") == "aileme_iftira").ToArray();
                if (modifier.Length != 1 || modifier[0].Tick != t + 1 || modifier[0].KuralId != "aile_iftirasi")
                {
                    basarisiz.Add($"seed {seed}: aileme_iftira modifier beklenen T+1={t + 1} tick'te tek ve aile_iftirasi'ndan değil ({string.Join(",", modifier.Select(m => m.Tick + ":" + m.KuralId))})");
                }
                else
                {
                    var zincir = taban.Neden(modifier[0].Id);
                    if (!zincir.Any(o => o.Tur == "anlatti" && o.Fail == "oyuncu" && o.Hedef == "Ayse") || !zincir.Any(o => o.Tur == "anlatti" && o.Fail == "Ayse" && o.Hedef == "Can"))
                    {
                        basarisiz.Add($"seed {seed}: why zinciri oyuncu → Ayşe → Can anlatımlarına inmiyor");
                    }
                    else
                    {
                        zincirOlan++;
                    }
                }

                var (once, motorOnce) = KosOzel(seed, new[] { BorcuAyseyeAnlat(), CanaSelam(t.Value) }, t.Value + 1, diyalog: true);
                using (motorOnce)
                {
                    if (AyseCanTicki(once) != t)
                    {
                        basarisiz.Add($"seed {seed}: selam eklenince Ayşe'nin anlatım tick'i değişti ({t} → {AyseCanTicki(once)})");
                    }

                    var satir = motorOnce!.Satirlar.FirstOrDefault(l => l.Konusan == "Can" && l.Dinleyen == "oyuncu" && l.Tick == t.Value);
                    if (satir == null || satir.SablonId != "selam_sakin")
                    {
                        basarisiz.Add($"seed {seed}: T={t} selamı Durum 1 (selam_sakin) değil: {satir?.SablonId ?? "satır yok"}");
                    }
                }

                var (sonra, motorSonra) = KosOzel(seed, new[] { BorcuAyseyeAnlat(), CanaSelam(t.Value + 2) }, t.Value + 2, diyalog: true);
                using (motorSonra)
                {
                    var satir = motorSonra!.Satirlar.FirstOrDefault(l => l.Konusan == "Can" && l.Dinleyen == "oyuncu" && l.Tick == t.Value + 2);
                    if (satir == null || satir.SablonId != "selam_iftira_ayse")
                    {
                        basarisiz.Add($"seed {seed}: T+2={t + 2} selamı Durum 2 (selam_iftira_ayse) değil: {satir?.SablonId ?? "satır yok"} \"{satir?.Metin}\"");
                    }
                    else
                    {
                        Assert.Equal("Ağabeyim hakkında ne dediğini Ayşe'den duydum.", satir.Metin);
                    }
                }
            }

            Assert.True(anlatimOlan > 0, "hiçbir seed'de Ayşe Can'a anlatmadı: test anlamsız");
            Assert.True(basarisiz.Count == 0, $"anlatım olan {anlatimOlan}/{PlanYediSeed} seed, zincir tamam {zincirOlan}; başarısız:\n" + string.Join("\n", basarisiz));
            Cikti.WriteLine($"Plan 7 sırası: {PlanYediSeed} seed içinde Ayşe→Can anlatımı olan {anlatimOlan}, hepsinde Durum 1 → Durum 2 ve zincir tamam.");
        }

        /// <summary>
        /// Rıza kökenli (başlangıç inancı) söylenti plan 7'yi bozmaz: <c>aile_iftirasi_npc</c> (hedef: anlatan, kendi <c>en_fazla</c>'sı) önce çalışsa bile oyuncu kökenli anlatım gelince
        /// <c>aile_iftirasi</c> (hedef: oyuncu) yine tam bir kez çalışır. Önce NPC kuralının çalıştığı en az bir seed bulunmalıdır (test boş geçmesin).
        /// </summary>
        [Fact]
        public void NPC_kokenli_soylenti_kuralinin_hedefi_ve_en_fazlasi_oyuncu_kokenli_kurali_engellemez()
        {
            var npcOnce = 0;
            for (ulong seed = 1; seed <= PlanYediSeed; seed++)
            {
                var (sim, _) = TabanErken(seed);
                var npc = sim.Gunluk.Olaylar.Where(o => o.Tur == "modifier_eklendi" && o.KuralId == "aile_iftirasi_npc" && o.Deger("npc") == "Can").ToArray();
                var oyuncu = sim.Gunluk.Olaylar.Where(o => o.Tur == "modifier_eklendi" && o.KuralId == "aile_iftirasi" && o.Deger("npc") == "Can").ToArray();

                Assert.True(npc.Length <= 1 && oyuncu.Length <= 1, $"seed {seed}: en_fazla 1 aşıldı");
                Assert.All(npc, m => Assert.NotEqual("oyuncu", m.Deger("hedef")));          // hedef anlatan NPC
                Assert.All(oyuncu, m => Assert.Equal("oyuncu", m.Deger("hedef")));
                if (AyseCanTicki(sim) != null)
                {
                    Assert.Single(oyuncu);                                                  // anlatım oldu: oyuncu kökenli kural çalıştı
                }

                if (npc.Length == 1 && oyuncu.Length == 1 && npc[0].Tick < oyuncu[0].Tick)
                {
                    npcOnce++;
                }
            }

            Assert.True(npcOnce > 0, "hiçbir seed'de NPC kökenli kural oyuncu kökenliden önce çalışmadı: test anlamsız");
            Cikti.WriteLine($"NPC kuralı oyuncu kuralından önce çalışan seed sayısı: {npcOnce}/{PlanYediSeed}.");
        }

        /// <summary>
        /// Durum 2b (koşul biçiminde): oyuncu Can'dan Mehmet'i ele vermesini isterse, Can'ın <c>kardesi_ele_ver</c> eylemini SEÇTİĞİ her tick'te stresi artar (merhametine aykırı eylem).
        /// Seçilme olasılıksaldır; en az bir seed'de seçilmelidir (test boş geçmesin).
        /// </summary>
        [Fact]
        public void Plan_7_Durum_2b_Can_ele_verme_eylemini_secerse_stresi_artar()
        {
            var secildi = 0;
            for (ulong seed = 1; seed <= PlanYediSeed; seed++)
            {
                var (sim, _) = KosOzel(seed, new[] { BorcuAyseyeAnlat(), (7L, new PlayerAction("ele_verdirme_istendi", "Can")) }, 60, diyalog: false);
                foreach (var secim in sim.Gunluk.Olaylar.Where(o => o.KuralId == "eylem:kardesi_ele_ver" && o.Tur == "kardes_ele_verildi"))
                {
                    secildi++;
                    Assert.Equal("Can", secim.Fail);
                    Assert.Contains(sim.Gunluk.Olaylar, o => o.Tur == "stres_degisti" && o.Deger("npc") == "Can" && o.Tick == secim.Tick
                        && decimal.Parse(o.Deger("delta"), System.Globalization.CultureInfo.InvariantCulture) > 0m);
                }
            }

            Assert.True(secildi > 0, "hiçbir seed'de Can ele verme eylemini seçmedi: test anlamsız");
            Cikti.WriteLine($"Durum 2b: ele verme eylemi seçilen toplam {secildi} (20 seed).");
        }

        // ---- selam_muhur_hafiza: uydurma gönderme yok (hafiza_var konuya bakmaz; şablon kok ile mühüre bağlanır) ----

        private static PlayerAction Anlat(string hedef, string onerme) =>
            new PlayerAction("anlatti", hedef, new[] { new KeyValuePair<string, string>("onerme", onerme) });

        [Fact]
        public void Muhur_disinda_bir_sey_anlatilan_NPC_selam_muhur_hafizayi_secmez()
        {
            for (ulong seed = 1; seed <= 5; seed++)
            {
                // Oyuncu Rıza'ya yalnızca borcu anlatır (mühürden söz etmez); sonra Rıza'ya selam verir.
                var (sim, motor) = KosOzel(seed, new[] { (1L, Anlat("Riza", "borclu(Mehmet,Riza,500)")), (2L, new PlayerAction("selam_oldu", "Riza")) }, 3, diyalog: true);
                using (motor)
                {
                    var satir = motor!.Satirlar.Single(l => l.Konusan == "Riza" && l.Dinleyen == "oyuncu" && l.Tick == 2);
                    Assert.NotEqual("selam_muhur_hafiza", satir.SablonId);
                    Assert.DoesNotContain("Mührün", satir.Metin, StringComparison.Ordinal);
                    Assert.NotNull(sim);
                }
            }
        }

        [Fact]
        public void Muhur_yerini_oyuncudan_duyan_NPC_mühür_tasindiktan_sonra_da_selam_muhur_hafizayi_secer()
        {
            var secildi = 0;
            for (ulong seed = 1; seed <= 5; seed++)
            {
                var (sim, motor) = KosOzel(seed, new[] { (1L, Anlat("Osman", "konum!(muhur,depo)")), (3L, new PlayerAction("selam_oldu", "Osman")) }, 3, diyalog: true);
                using (motor)
                {
                    // mühür muhtarlığa taşındı (eski konum gerçeği düştü); Osman'ın kanıtı silinmedi, kök oyuncu
                    Assert.Equal("muhtarlik", sim.Dunya.TekDeger("konum!", "muhur"));
                    var satir = motor!.Satirlar.Single(l => l.Konusan == "Osman" && l.Dinleyen == "oyuncu" && l.Tick == 3);
                    // Aynı puanlı doğru hafıza şablonu (Osman aynı pencerede hırsızlığı da gördüyse selam_hirsiz_hafiza) eşitlikte RNG ile seçilebilir; ikisi de doğrudur.
                    Assert.Contains(satir.SablonId, new[] { "selam_muhur_hafiza", "selam_hirsiz_hafiza" });
                    if (satir.SablonId == "selam_muhur_hafiza")
                    {
                        Assert.Equal("Mührün yerini sen söylemiştin yabancı, minnettarım.", satir.Metin);
                        secildi++;
                    }
                }
            }

            Assert.True(secildi > 0, "hiçbir seed'de selam_muhur_hafiza seçilmedi: test anlamsız");
        }

        // ---- Üç gerilim tohumu ----

        [Fact]
        public void Her_gerilim_tohumu_en_az_bir_betikte_tetiklenir()
        {
            var borc = false;
            var eşya = false;
            var hirsizlik = false;
            foreach (var ad in BetikAdlari)
            {
                var (sim, _) = Kos(1UL, ad, 168, diyalog: false);
                borc |= sim.Gunluk.Olaylar.Any(o => o.KuralId == "aile_iftirasi" || o.KuralId == "borc_odendi_alacakli");
                eşya |= sim.Gunluk.Olaylar.Any(o => o.KuralId == "muhur_yeri_ogrenildi");
                hirsizlik |= sim.Gunluk.Olaylar.Any(o => o.Tur == "kese_calindi");
            }

            Assert.True(borc, "borç tohumu hiçbir betikte tetiklenmedi");
            Assert.True(eşya, "kayıp eşya tohumu hiçbir betikte tetiklenmedi");
            Assert.True(hirsizlik, "hırsızlık tohumu hiçbir betikte tetiklenmedi");
        }

        // ---- Korumalı varlık ve yaylar ----

        [Theory]
        [InlineData(1UL)]
        [InlineData(2UL)]
        [InlineData(3UL)]
        public void Korumali_varlik_hicbir_betikte_ihlal_edilmez_yaylar_sinirda_kalir(ulong seed)
        {
            foreach (var ad in BetikAdlari)
            {
                var (sim, _) = Kos(seed, ad, 168, diyalog: true);
                // yay sınırı: her yay_degisti olayının yeni değeri aralıkta (günlükten; tick tick denetimle eşdeğer)
                foreach (var o in sim.Gunluk.Olaylar.Where(x => x.Tur == "yay_degisti"))
                {
                    Assert.InRange(decimal.Parse(o.Deger("deger"), System.Globalization.CultureInfo.InvariantCulture), 0m, 10m);
                }

                Assert.InRange(sim.Yay("kasaba_gerginligi"), 0m, 10m);
                Assert.InRange(sim.Yay("han_itibari"), 0m, 10m);

                Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.Tur == "ayrildi" && o.Fail == "Riza");          // mekandan_ayrilamaz
                Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.Tur == "kasa_acildi");                           // calinamaz: kasa hiç açılmaz
                Assert.Contains(sim.Gunluk.Olaylar, o => o.Tur == "fren_uygulandi" && o.KuralId == "fren:korumali"); // ama denendi ve engellendi
            }
        }

        // ---- Seed farkı ve NedenOlaylar ----

        [Fact]
        public void Seed_42_ve_7_olay_ve_diyalog_satiri_bakimindan_olculebilir_sekilde_ayrisir()
        {
            var (a, ma) = Kos(42UL, "plan7_borc", 48, diyalog: true);
            var (b, mb) = Kos(7UL, "plan7_borc", 48, diyalog: true);
            using (ma)
            using (mb)
            {
                Assert.NotEqual(a.DurumHash(), b.DurumHash());
                var farkliOlay = a.Gunluk.Olaylar.Zip(b.Gunluk.Olaylar, (x, y) => x.Tur != y.Tur || x.Fail != y.Fail || x.Hedef != y.Hedef).Count(f => f);
                Assert.True(farkliOlay > 0 || a.Gunluk.Olaylar.Count != b.Gunluk.Olaylar.Count);
                Assert.NotEqual(ma!.DiyalogHash(), mb!.DiyalogHash());
            }
        }

        [Fact]
        public void Her_olayin_neden_olaylari_gunluktedir_ve_kendisinden_kucuktur()
        {
            var (sim, _) = Kos(42UL, "hirsizlik_suclama", 168, diyalog: false);
            var kimlikler = new HashSet<long>(sim.Gunluk.Olaylar.Select(o => o.Id));

            foreach (var o in sim.Gunluk.Olaylar)
            {
                Assert.All(o.NedenOlaylar, n => Assert.True(kimlikler.Contains(n) && n < o.Id, $"#{o.Id} nedeni #{n} günlükte yok ya da ileri"));
            }
        }

        // ---- Diyalog açık/kapalı hash eşitliği (ADR-014) ----

        [Theory]
        [InlineData(42UL)]
        [InlineData(7UL)]
        public void Diyalog_acik_ya_da_kapali_son_hash_ve_gunluk_hash_ayni(ulong seed)
        {
            var (kapali, _) = KosYeni(seed, "plan7_borc", 168, diyalog: false);
            var (acik, motor) = KosYeni(seed, "plan7_borc", 168, diyalog: true);
            using (motor)
            {
                Assert.Equal(kapali.DurumHash(), acik.DurumHash());
                Assert.Equal(GunlukHashu.Hesapla(kapali.Gunluk), GunlukHashu.Hesapla(acik.Gunluk));
                Assert.NotEmpty(motor!.Satirlar);
            }
        }

        // ---- Yalan gizliliği (ADR-012; 6. hafta biçimi: olay türü, yük, NedenOlaylar sayısı, güç ve diyalog niyeti alan alan aynı) ----

        private static readonly string[] YalanBetikleri = { "hirsizlik_suclama", "plan7_borc", "kotuluk_saklama", "iyilik_muhur" };

        [Fact]
        public void Yalan_dogru_anlatimdan_alan_alan_ayirt_edilemez_ve_yalan_gercekten_soylenir()
        {
            var yalanAnlatimlari = new List<Olay>();
            var dogruAnlatimlari = new List<Olay>();
            var yalanKanitlari = new List<Olay>();
            var dogruKanitlari = new List<Olay>();
            var yalanSatirlari = new List<DiyalogSatiri>();
            var dogruSatirlari = new List<DiyalogSatiri>();

            foreach (var betik in YalanBetikleri)
            {
                for (ulong seed = 1; seed <= 4; seed++)
                {
                    var (sim, motor) = Kos(seed, betik, 168, diyalog: true);
                    using (motor)
                    {
                        var olaylar = sim.Gunluk.Olaylar;
                        var yalanIdleri = new HashSet<long>();
                        foreach (var a in olaylar.Where(o => o.Tur == "aktarim_kaydedildi" && o.Deger("yalan") == "1"))
                        {
                            foreach (var n in a.NedenOlaylar)
                            {
                                if (olaylar.First(o => o.Id == n).Tur == "anlatti")
                                {
                                    yalanIdleri.Add(n);
                                }
                            }
                        }

                        foreach (var o in olaylar.Where(o => o.Tur == "anlatti" && o.KuralId.StartsWith("eylem:", StringComparison.Ordinal)))
                        {
                            (yalanIdleri.Contains(o.Id) ? yalanAnlatimlari : dogruAnlatimlari).Add(o);
                            var kanit = olaylar.Where(k => k.Tur == "kanit_eklendi" && k.Deger("tur") == "duydu" && k.NedenOlaylar.Contains(o.Id)).ToArray();
                            (yalanIdleri.Contains(o.Id) ? yalanKanitlari : dogruKanitlari).AddRange(kanit);
                            foreach (var satir in motor!.Satirlar.Where(l => l.OlayId == o.Id))
                            {
                                (yalanIdleri.Contains(o.Id) ? yalanSatirlari : dogruSatirlari).Add(satir);
                            }
                        }
                    }
                }
            }

            Assert.NotEmpty(yalanAnlatimlari);   // test boş geçmesin: Kasaba'da Kerem gerçekten yalan söyler (yalan_soyle)
            Assert.NotEmpty(dogruAnlatimlari);
            Assert.NotEmpty(yalanKanitlari);
            Assert.NotEmpty(yalanSatirlari);

            string Anahtarlar(Olay o) => string.Join(",", o.Yuk.Select(kv => kv.Key).OrderBy(x => x, StringComparer.Ordinal));
            // olay türü
            Assert.Equal(new[] { "anlatti" }, yalanAnlatimlari.Concat(dogruAnlatimlari).Select(o => o.Tur).Distinct().ToArray());
            // yük anahtarları, NedenOlaylar sayısı
            Assert.Single(yalanAnlatimlari.Concat(dogruAnlatimlari).Select(Anahtarlar).Distinct());
            Assert.Single(yalanAnlatimlari.Concat(dogruAnlatimlari).Select(o => o.NedenOlaylar.Count).Distinct());
            Assert.DoesNotContain(yalanAnlatimlari.Concat(dogruAnlatimlari), o => o.Yuk.Any(kv => kv.Key.Contains("yalan") || kv.Value.Contains("yalan")));
            // güç: dinleyene yazılan kanıtın yük anahtarları ve türü aynı; güç 0..1 aralığında
            Assert.Single(yalanKanitlari.Concat(dogruKanitlari).Select(o => Anahtarlar(o) + "|" + o.Deger("tur")).Distinct());
            Assert.All(yalanKanitlari.Concat(dogruKanitlari), k => Assert.InRange(decimal.Parse(k.Deger("guc"), System.Globalization.CultureInfo.InvariantCulture), 0m, 1m));
            // diyalog niyeti: yalan da doğru da "anlatma" niyetinin şablonlarıyla söylenir (anlat_*)
            Assert.All(yalanSatirlari.Concat(dogruSatirlari), l => Assert.StartsWith("anlat_", l.SablonId, StringComparison.Ordinal));
            // yalan bilgisi yalnızca aktarım kaydında; başka hiçbir olayın yükünde yalan alanı yok
            foreach (var betik in YalanBetikleri.Take(1))
            {
                var (sim, _) = Kos(1UL, betik, 168, diyalog: false);
                Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.Tur != "aktarim_kaydedildi" && o.Yuk.Any(kv => kv.Key == "yalan"));
            }
        }

        // ---- 9. hafta DURAK 2 maddeleri 3, 4, 7 ----

        [Fact]
        public void Golden_betiginde_atlanan_eylem_yoktur_plan7_borc_seed_42_tick_48()
        {
            var girdi = DogrulamaKomutu.GirdiOku(Dizin, null, new StringWriter())!;

            Assert.Empty(KapsamHesabi.AtlananEylemler(girdi, BetikOku("plan7_borc"), 42UL, 48));
        }

        [Fact]
        public void Muhuru_tasiyan_her_etki_tek_degerli_konum_yazar_ve_eski_konumu_dusurur()
        {
            var y = Yukle();
            var muhurEtkileri = y.Kurallar.SelectMany(k => k.Etkiler).Concat(y.Eylemler.SelectMany(e => e.Etkiler)).Concat(y.Krizler.SelectMany(k => k.Etkiler))
                .Where(e => e.Tur == Yanki.Core.Rules.EtkiTuru.Gercek && e.GercekSablonu.Contains("muhur", StringComparison.Ordinal)).ToArray();
            Assert.NotEmpty(muhurEtkileri);
            Assert.All(muhurEtkileri, e => Assert.StartsWith("konum!(muhur,", e.GercekSablonu, StringComparison.Ordinal));

            var (sim, _) = Kos(1UL, "iyilik_muhur", 168, diyalog: false);

            var anlatim = sim.Gunluk.Olaylar.Single(o => o.Tur == "anlatti" && o.Fail == "oyuncu" && o.Hedef == "Osman");
            Assert.Equal("muhtarlik", sim.Dunya.TekDeger("konum!", "muhur"));
            Assert.Contains(sim.Gunluk.Olaylar, o => o.Tur == "gercek_eklendi" && o.Deger("gercek") == "konum!(muhur,muhtarlik)" && o.KuralId == "muhur_yeri_ogrenildi" && o.Tick == anlatim.Tick);   // oyuncu eylemi adım 1, kural adım 3: aynı tick
            Assert.False(sim.Dunya.Var(Yanki.Core.World.Gercek.Ayristir("konum!(muhur,depo)")));
        }

        [Fact]
        public void Calinan_kese_tek_sahipli_Ayse_nin_kesesidir_hirsiz_Keremdir()
        {
            var calindi = new List<Olay>();
            foreach (var betik in BetikAdlari)
            {
                for (ulong seed = 1; seed <= 4; seed++)
                {
                    calindi.AddRange(Kos(seed, betik, 168, diyalog: false).Sim.Gunluk.Olaylar.Where(o => o.Tur == "kese_calindi"));
                }
            }

            Assert.NotEmpty(calindi);
            Assert.All(calindi, o => Assert.Equal(("Kerem", "Ayse"), (o.Fail, o.Hedef)));
        }

        // ---- Menü ve validate ----

        [Fact]
        public void Validate_temiz_oyuncu_menusu_sekiz_ile_on_girdi_arasinda_dort_aileyi_karsilar()
        {
            var girdi = DogrulamaKomutu.GirdiOku(Dizin, null, new StringWriter())!;
            var sonuc = SenaryoDogrulayici.Dogrula(girdi);

            Assert.True(sonuc.Bulgular.Count == 0, string.Join("\n", sonuc.Bulgular.Select(b => b.ToString())));
            var menu = Yanki.Core.Json.JsonAyristirici.Ayristir(File.ReadAllText(Path.Combine(Dizin, "oyuncu_eylemleri.json"))).Al("menu")!;
            Assert.InRange(menu.Elemanlar.Count, 8, 10);
            var aileler = menu.Elemanlar.Select(e => e.Al("aile")!.MetinDeger).Distinct().OrderBy(a => a, StringComparer.Ordinal).ToArray();
            Assert.Equal(new[] { "bilgi_ver", "iyilik", "konus", "kotuluk", "sakla" }, aileler);
        }
    }
}
