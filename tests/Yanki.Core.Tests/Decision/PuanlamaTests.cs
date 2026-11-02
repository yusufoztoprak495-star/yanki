using System.Linq;
using Xunit;
using Yanki.Core.Decision;
using Yanki.Core.Simulation;
using Yanki.Core.Tests.Simulation;

namespace Yanki.Core.Tests.Decision
{
    /// <summary>Puanlama: <c>puan = taban × Π(olcut_egrisi(girdi)) × kisilik_carpani × fikir_carpani × ruh_hali_carpani</c> (plan 5.5, ADR-011).</summary>
    public class PuanlamaTests
    {
        // Ali: acgozluluk 0.5, durustluk 0.7, cesaret 0.5 (SimFikstur). Veli ve Ali han'da.
        private static AdayPuani Puanla(string npc, string eylemJson, string hedef = "", Simulasyon? sim = null)
        {
            var eylemler = KararFikstur.Yukle(eylemJson);
            sim ??= KararFikstur.Sim(eylemler);
            return Puanlayici.Puanla(KararFikstur.Baglam(sim, npc), new Aday(eylemler.Single(), hedef));
        }

        private static string Olcut(string alan, string aralik = "", string egri = "") =>
            "{ \"alan\": \"" + alan + "\"" + (aralik.Length > 0 ? ", " + aralik : string.Empty) + (egri.Length > 0 ? ", \"egri\": " + egri : string.Empty) + " }";

        private static string Olcutlu(string olcutler, string ek = "", decimal taban = 1m, string hedef = "yok") =>
            KararFikstur.Eylem("e", hedef, "\"olcutler\": [ " + olcutler + " ]" + (ek.Length > 0 ? ", " + ek : string.Empty), taban);

        // ---- formül ----

        [Fact]
        public void Formul_taban_olcut_carpimlari_ve_uc_carpanla_hesaplanir()
        {
            // taban 2 × dogrusal(cesaret 0.5 → 0.5) × ustel2(durustluk 0.7 → 0.49) × kisilik(durustluk 0.7 → 1.4) = 0.686
            var json = Olcutlu(
                Olcut("kisilik(cesaret)") + ", " + Olcut("kisilik(durustluk)", egri: "{ \"tur\": \"ustel\", \"k\": 2 }"),
                "\"kisilik_carpani\": { \"ozellikler\": [ { \"ozellik\": \"durustluk\" } ] }",
                taban: 2m);

            var p = Puanla("Ali", json);

            Assert.Equal(2m, p.Taban);
            Assert.Equal(0.5m, p.Olcutler[0].Cikti);
            Assert.Equal(0.49m, p.Olcutler[1].Cikti);
            Assert.Equal(1.4m, p.KisilikCarpani);
            Assert.Equal(1m, p.FikirCarpani);
            Assert.Equal(1m, p.RuhHaliCarpani);
            Assert.Equal(0.686m, p.Puan);
        }

        [Fact]
        public void Olcutsuz_ve_carpansiz_eylemin_puani_tabandir()
        {
            var p = Puanla("Ali", KararFikstur.Eylem("e", taban: 3.5m));

            Assert.Equal(3.5m, p.Puan);
            Assert.Empty(p.Olcutler);
        }

        [Fact]
        public void Olcut_girdisi_min_max_ile_sifir_bire_normallesir()
        {
            // cesaret 0.5, min 0.2 max 0.7 → 0.6
            var p = Puanla("Ali", Olcutlu(Olcut("kisilik(cesaret)", "\"min\": 0.2, \"max\": 0.7")));

            Assert.Equal(0.5m, p.Olcutler[0].Girdi);
            Assert.Equal(0.6m, p.Olcutler[0].Normal);
            Assert.Equal(0.6m, p.Olcutler[0].Cikti);
        }

