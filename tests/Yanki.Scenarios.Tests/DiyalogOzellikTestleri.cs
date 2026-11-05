using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CsCheck;
using Xunit;
using Yanki.Core.Events;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;
using Yanki.Dialogue;

namespace Yanki.Scenarios.Tests
{
    /// <summary>
    /// Diyalog değişmezleri (8. hafta, görev metni 17; CsCheck, sabit tohum, CI'da makul örnek sayısı): rastgele GEÇERLİ oyuncu eylem dizileriyle. Şablon dosyası bir TEST FİKSTÜRÜDÜR
    /// (Kasaba Hanı ya da HanGerilim şablon içeriği değil; metinler Yusuf'un alanıdır, DURAK 2). Örnek sayısı ve tohum <see cref="OzellikTestleri"/> ile aynı ortam değişkenleriyle ayarlanır.
    /// Her değişmez, boş geçmediğini sayaçlarla denetler.
    /// </summary>
    public class DiyalogOzellikTestleri
    {
        // Sınırlı tekrar kuralları: anlat_bekle (tekrar_bekleme 6), anlat_tek (bir_kez); tehdit_gecmis hafıza göndermesi; tehdit genel şablonu cevap followup'ı tetikler.
        private const string Sablon = @"{ 'dil': 'tr',
 'adlar': { 'Ayse': 'Ayşe', 'Can': 'Can', 'Hatice': 'Hatice', 'Kemal': 'Kemal', 'Mehmet': 'Mehmet', 'Selim': 'Selim', 'oyuncu': { 'ad': 'yabancı', 'cins': true } },
 'onerme_metinleri': {
  'borclu': { 'metin': '{0}, {1.yonelme} {2} lira borçlu', 'olumsuz': '{0}, {1.yonelme} {2} lira borçlu değil', 'sayilar': [2] },
  'kardes': { 'metin': '{0} ile {1} kardeş', 'olumsuz': '{0} ile {1} kardeş değil' },
  'sir_sakliyor': { 'metin': '{0}, {1.ayrilma} bir şey saklıyor', 'olumsuz': '{0}, {1.ayrilma} bir şey saklamıyor' }
 },
 'esleme': {
  'anlatti': { 'niyet': 'anlatma', 'konusan': 'fail' },
  'tehdit_etti': { 'niyet': 'tehdit', 'konusan': 'hedef' },
  'itiraf_etti': { 'niyet': 'itiraf', 'konusan': 'fail' },
  'patladi': { 'niyet': 'patlama', 'konusan': 'fail' }
 },
 'gramer': { 'giris': ['Dinle', 'Bak', 'Şunu duydum'] },
 'sablonlar': [
  { 'id': 'anlat_genel', 'concept': 'anlatma', 'genel': true, 'sonra': { 'hedef': 'dinleyen', 'concept': 'cevap' }, 'metin': '#giris# #dinleyen#: #konu#.' },
  { 'id': 'anlat_bekle', 'concept': 'anlatma', 'kriterler': [ { 'alan': 'kisilik(sosyallik)', 'op': '>=', 'deger': 0, 'zorunlu': true, 'agirlik': 3 } ], 'tekrar': { 'tekrar_bekleme': 6 }, 'sonra': { 'hedef': 'dinleyen', 'concept': 'cevap' }, 'metin': 'Bekle: #konu#.' },
  { 'id': 'anlat_tek', 'concept': 'anlatma', 'kriterler': [ { 'alan': 'kisilik(sosyallik)', 'op': '>=', 'deger': 0, 'zorunlu': true, 'agirlik': 2 } ], 'tekrar': 'bir_kez', 'sonra': { 'hedef': 'dinleyen', 'concept': 'cevap' }, 'metin': 'Bir kez: #konu#.' },
  { 'id': 'anlat_gergin', 'concept': 'anlatma', 'kriterler': [ { 'alan': 'stres()', 'op': '>=', 'deger': 5, 'zorunlu': true, 'agirlik': 4 } ], 'sonra': { 'hedef': 'dinleyen', 'concept': 'cevap' }, 'metin': 'Gergin: #konu#.' },
  { 'id': 'tehdit_genel', 'concept': 'tehdit', 'genel': true, 'metin': 'Beni tehdit etme #dinleyen#.', 'sonra': { 'hedef': 'dinleyen', 'concept': 'cevap' } },
  { 'id': 'tehdit_gecmis', 'concept': 'tehdit', 'kriterler': [ { 'alan': 'hafiza_var(tehdit_etti, dinleyen, 48)', 'op': '==', 'deger': true, 'zorunlu': true } ],
    'hafiza': { 'tur': 'tehdit_etti', 'fail': 'dinleyen', 'son_n_tick': 48 }, 'metin': '#hafiza.zaman# da böyle yapmıştın #dinleyen#.' },
  { 'id': 'cevap_genel', 'concept': 'cevap', 'genel': true, 'metin': 'Haklısın.' },
  { 'id': 'cevap_kinli', 'concept': 'cevap', 'kriterler': [ { 'alan': 'kisilik(kin_tutma)', 'op': '>=', 'deger': 0.8, 'zorunlu': true } ], 'metin': 'Kinli cevap.' },
  { 'id': 'itiraf_genel', 'concept': 'itiraf', 'genel': true, 'metin': 'İtiraf ediyorum #dinleyen.yonelme#.' },
  { 'id': 'patlama_genel', 'concept': 'patlama', 'genel': true, 'metin': 'Yeter artık!' }
 ] }";

