using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Events;
using Yanki.Core.Mind;
using Yanki.Core.Simulation;

namespace Yanki.Dialogue.Tests
{
    /// <summary>
    /// Niyet üretimi (8. hafta, A ve B kararları): niyet olaydan türer; eşlemesi olmayan olay satır üretmez; oyuncu eylemi hedef NPC'ye satır üretir; doğru ve yalan anlatım aynı
    /// biçimde niyet verir; niyette KuralId, eylem kimliği, kök, karar izi ve aktarım kaydı yoktur (ADR-012).
    /// </summary>
    public class NiyetUreticiTests
    {
        private static readonly SablonDosyasi Dosya = DiyalogFikstur.Sablon(@"{
 'dil': 'tr',
 'esleme': {
  'yardim_etti': { 'niyet': 'tesekkur', 'konusan': 'hedef' },
  'anlatti': { 'niyet': 'anlatma', 'konusan': 'fail' },
  'selam_oldu': { 'niyet': 'selam', 'konusan': 'hedef' }
 },
 'sablonlar': [
  { 'id': 't', 'concept': 'tesekkur', 'genel': true, 'metin': 'Sağ ol.' },
  { 'id': 'a', 'concept': 'anlatma', 'genel': true, 'metin': 'Duydun mu?' },
  { 'id': 's', 'concept': 'selam', 'genel': true, 'metin': 'Selam.' }
 ] }");

        private static readonly DiyalogAyarlari Ayar = new DiyalogAyarlari();

        private static Olay Anlatti(string fail, string hedef, string onerme, string kuralId, string kok, IEnumerable<long> nedenler, string gorunurluk = "sessiz") =>
            new Olay(
                50, 3, 0, "anlatti", fail, hedef, "han",
                new[] { new KeyValuePair<string, string>("onerme", onerme), new KeyValuePair<string, string>("kok", kok), new KeyValuePair<string, string>("gorunurluk", gorunurluk) },
                nedenler, kuralId);

        [Fact]
        public void Eslemesi_olmayan_olay_satir_uretmez()
        {
            var sim = DiyalogFikstur.Sim();
            sim.Submit(new PlayerAction("tehdit_etti", "Ali"));
            sim.Tick(1);

            Assert.Null(NiyetUretici.Uret(DiyalogFikstur.Olayi(sim, "tehdit_etti"), sim, Dosya, Ayar));
        }

        [Fact]
        public void Oyuncu_eylemi_hedef_NPCye_niyet_uretir_konusan_hedeftir()
        {
            var sim = DiyalogFikstur.Sim();
            sim.Submit(new PlayerAction("yardim_etti", "Ali"));
            sim.Tick(1);
            var olay = DiyalogFikstur.Olayi(sim, "yardim_etti");

            var niyet = NiyetUretici.Uret(olay, sim, Dosya, Ayar)!;

            Assert.Equal(("Ali", "oyuncu", "tesekkur"), (niyet.Konusan, niyet.Dinleyen, niyet.Ad));
            Assert.Equal(olay.Id, niyet.OlayId);
            Assert.Equal(string.Empty, niyet.Konu);
            Assert.Equal(sim.Zihinler["Ali"].FikirBandi("oyuncu", sim.SimdikiTick), niyet.FikirBandi);   // mevcut fikir_bandi sorgusu
            Assert.Equal(niyet.FikirBandi, niyet.Gerekce.FikirBandi);
            Assert.False(niyet.Followup);
        }

        [Fact]
        public void Oyuncunun_kendi_anlattisi_satir_uretmez_cunku_yalnizca_NPCler_konusur()
        {
            var sim = DiyalogFikstur.Sim();
            var olay = Anlatti("oyuncu", "Ali", "borclu(Mehmet,Ayse,500)", "player", "oyuncu", new long[0]);

            Assert.Null(NiyetUretici.Uret(olay, sim, Dosya, Ayar));
        }

        [Fact]
        public void Hedefi_mekan_ya_da_kendisi_olan_olay_niyet_uretmez()
        {
            var sim = DiyalogFikstur.Sim();

            Assert.Null(NiyetUretici.Uret(Anlatti("Ali", "han", "borclu(Mehmet,Ayse,500)", "eylem:x", "Ali", new long[0]), sim, Dosya, Ayar));
            Assert.Null(NiyetUretici.Uret(Anlatti("Ali", "Ali", "borclu(Mehmet,Ayse,500)", "eylem:x", "Ali", new long[0]), sim, Dosya, Ayar));
            Assert.Null(NiyetUretici.Uret(Anlatti("Ali", string.Empty, "borclu(Mehmet,Ayse,500)", "eylem:x", "Ali", new long[0]), sim, Dosya, Ayar));
        }

        [Fact]
        public void Anlatti_niyeti_konuyu_onerme_yukunden_alir()
        {
            var sim = DiyalogFikstur.Sim();

            var niyet = NiyetUretici.Uret(Anlatti("Ali", "Veli", "borclu(Mehmet,Ayse,500)", "eylem:anlat", "Mehmet", new long[] { 1, 2 }), sim, Dosya, Ayar)!;

            Assert.Equal(("Ali", "Veli", "anlatma", "borclu(Mehmet,Ayse,500)"), (niyet.Konusan, niyet.Dinleyen, niyet.Ad, niyet.Konu));
        }

