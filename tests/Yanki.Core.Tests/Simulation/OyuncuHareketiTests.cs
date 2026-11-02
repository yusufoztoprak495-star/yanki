using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Simulation;

namespace Yanki.Core.Tests.Simulation
{
    /// <summary>
    /// Oyuncu hareketi (ADR-011): ayrılmış <c>hareket</c> eylemi, hedef komşu mekan. Bir hareket iki dış olay üretir
    /// (<c>ayrildi</c> Mekan=kalkış, <c>geldi</c> Mekan=varış; <c>Hedef</c> karşı mekan) ve <c>konum!</c> gerçeği ayrı olayla değişir.
    /// Oyuncu olayları 1. adımda işlenir; oyuncunun konumu hemen değişir, NPC konumları değişmez.
    /// Fikstür: Ali ve Veli han'da, Can pazar'da, oyuncu han'da; han ile pazar bağlı.
    /// </summary>
    public class OyuncuHareketiTests
    {
        private static PlayerAction Git(string mekan) => new PlayerAction("hareket", mekan);

        private static Simulasyon Yeni() => SimFikstur.Yap();

        // ---- Submit denetimleri ----

        [Fact]
        public void Komsu_mekana_hareket_kabul_edilir() => Yeni().Submit(Git("pazar"));

        [Theory]
        [InlineData("han")] // bulunduğu mekan: kendine bağlı değil
        [InlineData("hayalet")] // dünyada yok
        [InlineData("Ali")] // mekan değil
        [InlineData("oyuncu")]
        [InlineData("")]
        public void Komsu_olmayan_ya_da_mekan_olmayan_hedef_reddedilir(string hedef) =>
            Assert.Throws<ArgumentException>(() => Yeni().Submit(Git(hedef)));

        [Fact]
        public void Baglanti_olmayan_mekan_reddedilir()
        {
            var json = SimFikstur.SenaryoJson.Replace("\"mekanlar\": [\"han\", \"pazar\"]", "\"mekanlar\": [\"han\", \"pazar\", \"degirmen\"]");
            var senaryo = SenaryoYukleyici.Yukle(json, "s.json").Senaryo!;
            var sim = Simulasyon.Olustur(senaryo, SimFikstur.Kurallar(SimFikstur.YardimKurali), 1);

            var ex = Assert.Throws<ArgumentException>(() => sim.Submit(Git("degirmen")));

            Assert.Contains("bağlı", ex.Message);
        }

        [Fact]
        public void Bir_tickte_ikinci_hareket_acik_hatadir()
        {
            var sim = Yeni();
            sim.Submit(Git("pazar"));

            var ex = Assert.Throws<ArgumentException>(() => sim.Submit(Git("pazar")));

            Assert.Contains("bir hareket", ex.Message);
        }

        [Fact]
        public void Hareket_baska_yuk_tasiyamaz()
        {
            var hata = Assert.Throws<ArgumentException>(() =>
                Yeni().Submit(new PlayerAction("hareket", "pazar", new[] { new KeyValuePair<string, string>("karsi_mekan", "x") })));

            Assert.Contains("yük", hata.Message);
        }

        [Fact]
        public void Hareket_gorunurluk_yukunu_alabilir()
        {
            var sim = Yeni();
            sim.Submit(new PlayerAction("hareket", "pazar", new[] { new KeyValuePair<string, string>("gorunurluk", "gizli") }));
            sim.Tick(1);

            // gizli: yalnızca fail (oyuncu NPC değil) → kimse algılamaz.
            Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.KuralId == "algi");
        }

        [Fact]
        public void Hareket_senaryoda_bildirilmeden_kullanilabilir()
        {
            Assert.DoesNotContain("hareket", SimFikstur.Senaryo().OlayTurleri);

            Yeni().Submit(Git("pazar"));
        }

        [Fact]
        public void Komsuluk_gunceli_gore_denetlenir_hareketten_sonra_geri_donulebilir()
        {
            var sim = Yeni();
            sim.Submit(Git("pazar"));
            sim.Tick(1);

            Assert.Throws<ArgumentException>(() => sim.Submit(Git("pazar")));
            sim.Submit(Git("han"));
        }

        // ---- olaylar ----

        [Fact]
        public void Hareket_ayrildi_geldi_ve_konum_olaylarini_bu_sirayla_yazar()
        {
            var sim = Yeni();
            sim.Submit(Git("pazar"));
            sim.Tick(1);

            var ayrildi = Assert.Single(GunlukImzasi.Turde(sim, "ayrildi"));
            var geldi = Assert.Single(GunlukImzasi.Turde(sim, "geldi"));
            var konum = Assert.Single(sim.Gunluk.Olaylar, o => o.Tur == "gercek_eklendi" && o.KuralId == "player");

            Assert.Equal(("oyuncu", "pazar", "han", "player"), (ayrildi.Fail, ayrildi.Hedef, ayrildi.Mekan, ayrildi.KuralId));
            Assert.Equal(("oyuncu", "han", "pazar", "player"), (geldi.Fail, geldi.Hedef, geldi.Mekan, geldi.KuralId));
            Assert.Equal("pazar", ayrildi.Deger("karsi_mekan"));
            Assert.Equal("han", geldi.Deger("karsi_mekan"));
            Assert.Equal("konum!(oyuncu,pazar)", konum.Deger("gercek"));
            Assert.Equal(new[] { geldi.Id }, konum.NedenOlaylar.ToArray());
            Assert.True(ayrildi.Id < geldi.Id && geldi.Id < konum.Id);
            Assert.Equal(1, ayrildi.Tick);
            Assert.Equal("pazar", sim.Dunya.TekDeger("konum!", "oyuncu"));
        }

