using System;
using System.Linq;
using Xunit;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;

namespace Yanki.Scenarios.Tests
{
    /// <summary>
    /// 9. hafta (Yusuf, DURAK 2b madde 1): senaryoda isteğe bağlı <c>baslangic_inanclari: [{ npc, onerme }]</c>. Tick 0'da mevcut <c>kanit_eklendi</c> olayıyla (<c>gordu</c>, kaynak ve kök NPC'nin kendisi,
    /// <c>KuralId: senaryo</c>, neden yok) yazılır; yeni olay türü yoktur. Alan yokken davranış ve hash aynıdır.
    /// </summary>
    public class BaslangicInancTests
    {
        private const string Kisilik = "{ \"acgozluluk\": 0.5, \"durustluk\": 0.5, \"cesaret\": 0.5, \"sosyallik\": 0.5, \"kin_tutma\": 0.5, \"merhamet\": 0.5 }";

        private static string Senaryo(string inanclar) =>
            "{ \"surum\": 1, \"ad\": \"B\", \"mekanlar\": [\"han\"], \"baglantilar\": [], \"oyuncu\": { \"id\": \"oyuncu\", \"konum\": \"han\" },"
            + " \"npcler\": [ { \"id\": \"Riza\", \"konum\": \"han\", \"kisilik\": " + Kisilik + " }, { \"id\": \"Ali\", \"konum\": \"han\", \"kisilik\": " + Kisilik + " } ],"
            + " \"gercekler\": [\"borclu(Mehmet,Riza,500)\"], \"olay_turleri\": [], \"kural_dosyalari\": [\"k.json\"]" + inanclar + " }";

        private static Simulasyon Kur(string inanclar, ulong seed = 42)
        {
            var s = SenaryoYukleyici.Yukle(Senaryo(inanclar), "s.json");
            Assert.True(s.Basarili, string.Join("\n", s.Hatalar));
            var k = KuralYukleyici.Yukle("{ \"kurallar\": [] }", "k.json");
            return Simulasyon.Olustur(s.Senaryo!, k.Kurallar, seed);
        }

        private const string Inanc = ", \"baslangic_inanclari\": [ { \"npc\": \"Riza\", \"onerme\": \"borclu(Mehmet,Riza,500)\" } ]";

        [Fact]
        public void Baslangic_inanci_tick_0da_kanit_eklendi_olayi_olarak_senaryo_kuralidyla_yazilir()
        {
            var sim = Kur(Inanc);

            var olay = Assert.Single(sim.Gunluk.Olaylar, o => o.Tur == "kanit_eklendi");
            Assert.Equal((0L, "senaryo", "Riza", "borclu(Mehmet,Riza,500)", "gordu", "Riza"), (olay.Tick, olay.KuralId, olay.Deger("npc"), olay.Deger("onerme"), olay.Deger("tur"), olay.Deger("kaynak")));
            Assert.Empty(olay.NedenOlaylar);
            Assert.DoesNotContain(olay.Yuk, kv => kv.Key == "kok");
            Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.Tur == "hafiza_eklendi");   // hafıza olayı yok

            // gerçekler yazıldıktan sonra gelir
            var sonGercek = sim.Gunluk.Olaylar.Last(o => o.Tur == "gercek_eklendi");
            Assert.True(olay.Id > sonGercek.Id);
        }

        [Fact]
        public void Kok_NPCnin_kendisidir_inanc_kaynak_ve_kok_sorgularina_yansir()
        {
            var sim = Kur(Inanc);
            var riza = sim.Zihinler["Riza"];

            var kanit = Assert.Single(riza.Kanitlar("borclu(Mehmet,Riza,500)"));
            Assert.Equal(("Riza", "Riza"), (kanit.Kaynak, riza.EtkinKok(kanit)));
            Assert.True(riza.KokVar(Yanki.Core.Mind.OnermeDeseni.Ayristir("borclu(Mehmet,*,*)"), "Riza"));
            Assert.False(riza.KokVar(Yanki.Core.Mind.OnermeDeseni.Ayristir("borclu(Mehmet,*,*)"), "oyuncu"));
            Assert.Empty(sim.Zihinler["Ali"].Kanitlar("borclu(Mehmet,Riza,500)"));
        }