        [Fact]
        public void Dogru_ve_yalan_anlatim_ayni_bicimde_niyet_verir_KuralId_kok_ve_neden_niyeti_degistirmez()
        {
            var sim = DiyalogFikstur.Sim();
            const string onerme = "degil(borclu(Mehmet,Ayse,500))";
            var yalan = Anlatti("Ali", "Veli", onerme, "eylem:yalan_soyle", "Ali", new long[] { 7 }, "sessiz");
            var dogruGibi = Anlatti("Ali", "Veli", onerme, "eylem:anlat", "Mehmet", new long[] { 1, 2, 3 }, "acik");

            var a = NiyetUretici.Uret(yalan, sim, Dosya, Ayar)!;
            var b = NiyetUretici.Uret(dogruGibi, sim, Dosya, Ayar)!;

            Assert.Equal(Ozet(a), Ozet(b));
        }

        private static string Ozet(Niyet n) =>
            string.Join("|", typeof(Niyet).GetProperties().OrderBy(p => p.Name, StringComparer.Ordinal).Select(p => p.Name + "=" + Deger(p.GetValue(n))));

        private static string Deger(object? o) =>
            o is NiyetGerekcesi g ? g.FikirBandi + "/" + g.BaskinDuygu : o?.ToString() ?? string.Empty;

        [Fact]
        public void Niyet_yalnizca_izinli_alanlari_tasir_KuralId_eylem_kok_yalan_alani_yoktur()
        {
            var izinli = new[] { "Ad", "Dinleyen", "FikirBandi", "Followup", "Gerekce", "Konu", "Konusan", "Mekan", "OlayId", "Tick", "Ton" };
            var gercek = typeof(Niyet).GetProperties().Select(p => p.Name).OrderBy(x => x, StringComparer.Ordinal).ToArray();

            Assert.Equal(izinli, gercek);
            Assert.Equal(new[] { "BaskinDuygu", "FikirBandi" }, typeof(NiyetGerekcesi).GetProperties().Select(p => p.Name).OrderBy(x => x, StringComparer.Ordinal).ToArray());
            Assert.DoesNotContain(gercek, ad => ad.Contains("KuralId", StringComparison.OrdinalIgnoreCase) || ad.Contains("Eylem", StringComparison.OrdinalIgnoreCase)
                || ad.Contains("Kok", StringComparison.OrdinalIgnoreCase) || ad.Contains("Yalan", StringComparison.OrdinalIgnoreCase) || ad.Contains("Neden", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Ton_baskin_duygudan_turer_duygu_yoksa_varsayilan_ton()
        {
            var sim = DiyalogFikstur.Sim();
            sim.Submit(new PlayerAction("yardim_etti", "Ali"));
            sim.Tick(1);
            var yardim = DiyalogFikstur.Olayi(sim, "yardim_etti");
            var once = NiyetUretici.Uret(yardim, sim, Dosya, Ayar)!;
            Assert.Equal(("notr", "yok"), (once.Ton, once.Gerekce.BaskinDuygu));

            sim.Zihinler["Ali"].Uygula(ZihinOlaylari.DuyguDegisti(sim.Gunluk.SonrakiId, sim.SimdikiTick, 0, "Ali", "ofke", 0.9m, "oyuncu", new[] { yardim.Id }, "kural"));
            var gergin = NiyetUretici.Uret(yardim, sim, Dosya, Ayar)!;

            Assert.Equal(("ofkeli", "ofke"), (gergin.Ton, gergin.Gerekce.BaskinDuygu));   // ofke 0.9 >= 0.6

            var ayar = new DiyalogAyarlari { VarsayilanTon = "sakin" };
            ayar.TonKurallari.Insert(0, new TonKurali("ofke", 0.95m, "sert"));
            Assert.Equal("ofkeli", NiyetUretici.Uret(yardim, sim, Dosya, ayar)!.Ton);   // 0.9 < 0.95: ilk eşleşen kural kazanır
            ayar.TonKurallari[0] = new TonKurali("ofke", 0.5m, "sert");
            Assert.Equal("sert", NiyetUretici.Uret(yardim, sim, Dosya, ayar)!.Ton);
        }

        [Fact]
        public void Niyet_simulasyonu_degistirmez()
        {
            var sim = DiyalogFikstur.Sim();
            sim.Submit(new PlayerAction("yardim_etti", "Ali"));
            sim.Tick(2);
            var hash = sim.DurumHash();
            var sayi = sim.Gunluk.Sayi;

            NiyetUretici.Uret(DiyalogFikstur.Olayi(sim, "yardim_etti"), sim, Dosya, Ayar);

            Assert.Equal((hash, sayi), (sim.DurumHash(), sim.Gunluk.Sayi));
        }
    }
}
