using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Yanki.Cli;
using Yanki.Core.Events;
using Yanki.Core.Simulation;
using Yanki.Dialogue;

namespace Yanki.Scenarios.Tests
{
    /// <summary>
    /// Sıra etkisi diyalogda (8. hafta, DURAK 2 kararı 2, Seçenek A): aynı iki oyuncu eylemi (Can'a yardım, söylentiyi Ayşe'ye anlatma), yalnızca sırası farklı. Sıra A'da yardım önce:
    /// Can minnet duyar, teşekkür satırı MİNNET şablonudur. Sıra B'de söylenti önce: Can iftirayı duymuştur, teşekkür satırı GÜVENSİZLİK (şüphe/iftira) şablonudur, minnet satırı yoktur.
    /// Değişmezler şablon dosyasından ÖNCE yazıldı. Sayılar ve metinler yer tutucudur; Kasaba Hanı içeriği değildir.
    /// </summary>
    public class DiyalogSiraEtkisiTests
    {
        private const string Klasor = "SiraEtkisiSoylenti";

        private static string Kok()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "Yanki.slnx")))
            {
                d = d.Parent;
            }

            return d!.FullName;
        }

        private static SablonDosyasi Sablon()
        {
            var yol = Path.Combine(Kok(), "samples", Klasor, "sablonlar.json");
            var sonuc = SablonYukleyici.Yukle(File.ReadAllText(yol), "samples/" + Klasor + "/sablonlar.json");
            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            return sonuc.Dosya!;
        }

        private static PlayerAction Anlat(string onerme) =>
            new PlayerAction("anlatti", "Ayse", new[] { new KeyValuePair<string, string>("onerme", onerme) });

        private static DiyalogMotoru Kos(IReadOnlyDictionary<int, PlayerAction[]> slotlar)
        {
            var hata = new StringWriter();
            var sim = SenaryoDosyasi.Yukle(Path.Combine(Kok(), "samples", Klasor, "scenario.json"), 42, hata);
            Assert.True(sim != null, hata.ToString());
            var motor = DiyalogMotoru.Baglan(sim!, Sablon());
            for (var t = 1; t <= 14; t++)
            {
                if (slotlar.TryGetValue(t, out var eylemler))
                {
                    foreach (var e in eylemler)
                    {
                        sim!.Submit(e);
                    }
                }

                sim!.Tick(1);
            }

            return motor;
        }

        private static readonly PlayerAction[] Yardim = { new PlayerAction("yardim_etti", "Can") };

        private static readonly PlayerAction[] Soylenti = { Anlat("borclu(Mehmet,Ayse,500)"), Anlat("kardes(Mehmet,Can)") };

        private static (DiyalogMotoru A, DiyalogMotoru B) Ikili() => (
            Kos(new Dictionary<int, PlayerAction[]> { [1] = Yardim, [4] = Soylenti }),
            Kos(new Dictionary<int, PlayerAction[]> { [1] = Soylenti, [4] = Yardim }));

        private static IEnumerable<DiyalogSatiri> CanTesekkur(DiyalogMotoru m) =>
            m.Satirlar.Where(s => s.Konusan == "Can" && s.Dinleyen == "oyuncu" && s.SablonId.StartsWith("tesekkur_", StringComparison.Ordinal));

        [Fact]
        public void Sira_A_yardim_once_Can_in_tesekkuru_minnet_satiridir_guvensizlik_satiri_yoktur()
        {
            var (a, _) = Ikili();

            var satirlar = CanTesekkur(a).ToArray();

            var satir = Assert.Single(satirlar);
            Assert.Equal("tesekkur_minnet", satir.SablonId);
            Assert.DoesNotContain(satirlar, s => s.SablonId == "tesekkur_supheli" || s.SablonId == "tesekkur_iftira");
        }

        [Fact]
        public void Sira_B_soylenti_once_Can_in_tesekkuru_guvensizlik_satiridir_minnet_satiri_yoktur()
        {
            var (_, b) = Ikili();

            var satirlar = CanTesekkur(b).ToArray();

            var satir = Assert.Single(satirlar);
            Assert.True(satir.SablonId == "tesekkur_supheli" || satir.SablonId == "tesekkur_iftira", satir.SablonId);
            Assert.DoesNotContain(satirlar, s => s.SablonId == "tesekkur_minnet");
        }

        [Fact]
        public void Iki_sirada_Can_in_teskkur_metni_ve_diyalog_hash_i_farkli_her_kos_kendi_icinde_tekrarlanir()
        {
            var (a, b) = Ikili();

            Assert.NotEqual(CanTesekkur(a).Single().Metin, CanTesekkur(b).Single().Metin);
            Assert.NotEqual(a.DiyalogHash(), b.DiyalogHash());

            var (a2, b2) = Ikili();
            Assert.Equal(a.DiyalogHash(), a2.DiyalogHash());
            Assert.Equal(b.DiyalogHash(), b2.DiyalogHash());
        }

        [Fact]
        public void Metin_gercek_nedeni_tasir_yardim_ya_da_soylenti_gonderme_yok_gizlilik_korunur()
        {
            var (a, b) = Ikili();

            // Teşekkür satırı konuşanın gerçekten bildiğiyle sınırlı: Can iftirayı yalnızca B'de duymuştur; A'daki satır iftiradan söz etmez.
            Assert.DoesNotContain("duydum", CanTesekkur(a).Single().Metin, StringComparison.Ordinal);
            Assert.DoesNotContain("Kardeşim", CanTesekkur(a).Single().Metin, StringComparison.Ordinal);
            Assert.True(CanTesekkur(b).Single().Metin.Length > 0);
        }
    }
}
