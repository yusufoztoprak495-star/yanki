using System;
using System.Linq;
using Xunit;
using Yanki.Core.Brakes;
using Yanki.Core.Criteria;
using Yanki.Core.Decision;
using Yanki.Core.Events;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;
using Yanki.Core.Tests.Decision;
using Yanki.Core.Tests.Simulation;

namespace Yanki.Core.Tests.Brakes
{
    /// <summary>
    /// 7. hafta (D): korumalı bayraklar. Fikstür: Ali ve Veli han'da, Can pazar'da. Ali "oldurulemez" ve "mekandan_ayrilamaz", "kese" varlığı
    /// "calinamaz" korumalıdır. Kapsam sınırı: oldurulemez ve calinamaz YALNIZCA ihlal_bayraklari bildiren eylem ve kurallarda uygulanır.
    /// </summary>
    public class KorumaliTests
    {
        private const string Koruma =
            "\"gercekler\": [], \"korumali\": { \"Ali\": [\"oldurulemez\", \"mekandan_ayrilamaz\"], \"kese\": [\"calinamaz\"] }, "
            + "\"oyuncu_ihlalleri\": { \"oldur_et\": [ { \"bayrak\": \"oldurulemez\", \"varlik\": \"eylem.hedef\" } ] },";

        private static string SenaryoJson(string koruma = Koruma) =>
            SimFikstur.SenaryoJson.Replace("\"gercekler\": [],", koruma).Replace("[\"yardim_etti\", \"tehdit_etti\"]", "[\"yardim_etti\", \"tehdit_etti\", \"oldur_et\"]");

        private static SenaryoTanimi Senaryo(string koruma = Koruma)
        {
            var sonuc = SenaryoYukleyici.Yukle(SenaryoJson(koruma), "s.json");
            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            return sonuc.Senaryo!;
        }

        private const string Oldur = "\"ihlal_bayraklari\": [ { \"bayrak\": \"oldurulemez\", \"varlik\": \"eylem.hedef\" } ]";
        private const string KeseCal = "\"ihlal_bayraklari\": [ { \"bayrak\": \"calinamaz\", \"varlik\": \"kese\" } ]";

        private static Simulasyon Kur(string[] eylemler, string? kuralJson = null, string koruma = Koruma)
        {
            var senaryo = Senaryo(koruma);
            var kurallar = kuralJson == null ? new KuralTanimi[0].ToList() : KuralYukleyici.Yukle(kuralJson, "k.json", senaryo.OlayTurleri).Kurallar.ToList();
            return Simulasyon.Olustur(senaryo, kurallar, 42, eylemler: eylemler.Length == 0 ? null : KararFikstur.Yukle(eylemler));
        }

        private static KararBaglami Baglam(Simulasyon s, string npc) =>
            new KararBaglami(
                npc, s.OyuncuId, s.Seed, s.SimdikiTick + 1, s.Dunya, s.Zihinler[npc], s.Gunluk,
                new ZihinSorguCozucusu(s.Dunya, s.Zihinler, s.Gunluk, s.SimdikiTick + 1), korumali: s.Senaryo.Korumali);

        [Fact]
        public void Korumali_hedef_adaylar_listesine_hic_girmez_elenme_nedeniyle_yazilir()
        {
            var s = Kur(new[] { KararFikstur.Eylem("ldur", "npc", Oldur) });
            var uretim = AdayUretici.Uret(Baglam(s, "Veli"), s.Eylemler);

            Assert.DoesNotContain(uretim.Adaylar, a => a.Hedef == "Ali");
            var elenen = Assert.Single(uretim.Elenenler, e => e.Hedef == "Ali");
            Assert.Contains("korumali", elenen.Neden);
            Assert.Contains("Ali", elenen.Neden);
            Assert.Contains("oldurulemez", elenen.Neden);
            Assert.Single(uretim.Frenler);
            Assert.Equal((FrenTurleri.Korumali, "ldur", "Veli", "Ali"), (uretim.Frenler[0].Tur, uretim.Frenler[0].Kimlik, uretim.Frenler[0].Npc, uretim.Frenler[0].Hedef));
        }

        [Fact]
        public void Korumali_hedef_secilemez_baska_hedef_serbesttir_ve_neden_karar_izinde_gorunur()
        {
            var s = Kur(new[] { KararFikstur.Eylem("ldur", "npc", Oldur), KararFikstur.Eylem("dinlen", "yok", string.Empty, 0.1m) });
            s.Tick(3);

            Assert.DoesNotContain(s.Gunluk.Olaylar, o => o.Tur == "ldur_oldu" && o.Hedef == "Ali");
            Assert.Contains(s.Gunluk.Olaylar, o => o.Tur == "ldur_oldu" && o.Fail == "Ali" && o.Hedef == "Veli");   // Ali korumalı ama Veli'yi hedefleyebilir
            var iz = s.KararIzi("Veli")!.Adaylar.Single(a => a.Aday.Eylem.Id == "ldur" && a.Aday.Hedef == "Ali");
            Assert.True(iz.Elendi);
            Assert.Contains("korumali", iz.Neden);
        }

