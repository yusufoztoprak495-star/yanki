using System.Linq;
using Xunit;
using Yanki.Core.Decision;
using Yanki.Core.Simulation;

namespace Yanki.Core.Tests.Decision
{
    /// <summary>Aday üretimi (ADR-011): aday = (eylem, hedef); ön koşullar hepsi tutmalı; sıra eylem kimliği sonra hedef, ordinal.</summary>
    public class AdayUreticiTests
    {
        private static string[] Anahtarlar(AdayUretimi u) => u.Adaylar.Select(a => a.Anahtar).ToArray();

        private static AdayUretimi Uret(string npc, params string[] eylemler)
        {
            var yuklenen = KararFikstur.Yukle(eylemler);
            var sim = KararFikstur.Sim(yuklenen);
            return AdayUretici.Uret(KararFikstur.Baglam(sim, npc), sim.Eylemler);
        }

        [Fact]
        public void Hedefsiz_eylem_tek_aday_uretir()
        {
            var u = Uret("Ali", KararFikstur.Eylem("selam"));

            var aday = Assert.Single(u.Adaylar);
            Assert.Equal("selam", aday.Eylem.Id);
            Assert.Equal(string.Empty, aday.Hedef);
            Assert.Empty(u.Elenenler);
        }

        [Fact]
        public void Npc_hedefli_eylem_ayni_mekandaki_baska_npcler_icin_aday_uretir()
        {
            // Ali ve Veli han'da, Can pazar'da: Ali için yalnızca Veli.
            Assert.Equal(new[] { "yardim|Veli" }, Anahtarlar(Uret("Ali", KararFikstur.Eylem("yardim", "npc"))));
            Assert.Equal(new[] { "yardim|Ali" }, Anahtarlar(Uret("Veli", KararFikstur.Eylem("yardim", "npc"))));
        }

        [Fact]
        public void Npc_hedefli_eylem_kendini_ve_oyuncuyu_hedef_almaz()
        {
            var u = Uret("Ali", KararFikstur.Eylem("yardim", "npc"));

            Assert.DoesNotContain(u.Adaylar, a => a.Hedef == "Ali" || a.Hedef == "oyuncu");
        }

        [Fact]
        public void Uygun_hedef_yoksa_aday_yoktur_ve_neden_elenenlerde_gorunur()
        {
            // Can pazar'da tek başına.
            var u = Uret("Can", KararFikstur.Eylem("yardim", "npc"));

            Assert.Empty(u.Adaylar);
            var elenen = Assert.Single(u.Elenenler);
            Assert.Equal("yardim", elenen.Eylem.Id);
            Assert.Equal(string.Empty, elenen.Hedef);
            Assert.Contains("uygun hedef yok", elenen.Neden);
        }

        [Fact]
        public void Oyuncu_hedefli_eylem_yalnizca_ayni_mekandaysa_aday_uretir()
        {
            Assert.Equal(new[] { "sor|oyuncu" }, Anahtarlar(Uret("Ali", KararFikstur.Eylem("sor", "oyuncu"))));
            Assert.Empty(Uret("Can", KararFikstur.Eylem("sor", "oyuncu")).Adaylar);
        }

        [Fact]
        public void Komsu_mekan_eylemi_bagli_komsular_icin_aday_uretir()
        {
            Assert.Equal(new[] { "git|pazar" }, Anahtarlar(Uret("Ali", KararFikstur.Eylem("git", "komsu_mekan"))));
            Assert.Equal(new[] { "git|han" }, Anahtarlar(Uret("Can", KararFikstur.Eylem("git", "komsu_mekan"))));
        }

        [Fact]
        public void Komsu_mekanlar_ordinal_sirada_uretilir()
        {
            var json = @"{ ""surum"": 1, ""mekanlar"": [""a"", ""c"", ""b""], ""baglantilar"": [[""a"", ""c""], [""a"", ""b""]],
  ""oyuncu"": { ""id"": ""oyuncu"", ""konum"": ""a"" },
  ""npcler"": [ { ""id"": ""Ali"", ""konum"": ""a"", ""kisilik"": { ""acgozluluk"": 0.5, ""durustluk"": 0.5, ""cesaret"": 0.5, ""sosyallik"": 0.5, ""kin_tutma"": 0.5, ""merhamet"": 0.5 } } ],
  ""olay_turleri"": [""x_oldu""] }";
            var senaryo = SenaryoYukleyici.Yukle(json, "s.json").Senaryo!;
            var sim = KararFikstur.Sim(senaryo, KararFikstur.Yukle(KararFikstur.Eylem("git", "komsu_mekan")));