        private static readonly SablonDosyasi Dosya = Yukle();

        private static SablonDosyasi Yukle()
        {
            var sonuc = SablonYukleyici.Yukle(Sablon.Replace('\'', '"'), "fikstur-sablon.json");
            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            return sonuc.Dosya!;
        }

        private static int Ornek => OzellikTestleri.Ornek;

        private static string Tohum => OzellikTestleri.Tohum;

        private static (Simulasyon Sim, DiyalogMotoru Motor) DiyalogluKos(string klasor, int seed, ILookup<int, PlayerAction> takvim, int tick, Action<Simulasyon, DiyalogMotoru>? herTick = null)
        {
            var sim = OzellikTestleri.Yukle(klasor, seed);
            var motor = DiyalogMotoru.Baglan(sim, Dosya);
            for (var t = 1; t <= tick; t++)
            {
                OzellikTestleri.Gonder(sim, takvim[t]);
                sim.Tick(1);
                herTick?.Invoke(sim, motor);
            }

            return (sim, motor);
        }

        private static string Ozet(DiyalogMotoru m) =>
            string.Join("\n", m.Satirlar.Select(s => $"{s.No}|{s.Tick}|{s.OlayId}|{s.Konusan}|{s.Dinleyen}|{s.SablonId}|{s.Metin}|{s.Followup}"));

        private static readonly (string Ad, int Bekleme)[] Sinirli = { ("anlat_bekle", 6), ("anlat_tek", -1) };

        // ---- 1. diyalog açık ve kapalı ----

        [Fact]
        public void Diyalog_acik_ve_kapali_son_hash_ve_gunluk_hash_ayni()
        {
            var satir = 0;
            var followup = 0;
            OzellikTestleri.Girdi(OzellikTestleri.HanHavuzu.Length).Sample(
                g =>
                {
                    var takvim = OzellikTestleri.Takvim(g.Eylemler, OzellikTestleri.HanHavuzu);
                    var kapali = OzellikTestleri.Kos("HanGerilim", g.Seed, takvim, OzellikTestleri.Tick);
                    var (acik, motor) = DiyalogluKos("HanGerilim", g.Seed, takvim, OzellikTestleri.Tick);

                    Assert.Equal(kapali.SonHash, acik.SonHash);
                    Assert.Equal(GunlukHashu.Hesapla(kapali.Gunluk), GunlukHashu.Hesapla(acik.Gunluk));
                    satir += motor.Satirlar.Count;
                    followup += motor.Satirlar.Count(s => s.Followup);
                },
                iter: Ornek, seed: Tohum, threads: 1);

            Assert.True(satir > 0, "hiçbir örnekte diyalog satırı üretilmedi: değişmez anlamsız kaldı");
            Assert.True(followup > 0, "hiçbir örnekte followup üretilmedi");
        }

        // ---- 2. aynı seed ve girdi ----

