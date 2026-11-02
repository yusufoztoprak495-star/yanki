using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Decision;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;

namespace Yanki.Core.Tests.Simulation
{
    /// <summary>Senaryonun <c>eylem_dosyalari</c> alanı ve <see cref="Simulasyon.Olustur"/>'un eylem denetimleri (ADR-011).</summary>
    public class EylemKurulumuTests
    {
        private static string Degistir(string eski, string yeni) => SimFikstur.SenaryoJson.Replace(eski, yeni);

        private static SenaryoYuklemeSonucu Yukle(string json) => SenaryoYukleyici.Yukle(json, "senaryo.json");

        private const string EylemJson = @"{ ""eylemler"": [ {
  ""id"": ""selam_ver"", ""olay_turu"": ""selam_verdi"", ""hedef"": ""yok"", ""taban"": 1,
  ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.5 } ]
} ] }";

        private static IReadOnlyList<EylemTanimi> Eylemler(string json = EylemJson)
        {
            var sonuc = EylemYukleyici.Yukle(json, "eylemler.json");
            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            return sonuc.Eylemler;
        }

        // ---- senaryo alanı ----

        [Fact]
        public void Eylem_dosyalari_okunur()
        {
            var sonuc = Yukle(Degistir("\"kural_dosyalari\": [\"kurallar.json\"]", "\"kural_dosyalari\": [\"kurallar.json\"], \"eylem_dosyalari\": [\"eylemler.json\", \"git.json\"]"));

            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            Assert.Equal(new[] { "eylemler.json", "git.json" }, sonuc.Senaryo!.EylemDosyalari.ToArray());
        }

        [Fact]
        public void Eylem_dosyalari_yoksa_liste_bostur() =>
            Assert.Empty(Yukle(SimFikstur.SenaryoJson).Senaryo!.EylemDosyalari);

        [Theory]
        [InlineData("[5]")]
        [InlineData("[\"\"]")]
        [InlineData("\"eylemler.json\"")]
        public void Gecersiz_eylem_dosyasi_girdisi_reddedilir(string deger)
        {
            var sonuc = Yukle(Degistir("\"kural_dosyalari\": [\"kurallar.json\"]", "\"kural_dosyalari\": [\"kurallar.json\"], \"eylem_dosyalari\": " + deger));

            Assert.False(sonuc.Basarili);
            Assert.Contains(sonuc.Hatalar, h => h.ToString().Contains("Eylem dosyası") || h.ToString().Contains("eylem_dosyalari"));
        }

        [Theory]
        [InlineData("ayrildi")]
        [InlineData("geldi")]
        [InlineData("karar_verildi")]
        [InlineData("hareket")]
        public void Yerlesik_turler_senaryoda_yeniden_tanimlanamaz(string tur)
        {
            var sonuc = Yukle(Degistir("[\"yardim_etti\", \"tehdit_etti\"]", "[\"yardim_etti\", \"" + tur + "\"]"));

            Assert.False(sonuc.Basarili);
            Assert.Contains(sonuc.Hatalar, h => h.ToString().Contains("çekirdek") && h.ToString().Contains(tur));
        }

        // ---- Simulasyon.Olustur ----

        [Fact]
        public void Olustur_eylemleri_kimlige_gore_sirali_sunar()
        {
            var sim = Simulasyon.Olustur(SimFikstur.Senaryo(), SimFikstur.Kurallar(SimFikstur.YardimKurali), 1, eylemler: Eylemler());

            Assert.Equal("selam_ver", Assert.Single(sim.Eylemler).Id);
        }

        [Fact]
        public void Eylem_verilmezse_katalog_bostur() =>
            Assert.Empty(SimFikstur.Yap().Eylemler);

        [Fact]
        public void Kural_npc_eyleminin_olay_turunu_ne_zaman_olarak_kullanabilir()
        {
            var kural = KuralYukleyici.Yukle(
                "{ \"kurallar\": [ { \"id\": \"selam_kurali\", \"ne_zaman\": \"selam_verdi\", \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.4 } ] } ] }",
                "kurallar.json", new[] { "selam_verdi" });
            Assert.True(kural.Basarili);

            var sim = Simulasyon.Olustur(SimFikstur.Senaryo(), kural.Kurallar, 1, eylemler: Eylemler());

            Assert.Single(sim.Kurallar);
        }

        [Fact]
        public void Kural_bilinmeyen_olay_turunu_hala_reddeder_eylem_olmadan()
        {
            var kural = KuralYukleyici.Yukle(
                "{ \"kurallar\": [ { \"id\": \"selam_kurali\", \"ne_zaman\": \"selam_verdi\", \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.4 } ] } ] }",
                "kurallar.json", new[] { "selam_verdi" });

            var ex = Assert.Throws<ArgumentException>(() => Simulasyon.Olustur(SimFikstur.Senaryo(), kural.Kurallar, 1));

            Assert.Contains("selam_verdi", ex.Message);
        }

        [Fact]
        public void Eylem_etki_hedefi_senaryoda_yoksa_reddedilir()
        {
            var json = EylemJson.Replace("{ \"tur\": \"hafiza\", \"onem\": 0.5 }",
                "{ \"tur\": \"eksen\", \"hedef\": \"Hayalet\", \"eksen\": \"guven\", \"delta\": 1 }");

            var ex = Assert.Throws<ArgumentException>(() => Simulasyon.Olustur(SimFikstur.Senaryo(), SimFikstur.Kurallar(SimFikstur.YardimKurali), 1, eylemler: Eylemler(json)));

            Assert.Contains("selam_ver", ex.Message);
            Assert.Contains("Hayalet", ex.Message);
        }

        [Fact]
        public void Yinelenen_eylem_kimligi_olustururken_reddedilir()
        {
            var tekrar = new List<EylemTanimi>(Eylemler());
            tekrar.AddRange(Eylemler());

            Assert.Throws<ArgumentException>(() => Simulasyon.Olustur(SimFikstur.Senaryo(), SimFikstur.Kurallar(SimFikstur.YardimKurali), 1, eylemler: tekrar));
        }
    }
}
