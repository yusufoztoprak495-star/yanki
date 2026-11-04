using System;
using System.Linq;
using Xunit;
using Yanki.Core.Brakes;
using Yanki.Core.Decision;
using Yanki.Core.Events;
using Yanki.Core.Mind;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;
using Yanki.Core.Tests.Decision;
using Yanki.Core.Tests.Simulation;

namespace Yanki.Core.Tests.Brakes
{
    /// <summary>
    /// 7. hafta (E): yay değişkenleri. Fikstür: Ali ve Veli han'da; <c>g</c> değişkeni 0..3, başlangıç 0 (ikinci değişken <c>h</c> 5..9, başlangıç 7).
    /// <c>yay_degisti</c> ve <c>fren_uygulandi</c> zihin olayı değildir: algılanmaz, kural tetiklemez.
    /// </summary>
    public class YayDegiskenleriTests
    {
        private const string Yay = "\"gercekler\": [], \"yay_degiskenleri\": { \"g\": { \"baslangic\": 0, \"min\": 0, \"max\": 3 }, \"h\": { \"baslangic\": 7, \"min\": 5, \"max\": 9 } },";

        private static string YayEtkisi(string degisken = "g", int delta = 1) => "{ \"tur\": \"yay\", \"degisken\": \"" + degisken + "\", \"delta\": " + delta + " }";

        private static string Kural(string id, string kriter, string etkiler) =>
            "{ \"id\": \"" + id + "\", \"ne_zaman\": \"yardim_etti\", " + (kriter.Length > 0 ? "\"kriterler\": [ " + kriter + " ], " : string.Empty) + "\"etkiler\": [ " + etkiler + " ] }";

        private static Simulasyon Kur(string[] kurallar, string[]? eylemler = null, string yay = Yay)
        {
            var senaryo = SenaryoYukleyici.Yukle(SimFikstur.SenaryoJson.Replace("\"gercekler\": [],", yay), "s.json");
            Assert.True(senaryo.Basarili, string.Join("\n", senaryo.Hatalar));
            var kur = KuralYukleyici.Yukle("{ \"kurallar\": [ " + string.Join(", ", kurallar) + " ] }", "k.json", senaryo.Senaryo!.OlayTurleri);
            Assert.True(kur.Basarili, string.Join("\n", kur.Hatalar));
            return Simulasyon.Olustur(senaryo.Senaryo!, kur.Kurallar, 42, eylemler: eylemler == null ? null : KararFikstur.Yukle(eylemler));
        }

        private static readonly PlayerAction Y = new PlayerAction("yardim_etti", "Ali");

        private static Olay[] Degisimler(Simulasyon s) => s.Gunluk.Olaylar.Where(o => o.Tur == "yay_degisti").ToArray();

        [Fact]
        public void Yalniz_yay_etkili_kuralda_cooldown_uygulanir_sayac_gunlukten_dogru_NPC_ile_turer()
        {
            var s = Kur(new[] { "{ \"id\": \"r\", \"ne_zaman\": \"yardim_etti\", \"cooldown\": 10, \"etkiler\": [ " + YayEtkisi("h") + " ] }" });
            for (var t = 0; t < 4; t++)
            {
                s.Submit(Y);
                s.Tick(1);
            }

            Assert.Equal(2, Degisimler(s).Length);                                    // Ali ve Veli birer kez; sonraki tick'ler cooldown'da
            Assert.NotEmpty(s.Frenler("cooldown"));
            Assert.DoesNotContain("SAYAC|r||", s.SayaclarKanonikMetni);            // NPC alanı boş sayaç yok
        }

