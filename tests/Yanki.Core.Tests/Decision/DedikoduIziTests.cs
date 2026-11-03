using System.Linq;
using Xunit;
using Yanki.Core.Decision;
using Yanki.Core.Json;
using Yanki.Core.Mind;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;
using Yanki.Core.Tests.Simulation;

namespace Yanki.Core.Tests.Decision
{
    /// <summary>6. hafta karar izi: aday başına konu puanı ve uygulanan stres artışı (yalnızca ilgili eylemlerde yazılır).</summary>
    public class DedikoduIziTests
    {
        private static readonly string[] Eylemler =
        {
            "{ \"id\": \"anlat\", \"olay_turu\": \"anlatti\", \"hedef\": \"npc\", \"taban\": 1, \"aktarim\": \"dogru\", \"konu_desenleri\": [\"borclu(*,*,*)\"] }",
            "{ \"id\": \"yalan_soyle\", \"olay_turu\": \"anlatti\", \"hedef\": \"npc\", \"taban\": 0.5, \"aktarim\": \"yalan\", \"yalan_onermeleri\": [\"degil(borclu(Mehmet,Ayse,500))\"], \"celisen_ozellikler\": [\"durustluk\"], \"stres_tabani\": 15 }",
        };

        private static Simulasyon Kur(string[] eylemler)
        {
            var senaryo = SenaryoYukleyici.Yukle(OyuncuAnlatimiTests.SenaryoJson, "s.json");
            Assert.True(senaryo.Basarili, string.Join("\n", senaryo.Hatalar));
            var kurallar = KuralYukleyici.Yukle(OyuncuAnlatimiTests.IftiraKurali, "k.json", senaryo.Senaryo!.OlayTurleri);
            return Simulasyon.Olustur(senaryo.Senaryo!, kurallar.Kurallar, 42, new ZihinAyarlari { KanitSonumuTickBasina = 0m }, eylemler: KararFikstur.Yukle(eylemler));
        }

        private static JsonDegeri Iz(Simulasyon sim, string npc) => JsonAyristirici.Ayristir(KararIziYazici.Yaz(sim.KararIzi(npc)!));

        [Fact]
        public void Stres_artisi_tabani_carpi_celisen_ozellik_degerlerinin_toplamidir()
        {
            var e = KararFikstur.Yukle(Eylemler).Single(x => x.Id == "yalan_soyle");
            var kisilik = new Kisilik(0.5m, 0.8m, 0.5m, 0.5m, 0.5m, 0.5m);

            Assert.Equal(12m, e.StresArtisi(kisilik));   // 15 × durustluk 0.8
            Assert.Equal(0m, KararFikstur.Yukle(Eylemler).Single(x => x.Id == "anlat").StresArtisi(kisilik));
        }

        [Fact]
        public void Karar_izi_secilen_celisen_eylemin_stres_artisini_gosterir()
        {
            var sim = Kur(new[] { Eylemler[1] });   // tek aday: yalan_soyle
            sim.Submit(OyuncuAnlatimiTests.Anlat("Ayse", OyuncuAnlatimiTests.Borc));
            sim.Tick(2);

            var iz = sim.KararIzi("Ayse")!;
            Assert.Equal("yalan_soyle", iz.Secilen!.Eylem.Id);
            Assert.Equal(7.5m, iz.StresArtisi);   // 15 × durustluk 0.5
            Assert.Equal(7.5m, Iz(sim, "Ayse").Al("secilen")!.Al("stres_artisi")!.SayiDeger);
            Assert.Contains(KararIziOzeti.Satirlar(sim.SonKararOlayi("Ayse")!, iz), x => x.Contains("stres artışı: +7.5"));
        }

        [Fact]
        public void Celiskisiz_secimde_stres_artisi_alani_yazilmaz()
        {
            var sim = Kur(new[] { Eylemler[0] });
            sim.Submit(OyuncuAnlatimiTests.Anlat("Ayse", OyuncuAnlatimiTests.Borc));
            sim.Tick(2);

            var secilen = Iz(sim, "Ayse").Al("secilen")!;
            Assert.Null(secilen.Al("stres_artisi"));
        }

        [Fact]
        public void Konusma_adayinin_konu_puani_ve_konulari_tam_izde_gorunur()
        {
            var sim = Kur(new[] { Eylemler[0] });
            sim.Submit(OyuncuAnlatimiTests.Anlat("Ayse", OyuncuAnlatimiTests.Borc));
            sim.Tick(2);

            var iz = Iz(sim, "Ayse");
            var aday = iz.Al("adaylar")!.Elemanlar.First(a => a.Al("eylem")!.MetinDeger == "anlat" && a.Al("hedef")!.MetinDeger == "Can");
            Assert.True(aday.Al("konu_puani")!.SayiDeger > 0m);
            var konu = Assert.Single(aday.Al("konular")!.Elemanlar);
            Assert.Equal(OyuncuAnlatimiTests.Borc, konu.Al("onerme")!.MetinDeger);
            Assert.True(konu.Al("puan")!.SayiDeger > 0m);

            var metin = string.Join("\n", KararIziOzeti.Satirlar(sim.SonKararOlayi("Ayse")!, sim.KararIzi("Ayse")));
            Assert.Contains("konu puanı", metin);
        }

        [Fact]
        public void Konusma_olmayan_adayda_konu_alanlari_yazilmaz()
        {
            var sim = Kur(new[] { Eylemler[0], KararFikstur.Eylem("selam", "yok") });
            sim.Submit(OyuncuAnlatimiTests.Anlat("Ayse", OyuncuAnlatimiTests.Borc));
            sim.Tick(2);

            var selam = Iz(sim, "Ayse").Al("adaylar")!.Elemanlar.First(a => a.Al("eylem")!.MetinDeger == "selam");
            Assert.Null(selam.Al("konu_puani"));
            Assert.Null(selam.Al("konular"));
        }

        [Fact]
        public void Konusu_olmayan_konusma_adayi_elenir_ve_neden_izde_gorunur()
        {
            var sim = Kur(new[] { Eylemler[0], KararFikstur.Eylem("selam", "yok") });
            sim.Tick(2);   // kimse bir şey bilmiyor

            var elenen = Iz(sim, "Ayse").Al("adaylar")!.Elemanlar.First(a => a.Al("eylem")!.MetinDeger == "anlat");
            Assert.Equal("elendi", elenen.Al("durum")!.MetinDeger);
            Assert.StartsWith("konu yok", elenen.Al("neden")!.MetinDeger);
        }
    }
}