        [Fact]
        public void Why_ve_olay_ozeti_baslangic_kanitini_senaryo_baslangici_olarak_gosterir()
        {
            var sim = Kur(Inanc);
            var olay = sim.Gunluk.Olaylar.Single(o => o.Tur == "kanit_eklendi");

            Assert.Contains("senaryo başlangıcı", NedenYazici.Ozet(olay), StringComparison.Ordinal);
            Assert.DoesNotContain("kural senaryo", NedenYazici.Ozet(olay), StringComparison.Ordinal);
            Assert.Empty(olay.NedenOlaylar);   // kök olay: neden yok
            Assert.Contains(NedenYazici.Yaz(sim.Gunluk, olay.Id), s => s.Contains("senaryo başlangıcı", StringComparison.Ordinal));
        }

        [Fact]
        public void Alan_yokken_ve_bos_dizide_davranis_ve_hash_ayni()
        {
            var yok = Kur(string.Empty);
            var bos = Kur(", \"baslangic_inanclari\": []");
            yok.Tick(30);
            bos.Tick(30);

            Assert.Equal(yok.DurumHash(), bos.DurumHash());
            Assert.Equal(GunlukHashu.Hesapla(yok.Gunluk), GunlukHashu.Hesapla(bos.Gunluk));
            Assert.DoesNotContain(yok.Gunluk.Olaylar, o => o.Tur == "kanit_eklendi" && o.KuralId == "senaryo");
        }

        [Theory]
        [InlineData(", \"baslangic_inanclari\": [ { \"npc\": \"Yok\", \"onerme\": \"borclu(Mehmet,Riza,500)\" } ]", "Yok")]
        [InlineData(", \"baslangic_inanclari\": [ { \"npc\": \"Riza\", \"onerme\": \"borclu(((\" } ]", "onerme")]
        [InlineData(", \"baslangic_inanclari\": [ { \"npc\": \"oyuncu\", \"onerme\": \"borclu(Mehmet,Riza,500)\" } ]", "oyuncu")]
        [InlineData(", \"baslangic_inanclari\": [ { \"npc\": \"Riza\", \"onerme\": \"borclu(Mehmet,Riza,500)\" }, { \"npc\": \"Riza\", \"onerme\": \"borclu(Mehmet,Riza,500)\" } ]", "tekrar")]
        [InlineData(", \"baslangic_inanclari\": [ { \"npc\": \"Riza\", \"onerme\": \"borclu(Mehmet,Riza,500)\", \"x\": 1 } ]", "bilinmeyen")]
        [InlineData(", \"baslangic_inanclari\": {}", "dizi")]
        public void Gecersiz_baslangic_inanci_dosya_satirla_hata_verir(string inanclar, string parca)
        {
            var s = SenaryoYukleyici.Yukle(Senaryo(inanclar), "s.json");

            Assert.False(s.Basarili);
            Assert.Contains(s.Hatalar, h => h.Mesaj.Contains(parca, StringComparison.Ordinal) && h.Satir > 0);
        }

        [Fact]
        public void Birden_cok_inanc_NPC_kimligine_gore_ordinal_sirayla_yazilir()
        {
            var sim = Kur(", \"baslangic_inanclari\": [ { \"npc\": \"Riza\", \"onerme\": \"borclu(Mehmet,Riza,500)\" }, { \"npc\": \"Ali\", \"onerme\": \"kardes(Mehmet,Can)\" } ]");

            var npcler = sim.Gunluk.Olaylar.Where(o => o.Tur == "kanit_eklendi" && o.KuralId == "senaryo").Select(o => o.Deger("npc")).ToArray();
            Assert.Equal(new[] { "Ali", "Riza" }, npcler);
        }
    }
}