        [Theory]
        [InlineData("\"min\": 0.6, \"max\": 1", "0")]
        [InlineData("\"min\": 0, \"max\": 0.4", "1")]
        public void Aralik_disindaki_girdi_kirpilir(string aralik, string beklenenNormal)
        {
            var p = Puanla("Ali", Olcutlu(Olcut("kisilik(cesaret)", aralik)));

            Assert.Equal(decimal.Parse(beklenenNormal, System.Globalization.CultureInfo.InvariantCulture), p.Olcutler[0].Normal);
        }

        [Fact]
        public void Dogru_yanlis_sonuc_bir_ve_sifir_sayilir()
        {
            var dogru = Puanla("Ali", Olcutlu(Olcut("ayni_mekanda(eylem.hedef)"), hedef: "npc"), "Veli");
            var yanlis = Puanla("Ali", Olcutlu(Olcut("modifier_var(eylem.hedef, minnet)"), hedef: "npc"), "Veli");

            Assert.Equal(1m, dogru.Olcutler[0].Girdi);
            Assert.Equal(1m, dogru.Puan);
            Assert.Equal(0m, yanlis.Olcutler[0].Girdi);
            Assert.Equal(0m, yanlis.Puan);
        }

        [Fact]
        public void Eğrisi_sifir_veren_olcut_puani_sifirlar()
        {
            var p = Puanla("Ali", Olcutlu(Olcut("kisilik(cesaret)", egri: "{ \"tur\": \"basamak\", \"esik\": 0.9 }") + ", " + Olcut("kisilik(durustluk)"), taban: 5m));

            Assert.Equal(0m, p.Puan);
        }

        [Fact]
        public void Lojistik_olcut_altin_degerle_uyusur()
        {
            // cesaret 0.5, lojistik k=10 m=0.5 → 0.5; durustluk 0.7, k=10 m=0.5 → 1/(1+e^-2) = 0.880797
            var p = Puanla("Ali", Olcutlu(
                Olcut("kisilik(durustluk)", egri: "{ \"tur\": \"lojistik\", \"k\": 10, \"m\": 0.5 }")));

            Assert.Equal(0.880797m, p.Olcutler[0].Cikti);
            Assert.Equal(0.880797m, p.Puan);
        }

        [Fact]
        public void Ara_degerler_ve_puan_6_ondaliga_yuvarlanir()
        {
            var p = Puanla("Ali", Olcutlu(Olcut("kisilik(durustluk)", egri: "{ \"tur\": \"lojistik\", \"k\": 7, \"m\": 0.3 }"), taban: 3.3m));

            Assert.Equal(System.Math.Round(p.Puan, 6), p.Puan);
            Assert.Equal(System.Math.Round(p.Olcutler[0].Cikti, 6), p.Olcutler[0].Cikti);
            Assert.Equal(System.Math.Round(p.Olcutler[0].Normal, 6), p.Olcutler[0].Normal);
        }

        // ---- çarpanlar ----

        [Fact]
        public void Kisilik_sinirlari_en_uc_degerlerde_bile_asilmaz()
        {
            var senaryo = SimFikstur.SenaryoJson
                .Replace("\"acgozluluk\": 0.5, \"durustluk\": 0.7, \"cesaret\": 0.5", "\"acgozluluk\": 0, \"durustluk\": 1, \"cesaret\": 0.5");
            var eylemler = KararFikstur.Yukle(Olcutlu(string.Empty, "\"kisilik_carpani\": { \"ozellikler\": [ { \"ozellik\": \"durustluk\", \"agirlik\": 100 }, { \"ozellik\": \"acgozluluk\", \"ters\": true } ] }").Replace("\"olcutler\": [  ], ", string.Empty));
            var sim = KararFikstur.Sim(SenaryoYukleyici.Yukle(senaryo, "s.json").Senaryo!, eylemler);

            var p = Puanlayici.Puanla(KararFikstur.Baglam(sim, "Ali"), new Aday(eylemler.Single(), string.Empty));

            Assert.InRange(p.KisilikCarpani, CarpanSiniri.KisilikAlt, CarpanSiniri.KisilikUst);
            Assert.Equal(2m, p.KisilikCarpani);
        }

