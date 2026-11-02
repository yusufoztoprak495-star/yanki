using System.Linq;
using Xunit;
using Yanki.Core.Simulation;

namespace Yanki.Core.Tests.Simulation
{
    public class SenaryoYukleyiciTests
    {
        private static SenaryoYuklemeSonucu Yukle(string json) => SenaryoYukleyici.Yukle(json, "senaryo.json");

        private static void HataVar(string json, string parca)
        {
            var sonuc = Yukle(json);
            Assert.False(sonuc.Basarili);
            Assert.Contains(sonuc.Hatalar, h => h.ToString().Contains(parca));
        }

        // Verilen parçayı değiştirerek geçerli senaryodan hatalı bir sürüm üretir.
        private static string Degistir(string eski, string yeni)
        {
            Assert.Contains(eski, SimFikstur.SenaryoJson);
            return SimFikstur.SenaryoJson.Replace(eski, yeni);
        }

        [Fact]
        public void Gecerli_senaryo_okunur()
        {
            var sonuc = Yukle(SimFikstur.SenaryoJson);

            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            var s = sonuc.Senaryo!;
            Assert.Equal("Deneme", s.Ad);
            Assert.Equal(new[] { "han", "pazar" }, s.Mekanlar.ToArray());
            Assert.Equal("oyuncu", s.OyuncuId);
            Assert.Equal("han", s.OyuncuKonumu);
            Assert.Equal(new[] { "Ali", "Can", "Veli" }, s.Npcler.Select(n => n.Id).ToArray());
            Assert.Equal("pazar", s.Npcler.Single(n => n.Id == "Can").Konum);
            Assert.Equal(0.7m, s.Npcler.Single(n => n.Id == "Ali").Kisilik.Deger("durustluk"));
            Assert.Equal(new[] { "tehdit_etti", "yardim_etti" }, s.OlayTurleri.ToArray());
            Assert.Equal(new[] { "kurallar.json" }, s.KuralDosyalari.ToArray());
            Assert.Single(s.Baglantilar);
        }

        [Fact]
        public void Bozuk_json_ve_kok_hatalari()
        {
            HataVar("{", "JSON");
            HataVar("[]", "nesne");
        }

        [Fact]
        public void Zorunlu_alanlar_eksikse_reddeder()
        {
            HataVar(Degistir("\"oyuncu\": { \"id\": \"oyuncu\", \"konum\": \"han\" },", string.Empty), "oyuncu");
            HataVar(Degistir("\"mekanlar\": [\"han\", \"pazar\"],", string.Empty), "mekanlar");
            HataVar(Degistir("\"olay_turleri\": [\"yardim_etti\", \"tehdit_etti\"],", string.Empty), "olay_turleri");
        }

        [Fact]
        public void Bilinmeyen_mekan_kimlik_cakismasi_ve_gecersiz_degerler_reddedilir()
        {
            HataVar(Degistir("\"id\": \"Can\", \"konum\": \"pazar\"", "\"id\": \"Can\", \"konum\": \"yok\""), "yok");
            HataVar(Degistir("\"id\": \"Can\"", "\"id\": \"Ali\""), "Ali");
            HataVar(Degistir("[\"han\", \"pazar\"]", "[\"han\", \"han\"]"), "han");
            HataVar(Degistir("\"id\": \"Can\"", "\"id\": \"Türkçe\""), "Türkçe");
            HataVar(Degistir("\"durustluk\": 0.7", "\"durustluk\": 1.5"), "durustluk");
            HataVar(Degistir("\"durustluk\": 0.7", "\"durustlugu\": 0.7"), "durustlugu");
            HataVar(Degistir("[\"han\", \"pazar\"]]", "[\"han\", \"yok\"]]"), "yok");
            HataVar(Degistir("\"surum\": 1,", "\"surum\": 2,"), "surum");
        }

        [Fact]
        public void Olay_turu_yerlesik_bir_turle_cakisamaz()
        {
            HataVar(Degistir("\"yardim_etti\", \"tehdit_etti\"", "\"hafiza_eklendi\""), "hafiza_eklendi");
        }

        [Fact]
        public void Gecersiz_baslangic_gercegi_reddedilir()
        {
            HataVar(Degistir("\"gercekler\": []", "\"gercekler\": [\"bozuk(\"]"), "bozuk(");
        }

        [Fact]
        public void Hata_dosya_adini_ve_satiri_icerir()
        {
            var sonuc = Yukle(Degistir("\"id\": \"Can\", \"konum\": \"pazar\"", "\"id\": \"Can\", \"konum\": \"yok\""));
            var hata = sonuc.Hatalar.First();
            Assert.Equal("senaryo.json", hata.Kaynak);
            Assert.True(hata.Satir > 0);
        }
    }
}
