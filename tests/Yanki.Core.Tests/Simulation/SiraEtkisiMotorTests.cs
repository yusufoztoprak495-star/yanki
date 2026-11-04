using System.Linq;
using Xunit;
using Yanki.Core.Events;
using Yanki.Core.Mind;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;
using Yanki.Core.Tests.Decision;
using Kaydedici = Yanki.Core.Tests.Simulation.KonusmaAdimiTests.Kaydedici;

namespace Yanki.Core.Tests.Simulation
{
    /// <summary>
    /// Sıra etkisi, motor mekaniği (plan 7, Durum 1 / Durum 2): oyuncu Can'la söylentiden ÖNCE mi SONRA mı konuşuyor. Diyalog 8. haftada gelir; fark
    /// fikir, modifier ve <c>why</c> üzerinden ölçülür. Fikstür (Kasaba Hanı içeriği DEĞİLDİR): Ayse, Can, Zeki han'da; kural sırası aynı, eylem sırası farklı.
    /// </summary>
    public class SiraEtkisiMotorTests
    {
        private const string Borc = "borclu(Mehmet,Ayse,500)";

        private const string Kurallar = @"{ ""kurallar"": [
  { ""id"": ""aile_iftirasi"", ""ne_zaman"": ""anlatti"",
    ""kriterler"": [ { ""alan"": ""olay_konusu(borclu(Mehmet, *, *))"", ""op"": ""=="", ""deger"": true } ],
    ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""oyuncu"", ""modifier"": ""aileme_iftira"", ""deger"": -22, ""sure"": 100 } ] },
  { ""id"": ""yardim_minnet"", ""ne_zaman"": ""haber"",
    ""kriterler"": [ { ""alan"": ""olay_rolu(hedef)"", ""op"": ""=="", ""deger"": true }, { ""alan"": ""modifier_var(olay.fail, aileme_iftira)"", ""op"": ""=="", ""deger"": false } ],
    ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""minnet"", ""deger"": 15, ""sure"": 100 } ] },
  { ""id"": ""yardim_supheli"", ""ne_zaman"": ""haber"",
    ""kriterler"": [ { ""alan"": ""olay_rolu(hedef)"", ""op"": ""=="", ""deger"": true }, { ""alan"": ""modifier_var(olay.fail, aileme_iftira)"", ""op"": ""=="", ""deger"": true } ],
    ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""supheli"", ""deger"": -10, ""sure"": 100 } ] }
] }";

        // "haber": oyuncunun Can'a yaptığı iyilik (senaryo olay türü).
        private static Simulasyon Kur()
        {
            var senaryo = SenaryoYukleyici.Yukle(KonusmaAdimiTests.Senaryo(), "s.json").Senaryo!;
            var kural = KuralYukleyici.Yukle(Kurallar, "k.json", senaryo.OlayTurleri);
            Assert.True(kural.Basarili, string.Join("\n", kural.Hatalar));
            var anlat = "{ \"id\": \"anlat\", \"olay_turu\": \"anlatti\", \"hedef\": \"npc\", \"taban\": 1, \"aktarim\": \"dogru\", \"konu_desenleri\": [\"borclu(*,*,*)\"] }";
            var secici = new Kaydedici().Sec("Ayse", 2, "anlat|Can");
            return Simulasyon.Olustur(
                senaryo, kural.Kurallar, 42, new ZihinAyarlari { KanitSonumuTickBasina = 0m }, eylemler: KararFikstur.Yukle(anlat), secici: secici);
        }

        private static void Soylenti(Simulasyon s) => s.Submit(OyuncuAnlatimiTests.Anlat("Ayse", Borc));

        private static void Iyilik(Simulasyon s) => s.Submit(new PlayerAction("haber", "Can"));

        private static Olay[] Modifierlar(Simulasyon s, string tur) =>
            s.Gunluk.Olaylar.Where(o => o.Tur == "modifier_eklendi" && o.Deger("npc") == "Can" && o.Deger("tur") == tur).ToArray();

        [Fact]
        public void Durum_1_oyuncu_Can_la_soylentiden_once_konusur_Can_minnet_duyar()
        {
            var s = Kur();
            Soylenti(s);
            Iyilik(s);          // aynı tick: Ayşe söylentiyi daha Can'a anlatmadı (tick 2'de anlatır)
            s.Tick(6);

            Assert.Single(Modifierlar(s, "minnet"));
            Assert.Empty(Modifierlar(s, "supheli"));
            Assert.Single(Modifierlar(s, "aileme_iftira"));                // söylenti sonradan yine ulaşır
        }

        [Fact]
        public void Durum_2_oyuncu_Can_la_soylentiden_sonra_konusur_Can_supheli_olur()
        {
            var s = Kur();
            Soylenti(s);
            s.Tick(4);          // t2: Ayşe Can'a anlatır, t3: kural modifier'ı yazar
            Iyilik(s);
            s.Tick(3);

            Assert.Empty(Modifierlar(s, "minnet"));
            var supheli = Assert.Single(Modifierlar(s, "supheli"));
            Assert.Single(Modifierlar(s, "aileme_iftira"));

            // why: şüphe modifier'ı oyuncunun iyiliğine iner.
            var zincir = s.Neden(supheli.Id);
            Assert.Contains(zincir, o => o.Tur == "haber" && o.KuralId == "player");
        }

        [Fact]
        public void Ayni_iki_eylem_farkli_sirada_Can_in_fikrini_ve_modifier_kumesini_ayirir()
        {
            var once = Kur();
            Soylenti(once);
            Iyilik(once);
            once.Tick(8);

            var sonra = Kur();
            Soylenti(sonra);
            sonra.Tick(4);
            Iyilik(sonra);
            sonra.Tick(4);

            Assert.Equal(8, once.SimdikiTick);
            Assert.Equal(8, sonra.SimdikiTick);
            var a = once.Zihinler["Can"].Fikir("oyuncu", 8);
            var b = sonra.Zihinler["Can"].Fikir("oyuncu", 8);
            Assert.NotEqual(a, b);                       // aynı eylemler, aynı seed, farklı sıra: fikir ölçülebilir biçimde farklı
            Assert.NotEqual(once.SonHash, sonra.SonHash);
        }
    }
}