        [Fact]
        public void Ayni_seed_ve_girdi_ayni_satir_dizisi_ve_diyalog_hash()
        {
            OzellikTestleri.Girdi(OzellikTestleri.HanHavuzu.Length).Sample(
                g =>
                {
                    var takvim = OzellikTestleri.Takvim(g.Eylemler, OzellikTestleri.HanHavuzu);
                    var a = DiyalogluKos("HanGerilim", g.Seed, takvim, OzellikTestleri.Tick).Motor;
                    var b = DiyalogluKos("HanGerilim", g.Seed, takvim, OzellikTestleri.Tick).Motor;

                    Assert.Equal(Ozet(a), Ozet(b));
                    Assert.Equal(a.DiyalogHash(), b.DiyalogHash());
                },
                iter: Ornek, seed: Tohum, threads: 1);
        }

        // ---- 3. N tick koş, kaydet, yükle, M tick koş ≡ N+M tick koş (bekleyen followup durumu dahil) ----

        [Fact]
        public void N_tick_kos_kaydet_yukle_M_tick_kos_ile_N_arti_M_ayni_satirlari_ve_bekleyen_followupi_verir()
        {
            var bekleyenGorulen = 0;
            OzellikTestleri.Girdi(OzellikTestleri.HanHavuzu.Length).Sample(
                x =>
                {
                    var takvim = OzellikTestleri.Takvim(x.Eylemler, OzellikTestleri.HanHavuzu);
                    var kesintisiz = DiyalogluKos("HanGerilim", x.Seed, takvim, OzellikTestleri.Tick).Motor;

                    var (ilkSim, _) = DiyalogluKos("HanGerilim", x.Seed, takvim, x.N);
                    if (x.Bekleyen)
                    {
                        OzellikTestleri.Gonder(ilkSim, takvim[x.N + 1]);
                    }

                    var kayit = OturumKaydi.Oku(OturumKaydi.Yaz(OturumKaydi.Uret(ilkSim, "../../samples/HanGerilim/scenario.json")), "kayit.json").Kayit!;
                    var yuklenen = OzellikTestleri.Yukle("HanGerilim", x.Seed);
                    var motor = DiyalogMotoru.Baglan(yuklenen, Dosya);   // yükleme (baştan oynatma) öncesi bağlanır: diyalog durumu aynen yeniden kurulur
                    var sonuc = OturumOynatici.Oynat(yuklenen, kayit);
                    Assert.True(sonuc.HashTutuyor && sonuc.GunlukHashTutuyor);
                    bekleyenGorulen += motor.BekleyenFollowupSayisi;

                    for (var t = x.N + 1; t <= OzellikTestleri.Tick; t++)
                    {
                        if (!(x.Bekleyen && t == x.N + 1))
                        {
                            OzellikTestleri.Gonder(yuklenen, takvim[t]);
                        }

                        yuklenen.Tick(1);
                    }

                    Assert.Equal(Ozet(kesintisiz), Ozet(motor));
                    Assert.Equal(kesintisiz.BekleyenFollowupSayisi, motor.BekleyenFollowupSayisi);
                    Assert.Equal(kesintisiz.DiyalogHash(), motor.DiyalogHash());
                },
                iter: Ornek, seed: Tohum, threads: 1);

            Assert.True(bekleyenGorulen > 0, "kayıt noktasında hiç bekleyen followup görülmedi: değişmezin bekleyen durum kolu sınanmadı");
        }

        // ---- 4. k tick'e kadar oynatma ≡ tam oynatmanın ilk k tick'lik satırları ----

        [Fact]
        public void K_tick_oynatma_tam_oynatmanin_ilk_k_tick_satirlariyla_birebir_ayni()
        {
            var karsilastirilan = 0;
            OzellikTestleri.Girdi(OzellikTestleri.HanHavuzu.Length).Sample(
                x =>
                {
                    var takvim = OzellikTestleri.Takvim(x.Eylemler, OzellikTestleri.HanHavuzu);
                    var tam = DiyalogluKos("HanGerilim", x.Seed, takvim, OzellikTestleri.Tick).Motor;
                    var kismi = DiyalogluKos("HanGerilim", x.Seed, takvim, x.N).Motor;

                    var tamOnek = string.Join("\n", tam.Satirlar.Where(s => s.Tick <= x.N).Select(s => $"{s.No}|{s.Tick}|{s.OlayId}|{s.Konusan}|{s.Dinleyen}|{s.SablonId}|{s.Metin}|{s.Followup}"));
                    Assert.Equal(tamOnek, Ozet(kismi));
                    karsilastirilan += kismi.Satirlar.Count;
                },
                iter: Ornek, seed: Tohum, threads: 1);

            Assert.True(karsilastirilan > 0, "hiçbir örnekte önek satırı yoktu");
        }

