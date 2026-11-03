using System.Linq;
using Xunit;
using Yanki.Core.Brakes;
using Yanki.Core.Decision;
using Yanki.Core.Simulation;
using Yanki.Core.Tests.Decision;
using Yanki.Core.Tests.Simulation;

namespace Yanki.Core.Tests.Brakes
{
    /// <summary>7. hafta (B): senaryo <c>frenler</c> nesnesi ile eylem <c>cooldown</c>/<c>cooldown_kapsam</c>/<c>en_fazla</c> şeması ve doğrulayıcı.</summary>
    public class FrenAlanlariYukleyiciTests
    {
        private static SenaryoYuklemeSonucu Senaryo(string frenler) =>
            SenaryoYukleyici.Yukle(SimFikstur.SenaryoJson.Replace("\"gercekler\": [],", "\"gercekler\": [], \"frenler\": " + frenler + ","), "s.json");

        [Fact]
        public void Senaryo_frenler_M_degerini_okur_yazilmadiysa_sinirsizdir()
        {
            Assert.Equal(5, Senaryo("{ \"tick_basina_en_fazla_sonuc\": 5 }").Senaryo!.Frenler.TickBasinaEnFazlaSonuc);
            Assert.Null(SimFikstur.Senaryo().Frenler.TickBasinaEnFazlaSonuc);
        }

        [Theory]
        [InlineData("{ \"tick_basina_en_fazla_sonuc\": 0 }")]
        [InlineData("{ \"tick_basina_en_fazla_sonuc\": 2.5 }")]
        [InlineData("{ \"tick_basina_en_fazla_sonuc\": \"on\" }")]
        [InlineData("{ \"bilinmeyen\": 1 }")]
        [InlineData("[1]")]
        public void Senaryo_frenler_gecersizse_reddedilir(string frenler)
        {
            var sonuc = Senaryo(frenler);
            Assert.False(sonuc.Basarili);
            Assert.Contains(sonuc.Hatalar, h => h.Mesaj.Contains("frenler") || h.Mesaj.Contains("tick_basina_en_fazla_sonuc"));
        }

        [Fact]
        public void Eylem_fren_alanlari_okunur_varsayilan_kapsam_hedeftir()
        {
            var e = KararFikstur.Yukle(KararFikstur.Eylem("selam", "yok", "\"cooldown\": 5, \"en_fazla\": 3")).Single();
            Assert.Equal(5, e.TetikSiniri!.Cooldown);
            Assert.Equal(3, e.TetikSiniri.EnFazla);
            Assert.Equal(SinirKapsami.Hedef, e.TetikSiniri.Kapsam);
            Assert.Null(KararFikstur.Yukle(KararFikstur.Eylem("selam", "yok")).Single().TetikSiniri);
        }

        [Theory]
        [InlineData("\"cooldown_kapsam\": \"kural\", \"cooldown\": 5")]
        [InlineData("\"tick_basina_en_fazla\": 2")]
        [InlineData("\"cooldown\": 0")]
        [InlineData("\"en_fazla\": 1.5")]
        [InlineData("\"cooldown_kapsam\": \"npc\"")]
        public void Eylem_gecersiz_fren_alanlari_reddedilir(string ek)
        {
            var sonuc = EylemYukleyici.Yukle(KararFikstur.Dosya(KararFikstur.Eylem("selam", "yok", ek)), "e.json");
            Assert.False(sonuc.Basarili);
        }

        [Fact]
        public void Kural_varsayilan_kapsami_npc_eylemin_hedeftir()
        {
            var k = Yanki.Core.Rules.KuralYukleyici.Yukle(
                @"{ ""kurallar"": [ { ""id"": ""r"", ""ne_zaman"": ""yardim_etti"", ""cooldown"": 3, ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.5 } ] } ] }",
                "k.json", SimFikstur.Senaryo().OlayTurleri).Kurallar.Single();
            Assert.Equal(SinirKapsami.Npc, k.TetikSiniri!.Kapsam);
        }
    }
}
