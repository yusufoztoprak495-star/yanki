using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Events;
using Yanki.Core.Mind;
using Yanki.Core.Perception;
using Yanki.Core.Tests.Mind;
using Yanki.Core.Tests.World;
using Yanki.Core.World;

namespace Yanki.Core.Tests.Perception
{
    /// <summary>
    /// Sabit dünya: han'da Ali ve Ayse, pazar'da Mehmet (hepsi npc). Olaylar han'da geçer.
    /// </summary>
    public class DogrudanAlgiTests
    {
        private static KeyValuePair<string, string> Y(string k, string v) => new KeyValuePair<string, string>(k, v);

        private static Olay Gozlem(
            string tur, string fail, string hedef, string mekan, string? gorunurluk = null, long id = 1, long tick = 5, params KeyValuePair<string, string>[] ek)
        {
            var yuk = new List<KeyValuePair<string, string>>(ek);
            if (gorunurluk != null)
            {
                yuk.Add(Y("gorunurluk", gorunurluk));
            }

            return new Olay(id, tick, 0, tur, fail, hedef, mekan, yuk, new long[0], "player");
        }

        private static IReadOnlyList<string> Algi(Olay olay) => DogrudanAlgi.Algilayanlar(OlayUretici.SabitDunya(), olay);

        [Fact]
        public void Acik_olayi_mekandaki_butun_npcler_algilar_sirali()
        {
            Assert.Equal(new[] { "Ali", "Ayse" }, Algi(Gozlem("yardim_etti", "Ali", "Ayse", "han", "acik")));
        }

        [Fact]
        public void Gorunurluk_yoksa_acik_sayilir()
        {
            Assert.Equal(new[] { "Ali", "Ayse" }, Algi(Gozlem("yardim_etti", "Ali", "Ayse", "han")));
        }

        [Fact]
        public void Acik_olayi_baska_mekandaki_npc_algilamaz()
        {
            Assert.Equal(new[] { "Mehmet" }, Algi(Gozlem("yardim_etti", "Mehmet", "", "pazar", "acik")));
        }

        [Fact]
        public void Mekani_olmayan_acik_olayi_yalniz_fail_algilar()
        {
            Assert.Equal(new[] { "Ali" }, Algi(Gozlem("duyuru", "Ali", "", "", "acik")));
            Assert.Empty(Algi(Gozlem("duyuru", "", "", "", "acik")));
        }

        [Theory]
        [InlineData("acik")]
        [InlineData("sessiz")]
        [InlineData("gizli")]
        public void Fail_her_gorunurlukte_ve_mekan_bos_olsa_bile_kendi_olayini_algilar(string gorunurluk)
        {
            Assert.Contains("Mehmet", Algi(Gozlem("yardim_etti", "Mehmet", "Ayse", "", gorunurluk)));
        }

        [Fact]
        public void Fail_olayin_mekaninda_degilse_bile_acik_olayi_algilar()
        {
            // Mehmet pazar'da, olay han'da.
            Assert.Equal(new[] { "Ali", "Ayse", "Mehmet" }, Algi(Gozlem("yardim_etti", "Mehmet", "Ali", "han", "acik")));
        }

        [Fact]
        public void Oyuncu_npc_degildir_konum_fail_ve_hedef_olabilir_ama_zihni_ve_algisi_yoktur()
        {
            var u = new OlayUretici();
            var dunya = OlayUretici.SabitDunya();
            dunya.Uygula(u.Varlik("oyuncu", "oyuncu"));
            dunya.Uygula(u.GercekEkle("konum!(oyuncu,han)"));
            var depo = new ZihinDeposu();
            foreach (var id in new[] { "Ali", "Ayse" })
            {
                depo.Ekle(new Zihin(id, new Kisilik(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, 0.5m)));
            }

            var oyuncuFail = Gozlem("yardim_etti", "oyuncu", "Ayse", "han", "acik");
            var oyuncuHedef = Gozlem("tehdit_etti", "Ali", "oyuncu", "han", "sessiz");

            Assert.Equal("han", dunya.TekDeger("konum!", "oyuncu"));
            Assert.Equal(new[] { "Ali", "Ayse" }, DogrudanAlgi.Algilayanlar(dunya, oyuncuFail));
            Assert.Equal(new[] { "Ali" }, DogrudanAlgi.Algilayanlar(dunya, oyuncuHedef));
            Assert.Empty(DogrudanAlgi.Algilayanlar(dunya, Gozlem("gezdi", "oyuncu", "", "han", "gizli")));
            Assert.False(depo.Var("oyuncu"));

            // Üretilen olayların hiçbiri oyuncuya ait değil.
            var (gunluk, gozlenen) = GunlukKur(oyuncuFail);
            Assert.All(DogrudanAlgi.Uret(dunya, gozlenen, gunluk), o => Assert.NotEqual("oyuncu", o.Deger("npc")));

            // Fikir hedefi olabilir.
            depo.Uygula(new ZihinOlayUretici().Eksen("Ali", "oyuncu", "guven", 50m));
            Assert.Equal(20m, depo.Fikir("Ali", "oyuncu", 0));
        }

