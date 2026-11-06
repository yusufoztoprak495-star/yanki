using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Authoring;
using Yanki.Core.Decision;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;

namespace Yanki.Scenarios.Tests
{
    /// <summary>
    /// 9. hafta (Yusuf, DURAK 2 madde 2): eylem şemasına isteğe bağlı <c>aktorler</c>. Kişilik davranışı fiyatlandırır, kimlik belirlemez.
    /// Alan yokken davranış ve hash aynı kalır (dört ana golden de bunu ayrıca kilitler).
    /// </summary>
    public class AktorlerTests
    {
        private const string Kisilik = "{ \"acgozluluk\": 0.5, \"durustluk\": 0.5, \"cesaret\": 0.5, \"sosyallik\": 0.5, \"kin_tutma\": 0.5, \"merhamet\": 0.5 }";

        private static readonly string Senaryo =
            "{ \"surum\": 1, \"ad\": \"A\", \"mekanlar\": [\"han\"], \"baglantilar\": [], \"oyuncu\": { \"id\": \"oyuncu\", \"konum\": \"han\" },"
            + " \"npcler\": [ { \"id\": \"Ali\", \"konum\": \"han\", \"kisilik\": " + Kisilik + " }, { \"id\": \"Can\", \"konum\": \"han\", \"kisilik\": " + Kisilik + " },"
            + " { \"id\": \"Veli\", \"konum\": \"han\", \"kisilik\": " + Kisilik + " } ],"
            + " \"gercekler\": [], \"olay_turleri\": [], \"kural_dosyalari\": [\"k.json\"], \"eylem_dosyalari\": [\"e.json\"] }";

        private const string Kurallar = "{ \"kurallar\": [] }";

        private static string Eylem(string aktorler) =>
            "{ \"eylemler\": [ { \"id\": \"ozel\", \"olay_turu\": \"ozel_oldu\", \"hedef\": \"yok\", \"taban\": 5" + aktorler
            + ", \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.3 } ] },"
            + " { \"id\": \"dinlen\", \"olay_turu\": \"dinlendi\", \"hedef\": \"yok\", \"taban\": 0.3, \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.2 } ] } ] }";

        private static Simulasyon Kur(string aktorler, ulong seed = 42)
        {
            var s = SenaryoYukleyici.Yukle(Senaryo, "s.json");
            Assert.True(s.Basarili, string.Join("\n", s.Hatalar));
            var e = EylemYukleyici.Yukle(Eylem(aktorler), "e.json");
            Assert.True(e.Basarili, string.Join("\n", e.Hatalar));
            var k = KuralYukleyici.Yukle(Kurallar, "k.json", new[] { "ozel_oldu" });
            return Simulasyon.Olustur(s.Senaryo!, k.Kurallar, seed, eylemler: e.Eylemler);
        }

        [Fact]
        public void Yukleyici_aktorleri_okur_siralar_ve_bos_ya_da_tekrarli_diziyi_reddeder()
        {
            var ok = EylemYukleyici.Yukle(Eylem(", \"aktorler\": [\"Can\", \"Ali\"]"), "e.json");
            Assert.True(ok.Basarili);
            Assert.Equal(new[] { "Ali", "Can" }, ok.Eylemler.Single(x => x.Id == "ozel").Aktorler.ToArray());
            Assert.Empty(ok.Eylemler.Single(x => x.Id == "dinlen").Aktorler);

            Assert.Contains(EylemYukleyici.Yukle(Eylem(", \"aktorler\": []"), "e.json").Hatalar, h => h.Mesaj.Contains("aktorler", StringComparison.Ordinal));
            Assert.Contains(EylemYukleyici.Yukle(Eylem(", \"aktorler\": [\"Ali\", \"Ali\"]"), "e.json").Hatalar, h => h.Mesaj.Contains("tekrar", StringComparison.Ordinal));
        }

        [Fact]
        public void Aktor_kisitli_eylemi_yalnizca_listedeki_npc_secer()
        {
            var sim = Kur(", \"aktorler\": [\"Ali\"]");
            sim.Tick(40);

            var failler = sim.Gunluk.Olaylar.Where(o => o.Tur == "ozel_oldu").Select(o => o.Fail).Distinct().ToArray();
            Assert.Equal(new[] { "Ali" }, failler);

            // Kısıt yokken diğerleri de yapar (taban 5 baskındır): kısıt gerçekten daraltıyor.
            var serbest = Kur(string.Empty);
            serbest.Tick(40);
            Assert.True(serbest.Gunluk.Olaylar.Where(o => o.Tur == "ozel_oldu").Select(o => o.Fail).Distinct().Count() > 1);
        }

        [Fact]
        public void Tum_npcleri_listelemek_alan_yazmamakla_ayni_son_hash_ve_gunluk_hash_verir()
        {
            var yok = Kur(string.Empty);
            var hepsi = Kur(", \"aktorler\": [\"Ali\", \"Can\", \"Veli\"]");
            yok.Tick(40);
            hepsi.Tick(40);

            Assert.Equal(yok.DurumHash(), hepsi.DurumHash());
            Assert.Equal(GunlukHashu.Hesapla(yok.Gunluk), GunlukHashu.Hesapla(hepsi.Gunluk));
        }

        [Fact]
        public void Senaryoda_olmayan_ya_da_npc_olmayan_aktor_olustururken_reddedilir()
        {
            var s = SenaryoYukleyici.Yukle(Senaryo, "s.json").Senaryo!;
            foreach (var kimlik in new[] { "Yok", "oyuncu", "han" })
            {
                var e = EylemYukleyici.Yukle(Eylem(", \"aktorler\": [\"" + kimlik + "\"]"), "e.json");
                var k = KuralYukleyici.Yukle(Kurallar, "k.json", new[] { "ozel_oldu" });
                Assert.Throws<ArgumentException>(() => Simulasyon.Olustur(s, k.Kurallar, 42UL, eylemler: e.Eylemler));
            }
        }

        [Fact]
        public void Validate_senaryoda_olmayan_aktoru_eylemin_dosya_satiriyla_hata_sayar()
        {
            var girdi = new DogrulamaGirdisi(
                "x/scenario.json", Senaryo,
                new List<DosyaGirdisi> { new DosyaGirdisi("k.json", "x/k.json", Kurallar), new DosyaGirdisi("e.json", "x/e.json", Eylem(", \"aktorler\": [\"Veli\", \"Yok\"]")) },
                null, null);

            var sonuc = SenaryoDogrulayici.Dogrula(girdi);

            var hata = Assert.Single(sonuc.Bulgular, b => b.Hata);
            Assert.Equal(("x/e.json", "eylem", "ozel"), (hata.Dosya, hata.Tur, hata.Kimlik));
            Assert.Contains("Yok", hata.Mesaj, StringComparison.Ordinal);
            Assert.True(hata.Satir > 0);
        }
    }
}