        [Fact]
        public void Korumali_fren_olayi_engel_basina_bir_kez_yazilir_ve_ana_kosul_tutmayana_yazilmaz()
        {
            var s = Kur(new[] { KararFikstur.Eylem("ldur", "npc", Oldur), KararFikstur.Eylem("dinlen", "yok", string.Empty, 0.1m) });
            s.Tick(4);

            var fren = s.Frenler("korumali", "Veli");
            var f = Assert.Single(fren);                                          // 4 tick boyunca tek olay (tekilleştirme)
            Assert.Equal(("ldur", "Ali", 1L), (f.Deger("kimlik"), f.Deger("hedef"), f.Tick));

            var tutmayan = Kur(new[]
            {
                KararFikstur.Eylem("ldur", "npc", Oldur + ", \"on_kosullar\": [ { \"alan\": \"kisilik(cesaret)\", \"op\": \">\", \"deger\": 0.99 } ]"),
                KararFikstur.Eylem("dinlen", "yok", string.Empty, 0.1m),
            });
            tutmayan.Tick(3);
            Assert.Empty(tutmayan.Frenler("korumali"));                           // ön koşul tutsaydı bile aday olmayacaktı
        }

        [Fact]
        public void Sabit_kimlikli_esya_korumasi_calinamaz_herkes_icin_adayi_eler()
        {
            var s = Kur(new[] { KararFikstur.Eylem("calkese", "yok", KeseCal), KararFikstur.Eylem("dinlen", "yok", string.Empty, 0.1m) });
            s.Tick(3);

            Assert.DoesNotContain(s.Gunluk.Olaylar, o => o.Tur == "calkese_oldu");
            Assert.Contains(s.Frenler("korumali"), f => f.Deger("hedef") == "kese");
        }

        [Fact]
        public void Yerlesik_harekette_mekandan_ayrilamaz_motor_tarafindan_uygulanir_yazar_bildirmez()
        {
            var s = Kur(new[] { KararFikstur.Eylem("git", "komsu_mekan") });
            s.Tick(4);

            Assert.DoesNotContain(s.Gunluk.Olaylar, o => o.Tur == "ayrildi" && o.Fail == "Ali");     // Ali korumalı
            Assert.Contains(s.Gunluk.Olaylar, o => o.Tur == "ayrildi" && o.Fail == "Veli");
            Assert.Equal("han", s.Dunya.TekDeger("konum!", "Ali"));
        }

