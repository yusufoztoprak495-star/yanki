using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Yanki.Cli;
using Yanki.Core.Events;
using Yanki.Core.Simulation;

namespace Yanki.Scenarios.Tests
{
    /// <summary>
    /// <c>samples/FrenliHan</c> (7. hafta, Yusuf'un seçimi F1) ve golden replay #4 (<c>tests/golden/frenli-han-1.json</c>, seed 42, 24 tick).
    /// Oyuncu Çiftçi'yi t2, t4, t8, t12 ve t16'da tehdit eder. Değişmezler senaryodan ÖNCE yazıldı; sayılar yer tutucudur (README).
    /// Kasaba Hanı içeriği DEĞİLDİR.
    /// </summary>
    public class FrenliHanTests
    {
        private static readonly int[] TehditTickleri = { 2, 4, 8, 12, 16 };

        private static string Kok()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "Yanki.slnx")))
            {
                d = d.Parent;
            }

            return d!.FullName;
        }

        internal static Simulasyon Calistir(ulong seed = 42)
        {
            var hata = new StringWriter();
            var sim = SenaryoDosyasi.Yukle(Path.Combine(Kok(), "samples", "FrenliHan", "scenario.json"), seed, hata);
            Assert.True(sim != null, hata.ToString());
            for (var t = 1; t <= 24; t++)
            {
                if (TehditTickleri.Contains(t))
                {
                    sim!.Submit(new PlayerAction("tehdit_etti", "Ciftci"));
                }

                sim!.Tick(1);
            }

            return sim!;
        }

        private static Olay[] Frenler(Simulasyon s, string fren, string? kimlik = null) =>
            s.Frenler(fren).Where(o => kimlik == null || o.Deger("kimlik") == kimlik).ToArray();

        private static long[] Tickler(IEnumerable<Olay> olaylar) => olaylar.Select(o => o.Tick).ToArray();

        // ---- her fren türünden en az bir olay ve hepsi için why ----

        [Fact]
        public void En_az_bir_cooldown_en_fazla_yay_siniri_ve_korumali_fren_olayi_vardir()
        {
            var s = Calistir();

            Assert.NotEmpty(Frenler(s, "cooldown", "tehdit_korkusu"));
            Assert.NotEmpty(Frenler(s, "en_fazla", "tehdit_gerginligi"));
            Assert.NotEmpty(Frenler(s, "yay_siniri", "tehdit_gerginligi"));
            Assert.NotEmpty(Frenler(s, "korumali"));
        }

        [Fact]
        public void Kural_frenlerinin_why_zinciri_oyuncunun_tehdidine_iner()
        {
            var s = Calistir();

            foreach (var tur in new[] { "cooldown", "en_fazla", "yay_siniri" })
            {
                foreach (var f in s.Frenler(tur))
                {
                    var zincir = s.Neden(f.Id);
                    Assert.Contains(zincir, o => o.Tur == "tehdit_etti" && o.KuralId == "player" && o.Hedef == "Ciftci" && o.Tick == f.Tick);
                }
            }
        }

        [Fact]
        public void Korumali_aday_freninin_nedeni_koruma_bildirimidir_why_calisir_zincir_bos()
        {
            var s = Calistir();

            foreach (var f in s.Frenler("korumali"))
            {
                Assert.Empty(s.Neden(f.Id));              // aday freninin tetikleyicisi yok: kök, senaryodaki korumali bildirimidir (çözülen varlık yükte)
                Assert.NotEqual(string.Empty, f.Deger("hedef"));
            }

            var oturum = Path.Combine(Kok(), "tests", "golden", "frenli-han-1.json");
            var id = s.Frenler("korumali").First().Id;
            var cikti = new StringWriter();
            var kod = KomutCalistirici.Calistir(new[] { "why", oturum, id.ToString() }, cikti, new StringWriter());
            Assert.Equal(0, kod);
            Assert.Contains("fren_uygulandi", cikti.ToString());
        }

        // ---- ayrıntılı davranış ----

        [Fact]
        public void Tehdit_gerginligi_kurali_kural_kapsaminda_uc_kez_calisir_ucuncuda_kirpilir_dorduncude_en_fazla_freni()
        {
            var s = Calistir();

            var degisimler = s.Gunluk.Olaylar.Where(o => o.Tur == "yay_degisti").ToArray();
            Assert.Equal(new[] { "2", "4", "5" }, degisimler.Select(o => o.Deger("deger")).ToArray());     // 0→2→4→6 kırpılır→5
            Assert.Equal(new long[] { 2, 4, 8 }, Tickler(degisimler));
            Assert.All(degisimler, o => Assert.Equal("2", o.Deger("delta")));
            Assert.Equal(5m, s.Yay("kasaba_gerginligi"));

            var kirpma = Assert.Single(Frenler(s, "yay_siniri"));
            Assert.Equal((8L, "kasaba_gerginligi"), (kirpma.Tick, kirpma.Deger("hedef")));

            Assert.Equal(new long[] { 12, 16 }, Tickler(Frenler(s, "en_fazla", "tehdit_gerginligi")));     // 4. ve 5. tehdit
        }

        [Fact]
        public void Tehdit_korkusu_kurali_cooldown_penceresi_icinde_fren_uretir_pencere_dolunca_yeniden_calisir()
        {
            var s = Calistir();

            var korku = s.Gunluk.Olaylar.Where(o => o.Tur == "modifier_eklendi" && o.KuralId == "tehdit_korkusu").ToArray();
            Assert.Equal(new long[] { 2, 16 }, Tickler(korku));                                           // t2 çalıştı; t4, t8, t12 pencerede; t16 açık (16 − 2 ≥ 12)
            Assert.All(korku, o => Assert.Equal("Ciftci", o.Deger("npc")));
            Assert.Equal(new long[] { 4, 8, 12 }, Tickler(Frenler(s, "cooldown", "tehdit_korkusu")));
        }

        [Fact]
        public void Yayi_okuyan_kural_gerginlik_dusukken_etki_vermez_yuksekken_verir()
        {
            var s = Calistir();

            var ofke = s.Gunluk.Olaylar.Where(o => o.Tur == "modifier_eklendi" && o.KuralId == "gergin_ofke").ToArray();
            Assert.Equal(new long[] { 8, 12, 16 }, Tickler(ofke));      // adım başında gerginlik 4, 5, 5 (≥ 4)
            Assert.All(ofke, o => Assert.Equal("Ciftci", o.Deger("npc")));

            // Aynı tehdit gerginlik düşükken (t2: 0, t4: 2) bu etkiyi vermedi.
            Assert.DoesNotContain(ofke, o => o.Tick == 2 || o.Tick == 4);
            foreach (var tick in new long[] { 2, 4 })
            {
                Assert.Contains(s.Gunluk.Olaylar, o => o.Tur == "tehdit_etti" && o.Tick == tick && o.KuralId == "player");
            }
        }

        [Fact]
        public void Korumali_Hanci_han_dan_ayrilmaz_git_adayi_elenir_fren_bir_kez_yazilir()
        {
            var s = Calistir();

            Assert.DoesNotContain(s.Gunluk.Olaylar, o => o.Tur == "ayrildi" && o.Fail == "Hanci");
            Assert.Equal("han", s.Dunya.TekDeger("konum!", "Hanci"));
            var fren = Assert.Single(s.Frenler("korumali", "Hanci"));                  // engel başına bir kez (24 tick boyunca tek olay)
            Assert.Equal(("git", "Hanci"), (fren.Deger("kimlik"), fren.Deger("npc")));
            Assert.Equal(1, fren.Tick);
            var elenen = s.KararIzi("Hanci")!.Adaylar.Where(a => a.Aday.Eylem.Id == "git").ToArray();
            Assert.All(elenen, a =>
            {
                Assert.True(a.Elendi);
                Assert.Contains("korumali", a.Neden);
            });
        }

        [Fact]
        public void Yabancinin_hirsizlik_eylemi_kese_calinamaz_oldugu_icin_aday_olmaz_ve_fren_yazilir()
        {
            var s = Calistir();

            Assert.DoesNotContain(s.Gunluk.Olaylar, o => o.Tur == "hirsizlik_yapildi");
            var fren = Assert.Single(s.Frenler("korumali", "Yabanci"));                // ön koşulları tutuyor (açgözlülük ≥ 0.7): fren gerçekten yazıldı
            Assert.Equal(("hirsizlik", "kese"), (fren.Deger("kimlik"), fren.Deger("hedef")));
            Assert.Empty(s.Frenler("korumali", "Ciftci"));                              // ön koşulu tutmayanlar için fren yok
            Assert.Equal(2, s.Frenler("korumali").Count);
        }

        [Fact]
        public void Golden_4_kaydi_tehditleri_icerir_ve_replay_hash_i_tutar()
        {
            var yol = Path.Combine(Kok(), "tests", "golden", "frenli-han-1.json");
            Assert.True(File.Exists(yol), "tests/golden/frenli-han-1.json (golden #4) yok");

            var okuma = OturumKaydi.Oku(File.ReadAllText(yol), yol);
            Assert.True(okuma.Basarili, string.Join("\n", okuma.Hatalar));
            var kayit = okuma.Kayit!;
            Assert.Equal((42UL, 24L, 2), (kayit.Seed, kayit.TickSayisi, kayit.Surum));
            Assert.Equal(TehditTickleri.Select(t => (long)t).ToArray(), kayit.Eylemler.Select(e => e.Tick).ToArray());
            Assert.NotNull(kayit.GunlukHash);
            Assert.NotNull(kayit.SenaryoHash);

            var sim = Calistir();
            Assert.Equal(kayit.SonHash, sim.SonHash);                                     // aynı seed ve girdi: aynı son_hash
            Assert.Equal(kayit.GunlukHash, GunlukHashu.Hesapla(sim.Gunluk));              // ve aynı gunluk_hash (fren olayları dahil)
        }

        [Fact]
        public void Yankilanan_deger_kontrolu_sinirlar_icinde_kalir()
        {
            var s = Calistir();
            Assert.InRange(s.Yay("kasaba_gerginligi"), 0m, 5m);
            Assert.True(s.Gunluk.Olaylar.Count(o => o.Tur == "fren_uygulandi") > 0);
        }
    }
}