        // ---- 5. her satırın olay kimliği günlükte var ----

        [Fact]
        public void Her_satirin_olay_kimligi_gunlukte_var_ve_eslenen_bir_olay_turudur()
        {
            var eslenen = new[] { "anlatti", "tehdit_etti", "itiraf_etti", "patladi" };
            var satir = 0;
            OzellikTestleri.Girdi(OzellikTestleri.HanHavuzu.Length).Sample(
                g =>
                {
                    var takvim = OzellikTestleri.Takvim(g.Eylemler, OzellikTestleri.HanHavuzu);
                    var (sim, motor) = DiyalogluKos("HanGerilim", g.Seed, takvim, OzellikTestleri.Tick);

                    foreach (var s in motor.Satirlar)
                    {
                        Assert.True(sim.Gunluk.Var(s.OlayId), $"satır #{s.No} olay #{s.OlayId} günlükte yok");
                        Assert.Contains(sim.Gunluk[s.OlayId].Tur, eslenen);
                        Assert.True(sim.Zihinler.Var(s.Konusan));
                        satir++;
                    }
                },
                iter: Ornek, seed: Tohum, threads: 1);

            Assert.True(satir > 0);
        }

        // ---- 7. tekrar_bekleme ve bir_kez ihlal edilmiyor ----

        [Fact]
        public void Tekrar_bekleme_ve_bir_kez_hicbir_girdi_dizisinde_ihlal_edilmez()
        {
            var sinirliKullanim = 0;
            OzellikTestleri.Girdi(OzellikTestleri.HanHavuzu.Length).Sample(
                g =>
                {
                    var takvim = OzellikTestleri.Takvim(g.Eylemler, OzellikTestleri.HanHavuzu);
                    var motor = DiyalogluKos("HanGerilim", g.Seed, takvim, OzellikTestleri.Tick).Motor;

                    foreach (var (ad, bekleme) in Sinirli)
                    {
                        foreach (var konusan in motor.Satirlar.Where(s => s.SablonId == ad).GroupBy(s => s.Konusan))
                        {
                            var kullanimlar = konusan.OrderBy(s => s.No).ToList();
                            sinirliKullanim += kullanimlar.Count;
                            if (bekleme < 0)
                            {
                                Assert.True(kullanimlar.Count <= 1, $"{ad}: {konusan.Key} için {kullanimlar.Count} kez");
                            }
                            else
                            {
                                for (var i = 1; i < kullanimlar.Count; i++)
                                {
                                    Assert.True(kullanimlar[i].Tick - kullanimlar[i - 1].Tick >= bekleme, $"{ad}: {konusan.Key} tick {kullanimlar[i - 1].Tick} → {kullanimlar[i].Tick}");
                                }
                            }
                        }
                    }
                },
                iter: Ornek, seed: Tohum, threads: 1);

            Assert.True(sinirliKullanim > 0, "sınırlı şablonlar hiç kullanılmadı: değişmez anlamsız kaldı");
        }

        // ---- 6. hafıza şablonunun olayı konuşanın hafızasında var (üretim anında) ----