        [Fact]
        public void Modifier_ve_yay_etkili_kuralda_tek_tetikleme_bir_kez_sayilir_en_fazla_kapsam_kural()
        {
            var s = Kur(new[]
            {
                "{ \"id\": \"r\", \"ne_zaman\": \"yardim_etti\", \"kapsam\": \"kural\", \"en_fazla\": 3, \"etkiler\": [ { \"tur\": \"modifier\", \"hedef\": \"olay.fail\", \"modifier\": \"m\", \"deger\": 1, \"sure\": 100 }, " + YayEtkisi("h") + " ] }",
            });
            s.Submit(Y);
            s.Tick(2);
            Assert.Empty(s.Frenler("en_fazla"));                                       // ilk eylem: Ali ve Veli = 2 tetikleme (sınır 3)

            s.Submit(Y);
            s.Tick(2);

            // Her tetikleme bir kez sayılırsa üçüncü tetikleme (Ali ya da Veli) çalışır, dördüncüsü en_fazla ile kesilir: 3 yay değişimi, 1 fren.
            // Modifier ve yay olayı iki kez sayılırsa (hatalı) sayaç ilk eylemde 4'e çıkar, ikinci eylemde hiçbiri çalışmaz: 2 değişim.
            Assert.Equal(3, Degisimler(s).Length);
            Assert.Single(s.Frenler("en_fazla"));
        }

        [Fact]
        public void Baslangic_degeri_senaryodandir_Yay_sorgusu_okur()
        {
            var s = Kur(new[] { Kural("r", string.Empty, YayEtkisi()) });
            Assert.Equal(0m, s.Yay("g"));
            Assert.Equal(7m, s.Yay("h"));
            Assert.Throws<ArgumentException>(() => s.Yay("yok"));
        }

        [Fact]
        public void Kural_etkisi_yay_degisti_olayi_yazar_nedeni_tetikleyicidir_why_zincire_iner()
        {
            var s = Kur(new[] { Kural("r", string.Empty, YayEtkisi()) });
            s.Submit(Y);
            s.Tick(1);

            var olaylar = Degisimler(s);
            Assert.Equal(2, olaylar.Length);                     // Ali ve Veli birer kez tetikledi
            Assert.Equal(new[] { "1", "2" }, olaylar.Select(o => o.Deger("deger")).ToArray());
            Assert.All(olaylar, o =>
            {
                Assert.Equal("r", o.KuralId);
                Assert.Equal("g", o.Deger("degisken"));
                Assert.Equal("1", o.Deger("delta"));
            });
            Assert.Equal(2m, s.Yay("g"));
            Assert.Contains(s.Neden(olaylar[0].Id), o => o.Tur == "yardim_etti" && o.KuralId == "player");
        }

        [Fact]
        public void Yay_degisti_ve_fren_uygulandi_zihin_olayi_degildir_algilanmaz_kural_tetiklemez()
        {
            Assert.False(ZihinOlaylari.ZihinOlayiMi(YayDegiskenleri.OlayTuru));
            Assert.False(ZihinOlaylari.ZihinOlayiMi(FrenOlayi.Tur));

            var s = Kur(new[] { Kural("r", string.Empty, YayEtkisi()) });
            s.Submit(Y);
            s.Tick(4);

            var ids = Degisimler(s).Select(o => o.Id).ToArray();
            Assert.NotEmpty(ids);
            Assert.DoesNotContain(s.Gunluk.Olaylar, o => o.NedenOlaylar.Any(n => ids.Contains(n)));   // hiçbir olay yay_degisti'den doğmaz (algı da kural da)
        }

        [Fact]
        public void Ne_zaman_yay_degisti_dogrulayicida_reddedilir()
        {
            var json = "{ \"kurallar\": [ { \"id\": \"x\", \"ne_zaman\": \"yay_degisti\", \"etkiler\": [ " + YayEtkisi() + " ] } ] }";
            var sonuc = KuralYukleyici.Yukle(json, "k.json", SimFikstur.Senaryo().OlayTurleri);
            Assert.False(sonuc.Basarili);
            Assert.Contains(sonuc.Hatalar, h => h.Mesaj.Contains("yay_degisti") && h.Mesaj.Contains("ne_zaman olamaz"));
        }

        [Fact]
        public void Sinira_kirpilir_ve_yay_siniri_freni_yazilir()
        {
            var s = Kur(new[] { Kural("r", string.Empty, YayEtkisi(delta: 2)) });
            s.Submit(Y);
            s.Tick(1);                                          // 0 → 2 (Ali), 2 → 3 (Veli, kırpıldı)

            Assert.Equal(3m, s.Yay("g"));
            var olaylar = Degisimler(s);
            Assert.Equal(new[] { "2", "3" }, olaylar.Select(o => o.Deger("deger")).ToArray());
            Assert.Equal("2", olaylar[1].Deger("delta"));        // olay istenen deltayı taşır, deger kırpılmış yeni değeri
            var fren = Assert.Single(s.Frenler("yay_siniri"));
            Assert.Equal(("r", "g", "Veli"), (fren.Deger("kimlik"), fren.Deger("hedef"), fren.Deger("npc")));
            Assert.Equal(new[] { olaylar[1].NedenOlaylar[0] }, fren.NedenOlaylar.ToArray());
        }