        [Fact]
        public void Hareket_eylem_gunlugune_girer_ve_replayde_ayni_hash_uretilir()
        {
            var sim = Yeni();
            sim.Submit(Git("pazar"));
            sim.Tick(2);
            sim.Submit(Git("han"));
            sim.Tick(1);

            var kayit = OturumKaydi.Uret(sim, "senaryo.json");
            var geri = OturumKaydi.Oku(OturumKaydi.Yaz(kayit), "oturum.json");

            Assert.True(geri.Basarili, string.Join("\n", geri.Hatalar));
            Assert.Equal(new[] { "hareket:pazar@1", "hareket:han@3" }, sim.EylemGunlugu.Select(e => e.Eylem.Tur + ":" + e.Eylem.Hedef + "@" + e.Tick).ToArray());

            var yeniSim = Yeni();
            var sonuc = OturumOynatici.Oynat(yeniSim, geri.Kayit!);
            Assert.True(sonuc.HashTutuyor, sonuc.GercekHash);
            Assert.Equal(GunlukImzasi.Al(sim), GunlukImzasi.Al(yeniSim));
        }

        [Fact]
        public void Hareketten_sonra_gelen_oyuncu_eylemi_varis_mekaninda_olur()
        {
            var sim = Yeni();
            sim.Submit(new PlayerAction("yardim_etti", "Ali")); // hareketten önce: han
            sim.Submit(Git("pazar"));
            sim.Submit(new PlayerAction("yardim_etti", "Can")); // hareketten sonra: pazar
            sim.Tick(1);

            var yardimlar = GunlukImzasi.Turde(sim, "yardim_etti").OrderBy(o => o.Id).ToList();
            Assert.Equal("han", yardimlar[0].Mekan);
            Assert.Equal("pazar", yardimlar[1].Mekan);
        }

        [Fact]
        public void Hareketten_sonra_gelen_eylemi_pazardaki_can_algilar_handakiler_algilamaz()
        {
            var sim = Yeni();
            sim.Submit(Git("pazar"));
            sim.Submit(new PlayerAction("yardim_etti", "Can"));
            sim.Tick(1);

            var yardim = Assert.Single(GunlukImzasi.Turde(sim, "yardim_etti"));
            var algilayanlar = sim.Gunluk.Olaylar.Where(o => o.KuralId == "algi" && o.NedenOlaylar.Contains(yardim.Id)).Select(o => o.Deger("npc")).Distinct().ToArray();
            Assert.Equal(new[] { "Can" }, algilayanlar);
        }

        [Fact]
        public void Npc_konumlari_oyuncu_hareketiyle_degismez()
        {
            var sim = Yeni();
            sim.Submit(Git("pazar"));
            sim.Tick(1);

            Assert.Equal("han", sim.Dunya.TekDeger("konum!", "Ali"));
            Assert.Equal("han", sim.Dunya.TekDeger("konum!", "Veli"));
            Assert.Equal("pazar", sim.Dunya.TekDeger("konum!", "Can"));
        }

        // ---- algı: mekan önermede görünür ----

        [Fact]
        public void Algilanan_onerme_karsi_mekani_tasir()
        {
            var sim = Yeni();
            sim.Submit(Git("pazar"));
            sim.Tick(1);

            // han'dakiler (Ali, Veli) oyuncunun pazara gittiğini, pazardaki Can oyuncunun pazara geldiğini görür.
            Assert.Single(sim.Zihinler["Ali"].Kanitlar("ayrildi(oyuncu,pazar)"));
            Assert.Single(sim.Zihinler["Veli"].Kanitlar("ayrildi(oyuncu,pazar)"));
            Assert.Single(sim.Zihinler["Can"].Kanitlar("geldi(oyuncu,han)"));
            Assert.Empty(sim.Zihinler["Can"].Kanitlar("ayrildi(oyuncu,pazar)"));
            Assert.Empty(sim.Zihinler["Ali"].Kanitlar("geldi(oyuncu,han)"));
        }

        [Fact]
        public void Hareket_olaylari_kural_tetikleyebilir_ve_olay_rolu_calisir()
        {
            var kural = Yanki.Core.Rules.KuralYukleyici.Yukle(
                "{ \"kurallar\": [ { \"id\": \"gelen_fark_edildi\", \"ne_zaman\": \"geldi\", \"kriterler\": [ { \"alan\": \"olay_rolu(tanik)\", \"op\": \"==\", \"deger\": true } ],"
                + " \"etkiler\": [ { \"tur\": \"modifier\", \"hedef\": \"olay.fail\", \"modifier\": \"fark_ettim\", \"deger\": 2, \"sure\": 20 } ] } ] }",
                "k.json", SimFikstur.Senaryo().OlayTurleri);
            Assert.True(kural.Basarili, string.Join("\n", kural.Hatalar));
            var sim = Simulasyon.Olustur(SimFikstur.Senaryo(), kural.Kurallar, 1);

            sim.Submit(Git("pazar"));
            sim.Tick(1);

            // Yalnızca pazardaki Can oyuncunun geldiğini görür; modifier onda, oyuncu (olay.fail) hedefindedir.
            var m = Assert.Single(sim.Gunluk.Olaylar, o => o.KuralId == "gelen_fark_edildi");
            Assert.Equal("Can", m.Deger("npc"));
            Assert.Equal("oyuncu", m.Deger("hedef"));
        }
    }
}
