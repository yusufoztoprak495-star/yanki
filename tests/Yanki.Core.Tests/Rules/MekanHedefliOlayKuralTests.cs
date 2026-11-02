using System.Linq;
using Xunit;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;
using Yanki.Core.Tests.Simulation;

namespace Yanki.Core.Tests.Rules
{
    /// <summary>
    /// <c>ayrildi</c> ve <c>geldi</c> yerleşik olay türleridir; <c>Hedef</c> alanı karşı MEKANDIR (<c>ayrildi(Can,pazar)</c>).
    /// Kural bir mekana modifier, eksen ya da duygu yazamaz; doğrulayıcı bunu bildiği yerde reddeder, bilemediği yerde
    /// (ör. hedefi mekan olan bir oyuncu eylemi) çalışma anında durum bozulmaz.
    /// </summary>
    public class MekanHedefliOlayKuralTests
    {
        private static KuralYuklemeSonucu Yukle(string neZaman, string etki) =>
            KuralYukleyici.Yukle(
                "{ \"kurallar\": [ { \"id\": \"k1\", \"ne_zaman\": \"" + neZaman + "\", \"etkiler\": [ " + etki + " ] } ] }", "k.json", new[] { "yardim_etti" });

        private const string HedefeModifier = "{ \"tur\": \"modifier\", \"hedef\": \"olay.hedef\", \"modifier\": \"x\", \"deger\": 1, \"sure\": 5 }";
        private const string HedefeEksen = "{ \"tur\": \"eksen\", \"hedef\": \"olay.hedef\", \"eksen\": \"guven\", \"delta\": 1 }";
        private const string HedefeDuygu = "{ \"tur\": \"duygu\", \"duygu\": \"korku\", \"siddet\": 0.3, \"hedef\": \"olay.hedef\" }";

        [Fact]
        public void Ayrildi_ve_geldi_yerlesik_olay_turleridir_karar_verildi_degildir()
        {
            Assert.Contains("ayrildi", KuralYukleyici.YerlesikOlayTurleri);
            Assert.Contains("geldi", KuralYukleyici.YerlesikOlayTurleri);
            Assert.DoesNotContain("karar_verildi", KuralYukleyici.YerlesikOlayTurleri);
        }

        [Theory]
        [InlineData("ayrildi")]
        [InlineData("geldi")]
        public void Hareket_olaylari_ne_zaman_olabilir(string tur)
        {
            var sonuc = Yukle(tur, "{ \"tur\": \"hafiza\", \"onem\": 0.4 }");

            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
        }

        [Theory]
        [InlineData("ayrildi", HedefeModifier)]
        [InlineData("geldi", HedefeModifier)]
        [InlineData("ayrildi", HedefeEksen)]
        [InlineData("geldi", HedefeEksen)]
        [InlineData("ayrildi", HedefeDuygu)]
        [InlineData("geldi", HedefeDuygu)]
        public void Hareket_olayinda_olay_hedefe_kisi_etkisi_yazmak_reddedilir(string tur, string etki)
        {
            var sonuc = Yukle(tur, etki);

            Assert.False(sonuc.Basarili);
            Assert.Contains(sonuc.Hatalar, h => h.ToString().Contains("olay.hedef") && h.ToString().Contains("mekan"));
        }

        [Fact]
        public void Hareket_olayinda_olay_failina_etki_yazilabilir() =>
            Assert.True(Yukle("geldi", HedefeModifier.Replace("olay.hedef", "olay.fail")).Basarili);

        [Fact]
        public void Hareket_olayinda_olay_hedefi_gercek_sablonunda_kullanilabilir() =>
            Assert.True(Yukle("geldi", "{ \"tur\": \"gercek\", \"islem\": \"ekle\", \"gercek\": \"geldi_buraya({olay.fail}, {olay.hedef})\" }").Basarili);

        [Fact]
        public void Baska_olaylarda_olay_hedefe_etki_serbest_kalir() =>
            Assert.True(Yukle("yardim_etti", HedefeModifier).Basarili);

        [Fact]
        public void Karar_verildi_ne_zaman_olamaz_ve_neden_acik_yazilir()
        {
            var sonuc = Yukle("karar_verildi", "{ \"tur\": \"hafiza\", \"onem\": 0.4 }");

            Assert.False(sonuc.Basarili);
            var hata = Assert.Single(sonuc.Hatalar, h => h.ToString().Contains("karar_verildi")).ToString();
            Assert.Contains("kural tetiklemez", hata);
        }

        // ---- çalışma anı: hedefi mekan olan olayda kişi etkisi durumu bozmaz ----

        [Fact]
        public void Hedefi_mekan_olan_oyuncu_eyleminde_olay_hedefe_modifier_durumu_bozmaz()
        {
            // Statik olarak bilinemez: yardim_etti oyuncu eylemidir ve hedefi bir mekan olabilir.
            var kural = KuralYukleyici.Yukle(
                "{ \"kurallar\": [ { \"id\": \"mekana_yaz\", \"ne_zaman\": \"yardim_etti\", \"etkiler\": [ " + HedefeModifier + ", " + HedefeEksen + ", " + HedefeDuygu + " ] } ] }",
                "k.json", new[] { "yardim_etti" });
            Assert.True(kural.Basarili, string.Join("\n", kural.Hatalar));

            var bos = KuralYukleyici.Yukle("{ \"kurallar\": [] }", "k.json", new[] { "yardim_etti" });
            var senaryo = SimFikstur.Senaryo();
            var etkili = Simulasyon.Olustur(senaryo, kural.Kurallar, 42);
            var etkisiz = Simulasyon.Olustur(senaryo, bos.Kurallar, 42);

            foreach (var sim in new[] { etkili, etkisiz })
            {
                sim.Submit(new PlayerAction("yardim_etti", "pazar"));
                sim.Tick(2);
            }

            Assert.Equal(etkisiz.DurumHash(), etkili.DurumHash());
            Assert.DoesNotContain(etkili.Gunluk.Olaylar, o => o.KuralId == "mekana_yaz");
        }

        [Fact]
        public void Hedefi_kisi_olan_ayni_kural_normal_calisir()
        {
            var kural = KuralYukleyici.Yukle(
                "{ \"kurallar\": [ { \"id\": \"kisiye_yaz\", \"ne_zaman\": \"yardim_etti\", \"etkiler\": [ " + HedefeModifier + " ] } ] }",
                "k.json", new[] { "yardim_etti" });
            var sim = Simulasyon.Olustur(SimFikstur.Senaryo(), kural.Kurallar, 42);

            sim.Submit(new PlayerAction("yardim_etti", "Veli"));
            sim.Tick(1);

            Assert.Contains(sim.Gunluk.Olaylar, o => o.KuralId == "kisiye_yaz" && o.Tur == "modifier_eklendi" && o.Deger("hedef") == "Veli");
        }
    }
}