        private const string MiniSenaryo = @"{ ""surum"": 1, ""ad"": ""M"", ""mekanlar"": [""han""], ""baglantilar"": [],
  ""oyuncu"": { ""id"": ""oyuncu"", ""konum"": ""han"" },
  ""npcler"": [
    { ""id"": ""Ali"", ""konum"": ""han"", ""kisilik"": { ""acgozluluk"": 0.5, ""durustluk"": 0.7, ""cesaret"": 0.5, ""sosyallik"": 0.5, ""kin_tutma"": 0.5, ""merhamet"": 0.5 } },
    { ""id"": ""Veli"", ""konum"": ""han"", ""kisilik"": { ""acgozluluk"": 0.2, ""durustluk"": 0.5, ""cesaret"": 0.5, ""sosyallik"": 0.5, ""kin_tutma"": 0.9, ""merhamet"": 0.5 } }
  ],
  ""gercekler"": [], ""olay_turleri"": [""yardim_etti""], ""kural_dosyalari"": [] }";

        private const string MiniSablon = @"{ 'dil': 'tr', 'adlar': { 'oyuncu': 'yabancı' },
 'esleme': { 'yardim_etti': { 'niyet': 'tesekkur', 'konusan': 'hedef' } },
 'sablonlar': [
  { 'id': 'genel', 'concept': 'tesekkur', 'genel': true, 'metin': 'Sağ ol.' },
  { 'id': 'gecmis', 'concept': 'tesekkur', 'kriterler': [ { 'alan': 'hafiza_var(yardim_etti, oyuncu, 60)', 'op': '==', 'deger': true, 'zorunlu': true } ],
    'hafiza': { 'tur': 'yardim_etti', 'fail': 'oyuncu', 'son_n_tick': 60 }, 'metin': '#hafiza.zaman# yardım ettin.' } ] }";

        [Fact]
        public void Hafiza_sablonunun_gonderdigi_olay_uretim_aninda_konusanin_hafizasinda_ve_pencerededir()
        {
            var dosya = SablonYukleyici.Yukle(MiniSablon.Replace('\'', '"'), "mini.json").Dosya!;
            var gonderme = 0;
            Gen.Int[1, 80].List[1, 8].Select(Gen.Int[0, 1].List[8, 8]).Sample(
                g =>
                {
                    var senaryo = SenaryoYukleyici.Yukle(MiniSenaryo, "m.json").Senaryo!;
                    var sim = Simulasyon.Olustur(senaryo, new KuralTanimi[0], 42, zihinAyarlari: new Yanki.Core.Mind.ZihinAyarlari { HafizaSonumuTickBasina = 0.01m });
                    var motor = DiyalogMotoru.Baglan(sim, dosya);
                    var kontrol = 0;
                    sim.OlayYayinlandi += _ =>
                    {
                        for (; kontrol < motor.Satirlar.Count; kontrol++)
                        {
                            var s = motor.Satirlar[kontrol];
                            var not = s.NedenSatirlari.FirstOrDefault(n => n.StartsWith("hafıza göndermesi: olay #", StringComparison.Ordinal));
                            if (s.SablonId != "gecmis")
                            {
                                continue;
                            }

                            Assert.NotNull(not);
                            var id = long.Parse(not!.Substring("hafıza göndermesi: olay #".Length).Split(' ')[0], CultureInfo.InvariantCulture);
                            Assert.Contains(sim.Zihinler[s.Konusan].Hafiza(sim.SimdikiTick), h => h.OlayId == id);
                            Assert.True(sim.SimdikiTick - sim.Gunluk[id].Tick <= 60);
                            Assert.Equal("yardim_etti", sim.Gunluk[id].Tur);
                            Assert.Equal(sim.OyuncuId, sim.Gunluk[id].Fail);
                            Assert.NotEqual(s.OlayId, id);
                            gonderme++;
                        }
                    };

                    var yardimlar = g.Item1.OrderBy(t => t).ToList();
                    for (var t = 1; t <= 85; t++)
                    {
                        foreach (var yardimTick in yardimlar.Where(y => y == t).Take(1))
                        {
                            sim.Submit(new PlayerAction("yardim_etti", g.Item2[t % 8] == 0 ? "Ali" : "Veli"));
                        }

                        sim.Tick(1);
                    }
                },
                iter: Ornek, seed: Tohum, threads: 1);

            Assert.True(gonderme > 0, "hiçbir örnekte hafıza göndermesi seçilmedi: değişmez anlamsız kaldı");
        }

        // ---- 8. doğru ve yalan anlatım diyalog niyetinde ayırt edilemiyor (yalanın GERÇEKTEN söylendiği senaryo) ----

        [Fact]
        public void Dogru_ve_yalan_anlatim_diyalog_niyetinde_ayirt_edilemez_yalan_gercekten_soylenir()
        {
            var yalanli = 0;
            var maskelenen = 0;
            PlayerAction Anlat(string h, string o) => new PlayerAction("anlatti", h, new[] { new KeyValuePair<string, string>("onerme", o) });
            var havuz = new List<PlayerAction> { Anlat("Hatice", "sir_sakliyor(Hatice,Mehmet)"), Anlat("Hatice", "borclu(Mehmet,Ayse,500)"), new PlayerAction("hakaret_et", "Hatice") };
            var ayar = new DiyalogAyarlari();
            Gen.Int[1, OzellikTestleri.Tick].Select(Gen.Int[0, 2]).List[1, 6].Select(Gen.Int[1, 60]).Sample(
                g =>
                {
                    var takvim = g.Item1.ToLookup(e => e.Item1, e => havuz[e.Item2]);
                    var (sim, _) = DiyalogluKos("SiraEtkisiKriz", g.Item2, takvim, OzellikTestleri.Tick);
                    // Oracle (yalnızca test): konuşanın aktarım kaydında yalan işaretli, aynı (tick, konuşan, hedef, önerme) anlatım olayı. Diyalog katmanı bunu göremez.
                    var yalanOlaylari = new HashSet<long>();
                    foreach (var npc in sim.Zihinler.Kimlikler)
                    {
                        foreach (var a in sim.Zihinler[npc].Aktarimlar.Where(a => a.Yalan))
                        {
                            foreach (var e in sim.Gunluk.Olaylar.Where(e => e.Tur == "anlatti" && e.Tick == a.Tick && e.Fail == npc && e.Hedef == a.Kime && e.Deger("onerme") == a.Onerme))
                            {
                                yalanOlaylari.Add(e.Id);
                            }
                        }
                    }

                    foreach (var o in sim.Gunluk.Olaylar.Where(o => o.Tur == "anlatti"))
                    {
                        // Aynı olayın KuralId'si, kökü, görünürlüğü ve nedenleri maskelenir: niyet alan alan aynı kalmalı.
                        var maske = new Olay(
                            o.Id, o.Tick, o.Sira, o.Tur, o.Fail, o.Hedef, o.Mekan,
                            new[] { new KeyValuePair<string, string>("onerme", o.Deger("onerme")), new KeyValuePair<string, string>("kok", "maske"), new KeyValuePair<string, string>("gorunurluk", "acik") },
                            new long[0], "eylem:maske");
                        var a = NiyetUretici.Uret(o, sim, Dosya, ayar);
                        var b = NiyetUretici.Uret(maske, sim, Dosya, ayar);

                        Assert.Equal(a == null, b == null);
                        if (a != null && b != null)
                        {
                            Assert.Equal(NiyetOzeti(a), NiyetOzeti(b));
                            if (yalanOlaylari.Contains(o.Id))
                            {
                                maskelenen++;
                            }
                        }
                    }

                    yalanli += yalanOlaylari.Count;
                },
                iter: Ornek, seed: Tohum, threads: 1);

            Assert.True(yalanli > 0, "hiçbir örnekte yalan söylenmedi: değişmez anlamsız kaldı");
            Assert.True(maskelenen > 0, "yalan olayından niyet üretilen hiçbir örnek yok");
        }

        private static string NiyetOzeti(Niyet n) =>
            string.Join("|", n.Konusan, n.Dinleyen, n.Ad, n.Konu, n.Ton, n.FikirBandi, n.Gerekce.FikirBandi, n.Gerekce.BaskinDuygu, n.OlayId, n.Tick, n.Followup);

        // ---- 9. tr-TR ve invariant kültürde aynı diyalog_hash ----

        [Fact]
        public void tr_TR_ve_invariant_kulturde_ayni_diyalog_hash()
        {
            var onceki = CultureInfo.CurrentCulture;
            var satir = 0;
            try
            {
                OzellikTestleri.Girdi(OzellikTestleri.HanHavuzu.Length).Sample(
                    g =>
                    {
                        var takvim = OzellikTestleri.Takvim(g.Eylemler, OzellikTestleri.HanHavuzu);
                        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                        var inv = DiyalogluKos("HanGerilim", g.Seed, takvim, OzellikTestleri.Tick).Motor;
                        CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                        var tr = DiyalogluKos("HanGerilim", g.Seed, takvim, OzellikTestleri.Tick).Motor;

                        Assert.Equal(inv.DiyalogHash(), tr.DiyalogHash());
                        Assert.Equal(Ozet(inv), Ozet(tr));
                        satir += inv.Satirlar.Count;
                    },
                    iter: Math.Min(Ornek, 10), seed: Tohum, threads: 1);
            }
            finally
            {
                CultureInfo.CurrentCulture = onceki;
            }

            Assert.True(satir > 0);
        }
    }
}