        private static Simulasyon MinnetliSim(System.Collections.Generic.IReadOnlyList<EylemTanimi> eylemler, string senaryoJson = SimFikstur.SenaryoJson)
        {
            var senaryo = SenaryoYukleyici.Yukle(senaryoJson, "s.json").Senaryo!;
            var sim = Simulasyon.Olustur(senaryo, SimFikstur.Kurallar(SimFikstur.YardimKurali), 42, eylemler: eylemler);
            sim.Submit(new PlayerAction("yardim_etti", "Ali"));
            sim.Tick(1);
            return sim;
        }

        private const string FikirliSor = "\"fikir_carpani\": { \"yon\": \"artan\" }";

        [Fact]
        public void Fikir_carpani_hedefe_karsi_fikirden_gelir()
        {
            // Oyuncu yardım etti: Ali'nin oyuncuya karşı minnet modifier'ı (+15) var → t = 0.15 → 1.075
            var eylemler = KararFikstur.Yukle(KararFikstur.Eylem("sor", "oyuncu", FikirliSor));
            var sim = MinnetliSim(eylemler);

            var p = Puanlayici.Puanla(KararFikstur.Baglam(sim, "Ali"), new Aday(eylemler.Single(), "oyuncu"));

            Assert.Equal(1.075m, p.FikirCarpani);
        }

        [Fact]
        public void Azalan_yon_fikri_tersine_cevirir()
        {
            var eylemler = KararFikstur.Yukle(KararFikstur.Eylem("sor", "oyuncu", "\"fikir_carpani\": { \"yon\": \"azalan\" }"));
            var sim = MinnetliSim(eylemler);

            var p = Puanlayici.Puanla(KararFikstur.Baglam(sim, "Ali"), new Aday(eylemler.Single(), "oyuncu"));

            Assert.Equal(0.94m, p.FikirCarpani);
        }

        [Fact]
        public void Hedefsiz_ve_komsu_mekan_eyleminde_fikir_carpani_birdir()
        {
            var eylemler = KararFikstur.Yukle(KararFikstur.Eylem("selam"), KararFikstur.Eylem("git", "komsu_mekan"));
            var sim = MinnetliSim(eylemler);

            foreach (var a in new[] { new Aday(eylemler.Single(e => e.Id == "selam"), string.Empty), new Aday(eylemler.Single(e => e.Id == "git"), "pazar") })
            {
                Assert.Equal(1m, Puanlayici.Puanla(KararFikstur.Baglam(sim, "Ali"), a).FikirCarpani);
            }
        }