        [Fact]
        public void Negatif_delta_alt_sinira_kirpilir()
        {
            var s = Kur(new[] { Kural("r", string.Empty, YayEtkisi("h", -5)) });
            s.Submit(Y);
            s.Tick(1);

            Assert.Equal(5m, s.Yay("h"));                        // 7 → 5 (Ali, kırpılmadan −2... aralık 5..9) → 5 (Veli kırpılır)
            Assert.NotEmpty(s.Frenler("yay_siniri"));
        }

        [Fact]
        public void Kriter_adim_basi_tek_goruntuyu_okur_birden_cok_delta_sabit_sirayla_toplanir()
        {
            var sifirsa = "{ \"alan\": \"yay(g)\", \"op\": \"==\", \"deger\": 0 }";
            var s = Kur(new[] { Kural("a", sifirsa, YayEtkisi()), Kural("b", sifirsa, YayEtkisi()) });
            s.Submit(Y);
            s.Tick(1);

            // a'nın çıktısı b'nin görüntüsünü değiştirmez: ikisi de 0 görüp çalışır (2 NPC × 2 kural = 4 delta, 3'e kırpılır).
            Assert.Equal(4, Degisimler(s).Length);
            Assert.Equal(3m, s.Yay("g"));
        }

        [Fact]
        public void Kriter_yay_degerine_gore_tutar_aralik_ile_kurulur()
        {
            var iki = "{ \"alan\": \"yay(g)\", \"op\": \">=\", \"deger\": 2 }";
            var s = Kur(new[] { Kural("artir", string.Empty, YayEtkisi()), Kural("esikli", iki, "{ \"tur\": \"modifier\", \"hedef\": \"olay.fail\", \"modifier\": \"gergin\", \"deger\": -5, \"sure\": 50 }") });
            s.Submit(Y);
            s.Tick(1);
            Assert.DoesNotContain(s.Gunluk.Olaylar, o => o.KuralId == "esikli");   // adım başında g = 0

            s.Submit(Y);
            s.Tick(1);                                                              // adım başında g = 2
            Assert.Contains(s.Gunluk.Olaylar, o => o.KuralId == "esikli");
        }

        [Fact]
        public void Eylem_etkisi_yay_yazar_on_kosul_yay_okur()
        {
            var eylem = KararFikstur.Eylem("tartis", "yok", "\"on_kosullar\": [ { \"alan\": \"yay(g)\", \"op\": \"<\", \"deger\": 3 } ]", 1m, "[ " + YayEtkisi() + " ]");
            var s = Kur(new string[] { Kural("bos", string.Empty, "{ \"tur\": \"hafiza\", \"onem\": 0.1 }") }, new[] { eylem });
            s.Tick(6);

            Assert.Equal(3m, s.Yay("g"));                                           // g 3'e ulaşınca ön koşul tutmaz: eylemler durur
            Assert.All(Degisimler(s), o => Assert.StartsWith("eylem:tartis", o.KuralId));
            Assert.Equal(3, Degisimler(s).Length);
            Assert.Empty(s.Frenler("yay_siniri"));
        }

        [Fact]
        public void Yay_durum_hashine_girer_degiskeni_olmayan_senaryoda_kanonik_metin_ayni_kalir()
        {
            var s = Kur(new[] { Kural("r", string.Empty, YayEtkisi()) });
            var once = s.DurumHash();
            s.Submit(Y);
            s.Tick(1);
            Assert.NotEqual(once, s.DurumHash());

            var yok = SimFikstur.Yap();
            var baslangic = yok.DurumHash();
            Assert.Equal(baslangic, SimFikstur.Yap().DurumHash());                  // YAY satırı yok: eski hash
        }

