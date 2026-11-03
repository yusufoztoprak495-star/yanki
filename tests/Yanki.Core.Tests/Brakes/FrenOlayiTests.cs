using System.Linq;
using Xunit;
using Yanki.Core.Events;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;
using Yanki.Core.Tests.Simulation;

namespace Yanki.Core.Tests.Brakes
{
    /// <summary>
    /// 7. hafta (A): <c>fren_uygulandi</c> olayı. Yük: fren, kimlik, npc, hedef; KuralId <c>fren:&lt;tur&gt;</c>; NedenOlaylar engellenen tetikleyici.
    /// Algılanmaz ve kural tetiklemez. Eski senaryolar fren yazmadığı sürece hiçbir fren olayı üretmez.
    /// </summary>
    public class FrenOlayiTests
    {
        private const string HedefineMinnet = @"{ ""kurallar"": [ {
  ""id"": ""hedefe_minnet"", ""ne_zaman"": ""yardim_etti"",
  ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.hedef"", ""modifier"": ""minnet"", ""deger"": 15, ""sure"": 100 } ]
} ] }";

        private static Olay[] Frenler(Simulasyon s) => s.Gunluk.Olaylar.Where(o => o.Tur == "fren_uygulandi").ToArray();

        [Fact]
        public void Tanimsiz_rol_kurali_o_olay_icin_atlar_ve_algilayan_basina_fren_olayi_yazar()
        {
            var s = SimFikstur.Yap(HedefineMinnet);
            s.Submit(new PlayerAction("yardim_etti"));   // hedefsiz: olay.hedef tanımsız
            s.Tick(1);

            var fren = Frenler(s);
            Assert.Equal(new[] { "Ali", "Veli" }, fren.Select(o => o.Deger("npc")).ToArray());
            Assert.All(fren, o =>
            {
                Assert.Equal("tanimsiz_rol", o.Deger("fren"));
                Assert.Equal("hedefe_minnet", o.Deger("kimlik"));
                Assert.Equal(string.Empty, o.Deger("hedef"));
                Assert.Equal("fren:tanimsiz_rol", o.KuralId);
                Assert.Equal(1, o.Tick);
            });
            var tetik = s.Gunluk.Olaylar.Single(o => o.Tur == "yardim_etti");
            Assert.All(fren, o => Assert.Equal(new[] { tetik.Id }, o.NedenOlaylar.ToArray()));
            Assert.DoesNotContain(s.Gunluk.Olaylar, o => o.Tur == "modifier_eklendi");
        }

        [Fact]
        public void Kisi_olmayan_hedef_atlanan_etkiyi_mekan_hedefli_olayda_iz_birakir()
        {
            var s = SimFikstur.Yap(HedefineMinnet);
            s.Submit(new PlayerAction("yardim_etti", "pazar"));   // hedef bir mekan
            s.Tick(1);

            var fren = Frenler(s);
            Assert.Equal(new[] { "Ali", "Veli" }, fren.Select(o => o.Deger("npc")).ToArray());
            Assert.All(fren, o =>
            {
                Assert.Equal("kisi_olmayan_hedef", o.Deger("fren"));
                Assert.Equal("hedefe_minnet", o.Deger("kimlik"));
                Assert.Equal("pazar", o.Deger("hedef"));
                Assert.Equal("fren:kisi_olmayan_hedef", o.KuralId);
            });
            Assert.DoesNotContain(s.Gunluk.Olaylar, o => o.Tur == "modifier_eklendi");
        }

        [Fact]
        public void Fren_olayi_algilanmaz_ve_kural_tetiklemez_sonraki_tickte_de_iz_birakmaz()
        {
            var s = SimFikstur.Yap(HedefineMinnet);
            s.Submit(new PlayerAction("yardim_etti", "pazar"));
            s.Tick(3);

            var kimlikler = Frenler(s).Select(o => o.Id).ToArray();
            Assert.NotEmpty(kimlikler);
            Assert.DoesNotContain(s.Gunluk.Olaylar, o => o.NedenOlaylar.Any(n => kimlikler.Contains(n)));   // hiçbir olay frenden doğmaz
            Assert.Equal(kimlikler.Length, Frenler(s).Length);                                                  // yineleme yok
        }

        [Fact]
        public void Why_fren_olayindan_engellenen_tetikleyiciye_iner()
        {
            var s = SimFikstur.Yap(HedefineMinnet);
            s.Submit(new PlayerAction("yardim_etti", "pazar"));
            s.Tick(1);

            var fren = Frenler(s).First();
            var zincir = s.Neden(fren.Id);
            Assert.Contains(zincir, o => o.Tur == "yardim_etti" && o.KuralId == "player");
        }

        [Fact]
        public void Frenler_sorgusu_tur_ve_npce_gore_suzer()
        {
            var s = SimFikstur.Yap(HedefineMinnet);
            s.Submit(new PlayerAction("yardim_etti"));
            s.Submit(new PlayerAction("yardim_etti", "pazar"));
            s.Tick(1);

            Assert.Equal(4, s.Frenler().Count);
            Assert.Equal(2, s.Frenler("tanimsiz_rol").Count);
            Assert.Equal(2, s.Frenler(npc: "Ali").Count);
            Assert.Single(s.Frenler("kisi_olmayan_hedef", "Veli"));
            Assert.Empty(s.Frenler("cooldown"));
        }

        [Fact]
        public void Ne_zaman_fren_uygulandi_dogrulayicida_reddedilir()
        {
            var json = @"{ ""kurallar"": [ { ""id"": ""x"", ""ne_zaman"": ""fren_uygulandi"", ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""oyuncu"", ""modifier"": ""m"", ""deger"": 1, ""sure"": 5 } ] } ] }";
            var sonuc = KuralYukleyici.Yukle(json, "kurallar.json", SimFikstur.Senaryo().OlayTurleri);

            Assert.False(sonuc.Basarili);
            Assert.Contains(sonuc.Hatalar, h => h.Mesaj.Contains("fren_uygulandi") && h.Mesaj.Contains("ne_zaman olamaz"));
        }

        [Fact]
        public void Fren_uygulandi_ayrilmis_olay_turudur_senaryo_ve_eylem_tanimlayamaz()
        {
            var json = SimFikstur.SenaryoJson.Replace("\"olay_turleri\": [\"yardim_etti\"", "\"olay_turleri\": [\"fren_uygulandi\", \"yardim_etti\"");
            var sonuc = SenaryoYukleyici.Yukle(json, "senaryo.json");

            Assert.False(sonuc.Basarili);
            Assert.Contains(sonuc.Hatalar, h => h.Mesaj.Contains("fren_uygulandi"));
            Assert.True(Yanki.Core.Decision.YerlesikTurler.AyrilmisMi("fren_uygulandi"));
        }

        [Fact]
        public void Fren_yazmayan_senaryoda_fren_olayi_yoktur()
        {
            var s = SimFikstur.Yap();
            s.Submit(new PlayerAction("yardim_etti", "Ali"));
            s.Tick(3);

            Assert.Empty(s.Frenler());
        }
    }
}