        [Fact]
        public void Kural_ihlali_engellenir_korumali_freni_yazilir_korumasiz_hedefte_calisir()
        {
            var kural = @"{ ""kurallar"": [ { ""id"": ""r"", ""ne_zaman"": ""tehdit_etti"", " + Oldur.Replace("eylem.hedef", "olay.hedef") + @",
  ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""korku"", ""deger"": 1, ""sure"": 100 } ] } ] }";
            var s = Kur(new string[0], kural);
            s.Submit(new PlayerAction("tehdit_etti", "Ali"));
            s.Submit(new PlayerAction("tehdit_etti", "Veli"));
            s.Tick(1);

            Assert.DoesNotContain(s.Gunluk.Olaylar, o => o.KuralId == "r" && o.NedenOlaylar.Any(n => s.Gunluk[n].Tur == "tehdit_etti" && s.Gunluk[n].Hedef == "Ali"));
            var fren = s.Frenler("korumali");
            Assert.NotEmpty(fren);
            Assert.All(fren, f =>
            {
                Assert.Equal("r", f.Deger("kimlik"));
                Assert.Equal("Ali", f.Deger("hedef"));
            });
            Assert.Contains(s.Gunluk.Olaylar, o => o.KuralId == "r");             // Veli hedefli tetik çalıştı
        }

        [Fact]
        public void Konum_gercegini_yazarak_korumali_varligi_tasimak_otomatik_ihlaldir_kural_calismaz()
        {
            var kural = @"{ ""kurallar"": [ { ""id"": ""tasi"", ""ne_zaman"": ""tehdit_etti"",
  ""etkiler"": [ { ""tur"": ""gercek"", ""islem"": ""ekle"", ""gercek"": ""konum!({olay.hedef},pazar)"" } ] } ] }";
            var s = Kur(new string[0], kural);
            s.Submit(new PlayerAction("tehdit_etti", "Ali"));
            s.Submit(new PlayerAction("tehdit_etti", "Veli"));
            s.Tick(1);

            Assert.Equal("han", s.Dunya.TekDeger("konum!", "Ali"));                         // korumalı: taşınamadı
            Assert.Equal("pazar", s.Dunya.TekDeger("konum!", "Veli"));                     // korumasız: taşındı
            Assert.Contains(s.Frenler("korumali"), f => f.Deger("kimlik") == "tasi" && f.Deger("hedef") == "Ali");
        }

        [Fact]
        public void Oyuncu_ihlali_Submit_tarafindan_reddedilir_korumasiz_hedef_ve_baska_tur_serbesttir()
        {
            var s = Kur(new string[0]);

            var hata = Assert.Throws<ArgumentException>(() => s.Submit(new PlayerAction("oldur_et", "Ali")));
            Assert.Contains("oldurulemez", hata.Message);
            s.Submit(new PlayerAction("oldur_et", "Veli"));           // korumasız hedef
            s.Submit(new PlayerAction("tehdit_etti", "Ali"));         // bu tür ihlal bildirmiyor: bildirmeyen eylemi motor yakalayamaz
            s.Tick(1);
            Assert.Equal(2, s.Gunluk.Olaylar.Count(o => o.KuralId == "player"));
        }

        [Fact]
        public void Korumali_oyuncu_hareket_edemez()
        {
            var s = Kur(new string[0], null, "\"gercekler\": [], \"korumali\": { \"oyuncu\": [\"mekandan_ayrilamaz\"] },");
            Assert.Throws<ArgumentException>(() => s.Submit(new PlayerAction("hareket", "pazar")));
        }

        [Fact]
        public void Fren_alani_olmayan_senaryoda_korumalilik_bostur_ve_hicbir_sey_degismez()
        {
            Assert.True(SimFikstur.Senaryo().Korumali.Bos);
            Assert.Empty(SimFikstur.Senaryo().OyuncuIhlalleri);
        }

        [Theory]
        [InlineData("\"korumali\": { \"Ali\": [\"olumsuz\"] }")]
        [InlineData("\"korumali\": { \"Ali\": [] }")]
        [InlineData("\"korumali\": [ \"Ali\" ]")]
        [InlineData("\"oyuncu_ihlalleri\": { \"x\": [ { \"bayrak\": \"mekandan_ayrilamaz\", \"varlik\": \"ben\" } ] }")]
        [InlineData("\"oyuncu_ihlalleri\": { \"x\": [ { \"bayrak\": \"oldurulemez\" } ] }")]
        public void Dogrulayici_gecersiz_koruma_alanlarini_reddeder(string ek)
        {
            var sonuc = SenaryoYukleyici.Yukle(SimFikstur.SenaryoJson.Replace("\"gercekler\": [],", "\"gercekler\": [], " + ek + ","), "s.json");
            Assert.False(sonuc.Basarili);
        }

        [Theory]
        [InlineData("\"ihlal_bayraklari\": [ { \"bayrak\": \"mekandan_ayrilamaz\", \"varlik\": \"ben\" } ]")]
        [InlineData("\"ihlal_bayraklari\": [ { \"bayrak\": \"oldurulemez\", \"varlik\": \"olay.hedef\" } ]")]
        [InlineData("\"ihlal_bayraklari\": [ { \"bayrak\": \"yok\", \"varlik\": \"ben\" } ]")]
        [InlineData("\"ihlal_bayraklari\": { }")]
        public void Eylem_dogrulayicisi_gecersiz_ihlal_bayraklarini_reddeder(string ek)
        {
            var sonuc = EylemYukleyici.Yukle(KararFikstur.Dosya(KararFikstur.Eylem("x", "npc", ek)), "e.json");
            Assert.False(sonuc.Basarili);
        }

        [Fact]
        public void Eylem_hedefsizken_eylem_hedef_varligi_reddedilir()
        {
            var sonuc = EylemYukleyici.Yukle(KararFikstur.Dosya(KararFikstur.Eylem("x", "yok", Oldur)), "e.json");
            Assert.False(sonuc.Basarili);
        }

        [Fact]
        public void Olustur_bilinmeyen_sabit_varligi_ve_kisi_olmayan_oldurulemez_korumayi_reddeder()
        {
            var bilinmeyen = KararFikstur.Eylem("c", "yok", "\"ihlal_bayraklari\": [ { \"bayrak\": \"calinamaz\", \"varlik\": \"yokkese\" } ]");
            Assert.Throws<ArgumentException>(() => Kur(new[] { bilinmeyen }));

            Assert.Throws<ArgumentException>(() => Kur(new string[0], null, "\"gercekler\": [], \"korumali\": { \"kese\": [\"oldurulemez\"] },"));
        }
    }
}