        [Fact]
        public void Bilinmeyen_degisken_Olustur_da_reddedilir()
        {
            var senaryo = SenaryoYukleyici.Yukle(SimFikstur.SenaryoJson.Replace("\"gercekler\": [],", Yay), "s.json").Senaryo!;
            KuralTanimi[] Yukle(string kural) => KuralYukleyici.Yukle("{ \"kurallar\": [ " + kural + " ] }", "k.json", senaryo.OlayTurleri).Kurallar.ToArray();

            Assert.Throws<ArgumentException>(() => Simulasyon.Olustur(senaryo, Yukle(Kural("r", string.Empty, YayEtkisi("yok"))), 1));
            Assert.Throws<ArgumentException>(() => Simulasyon.Olustur(senaryo, Yukle(Kural("r", "{ \"alan\": \"yay(yok)\", \"op\": \">\", \"deger\": 0 }", YayEtkisi())), 1));
            var eylem = KararFikstur.Yukle(KararFikstur.Eylem("e", "yok", "\"on_kosullar\": [ { \"alan\": \"yay(yok)\", \"op\": \">\", \"deger\": 0 } ]"));
            Assert.Throws<ArgumentException>(() => Simulasyon.Olustur(senaryo, new KuralTanimi[0], 1, eylemler: eylem));

            var tanimsizSenaryo = SimFikstur.Senaryo();
            Assert.Throws<ArgumentException>(() => Simulasyon.Olustur(tanimsizSenaryo, Yukle(Kural("r", string.Empty, YayEtkisi())), 1));
        }

        [Theory]
        [InlineData("{ \"g\": { \"baslangic\": 5, \"min\": 0, \"max\": 3 } }")]
        [InlineData("{ \"g\": { \"baslangic\": 0, \"min\": 4, \"max\": 3 } }")]
        [InlineData("{ \"g\": { \"baslangic\": 0, \"min\": 0 } }")]
        [InlineData("{ \"g\": { \"baslangic\": 0, \"min\": 0, \"max\": 3, \"fazla\": 1 } }")]
        [InlineData("{ \"g\": 5 }")]
        [InlineData("[ 1 ]")]
        public void Senaryo_dogrulayicisi_gecersiz_yay_tanimini_reddeder(string yay)
        {
            var sonuc = SenaryoYukleyici.Yukle(SimFikstur.SenaryoJson.Replace("\"gercekler\": [],", "\"gercekler\": [], \"yay_degiskenleri\": " + yay + ","), "s.json");
            Assert.False(sonuc.Basarili);
        }

        [Theory]
        [InlineData("{ \"tur\": \"yay\", \"degisken\": \"g\", \"delta\": 0 }")]
        [InlineData("{ \"tur\": \"yay\", \"degisken\": \"g\" }")]
        [InlineData("{ \"tur\": \"yay\", \"delta\": 1 }")]
        [InlineData("{ \"tur\": \"yay\", \"degisken\": \"g\", \"delta\": 1, \"hedef\": \"ben\" }")]
        public void Kural_yay_etkisi_gecersizse_reddedilir(string etki)
        {
            var sonuc = KuralYukleyici.Yukle("{ \"kurallar\": [ " + Kural("r", string.Empty, etki) + " ] }", "k.json", SimFikstur.Senaryo().OlayTurleri);
            Assert.False(sonuc.Basarili);
        }

        [Fact]
        public void Kriz_satirinda_yay_kriteri_ve_yay_etkisi_reddedilir()
        {
            var kriter = "{ \"id\": \"k\", \"seviye\": 1, \"olay_turu\": \"itiraf_etti\", \"hedef\": \"yok\", \"kriterler\": [ { \"alan\": \"yay(g)\", \"op\": \">\", \"deger\": 0 } ] }";
            var etki = "{ \"id\": \"k\", \"seviye\": 1, \"olay_turu\": \"itiraf_etti\", \"hedef\": \"yok\", \"etkiler\": [ " + YayEtkisi() + " ] }";
            Assert.False(KrizYukleyici.Yukle("{ \"krizler\": [ " + kriter + " ] }", "z.json").Basarili);
            Assert.False(KrizYukleyici.Yukle("{ \"krizler\": [ " + etki + " ] }", "z.json").Basarili);
        }
    }
}