        [Fact]
        public void Sessiz_olayi_olayin_mekanindaki_fail_ve_hedef_algilar()
        {
            Assert.Equal(new[] { "Ali", "Ayse" }, Algi(Gozlem("fisildadi", "Ali", "Ayse", "han", "sessiz")));
        }

        [Fact]
        public void Sessiz_olayda_hedef_olayin_mekaninda_degilse_algilamaz()
        {
            // Mehmet pazar'da, olay han'da.
            Assert.Equal(new[] { "Ali" }, Algi(Gozlem("fisildadi", "Ali", "Mehmet", "han", "sessiz")));
        }

        [Fact]
        public void Sessiz_olayda_fail_mekandan_bagimsiz_algilar()
        {
            Assert.Equal(new[] { "Ali", "Mehmet" }, Algi(Gozlem("fisildadi", "Mehmet", "Ali", "han", "sessiz")));
        }

        [Fact]
        public void Mekani_olmayan_sessiz_olayi_hedef_algilamaz()
        {
            Assert.Equal(new[] { "Ali" }, Algi(Gozlem("fisildadi", "Ali", "Ayse", "", "sessiz")));
        }

        [Fact]
        public void Gizli_olayi_yalniz_fail_algilar()
        {
            Assert.Equal(new[] { "Ali" }, Algi(Gozlem("calma", "Ali", "Ayse", "han", "gizli")));
        }

        [Fact]
        public void Fail_ile_hedef_ayniysa_tek_kez_sayilir()
        {
            Assert.Equal(new[] { "Ali" }, Algi(Gozlem("dusundu", "Ali", "Ali", "han", "sessiz")));
        }

        [Fact]
        public void Npc_olmayan_fail_ya_da_hedef_algilamaz()
        {
            // oyuncu dünyada kayıtlı değil, han bir mekan.
            Assert.Equal(new[] { "Ali" }, Algi(Gozlem("konustu", "oyuncu", "Ali", "han", "sessiz")));
            Assert.Empty(Algi(Gozlem("gezdi", "oyuncu", "han", "han", "gizli")));
        }

        [Fact]
        public void Konumu_bilinmeyen_npc_acik_olayi_algilamaz()
        {
            var u = new OlayUretici();
            var dunya = new Dunya();
            dunya.Uygula(u.Varlik("han", "mekan"));
            dunya.Uygula(u.Varlik("Ali", "npc"));
            dunya.Uygula(u.Varlik("Veli", "npc"));
            dunya.Uygula(u.GercekEkle("konum!(Ali,han)"));

            Assert.Equal(new[] { "Ali" }, DogrudanAlgi.Algilayanlar(dunya, Gozlem("duyuru", "", "", "han", "acik")));
        }

        [Fact]
        public void Bilinmeyen_gorunurluk_hata_verir()
        {
            Assert.Throws<ArgumentException>(() => Algi(Gozlem("duyuru", "Ali", "", "han", "yari_acik")));
        }

        [Fact]
        public void Algilayanlar_olayi_degistirmez_ve_her_cagrida_ayni_sonucu_verir()
        {
            var olay = Gozlem("yardim_etti", "Ali", "Ayse", "han", "acik");

            Assert.Equal(Algi(olay), Algi(olay));
        }

        // ---- önerme türetme ----

        [Fact]
        public void Gercek_eklendi_olayindan_o_gercek_turetilir()
        {
            var olay = Gozlem("gercek_eklendi", "", "", "han", null, 1, 5, Y("gercek", "borclu(Mehmet,Ayse,500)"));

            Assert.Equal("borclu(Mehmet,Ayse,500)", DogrudanAlgi.OnermeUret(olay));
        }

        [Fact]
        public void Gercek_kaldirildi_olayindan_olumsuzu_turetilir()
        {
            var olay = Gozlem("gercek_kaldirildi", "", "", "han", null, 1, 5, Y("gercek", "borclu(Mehmet,Ayse,500)"));

            Assert.Equal("degil(borclu(Mehmet,Ayse,500))", DogrudanAlgi.OnermeUret(olay));
        }

