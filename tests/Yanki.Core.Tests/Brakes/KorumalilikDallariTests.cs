using System;
using System.Collections.Generic;
using Xunit;
using Yanki.Core.Brakes;
using Yanki.Core.Json;

namespace Yanki.Core.Tests.Brakes
{
    /// <summary>
    /// 8. hafta: <c>Korumalilik.cs</c> kapsanmayan satırları (kapsam raporu: 35, 74, 92, 94, 96, 126–127, 135–136, 168–169). Ulaşılabilir dallar için davranış testi;
    /// <c>Rol.Oyuncu</c> dalı yükleyiciden ulaşılamaz (hiçbir bağlam <c>oyuncu</c> rolüne izin vermez) ama genel kurucuyla elle kurulan nesnede boş kimlik dönüp
    /// korumayı SESSİZCE kapatıyordu: artık açık hata fırlatır.
    /// </summary>
    public class KorumalilikDallariTests
    {
        private static (IReadOnlyList<IhlalBayragi> Sonuc, List<string> Hatalar) Oku(string json, params string[] roller)
        {
            var hatalar = new List<string>();
            var sonuc = IhlalBayragi.OkuDizi(JsonAyristirici.Ayristir(json), "ihlal_bayraklari", roller, (satir, mesaj) => hatalar.Add(satir + ": " + mesaj));
            return (sonuc, hatalar);
        }

        [Theory]
        [InlineData("ben", "N", "Ben")]
        [InlineData("eylem.hedef", "H", "EylemHedef")]
        [InlineData("olay.fail", "F", "OlayFail")]
        [InlineData("olay.hedef", "T", "OlayHedef")]
        [InlineData("Hancı", "Hancı", "sabit kimlik")]
        public void Coz_rolu_baglamdan_sabit_kimligi_oldugu_gibi_cozer(string varlik, string beklenen, string aciklama)
        {
            Assert.False(string.IsNullOrEmpty(aciklama));
            Assert.Equal(beklenen, new IhlalBayragi(KorumaBayraklari.Oldurulemez, varlik).Coz("N", "H", "F", "T"));
        }

        [Theory]
        [InlineData("oyuncu")]
        [InlineData("konusan")]
        [InlineData("dinleyen")]
        public void Coz_bu_baglamda_desteklenmeyen_rolde_sessizce_bos_donmez_acik_hata_firlatir(string rol)
        {
            var bayrak = new IhlalBayragi(KorumaBayraklari.Oldurulemez, rol);

            var hata = Assert.Throws<InvalidOperationException>(() => bayrak.Coz("N", "H", "F", "T"));
            Assert.Contains(rol, hata.Message);
        }

        [Fact]
        public void Yok_hicbir_varligi_korumaz()
        {
            Assert.True(Korumalilik.Yok.Bos);
            Assert.False(Korumalilik.Yok.Korunuyor("Ali", KorumaBayraklari.Oldurulemez));
        }

        [Fact]
        public void Okunan_bayrak_kaynak_satirini_tasir()
        {
            var (sonuc, hatalar) = Oku("{ \"ihlal_bayraklari\": [\n  { \"bayrak\": \"oldurulemez\", \"varlik\": \"eylem.hedef\" },\n  { \"bayrak\": \"calinamaz\", \"varlik\": \"kese\" }\n] }", "ben", "eylem.hedef");

            Assert.Empty(hatalar);
            Assert.Equal(new[] { 2, 3 }, new[] { sonuc[0].Satir, sonuc[1].Satir });
        }

        [Fact]
        public void Eleman_nesne_degilse_hata_verir()
        {
            var (sonuc, hatalar) = Oku("{ \"ihlal_bayraklari\": [ 5 ] }", "ben");

            Assert.Empty(sonuc);
            Assert.Contains(hatalar, h => h.Contains("nesnesi bekleniyor", StringComparison.Ordinal));
        }

        [Fact]
        public void Bilinmeyen_alan_hata_verir_ve_eleman_alinmaz()
        {
            var (sonuc, hatalar) = Oku("{ \"ihlal_bayraklari\": [ { \"bayrak\": \"oldurulemez\", \"varlik\": \"ben\", \"x\": 1 } ] }", "ben");

            Assert.Empty(sonuc);
            Assert.Contains(hatalar, h => h.Contains("bilinmeyen alan \"x\"", StringComparison.Ordinal));
        }

        [Fact]
        public void Gecersiz_sabit_kimlik_hata_verir()
        {
            var (sonuc, hatalar) = Oku("{ \"ihlal_bayraklari\": [ { \"bayrak\": \"oldurulemez\", \"varlik\": \"a b\" } ] }", "ben");

            Assert.Empty(sonuc);
            Assert.Contains(hatalar, h => h.Contains("geçersiz kimlik", StringComparison.Ordinal));
        }
    }
}
