using System;
using System.IO;
using System.Linq;
using Xunit;
using Yanki.Cli;
using Yanki.Core.Simulation;
using Yanki.Dialogue;
using Yanki.Dialogue.Gramer;

namespace Yanki.Scenarios.Tests
{
    /// <summary>
    /// <c>samples/HanGerilim/sablonlar.json</c> değişmezleri (8. hafta PR #13 düzeltme turu, madde 4/6/7). Şablon METİNLERİ Yusuf'un alanıdır; burada yalnızca yapısal değişmezler
    /// (konu eşleşmesi, tick içi tekrar, söylenti dili kalıbı) sınanır. Satırlar <c>tests/golden/han-gerilim-1.json</c> kaydının diyalog açık baştan oynatılmasından gelir.
    /// </summary>
    public class HanGerilimSablonTests
    {
        private static string Kok()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "Yanki.slnx")))
            {
                d = d.Parent;
            }

            return d!.FullName;
        }

        private static SablonYuklemeSonucu Yukle()
        {
            var yol = Path.Combine(Kok(), "samples", "HanGerilim", "sablonlar.json");
            return SablonYukleyici.Yukle(File.ReadAllText(yol), "samples/HanGerilim/sablonlar.json");
        }

        private static SablonDosyasi Sablon()
        {
            var sonuc = Yukle();
            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            return sonuc.Dosya!;
        }

        private static DiyalogMotoru Kos()
        {
            var yol = Path.Combine(Kok(), "tests", "golden", "han-gerilim-1.json");
            var kayit = OturumKaydi.Oku(File.ReadAllText(yol), yol);
            Assert.True(kayit.Basarili, string.Join("\n", kayit.Hatalar));
            var hata = new StringWriter();
            var sim = SenaryoDosyasi.Yukle(Path.Combine(Path.GetDirectoryName(yol)!, kayit.Kayit!.Senaryo), kayit.Kayit.Seed, hata);
            Assert.True(sim != null, hata.ToString());
            var motor = DiyalogMotoru.Baglan(sim!, Sablon());
            OturumOynatici.Oynat(sim!, kayit.Kayit);
            return motor;
        }

        [Fact]
        public void Sablon_dosyasi_hatasiz_ve_uyarisiz_yuklenir()
        {
            var sonuc = Yukle();

            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            Assert.Empty(sonuc.Uyarilar);
        }

        // Madde 4: cevap_anlatim_inanan yalnızca kardes konusunda aday olabilir.
        [Theory]
        [InlineData("borclu(Mehmet,Ayse,500)", false)]
        [InlineData("degil(borclu(Mehmet,Ayse,500))", false)]
        [InlineData("sir_sakliyor(Hatice,Mehmet)", false)]
        [InlineData("kardes(Mehmet,Can)", true)]
        public void Inanan_cevap_yalnizca_kardes_konusunda_aday_olur(string konu, bool uymali)
        {
            var sablon = Sablon().Sablonlar.Single(s => s.Id == "cevap_anlatim_inanan");

            Assert.NotNull(sablon.KonuDeseni);
            Assert.Equal(uymali, sablon.KonuDeseni!.Uyar(Yanki.Core.Mind.Onerme.Ayristir(konu)));
        }

        [Fact]
        public void Inanan_cevap_replayde_yalnizca_kardes_konusunda_secilir_ve_en_az_bir_kez_secilir()
        {
            var motor = Kos();

            var inanan = motor.Satirlar.Where(s => s.SablonId == "cevap_anlatim_inanan").ToList();

            Assert.NotEmpty(inanan);   // değişmez boş geçmesin
            Assert.All(inanan, s => Assert.Contains(", konu kardes(", s.NedenSatirlari[0], StringComparison.Ordinal));
        }

        // Madde 6: aynı NPC aynı tick'te aynı metni söylemez (anlatım cevapları ve genel şablonlar dahil; her satır için).
        [Fact]
        public void Ayni_NPC_ayni_tickte_ayni_metni_soylemez()
        {
            var motor = Kos();

            var tekrarlar = motor.Satirlar
                .GroupBy(s => (s.Tick, s.Konusan, s.Metin))
                .Where(g => g.Count() > 1)
                .Select(g => $"t{g.Key.Tick} {g.Key.Konusan}: \"{g.Key.Metin}\" ×{g.Count()} (şablon: {string.Join(", ", g.Select(x => x.SablonId))})")
                .ToList();

            Assert.True(tekrarlar.Count == 0, string.Join("\n", tekrarlar));
        }

        [Fact]
        public void Anlatim_cevabi_sablonlari_tekrar_bekleme_1_tasir_genel_sablon_tasimaz()
        {
            var cevaplar = Sablon().Sablonlar.Where(s => s.Concept == "anlatim_cevabi").ToList();

            foreach (var s in cevaplar.Where(c => !c.Genel))
            {
                Assert.Equal(TekrarTuru.Bekleme, s.Tekrar.Tur);
                Assert.Equal(1, s.Tekrar.Bekleme);   // tick − son ≥ 1: aynı tick'te ikinci kez seçilemez
            }

            Assert.All(cevaplar.Where(c => c.Genel), s => Assert.Equal(TekrarTuru.Yok, s.Tekrar.Tur));
        }

        // Madde 7: sen varyantları söylenti diliyle yazılır (olumlu ve olumsuz).
        [Theory]
        [InlineData("borclu(Selim,Hatice,200)", "Ayse", "Selim", "senin Hatice'ye 200 lira borçlu olduğun söyleniyor")]
        [InlineData("degil(borclu(Selim,Hatice,200))", "Ayse", "Selim", "senin Hatice'ye 200 lira borçlu olmadığın söyleniyor")]
        [InlineData("kardes(Mehmet,Can)", "Ayse", "Can", "Mehmet ile senin kardeş olduğunuz söyleniyor")]
        [InlineData("kardes(Can,Mehmet)", "Ayse", "Can", "Mehmet ile senin kardeş olduğunuz söyleniyor")]
        [InlineData("degil(kardes(Mehmet,Can))", "Ayse", "Can", "Mehmet ile senin kardeş olmadığınız söyleniyor")]
        [InlineData("degil(kardes(Can,Mehmet))", "Ayse", "Can", "Mehmet ile senin kardeş olmadığınız söyleniyor")]
        [InlineData("sir_sakliyor(Hatice,Mehmet)", "Ayse", "Hatice", "senin Mehmet'ten bir şey sakladığın söyleniyor")]
        [InlineData("degil(sir_sakliyor(Hatice,Mehmet))", "Ayse", "Hatice", "senin Mehmet'ten bir şey saklamadığın söyleniyor")]
        public void Sen_varyanti_soylenti_dilindedir(string konu, string konusan, string dinleyen, string beklenen)
        {
            Assert.Equal(beklenen, OnermeMetni.Uret(konu, Sablon(), konusan, dinleyen));
        }

        // Konuşan = özne ise (ben varyantı) değişmez: NPC kendi hakkında doğrudan konuşur.
        [Fact]
        public void Ben_varyantlari_degismez()
        {
            Assert.Equal("ben Hatice'ye 200 lira borçluyum", OnermeMetni.Uret("borclu(Selim,Hatice,200)", Sablon(), "Selim", "Ayse"));
            Assert.Equal("Mehmet ile ben kardeşiz", OnermeMetni.Uret("kardes(Mehmet,Can)", Sablon(), "Can", "Ayse"));
        }
    }
}