        [Fact]
        public void Diger_turlerde_tur_fail_hedef_onermesi_turetilir()
        {
            Assert.Equal("yardim_etti(oyuncu,Can)", DogrudanAlgi.OnermeUret(Gozlem("yardim_etti", "oyuncu", "Can", "han")));
            Assert.Equal("selamladi(Ali)", DogrudanAlgi.OnermeUret(Gozlem("selamladi", "Ali", "", "han")));
            Assert.Equal("kayboldu(Ali)", DogrudanAlgi.OnermeUret(Gozlem("kayboldu", "", "Ali", "han")));
            Assert.Null(DogrudanAlgi.OnermeUret(Gozlem("firtina", "", "", "han")));
        }

        // ---- üretilen olaylar ----

        private static (OlayGunlugu Gunluk, Olay Gozlenen) GunlukKur(Olay gozlenen)
        {
            var gunluk = new OlayGunlugu();
            gunluk.Ekle(gozlenen);
            return (gunluk, gozlenen);
        }

        [Fact]
        public void Her_algilayan_icin_kanit_ve_hafiza_olayi_uretilir_algilayan_sirasiyla()
        {
            var (gunluk, gozlenen) = GunlukKur(Gozlem("yardim_etti", "Ali", "Ayse", "han", "acik"));

            var uretilen = DogrudanAlgi.Uret(OlayUretici.SabitDunya(), gozlenen, gunluk);

            Assert.Equal(
                new[] { "kanit_eklendi", "hafiza_eklendi", "kanit_eklendi", "hafiza_eklendi" },
                uretilen.Select(o => o.Tur).ToArray());
            Assert.Equal(new[] { "Ali", "Ali", "Ayse", "Ayse" }, uretilen.Select(o => o.Deger("npc")).ToArray());
        }

        [Fact]
        public void Uretilen_olaylar_gunluge_sirayla_eklenebilir_kimlik_ve_sira_ardisiktir()
        {
            var (gunluk, gozlenen) = GunlukKur(Gozlem("yardim_etti", "Ali", "Ayse", "han", "acik"));

            var uretilen = DogrudanAlgi.Uret(OlayUretici.SabitDunya(), gozlenen, gunluk);

            Assert.Equal(new long[] { 2, 3, 4, 5 }, uretilen.Select(o => o.Id).ToArray());
            Assert.Equal(new[] { 1, 2, 3, 4 }, uretilen.Select(o => o.Sira).ToArray());
            foreach (var o in uretilen)
            {
                gunluk.Ekle(o);
            }

            Assert.Equal(5, gunluk.Sayi);
        }

        [Fact]
        public void Uretilen_olaylar_algilanan_olayi_neden_ve_algi_kural_kimligini_tasir()
        {
            var (gunluk, gozlenen) = GunlukKur(Gozlem("yardim_etti", "Ali", "Ayse", "han", "acik"));

            var uretilen = DogrudanAlgi.Uret(OlayUretici.SabitDunya(), gozlenen, gunluk);

            Assert.All(uretilen, o =>
            {
                Assert.Equal(new long[] { 1 }, o.NedenOlaylar);
                Assert.Equal("algi", o.KuralId);
                Assert.Equal("han", o.Mekan);
                Assert.Equal(5, o.Tick);
            });
        }

        [Fact]
        public void Kanit_gordu_turunde_varsayilan_guc_0_9_ve_kaynak_algilayandir()
        {
            var (gunluk, gozlenen) = GunlukKur(Gozlem("yardim_etti", "Ali", "Ayse", "han", "acik"));

            var kanit = DogrudanAlgi.Uret(OlayUretici.SabitDunya(), gozlenen, gunluk).First();

            Assert.Equal("kanit_eklendi", kanit.Tur);
            Assert.Equal("gordu", kanit.Deger("tur"));
            Assert.Equal("0.9", kanit.Deger("guc"));
            Assert.Equal("Ali", kanit.Deger("kaynak"));
            Assert.Equal("yardim_etti(Ali,Ayse)", kanit.Deger("onerme"));
        }