            var u = AdayUretici.Uret(KararFikstur.Baglam(sim, "Ali"), sim.Eylemler);

            Assert.Equal(new[] { "git|b", "git|c" }, Anahtarlar(u));
        }

        [Fact]
        public void Adaylar_eylem_kimligi_sonra_hedef_ordinal_siralidir()
        {
            var u = Uret(
                "Ali",
                KararFikstur.Eylem("z_eylem"),
                KararFikstur.Eylem("a_eylem", "npc"),
                KararFikstur.Eylem("m_eylem", "komsu_mekan"),
                KararFikstur.Eylem("B_eylem", "oyuncu"));

            Assert.Equal(new[] { "B_eylem|oyuncu", "a_eylem|Veli", "m_eylem|pazar", "z_eylem|" }, Anahtarlar(u));
        }

        [Fact]
        public void On_kosul_tutmayan_aday_elenir_ve_neden_kriteri_gosterir()
        {
            // durustluk: Ali 0.7, Veli 0.5.
            var eylem = KararFikstur.Eylem("itiraf", "yok", "\"on_kosullar\": [ { \"alan\": \"kisilik(durustluk)\", \"op\": \">=\", \"deger\": 0.6 } ]");

            Assert.Single(Uret("Ali", eylem).Adaylar);
            var veli = Uret("Veli", eylem);
            Assert.Empty(veli.Adaylar);
            var elenen = Assert.Single(veli.Elenenler);
            Assert.Contains("on_kosul #1 tutmadi", elenen.Neden);
            Assert.Contains("kisilik(durustluk) >= 0.6", elenen.Neden);
        }

        [Fact]
        public void On_kosullarin_hepsi_tutmali_ilk_tutmayan_raporlanir()
        {
            var eylem = KararFikstur.Eylem("itiraf", "yok",
                "\"on_kosullar\": [ { \"alan\": \"kisilik(durustluk)\", \"op\": \">=\", \"deger\": 0.4 }, { \"alan\": \"kisilik(cesaret)\", \"op\": \">\", \"deger\": 0.9 } ]");

            var u = Uret("Ali", eylem);

            Assert.Empty(u.Adaylar);
            Assert.Contains("on_kosul #2 tutmadi", Assert.Single(u.Elenenler).Neden);
        }

        [Fact]
        public void On_kosul_eylem_hedefine_gore_aday_basina_degerlendirilir()
        {
            // Ali için iki NPC adayı gerekir: aynı mekanda olanlar. Hedefin kişiliği koşulda kullanılamaz (zihin ben'in);
            // bu yüzden ayni_mekanda(eylem.hedef) hep doğrudur ve her hedef aday kalır.
            var eylem = KararFikstur.Eylem("yardim", "npc", "\"on_kosullar\": [ { \"alan\": \"ayni_mekanda(eylem.hedef)\", \"op\": \"==\", \"deger\": true } ]");

            Assert.Equal(new[] { "yardim|Veli" }, Anahtarlar(Uret("Ali", eylem)));
        }

        [Fact]
        public void Eylem_yoksa_sonuc_bostur()
        {
            var sim = KararFikstur.Sim(new EylemTanimi[0]);

            var u = AdayUretici.Uret(KararFikstur.Baglam(sim, "Ali"), sim.Eylemler);

            Assert.Empty(u.Adaylar);
            Assert.Empty(u.Elenenler);
        }

        [Fact]
        public void Uretim_zihni_ve_dunyayi_degistirmez()
        {
            var sim = KararFikstur.Sim(KararFikstur.Yukle(KararFikstur.Eylem("yardim", "npc"), KararFikstur.Eylem("git", "komsu_mekan")));
            var once = sim.DurumHash();
            var olaySayisi = sim.Gunluk.Sayi;

            AdayUretici.Uret(KararFikstur.Baglam(sim, "Ali"), sim.Eylemler);

            Assert.Equal(once, sim.DurumHash());
            Assert.Equal(olaySayisi, sim.Gunluk.Sayi);
        }

        [Fact]
        public void Elenenler_de_siralidir()
        {
            var u = Uret(
                "Can",
                KararFikstur.Eylem("z_eylem", "npc"),
                KararFikstur.Eylem("a_eylem", "oyuncu"));

            Assert.Equal(new[] { "a_eylem", "z_eylem" }, u.Elenenler.Select(e => e.Eylem.Id).ToArray());
        }
    }
}
