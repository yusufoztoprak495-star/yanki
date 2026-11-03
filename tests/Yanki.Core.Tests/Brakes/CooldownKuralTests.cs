using System.Linq;
using Xunit;
using Yanki.Core.Brakes;
using Yanki.Core.Events;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;
using Yanki.Core.Tests.Simulation;

namespace Yanki.Core.Tests.Brakes
{
    /// <summary>
    /// 7. hafta (B): kural cooldown ve tetiklenme sınırları. Fikstür: Ali ve Veli han'da (oyuncu da), Can pazar'da; her <c>yardim_etti</c>
    /// Ali ve Veli'yi tetikler. "Tetiklendi" = bütün kriterler tuttu ve en az bir etki üretildi.
    /// </summary>
    public class CooldownKuralTests
    {
        private static readonly PlayerAction Y = new PlayerAction("yardim_etti", "Ali");

        private static string Kural(string ek, string? etkiler = null) =>
            @"{ ""kurallar"": [ { ""id"": ""r"", ""ne_zaman"": ""yardim_etti"", " + (ek.Length > 0 ? ek + ", " : string.Empty) + @"
  ""etkiler"": " + (etkiler ?? @"[ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""minnet"", ""deger"": 15, ""sure"": 1000 } ]") + " } ] }";

        private static Simulasyon Yap(string ek, string? etkiler = null, string? senaryoJson = null)
        {
            var senaryo = senaryoJson == null ? SimFikstur.Senaryo() : SenaryoYukleyici.Yukle(senaryoJson, "s.json").Senaryo!;
            var sonuc = KuralYukleyici.Yukle(Kural(ek, etkiler), "k.json", senaryo.OlayTurleri);
            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            return Simulasyon.Olustur(senaryo, sonuc.Kurallar, 42);
        }

        private static Olay[] Mod(Simulasyon s, long tick) =>
            s.Gunluk.Olaylar.Where(o => o.KuralId == "r" && o.Tick == tick).ToArray();

        private static Olay[] Fren(Simulasyon s, string tur, long? tick = null) =>
            s.Gunluk.Olaylar.Where(o => o.Tur == "fren_uygulandi" && o.Deger("fren") == tur && (tick == null || o.Tick == tick)).ToArray();

        [Fact]
        public void Cooldown_24_t10da_calisan_kural_t33te_kapali_t34te_acik()
        {
            var s = Yap("\"cooldown\": 24");
            s.Tick(9);
            s.Submit(Y);
            s.Tick(1);                                 // t=10
            Assert.Equal(2, Mod(s, 10).Length);        // Ali ve Veli

            s.Tick(22);                                // t=32
            s.Submit(Y);
            s.Tick(1);                                 // t=33: kapalı
            Assert.Empty(Mod(s, 33));
            var fren = Fren(s, "cooldown", 33);
            Assert.Equal(new[] { "Ali", "Veli" }, fren.Select(f => f.Deger("npc")).ToArray());
            Assert.All(fren, f => Assert.Equal("r", f.Deger("kimlik")));

            s.Submit(Y);
            s.Tick(1);                                 // t=34: açık
            Assert.Equal(2, Mod(s, 34).Length);
        }

        [Fact]
        public void Cooldown_1_ayni_tickte_ikinci_tetigi_engeller_sonraki_tick_acar()
        {
            var s = Yap("\"cooldown\": 1");
            s.Submit(Y);
            s.Submit(Y);
            s.Tick(1);
            Assert.Equal(2, Mod(s, 1).Length);                 // ilk tetik: Ali + Veli
            Assert.Equal(2, Fren(s, "cooldown", 1).Length);    // ikinci tetik: ikisi de kapalı

            s.Submit(Y);
            s.Tick(1);
            Assert.Equal(2, Mod(s, 2).Length);
        }

        [Fact]
        public void Kapsam_kural_tum_npcler_icin_tek_sayac_kapsam_npc_npc_basina()
        {
            var kural = Yap("\"cooldown\": 10, \"cooldown_kapsam\": \"kural\"");
            kural.Submit(Y);
            kural.Tick(1);
            Assert.Equal(new[] { "Ali" }, Mod(kural, 1).Select(o => o.Deger("npc")).ToArray());    // ordinal ilk NPC
            Assert.Equal(new[] { "Veli" }, Fren(kural, "cooldown", 1).Select(o => o.Deger("npc")).ToArray());

            var npc = Yap("\"cooldown\": 10, \"cooldown_kapsam\": \"npc\"");
            npc.Submit(Y);
            npc.Tick(1);
            Assert.Equal(2, Mod(npc, 1).Length);
            Assert.Empty(Fren(npc, "cooldown"));
        }

        [Fact]
        public void Kapsam_varsayilani_npctir()
        {
            var s = Yap("\"cooldown\": 10");
            s.Submit(Y);
            s.Tick(1);
            Assert.Equal(2, Mod(s, 1).Length);
        }

        [Fact]
        public void Tick_basina_en_fazla_ayni_tickte_cok_tetigi_sinirlar()
        {
            var s = Yap("\"tick_basina_en_fazla\": 2");
            s.Submit(Y);
            s.Submit(Y);
            s.Submit(Y);
            s.Tick(1);

            Assert.Equal(4, Mod(s, 1).Length);                          // Ali 2, Veli 2
            Assert.Equal(2, Fren(s, "tick_siniri", 1).Length);          // üçüncü tetik: Ali, Veli

            s.Submit(Y);
            s.Tick(1);
            Assert.Equal(2, Mod(s, 2).Length);                          // yeni tick: sayaç tick başına sıfırlanır
        }

        [Fact]
        public void En_fazla_tam_sinirda_durur_ve_200_tick_sonra_da_sayilir()
        {
            var s = Yap("\"en_fazla\": 2");
            for (var i = 0; i < 2; i++)
            {
                s.Submit(Y);
                s.Tick(1);
                Assert.Equal(2, Mod(s, i + 1).Length);
            }

            s.Submit(Y);
            s.Tick(1);
            Assert.Empty(Mod(s, 3));
            Assert.Equal(2, Fren(s, "en_fazla", 3).Length);

            s.Tick(250);                                   // pencere yok: sınır hiç unutulmaz
            s.Submit(Y);
            s.Tick(1);
            Assert.Empty(Mod(s, 254));
            Assert.Equal(2, Fren(s, "en_fazla", 254).Length);
        }

        [Fact]
        public void Etkisiz_tetik_sayilmaz_yalnizca_etki_uretilen_tetikleme_sayilir()
        {
            // Aynı gerçeği ekleyen kural: ilk tetikte gerçek yazılır (tetiklendi), ikincisinde zaten var (etki üretilmez: tetiklenmedi).
            var s = Yap("\"en_fazla\": 1", @"[ { ""tur"": ""gercek"", ""islem"": ""ekle"", ""gercek"": ""yardim_gordu({olay.fail})"" } ]");
            s.Submit(Y);
            s.Tick(1);
            s.Submit(Y);
            s.Tick(1);

            Assert.Empty(Fren(s, "en_fazla"));   // ikinci tetik etkisizdi: sınıra takılacak bir tetikleme yok
        }

        [Fact]
        public void Genel_sinir_M_tam_sinirda_tetiklemeyi_kabul_eder_asani_dusurur_ertelemez()
        {
            var json = SimFikstur.SenaryoJson.Replace("\"gercekler\": [],", "\"gercekler\": [], \"frenler\": { \"tick_basina_en_fazla_sonuc\": 2 },");
            var s = Yap("\"cooldown\": 1000", senaryoJson: json);
            s.Submit(Y);
            s.Tick(1);
            Assert.Equal(2, Mod(s, 1).Length);                 // 2 çıktı = M: kabul
            Assert.Empty(Fren(s, "genel_sinir"));

            var t = Yap("\"tick_basina_en_fazla\": 5", senaryoJson: json);
            t.Submit(Y);
            t.Submit(Y);
            t.Tick(1);
            Assert.Equal(2, Mod(t, 1).Length);                 // ikinci tetik: M aşılır
            Assert.Equal(2, Fren(t, "genel_sinir", 1).Length);

            t.Tick(1);                                         // ertelenmez: sonraki tick'te kuyruk yok
            Assert.Empty(Mod(t, 2));
        }

        [Fact]
        public void Genel_sinir_kuralin_etkilerini_bolmez_kesim_tetikleme_sinirindadir()
        {
            var json = SimFikstur.SenaryoJson.Replace("\"gercekler\": [],", "\"gercekler\": [], \"frenler\": { \"tick_basina_en_fazla_sonuc\": 3 },");
            var iki = @"[ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""a"", ""deger"": 1, ""sure"": 100 },
                          { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""b"", ""deger"": 1, ""sure"": 100 } ]";
            var s = Yap("\"en_fazla\": 5", iki, json);
            s.Submit(Y);
            s.Tick(1);

            Assert.Equal(new[] { "Ali", "Ali" }, Mod(s, 1).Select(o => o.Deger("npc")).ToArray());   // Ali'nin ikisi de, Veli'nin hiçbiri
            Assert.Equal(new[] { "Veli" }, Fren(s, "genel_sinir", 1).Select(o => o.Deger("npc")).ToArray());
        }

        [Fact]
        public void Sonuc_NPC_islem_sirasindan_ve_senaryodaki_NPC_dizilisinden_bagimsizdir()
        {
            // NPC satırları ters sırada yazılsa da (ordinal sıra) olaylar ve hash birebir aynıdır; kapsam "kural" ilk NPC'yi sıraya bağlar.
            var satirlar = SimFikstur.SenaryoJson.Split('\n').ToList();
            var ilk = satirlar.FindIndex(l => l.Contains("\"id\": \"Ali\""));
            var npcSatirlari = satirlar.GetRange(ilk, 3).Select(l => l.TrimEnd().TrimEnd(',')).ToList();
            npcSatirlari.Reverse();
            satirlar.RemoveRange(ilk, 3);
            satirlar.Insert(ilk, string.Join(",\n", npcSatirlari));
            var ters = string.Join("\n", satirlar);

            var a = Yap("\"cooldown\": 10, \"cooldown_kapsam\": \"kural\"");
            a.Submit(Y);
            a.Tick(2);
            var b = Yap("\"cooldown\": 10, \"cooldown_kapsam\": \"kural\"", senaryoJson: ters);
            b.Submit(Y);
            b.Tick(2);

            Assert.Equal(a.SonHash, b.SonHash);
            string Ozet(Simulasyon x) => string.Join("\n", x.Gunluk.Olaylar.Select(
                o => o.Id + "|" + o.Tur + "|" + o.Fail + "|" + o.Hedef + "|" + o.KuralId + "|" + string.Join(",", o.Yuk.Select(kv => kv.Key + "=" + kv.Value))));
            Assert.Equal(Ozet(a), Ozet(b));
        }

        [Fact]
        public void Sayaclar_gunlukten_yeniden_kurulur_ve_durum_hashine_girer()
        {
            var s = Yap("\"cooldown\": 5");
            s.Submit(Y);
            s.Tick(3);

            var yeni = new TetikSayaclari(s.Kurallar, s.Eylemler);
            foreach (var o in s.Gunluk.Olaylar)
            {
                yeni.Isle(o);
            }

            Assert.NotEmpty(yeni.KanonikMetin());
            Assert.Equal(yeni.KanonikMetin(), s.SayaclarKanonikMetni);
        }

        [Fact]
        public void Fren_alani_olmayan_kural_ve_senaryoda_kanonik_metin_ve_olaylar_ayni_kalir()
        {
            var s = SimFikstur.Yap();
            s.Submit(Y);
            s.Tick(3);

            Assert.Equal(string.Empty, s.SayaclarKanonikMetni);
            Assert.Empty(s.Frenler());
            Assert.DoesNotContain(s.Gunluk.Olaylar, o => o.Yuk.Any(kv => kv.Key == "ben"));
        }

        [Fact]
        public void Sinirli_kuralin_gercek_olayina_ben_yuku_eklenir_sinirsiz_kuralin_eklenmez()
        {
            var gercek = @"[ { ""tur"": ""gercek"", ""islem"": ""ekle"", ""gercek"": ""yardim_gordu({ben})"" } ]";
            var sinirli = Yap("\"cooldown\": 5", gercek);
            sinirli.Submit(Y);
            sinirli.Tick(1);
            var evler = sinirli.Gunluk.Olaylar.Where(o => o.Tur == "gercek_eklendi" && o.KuralId == "r").ToArray();
            Assert.Equal(new[] { "Ali", "Veli" }, evler.Select(o => o.Deger("ben")).ToArray());

            var serbest = SimFikstur.Yap(Kural(string.Empty, gercek).Replace("\"ne_zaman\": \"yardim_etti\", ,", "\"ne_zaman\": \"yardim_etti\","));
            serbest.Submit(Y);
            serbest.Tick(1);
            Assert.DoesNotContain(serbest.Gunluk.Olaylar, o => o.Tur == "gercek_eklendi" && o.KuralId == "r" && o.Yuk.Any(kv => kv.Key == "ben"));
        }

        [Fact]
        public void Gercek_etkili_kuralda_npc_kapsami_ben_yukunden_sayilir()
        {
            var gercek = @"[ { ""tur"": ""gercek"", ""islem"": ""ekle"", ""gercek"": ""yardim_gordu({ben})"" } ]";
            var s = Yap("\"cooldown\": 10", gercek);
            s.Submit(Y);
            s.Tick(1);
            s.Submit(Y);
            s.Tick(1);

            Assert.DoesNotContain(Fren(s, "cooldown", 2), f => f.Deger("npc") == string.Empty);   // ikinci tetikte olay üretilmediği için fren yok
            var yeni = new TetikSayaclari(s.Kurallar, s.Eylemler);
            foreach (var o in s.Gunluk.Olaylar)
            {
                yeni.Isle(o);
            }

            Assert.Equal(s.SayaclarKanonikMetni, yeni.KanonikMetin());
        }

        [Theory]
        [InlineData("\"cooldown\": 0")]
        [InlineData("\"cooldown\": 1.5")]
        [InlineData("\"en_fazla\": -1")]
        [InlineData("\"tick_basina_en_fazla\": \"iki\"")]
        [InlineData("\"cooldown_kapsam\": \"npc\"")]
        [InlineData("\"cooldown\": 5, \"cooldown_kapsam\": \"hedef\"")]
        public void Dogrulayici_gecersiz_fren_alanlarini_reddeder(string ek)
        {
            var sonuc = KuralYukleyici.Yukle(Kural(ek), "k.json", SimFikstur.Senaryo().OlayTurleri);
            Assert.False(sonuc.Basarili);
        }
    }
}