        [Fact]
        public void Kanit_gucu_ve_hafiza_onemi_ayarlanabilir()
        {
            var (gunluk, gozlenen) = GunlukKur(Gozlem("yardim_etti", "Ali", "", "han", "gizli"));
            var ayar = new AlgiAyarlari { GorduKanitGucu = 0.6m, HafizaOnemi = 0.8m };

            var uretilen = DogrudanAlgi.Uret(OlayUretici.SabitDunya(), gozlenen, gunluk, ayar);

            Assert.Equal("0.6", uretilen[0].Deger("guc"));
            Assert.Equal("0.8", uretilen[1].Deger("onem"));
            Assert.Equal("1", uretilen[1].Deger("olay"));
        }

        [Fact]
        public void Onerme_turetilemezse_yalniz_hafiza_olayi_uretilir()
        {
            var (gunluk, gozlenen) = GunlukKur(Gozlem("firtina", "", "", "han", "acik"));

            var uretilen = DogrudanAlgi.Uret(OlayUretici.SabitDunya(), gozlenen, gunluk);

            Assert.Equal(new[] { "hafiza_eklendi", "hafiza_eklendi" }, uretilen.Select(o => o.Tur).ToArray());
        }

        [Fact]
        public void Algilayan_yoksa_olay_uretilmez()
        {
            var (gunluk, gozlenen) = GunlukKur(Gozlem("duyuru", "", "", "", "acik"));

            Assert.Empty(DogrudanAlgi.Uret(OlayUretici.SabitDunya(), gozlenen, gunluk));
        }

        [Fact]
        public void Uretim_gunluge_ya_da_dunyaya_dokunmaz()
        {
            var (gunluk, gozlenen) = GunlukKur(Gozlem("yardim_etti", "Ali", "Ayse", "han", "acik"));
            var dunya = OlayUretici.SabitDunya();
            var hash = dunya.DurumHash();

            DogrudanAlgi.Uret(dunya, gozlenen, gunluk);

            Assert.Equal(1, gunluk.Sayi);
            Assert.Equal(hash, dunya.DurumHash());
        }

        [Fact]
        public void Uctan_uca_gozlenen_olay_zihinlerde_gordu_kaniti_ve_hafiza_birakir()
        {
            var dunya = OlayUretici.SabitDunya();
            var depo = new ZihinDeposu();
            foreach (var id in new[] { "Ali", "Ayse", "Mehmet" })
            {
                depo.Ekle(new Zihin(id, new Kisilik(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, 0.5m)));
            }

            var (gunluk, gozlenen) = GunlukKur(Gozlem("yardim_etti", "Ali", "Ayse", "han", "acik"));
            foreach (var o in DogrudanAlgi.Uret(dunya, gozlenen, gunluk))
            {
                gunluk.Ekle(o);
                depo.Uygula(o);
            }

            Assert.Equal(0.9m, depo["Ayse"].Guven("yardim_etti(Ali,Ayse)", 5));
            Assert.Equal(0.9m, depo["Ali"].Guven("yardim_etti(Ali,Ayse)", 5));
            Assert.Equal(0m, depo["Mehmet"].Guven("yardim_etti(Ali,Ayse)", 5));
            Assert.Equal(new long[] { 1 }, depo["Ayse"].Hafiza(5).Select(h => h.OlayId).ToArray());
            Assert.Empty(depo["Mehmet"].Hafiza(5));
            Assert.True(depo["Ayse"].HafizaVar(gunluk, "yardim_etti", "Ali", 168, 10));
            Assert.Equal(new long[] { 1 }, gunluk.Neden(3).Select(o => o.Id).ToArray());
        }

        [Fact]
        public void Gercek_kaldirildi_algisi_celisen_inanc_dogurur()
        {
            var dunya = OlayUretici.SabitDunya();
            var depo = new ZihinDeposu();
            depo.Ekle(new Zihin("Ayse", new Kisilik(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, 0.5m)));
            var u = new ZihinOlayUretici();
            depo["Ayse"].Uygula(u.Kanit("Ayse", "borclu(Mehmet,Ayse,500)", "duydu", "Veli", 0.4m));

            var (gunluk, gozlenen) = GunlukKur(Gozlem("gercek_kaldirildi", "", "", "han", "acik", 1, 5, Y("gercek", "borclu(Mehmet,Ayse,500)")));
            foreach (var o in DogrudanAlgi.Uret(dunya, gozlenen, gunluk).Where(o => o.Deger("npc") == "Ayse"))
            {
                depo.Uygula(o);
            }

            Assert.False(depo["Ayse"].Inanilan("borclu(Mehmet,Ayse,500)", 5));
            Assert.True(depo["Ayse"].Inanilan("degil(borclu(Mehmet,Ayse,500))", 5));
        }
    }
}