        [Fact]
        public void Minnet_yalnizca_o_hedefe_yonelik_adayin_puanini_degistirir_kisiligi_degil()
        {
            // Açgözlü Ali: iki aday, aynı kişilik çarpanı tanımı; biri minnet duyduğu oyuncuya, biri Veli'ye yönelik.
            var acgozlu = SimFikstur.SenaryoJson.Replace("\"acgozluluk\": 0.5, \"durustluk\": 0.7", "\"acgozluluk\": 0.95, \"durustluk\": 0.7");
            var ek = FikirliSor + ", \"kisilik_carpani\": { \"ozellikler\": [ { \"ozellik\": \"acgozluluk\" } ] }";
            var eylemler = KararFikstur.Yukle(KararFikstur.Eylem("sor", "oyuncu", ek), KararFikstur.Eylem("yardim", "npc", ek));
            var senaryo = SenaryoYukleyici.Yukle(acgozlu, "s.json").Senaryo!;
            var onceSim = Simulasyon.Olustur(senaryo, SimFikstur.Kurallar(SimFikstur.YardimKurali), 42, eylemler: eylemler);
            var sonraSim = MinnetliSim(eylemler, acgozlu);

            Aday Oyuncuya() => new Aday(eylemler.Single(e => e.Id == "sor"), "oyuncu");
            Aday Veliye() => new Aday(eylemler.Single(e => e.Id == "yardim"), "Veli");

            var oyuncuOnce = Puanlayici.Puanla(KararFikstur.Baglam(onceSim, "Ali"), Oyuncuya());
            var oyuncuSonra = Puanlayici.Puanla(KararFikstur.Baglam(sonraSim, "Ali"), Oyuncuya());
            var veliOnce = Puanlayici.Puanla(KararFikstur.Baglam(onceSim, "Ali"), Veliye());
            var veliSonra = Puanlayici.Puanla(KararFikstur.Baglam(sonraSim, "Ali"), Veliye());

            // Minnet: yalnızca oyuncuya yönelik adayın fikir çarpanı ve puanı değişti.
            Assert.Equal(1m, oyuncuOnce.FikirCarpani);
            Assert.Equal(1.075m, oyuncuSonra.FikirCarpani);
            Assert.True(oyuncuSonra.Puan > oyuncuOnce.Puan);

            // Genel kişilik ve başka hedefli aday değişmedi.
            Assert.Equal(oyuncuOnce.KisilikCarpani, oyuncuSonra.KisilikCarpani);
            Assert.Equal(veliOnce.KisilikCarpani, veliSonra.KisilikCarpani);
            Assert.Equal(1.9m, veliSonra.KisilikCarpani);
            Assert.Equal(veliOnce.FikirCarpani, veliSonra.FikirCarpani);
            Assert.Equal(veliOnce.Puan, veliSonra.Puan);
        }

        [Fact]
        public void Ruh_hali_carpani_baskin_duygunun_guncel_siddetinden_gelir()
        {
            var kural = @"{ ""kurallar"": [ { ""id"": ""yardim_duygusu"", ""ne_zaman"": ""yardim_etti"",
  ""etkiler"": [ { ""tur"": ""duygu"", ""duygu"": ""neseli"", ""siddet"": 0.5, ""hedef"": ""olay.fail"" } ] } ] }";
            var eylemler = KararFikstur.Yukle(KararFikstur.Eylem("selam", "yok", "\"ruh_hali_carpani\": { \"duygular\": [ { \"duygu\": \"neseli\" } ] }"));
            var senaryo = SimFikstur.Senaryo();
            var sim = Simulasyon.Olustur(senaryo, SimFikstur.Kurallar(kural), 42, eylemler: eylemler);
            sim.Submit(new PlayerAction("yardim_etti", "Ali"));
            sim.Tick(1);
            var baglam = KararFikstur.Baglam(sim, "Ali");
            var siddet = sim.Zihinler["Ali"].DuyguSiddeti("neseli", baglam.Tick);
            Assert.True(siddet > 0m);

            var p = Puanlayici.Puanla(baglam, new Aday(eylemler.Single(), string.Empty));

            Assert.Equal(Egriler.Yuvarla(1m + (0.25m * siddet)), p.RuhHaliCarpani);
            Assert.InRange(p.RuhHaliCarpani, 1m, CarpanSiniri.RuhHaliUst);
        }

        [Fact]
        public void Puanlama_dunyayi_ve_zihni_degistirmez()
        {
            var eylemler = KararFikstur.Yukle(Olcutlu(Olcut("kisilik(cesaret)")));
            var sim = KararFikstur.Sim(eylemler);
            var hash = sim.DurumHash();
            var sayi = sim.Gunluk.Sayi;

            Puanlayici.Puanla(KararFikstur.Baglam(sim, "Ali"), new Aday(eylemler.Single(), string.Empty));

            Assert.Equal(hash, sim.DurumHash());
            Assert.Equal(sayi, sim.Gunluk.Sayi);
        }
    }
}
